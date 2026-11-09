import { HttpErrorResponse } from '@angular/common/http';
import { DestroyRef, Directive, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Observable, Subject, catchError, filter, finalize, map, of, switchMap, tap } from 'rxjs';

import { ConfirmDialogData, confirm } from '../../shared/ui/confirm-dialog/confirm-dialog';
import { errorMessage } from '../forms/form-errors';
import { LISTING_PAGE_SIZE, PagedResult } from './listing.models';

export type ListingListStatus = 'loading' | 'ready' | 'empty' | 'error';

type LoadResult<T> = { page: number; result: PagedResult<T> } | { error: string };

/**
 * A paged list of listings with loading, empty and error states, shared by the "My ..." pages and the
 * public profile sections. The action helpers keep one item busy at a time and report failures.
 */
@Directive()
export abstract class ListingListBase<T extends { id: string }> implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  readonly pageSize = LISTING_PAGE_SIZE;
  readonly status = signal<ListingListStatus>('loading');
  readonly loadError = signal<string | null>(null);
  readonly items = signal<T[]>([]);
  readonly page = signal(1);
  readonly totalCount = signal(0);
  /** The item an action is running on; its buttons are disabled meanwhile. */
  readonly busyId = signal<string | null>(null);

  private readonly requestedPage = new Subject<number>();

  protected abstract fetch(page: number): Observable<PagedResult<T>>;

  ngOnInit(): void {
    this.requestedPage
      .pipe(
        tap(() => {
          this.status.set('loading');
          this.loadError.set(null);
        }),
        switchMap((page) =>
          this.fetch(page).pipe(
            map((result): LoadResult<T> => ({ page, result })),
            catchError((err: unknown) => of<LoadResult<T>>({ error: errorMessage(err) })),
          ),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((outcome) => this.show(outcome));

    this.goTo(1);
  }

  goTo(page: number): void {
    this.requestedPage.next(page);
  }

  reload(): void {
    this.goTo(this.page());
  }

  /** Runs an action that returns the updated item and swaps it into the list. */
  protected runAction(item: T, action: Observable<T>, doneMessage: string): void {
    this.busyId.set(item.id);
    action
      .pipe(
        finalize(() => this.busyId.set(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (updated) => {
          this.items.update((items) => items.map((i) => (i.id === updated.id ? updated : i)));
          this.snackBar.open(doneMessage, 'OK', { duration: 4000 });
        },
        error: (err: unknown) => this.showFailure(err),
      });
  }

  /** Asks first, then runs an action that returns the updated item. */
  protected confirmAction(item: T, question: ConfirmDialogData, action: () => Observable<T>, doneMessage: string): void {
    confirm(this.dialog, question)
      .pipe(filter(Boolean), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.runAction(item, action(), doneMessage));
  }

  /** Asks first, then deletes the item and reloads the page (which may now be past the last one). */
  protected confirmDelete(item: T, question: ConfirmDialogData, remove: () => Observable<void>, doneMessage: string): void {
    confirm(this.dialog, question)
      .pipe(
        filter(Boolean),
        tap(() => this.busyId.set(item.id)),
        switchMap(() => remove().pipe(finalize(() => this.busyId.set(null)))),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.snackBar.open(doneMessage, 'OK', { duration: 4000 });
          this.reload();
        },
        error: (err: unknown) => this.showFailure(err),
      });
  }

  private show(outcome: LoadResult<T>): void {
    if ('error' in outcome) {
      this.loadError.set(outcome.error);
      this.status.set('error');
      return;
    }

    const { page, result } = outcome;
    if (!result.items.length && result.totalCount > 0 && page > 1) {
      // The page no longer exists (its last item was deleted): show the last one that does.
      this.goTo(Math.ceil(result.totalCount / this.pageSize));
      return;
    }

    this.page.set(page);
    this.items.set(result.items);
    this.totalCount.set(result.totalCount);
    this.status.set(result.totalCount ? 'ready' : 'empty');
  }

  /** A rejected status change arrives as a field error ("status"), whose message says why. */
  private showFailure(err: unknown): void {
    const fieldErrors = err instanceof HttpErrorResponse ? (err.error as { errors?: Record<string, string[]> } | null)?.errors : null;
    const message = (fieldErrors && Object.values(fieldErrors)[0]?.[0]) || errorMessage(err);
    this.snackBar.open(message, 'OK', { duration: 6000 });
  }
}
