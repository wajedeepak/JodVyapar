import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';

export type AlertType = 'error' | 'warning' | 'success' | 'info';

const ICONS: Record<AlertType, string> = {
  error: 'error',
  warning: 'warning',
  success: 'check_circle',
  info: 'info',
};

/**
 * Page- or form-level message. Errors and warnings are announced immediately (role="alert"),
 * success and info politely (role="status"). Message text goes in the content slot.
 */
@Component({
  selector: 'app-alert-banner',
  imports: [MatIcon, MatIconButton],
  template: `
    <mat-icon class="icon" aria-hidden="true">{{ icon() }}</mat-icon>
    <div class="body">
      @if (title(); as t) {
        <p class="title">{{ t }}</p>
      }
      <div class="message"><ng-content /></div>
    </div>
    @if (dismissible()) {
      <button mat-icon-button type="button" class="dismiss" aria-label="Dismiss message" (click)="dismissed.emit()">
        <mat-icon>close</mat-icon>
      </button>
    }
  `,
  styleUrl: './alert-banner.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': '"type-" + type()',
    '[attr.role]': 'role()',
  },
})
export class AlertBanner {
  readonly type = input<AlertType>('info');
  readonly title = input<string>();
  readonly dismissible = input(false);
  readonly dismissed = output<void>();

  protected readonly icon = computed(() => ICONS[this.type()]);
  protected readonly role = computed(() => (this.type() === 'error' || this.type() === 'warning' ? 'alert' : 'status'));
}
