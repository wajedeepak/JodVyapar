import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { STATUS_BADGES } from '../../../core/listings/listing-format';
import { Badge } from '../badge/badge';

/** A listing's status as a coloured badge: Active, Paused, Fulfilled or Expired. */
@Component({
  selector: 'app-status-badge',
  imports: [Badge],
  template: `<app-badge [variant]="badge().variant">{{ badge().label }}</app-badge>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatusBadge {
  readonly status = input.required<string>();

  protected readonly badge = computed(() => STATUS_BADGES[this.status()] ?? { label: this.status(), variant: 'neutral' as const });
}
