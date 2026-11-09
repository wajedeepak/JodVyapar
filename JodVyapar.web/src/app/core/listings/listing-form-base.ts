import { HttpErrorResponse } from '@angular/common/http';
import { DestroyRef, Directive, ElementRef, Injector, OnInit, afterNextRender, inject, input, signal } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { confirm } from '../../shared/ui/confirm-dialog/confirm-dialog';
import { HasUnsavedChanges } from '../auth/auth.guards';
import { focusFirstInvalid } from '../forms/focus-invalid';
import { applyServerErrors, errorMessage } from '../forms/form-errors';

export type ListingFormStatus = 'loading' | 'ready' | 'not-found' | 'error';

/**
 * What the Product and Requirement forms share: add or edit by route `id`, page states, the
 * "More details" panel, showing validation errors, and the unsaved-changes warning.
 */
@Directive({
  host: {
    '(window:beforeunload)': 'onBeforeUnload($event)',
  },
})
export abstract class ListingFormBase implements OnInit, HasUnsavedChanges {
  protected readonly destroyRef = inject(DestroyRef);
  protected readonly router = inject(Router);
  protected readonly snackBar = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);
  private readonly injector = inject(Injector);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  /** Route parameter; absent on the "new" pages. */
  readonly id = input<string>();

  readonly status = signal<ListingFormStatus>('loading');
  readonly loadError = signal<string | null>(null);
  readonly saving = signal(false);
  readonly formError = signal<string | null>(null);
  readonly moreOpen = signal(false);

  /** Set once the save succeeded, so the guard lets the navigation through. */
  protected saved = false;

  protected abstract readonly form: FormGroup;

  /** Controls inside "More details": the panel opens when one of them has an error. */
  protected abstract readonly optionalControls: readonly string[];

  protected abstract load(): void;

  ngOnInit(): void {
    this.load();
  }

  /** Unsaved work that isn't in the form, such as chosen images. */
  protected hasOtherChanges(): boolean {
    return false;
  }

  confirmLeave(): boolean | Promise<boolean> {
    if (!this.hasUnsavedChanges()) {
      return true;
    }
    return firstValueFrom(
      confirm(this.dialog, {
        title: 'Discard your changes?',
        message: "You have edits that haven't been saved. If you leave now, they will be lost.",
        confirmLabel: 'Discard changes',
        cancelLabel: 'Keep editing',
      }),
    );
  }

  protected onBeforeUnload(event: BeforeUnloadEvent): void {
    if (this.hasUnsavedChanges()) {
      event.preventDefault();
    }
  }

  protected loadFailed(err: unknown): void {
    if (err instanceof HttpErrorResponse && err.status === 404) {
      this.status.set('not-found');
      return;
    }
    this.loadError.set(errorMessage(err));
    this.status.set('error');
  }

  /** The form is invalid: show every error and move focus to the first one. */
  protected showInvalid(): void {
    this.form.markAllAsTouched();
    this.revealErrors();
  }

  protected saveFailed(err: unknown): void {
    this.formError.set(applyServerErrors(this.form, err));
    this.revealErrors();
  }

  private hasUnsavedChanges(): boolean {
    return !this.saved && (this.form.dirty || this.hasOtherChanges());
  }

  /** Opens "More details" if it hides an error; once rendered, focuses the first error or shows the banner. */
  private revealErrors(): void {
    if (this.optionalControls.some((name) => this.form.get(name)?.invalid)) {
      this.moreOpen.set(true);
    }

    afterNextRender(
      () => {
        const host = this.host.nativeElement;
        if (!focusFirstInvalid(host)) {
          host.querySelector('app-alert-banner')?.scrollIntoView({ block: 'center' });
        }
      },
      { injector: this.injector },
    );
  }
}
