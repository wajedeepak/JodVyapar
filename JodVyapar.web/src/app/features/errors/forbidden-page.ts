import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink } from '@angular/router';

import { EmptyState } from '../../shared/ui';

/** 403: where roleGuard sends users who don't hold the role a page needs. */
@Component({
  selector: 'app-forbidden-page',
  imports: [RouterLink, MatButtonModule, EmptyState],
  template: `
    <app-empty-state
      icon="lock_outline"
      title="You don't have access to this page"
      message="This page needs a role your account doesn't have. You can add the Buyer or Seller role from your profile."
      [headingLevel]="1"
    >
      <div emptyStateActions>
        <a mat-flat-button routerLink="/">Go to home</a>
        <a mat-stroked-button routerLink="/profile">Manage roles</a>
      </div>
    </app-empty-state>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ForbiddenPage {}
