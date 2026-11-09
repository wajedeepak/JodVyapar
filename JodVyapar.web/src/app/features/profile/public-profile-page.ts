import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { Title } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { Subject, catchError, map, merge, of, switchMap, tap } from 'rxjs';

import { AuthService } from '../../core/auth/auth.service';
import { errorMessage } from '../../core/forms/form-errors';
import { PublicProfile } from '../../core/profile/profile.models';
import { ProfileService } from '../../core/profile/profile.service';
import { AlertBanner, EmptyState, Skeleton } from '../../shared/ui';
import { ProfileHeaderCard } from './profile-header-card';
import { ProfileSection } from './profile-section';

type LoadStatus = 'loading' | 'ready' | 'not-found' | 'error';

type LoadResult =
  | { status: 'ready'; profile: PublicProfile }
  | { status: 'not-found' }
  | { status: 'error'; message: string };

/** /profile/:userId: what other businesses see. No email, mobile or GSTIN number (the API never sends them). */
@Component({
  selector: 'app-public-profile-page',
  imports: [RouterLink, MatButtonModule, MatIcon, AlertBanner, EmptyState, Skeleton, ProfileHeaderCard, ProfileSection],
  templateUrl: './public-profile-page.html',
  styleUrl: './profile-pages.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PublicProfilePage {
  private readonly profileService = inject(ProfileService);
  private readonly auth = inject(AuthService);
  private readonly title = inject(Title);

  /** Route parameter. */
  readonly userId = input.required<string>();

  protected readonly status = signal<LoadStatus>('loading');
  protected readonly loadError = signal<string | null>(null);
  protected readonly profile = signal<PublicProfile | null>(null);

  protected readonly isOwnProfile = computed(() => this.auth.currentUser()?.id === this.userId());
  protected readonly isSeller = computed(() => this.profile()?.roles.includes('Seller') ?? false);
  protected readonly isBuyer = computed(() => this.profile()?.roles.includes('Buyer') ?? false);

  private readonly retry = new Subject<void>();

  constructor() {
    merge(toObservable(this.userId), this.retry.pipe(map(() => this.userId())))
      .pipe(
        tap(() => {
          this.status.set('loading');
          this.loadError.set(null);
        }),
        switchMap((userId) =>
          this.profileService.getPublic(userId).pipe(
            map((profile): LoadResult => ({ status: 'ready', profile })),
            catchError((err: unknown) =>
              of<LoadResult>(
                err instanceof HttpErrorResponse && (err.status === 404 || err.status === 400)
                  ? { status: 'not-found' }
                  : { status: 'error', message: errorMessage(err) },
              ),
            ),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((result) => {
        this.status.set(result.status);
        if (result.status === 'ready') {
          this.profile.set(result.profile);
          this.title.setTitle(`${result.profile.companyName} | JodVyapar`);
        } else if (result.status === 'error') {
          this.loadError.set(result.message);
        }
      });
  }

  protected reload(): void {
    this.retry.next();
  }
}
