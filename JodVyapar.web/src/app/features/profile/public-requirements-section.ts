import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { Observable } from 'rxjs';

import { ListingListBase } from '../../core/listings/listing-list-base';
import { requirementFacts } from '../../core/listings/listing-format';
import { PagedResult, PublicRequirement } from '../../core/listings/listing.models';
import { RequirementService } from '../../core/listings/requirement.service';
import { AlertBanner, EmptyState, ListingCard, Pager, Skeleton } from '../../shared/ui';
import { ProfileSection } from './profile-section';

/** The Requirements section of a public profile: Active, unexpired ones only (the API filters them), paged. */
@Component({
  selector: 'app-public-requirements-section',
  imports: [MatButtonModule, AlertBanner, EmptyState, ListingCard, Pager, Skeleton, ProfileSection],
  template: `
    <app-profile-section title="Requirements">
      @switch (status()) {
        @case ('loading') {
          <div aria-busy="true">
            <span class="jv-sr-only" role="status">Loading requirements…</span>
            <app-skeleton shape="block" />
          </div>
        }
        @case ('error') {
          <app-alert-banner type="error" title="Couldn't load requirements">
            <p>{{ loadError() }}</p>
            <button mat-button type="button" (click)="reload()">Retry</button>
          </app-alert-banner>
        }
        @case ('empty') {
          <app-empty-state icon="assignment" title="No requirements to show yet" [message]="companyName() + ' has no open requirements right now.'" [compact]="true" />
        }
        @case ('ready') {
          <ul class="listing-grid">
            @for (requirement of items(); track requirement.id) {
              <li>
                <app-listing-card
                  [title]="requirement.title"
                  [category]="requirement.parentCategoryName + ' › ' + requirement.categoryName"
                  [description]="requirement.description"
                  [facts]="facts(requirement)"
                  [headingLevel]="3"
                />
              </li>
            }
          </ul>
          <app-pager [page]="page()" [pageSize]="pageSize" [totalCount]="totalCount()" (pageChange)="goTo($event)" />
        }
      }
    </app-profile-section>
  `,
  styleUrl: './public-listing-section.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PublicRequirementsSection extends ListingListBase<PublicRequirement> {
  private readonly requirements = inject(RequirementService);

  readonly userId = input.required<string>();
  readonly companyName = input.required<string>();

  protected readonly facts = requirementFacts;

  protected fetch(page: number): Observable<PagedResult<PublicRequirement>> {
    return this.requirements.getPublic(this.userId(), page, this.pageSize);
  }
}
