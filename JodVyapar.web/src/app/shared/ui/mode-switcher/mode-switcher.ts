import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { MatButtonToggleChange, MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatIcon } from '@angular/material/icon';

import { ActiveMode } from '../../../core/auth/auth.models';
import { AuthService } from '../../../core/auth/auth.service';

/**
 * Segmented Buyer / Seller control for users who hold both roles. Renders nothing for
 * single-role users. Changes only the UI mode (AuthService.setMode); permissions never depend on it.
 */
@Component({
  selector: 'app-mode-switcher',
  imports: [MatButtonToggleModule, MatIcon],
  template: `
    @if (auth.canSwitchMode()) {
      <mat-button-toggle-group
        [value]="auth.activeMode()"
        (change)="select($event)"
        [hideSingleSelectionIndicator]="true"
        aria-label="Mode: choose which tools to show"
      >
        <mat-button-toggle value="Buyer">
          <mat-icon aria-hidden="true">shopping_bag</mat-icon>
          Buying
        </mat-button-toggle>
        <mat-button-toggle value="Seller">
          <mat-icon aria-hidden="true">storefront</mat-icon>
          Selling
        </mat-button-toggle>
      </mat-button-toggle-group>
    }
  `,
  styles: `
    :host {
      display: inline-block;
    }
    :host(.full-width),
    :host(.full-width) mat-button-toggle-group {
      display: flex;
      width: 100%;
    }
    :host(.full-width) mat-button-toggle {
      flex: 1;
    }
    mat-button-toggle-group {
      --mat-button-toggle-height: var(--jv-space-10);
      --mat-button-toggle-shape: var(--jv-radius-pill);
      --mat-button-toggle-selected-state-background-color: var(--jv-color-primary);
      --mat-button-toggle-selected-state-text-color: var(--jv-color-on-primary);
      --mat-button-toggle-label-text-weight: var(--jv-font-weight-semibold);
      --mat-button-toggle-label-text-size: var(--jv-font-size-sm);
    }
    mat-icon {
      width: var(--jv-icon-size-sm);
      height: var(--jv-icon-size-sm);
      margin-right: var(--jv-space-1);
      font-size: var(--jv-icon-size-sm);
      vertical-align: middle;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class.full-width]': 'fullWidth()',
  },
})
export class ModeSwitcher {
  protected readonly auth = inject(AuthService);

  /** Stretch both segments across the container (menus, the Profile page on phones). */
  readonly fullWidth = input(false);

  protected select(change: MatButtonToggleChange): void {
    this.auth.setMode(change.value as ActiveMode);
  }
}
