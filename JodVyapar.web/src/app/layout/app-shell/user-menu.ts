import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatIcon } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { RouterLink } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';
import { Avatar, ModeSwitcher, RoleBadges } from '../../shared/ui';

/** The avatar button in the top bar and its menu: name, role badges, mode switcher, Profile, Admin, Sign out. */
@Component({
  selector: 'app-user-menu',
  imports: [RouterLink, MatButtonModule, MatDividerModule, MatIcon, MatMenuModule, Avatar, ModeSwitcher, RoleBadges],
  templateUrl: './user-menu.html',
  styleUrl: './user-menu.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserMenu {
  protected readonly auth = inject(AuthService);

  protected readonly firstName = computed(() => this.auth.currentUser()?.fullName.trim().split(/\s+/)[0] ?? '');
}
