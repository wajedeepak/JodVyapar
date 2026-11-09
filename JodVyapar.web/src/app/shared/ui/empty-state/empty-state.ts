import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatIcon } from '@angular/material/icon';

/**
 * The "nothing here (yet)" block: icon, title, a sentence and optional actions
 * (`<div emptyStateActions>`). Also used for not-found, forbidden and "Coming soon" pages.
 * `headingLevel` 1 makes the title the page's h1 (full-page states).
 */
@Component({
  selector: 'app-empty-state',
  imports: [MatIcon],
  template: `
    <span class="icon-tile" aria-hidden="true"><mat-icon>{{ icon() }}</mat-icon></span>
    <ng-content select="[emptyStateBadge]" />
    @if (headingLevel() === 1) {
      <h1 class="title">{{ title() }}</h1>
    } @else {
      <h2 class="title">{{ title() }}</h2>
    }
    @if (message(); as m) {
      <p class="message">{{ m }}</p>
    }
    <ng-content />
    <div class="actions"><ng-content select="[emptyStateActions]" /></div>
  `,
  styles: `
    :host {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: var(--jv-space-3);
      padding: var(--jv-space-8) var(--jv-space-4);
      text-align: center;
    }
    :host(.compact) {
      padding: var(--jv-space-6) var(--jv-space-4);
    }
    :host(.compact) .title {
      font-size: var(--jv-font-size-md);
    }
    .icon-tile {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: var(--jv-space-12);
      height: var(--jv-space-12);
      border-radius: var(--jv-radius-lg);
      background: var(--jv-color-primary-soft);
      color: var(--jv-color-primary);
    }
    .title {
      font-size: var(--jv-font-size-xl);
    }
    .message {
      max-width: var(--jv-card-width);
      color: var(--jv-color-text-muted);
      line-height: var(--jv-line-height-snug);
    }
    .actions {
      display: flex;
      flex-wrap: wrap;
      justify-content: center;
      gap: var(--jv-space-3);
      margin-top: var(--jv-space-2);
    }
    .actions:empty {
      display: none;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class.compact]': 'compact()',
  },
})
export class EmptyState {
  readonly icon = input('inbox');
  readonly title = input.required<string>();
  readonly message = input<string>();
  readonly headingLevel = input<1 | 2>(2);
  /** Less padding and a smaller title, for an empty section inside a card. */
  readonly compact = input(false);
}
