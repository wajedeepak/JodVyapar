import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { RouterLink } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';
import { errorMessage } from '../../core/forms/form-errors';
import { MyProfile } from '../../core/profile/profile.models';
import { ProfileService } from '../../core/profile/profile.service';
import { RoleActions } from '../../core/profile/role-actions.service';
import { AlertBanner, ModeSwitcher, PageHeader, PrimaryButton, RoleBadges, Skeleton } from '../../shared/ui';
import { ProfileHeaderCard } from './profile-header-card';
import { ProfileSection } from './profile-section';

type LoadStatus = 'loading' | 'ready' | 'error';

/** /profile: the signed-in user's profile in view mode, with roles and the mode switcher. */
@Component({
  selector: 'app-my-profile-page',
  imports: [
    RouterLink,
    MatButtonModule,
    MatIcon,
    AlertBanner,
    ModeSwitcher,
    PageHeader,
    PrimaryButton,
    RoleBadges,
    Skeleton,
    ProfileHeaderCard,
    ProfileSection,
  ],
  templateUrl: './my-profile-page.html',
  styleUrl: './profile-pages.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyProfilePage {
  private readonly profileService = inject(ProfileService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly auth = inject(AuthService);
  protected readonly roleActions = inject(RoleActions);

  protected readonly status = signal<LoadStatus>('loading');
  protected readonly loadError = signal<string | null>(null);
  protected readonly profile = signal<MyProfile | null>(null);

  /** Held roles come from the session, so a role added on this page shows at once. */
  protected readonly roles = this.auth.roles;

  protected readonly location = computed(() => {
    const company = this.profile()?.company;
    return company ? `${company.cityName}, ${company.stateName}` : null;
  });

  constructor() {
    this.load();
  }

  protected load(): void {
    this.status.set('loading');
    this.loadError.set(null);
    this.profileService
      .getMine()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (profile) => {
          this.profile.set(profile);
          this.status.set('ready');
        },
        error: (err: unknown) => {
          this.loadError.set(errorMessage(err));
          this.status.set('error');
        },
      });
  }

  protected addRole(): void {
    const role = this.roleActions.missingRole();
    if (role) {
      this.roleActions.addRole(role).subscribe();
    }
  }
}
