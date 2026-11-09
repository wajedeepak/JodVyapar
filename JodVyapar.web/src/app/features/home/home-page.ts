import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { RouterLink } from '@angular/router';

import { ActiveMode } from '../../core/auth/auth.models';
import { AuthService } from '../../core/auth/auth.service';
import { RoleActions } from '../../core/profile/role-actions.service';
import { PageHeader, PrimaryButton } from '../../shared/ui';

export interface ActionCard {
  title: string;
  description: string;
  icon: string;
  link: string;
}

/** What Home offers in each mode. Links go to placeholders until the features exist. */
export const MODE_ACTIONS: Record<ActiveMode, readonly ActionCard[]> = {
  Buyer: [
    { title: 'Search products', description: 'Find suppliers by product, city and state.', icon: 'search', link: '/search' },
    {
      title: 'Post a requirement',
      description: 'Tell sellers what you need and let them come to you.',
      icon: 'post_add',
      link: '/requirements/new',
    },
  ],
  Seller: [
    { title: 'Add a product', description: 'List what you sell so buyers can find you.', icon: 'add_box', link: '/products/new' },
    {
      title: 'Browse buyer requirements',
      description: 'See what buyers are looking for right now.',
      icon: 'assignment',
      link: '/requirements',
    },
  ],
};

const ROLE_PROMPTS = {
  Buyer: { title: 'Buying too?', text: 'Add the Buyer role to post requirements and contact sellers.' },
  Seller: { title: 'Selling too?', text: 'Add the Seller role to list products and reach new buyers.' },
} as const;

function greetingFor(hour: number): string {
  if (hour < 12) {
    return 'Good morning';
  }
  return hour < 17 ? 'Good afternoon' : 'Good evening';
}

@Component({
  selector: 'app-home-page',
  imports: [RouterLink, MatButtonModule, MatIcon, PageHeader, PrimaryButton],
  templateUrl: './home-page.html',
  styleUrl: './home-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomePage {
  protected readonly auth = inject(AuthService);
  protected readonly roleActions = inject(RoleActions);

  protected readonly greeting = computed(() => {
    const firstName = this.auth.currentUser()?.fullName.trim().split(/\s+/)[0];
    return `${greetingFor(new Date().getHours())}${firstName ? `, ${firstName}` : ''}`;
  });

  protected readonly subtitle = computed(() => {
    switch (this.auth.activeMode()) {
      case 'Buyer':
        return this.auth.canSwitchMode()
          ? "You're in Buying mode. Switch to Selling any time from the top bar or your menu."
          : 'Find suppliers and tell sellers what you need.';
      case 'Seller':
        return this.auth.canSwitchMode()
          ? "You're in Selling mode. Switch to Buying any time from the top bar or your menu."
          : 'Show buyers what you sell and find new business.';
      default:
        return 'Your admin account is ready.';
    }
  });

  protected readonly cards = computed(() => {
    const mode = this.auth.activeMode();
    return mode ? MODE_ACTIONS[mode] : [];
  });

  protected readonly prompt = computed(() => {
    const role = this.roleActions.missingRole();
    return role ? { role, ...ROLE_PROMPTS[role] } : null;
  });

  protected addMissingRole(): void {
    const role = this.roleActions.missingRole();
    if (role) {
      this.roleActions.addRole(role).subscribe();
    }
  }
}
