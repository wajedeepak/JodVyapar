import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatButton } from '@angular/material/button';
import { MatProgressSpinner } from '@angular/material/progress-spinner';

/**
 * The main call-to-action button. While `loading` it is disabled, shows a spinner and
 * (optionally) `loadingLabel`, and reports aria-busy. Use type="submit" inside a form;
 * handle clicks with (click) on the host for type="button".
 */
@Component({
  selector: 'app-primary-button',
  imports: [MatButton, MatProgressSpinner],
  template: `
    <button
      mat-flat-button
      [type]="type()"
      [disabled]="disabled() || loading()"
      [attr.aria-busy]="loading() || null"
    >
      @if (loading()) {
        <mat-progress-spinner class="spinner" mode="indeterminate" diameter="20" strokeWidth="2.5" aria-hidden="true" />
      }
      <span>{{ loading() && loadingLabel() ? loadingLabel() : label() }}</span>
    </button>
  `,
  styles: `
    :host {
      display: inline-block;
    }
    :host(.full-width),
    :host(.full-width) button {
      display: block;
      width: 100%;
    }
    button {
      transition:
        var(--jv-transition-colors),
        transform var(--jv-duration-fast) var(--jv-ease);
    }
    button:not(:disabled):hover {
      background-color: var(--jv-color-primary-hover);
      box-shadow: var(--jv-shadow-sm);
    }
    button:not(:disabled):active {
      background-color: var(--jv-color-primary-pressed);
      transform: scale(0.98);
    }
    /* Keep the brand colour (slightly faded) while loading instead of Material's grey disabled look. */
    button[aria-busy='true'] {
      background-color: var(--jv-color-primary);
      color: var(--jv-color-on-primary);
      opacity: 0.85;
    }
    button ::ng-deep .mdc-button__label {
      display: inline-flex;
      align-items: center;
      gap: var(--jv-space-2);
    }
    .spinner {
      --mat-progress-spinner-active-indicator-color: currentColor;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class.full-width]': 'fullWidth()',
  },
})
export class PrimaryButton {
  readonly label = input.required<string>();
  readonly loadingLabel = input<string>();
  readonly type = input<'button' | 'submit'>('button');
  readonly loading = input(false);
  readonly disabled = input(false);
  readonly fullWidth = input(false);
}
