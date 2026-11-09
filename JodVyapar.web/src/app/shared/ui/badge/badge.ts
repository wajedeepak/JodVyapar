import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatIcon } from '@angular/material/icon';

export type BadgeVariant = 'neutral' | 'primary' | 'success' | 'warning' | 'info';

/** A small pill label (role, status, "Coming soon"). Text goes in the slot; `icon` is optional. */
@Component({
  selector: 'app-badge',
  imports: [MatIcon],
  template: `
    @if (icon(); as i) {
      <mat-icon aria-hidden="true">{{ i }}</mat-icon>
    }
    <ng-content />
  `,
  styles: `
    :host {
      display: inline-flex;
      align-items: center;
      gap: var(--jv-space-1);
      padding: var(--jv-space-1) var(--jv-space-2);
      border: var(--jv-border-width) solid var(--jv-color-border);
      border-radius: var(--jv-radius-pill);
      background: var(--jv-color-surface-muted);
      color: var(--jv-color-text-muted);
      font-size: var(--jv-font-size-xs);
      font-weight: var(--jv-font-weight-semibold);
      line-height: var(--jv-line-height-tight);
      white-space: nowrap;
    }
    mat-icon {
      width: var(--jv-font-size-sm);
      height: var(--jv-font-size-sm);
      font-size: var(--jv-font-size-sm);
    }
    :host(.variant-primary) {
      border-color: var(--jv-color-primary-border);
      background: var(--jv-color-primary-soft);
      color: var(--jv-color-primary);
    }
    :host(.variant-success) {
      border-color: var(--jv-color-success-border);
      background: var(--jv-color-success-soft);
      color: var(--jv-color-success);
    }
    :host(.variant-warning) {
      border-color: var(--jv-color-warning-border);
      background: var(--jv-color-warning-soft);
      color: var(--jv-color-warning);
    }
    :host(.variant-info) {
      border-color: var(--jv-color-info-border);
      background: var(--jv-color-info-soft);
      color: var(--jv-color-info);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': '"variant-" + variant()',
  },
})
export class Badge {
  readonly variant = input<BadgeVariant>('neutral');
  /** Material Icons ligature shown before the text. */
  readonly icon = input<string>();
}
