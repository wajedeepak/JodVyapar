import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { MatIcon } from '@angular/material/icon';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';
import { LogoMark, ModeSwitcher } from '../../shared/ui';
import { UserMenu } from './user-menu';

export interface NavItem {
  label: string;
  icon: string;
  link: string;
  /** Only highlight on an exact URL match (Home). */
  exact?: boolean;
}

const MAIN_NAV: readonly NavItem[] = [
  { label: 'Home', icon: 'home', link: '/', exact: true },
  { label: 'Search', icon: 'search', link: '/search' },
  { label: 'Messages', icon: 'chat_bubble_outline', link: '/messages' },
  { label: 'Profile', icon: 'person_outline', link: '/profile' },
];

const ADMIN_NAV: NavItem = { label: 'Admin', icon: 'admin_panel_settings', link: '/admin' };

/**
 * Layout for every signed-in page (AuthLayout is for the sign-in pages). Phones (below 768px):
 * slim top bar + fixed bottom tab bar. From 768px: top bar with the navigation, the mode
 * switcher (Buyer + Seller users) and the user menu. Admins also get an Admin item.
 */
@Component({
  selector: 'app-shell',
  imports: [
    RouterLink,
    RouterLinkActive,
    RouterOutlet,
    MatIcon,
    LogoMark,
    ModeSwitcher,
    UserMenu,
  ],
  templateUrl: './app-shell.html',
  styleUrl: './app-shell.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppShell {
  protected readonly auth = inject(AuthService);

  /** Admin appears only for users who hold the Admin role (the route is guarded the same way). */
  protected readonly navItems = computed(() => (this.auth.hasRole('Admin') ? [...MAIN_NAV, ADMIN_NAV] : MAIN_NAV));
}
