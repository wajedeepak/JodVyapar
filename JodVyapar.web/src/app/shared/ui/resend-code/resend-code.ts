import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatButton } from '@angular/material/button';

/**
 * "Didn't get it? … Resend code" line for screens that take an emailed code. The button counts
 * down while `secondsLeft` > 0 (see `resendCooldown()` in core/auth/email-code.ts) and is
 * disabled while `sending`.
 */
@Component({
  selector: 'app-resend-code',
  imports: [MatButton],
  template: `
    <span class="jv-text-muted jv-text-small">Didn't get it? Check spam, or</span>
    <button
      mat-button
      type="button"
      [disabled]="secondsLeft() > 0 || sending()"
      [attr.aria-busy]="sending() || null"
      (click)="resend.emit()"
    >
      @if (sending()) {
        Sending…
      } @else if (secondsLeft() > 0) {
        Resend code in {{ secondsLeft() }}s
      } @else {
        Resend code
      }
    </button>
  `,
  styles: `
    :host {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      justify-content: space-between;
      gap: var(--jv-space-1) var(--jv-space-2);
      margin-top: calc(-1 * var(--jv-space-2));
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResendCode {
  readonly secondsLeft = input.required<number>();
  readonly sending = input(false);
  readonly resend = output<void>();
}
