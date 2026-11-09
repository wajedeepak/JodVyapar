import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink } from '@angular/router';

import { EmptyState } from '../../shared/ui';

/** 404 for any unknown URL inside the app. */
@Component({
  selector: 'app-not-found-page',
  imports: [RouterLink, MatButtonModule, EmptyState],
  template: `
    <app-empty-state
      icon="explore_off"
      title="Page not found"
      message="The page you're looking for doesn't exist or has moved. Check the address, or head back home."
      [headingLevel]="1"
    >
      <div emptyStateActions>
        <a mat-flat-button routerLink="/">Go to home</a>
        <a mat-stroked-button routerLink="/profile">My profile</a>
      </div>
    </app-empty-state>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotFoundPage {}
