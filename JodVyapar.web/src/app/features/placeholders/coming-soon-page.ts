import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink } from '@angular/router';

import { Badge, EmptyState } from '../../shared/ui';

/** Route data for this page (bound to the inputs below). */
export interface ComingSoonData {
  icon: string;
  heading: string;
  message: string;
}

/**
 * Placeholder for features that aren't built yet (Search, Messages, Products, Requirements, Admin),
 * so every nav item and Home card leads to a clear page rather than a dead button.
 */
@Component({
  selector: 'app-coming-soon-page',
  imports: [RouterLink, MatButtonModule, Badge, EmptyState],
  template: `
    <section class="card">
      <app-empty-state [icon]="icon()" [title]="heading()" [message]="message()" [headingLevel]="1">
        <app-badge emptyStateBadge variant="info" icon="schedule">Coming soon</app-badge>
        <div emptyStateActions>
          <a mat-flat-button routerLink="/">Back to home</a>
        </div>
      </app-empty-state>
    </section>
  `,
  styles: `
    .card {
      border: var(--jv-border-width) solid var(--jv-color-border);
      border-radius: var(--jv-radius-lg);
      background: var(--jv-color-surface);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ComingSoonPage {
  readonly icon = input('schedule');
  readonly heading = input('Coming soon');
  readonly message = input('This part of JodVyapar is on its way.');
}
