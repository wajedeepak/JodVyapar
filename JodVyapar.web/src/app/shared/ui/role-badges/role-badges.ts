import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { UserRole } from '../../../core/auth/auth.models';
import { Badge, BadgeVariant } from '../badge/badge';

const ROLE_BADGES: Record<UserRole, { icon: string; variant: BadgeVariant }> = {
  Buyer: { icon: 'shopping_bag', variant: 'info' },
  Seller: { icon: 'storefront', variant: 'primary' },
  Admin: { icon: 'admin_panel_settings', variant: 'warning' },
};

/** One badge per role, in the order given (the API and AuthService already sort them). */
@Component({
  selector: 'app-role-badges',
  imports: [Badge],
  template: `
    @for (role of roles(); track role) {
      <app-badge [variant]="badges[role].variant" [icon]="badges[role].icon">{{ role }}</app-badge>
    }
  `,
  styles: `
    :host {
      display: inline-flex;
      flex-wrap: wrap;
      gap: var(--jv-space-2);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoleBadges {
  readonly roles = input.required<readonly UserRole[]>();

  protected readonly badges = ROLE_BADGES;
}
