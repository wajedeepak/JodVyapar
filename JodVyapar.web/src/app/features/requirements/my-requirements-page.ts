import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { Observable } from 'rxjs';

import { ListingListBase } from '../../core/listings/listing-list-base';
import { ListingFact, formatDate, requirementFacts } from '../../core/listings/listing-format';
import { PagedResult, Requirement } from '../../core/listings/listing.models';
import { RequirementService } from '../../core/listings/requirement.service';
import { AlertBanner, EmptyState, ListingCard, PageHeader, Pager, Skeleton, StatusBadge } from '../../shared/ui';

/** /my/requirements: the buyer's own requirements, with edit, pause/activate, renew, fulfil and delete. */
@Component({
  selector: 'app-my-requirements-page',
  imports: [RouterLink, MatButtonModule, MatIcon, AlertBanner, EmptyState, ListingCard, PageHeader, Pager, Skeleton, StatusBadge],
  templateUrl: './my-requirements-page.html',
  styleUrl: '../catalog/listing-pages.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyRequirementsPage extends ListingListBase<Requirement> {
  private readonly requirements = inject(RequirementService);

  protected fetch(page: number): Observable<PagedResult<Requirement>> {
    return this.requirements.getMine(page, this.pageSize);
  }

  protected facts(requirement: Requirement): ListingFact[] {
    const expiry = requirement.status === 'Expired' ? 'Expired on' : 'Expires on';
    return [
      ...requirementFacts(requirement),
      ...(requirement.status === 'Fulfilled' ? [] : [{ icon: 'schedule', text: `${expiry} ${formatDate(requirement.expiresAt)}` }]),
    ];
  }

  protected pause(requirement: Requirement): void {
    this.runAction(requirement, this.requirements.pause(requirement.id), "Requirement paused. Sellers won't see it for now.");
  }

  protected activate(requirement: Requirement): void {
    this.runAction(requirement, this.requirements.activate(requirement.id), 'Requirement is live again.');
  }

  protected renew(requirement: Requirement): void {
    this.runAction(requirement, this.requirements.renew(requirement.id), 'Requirement renewed for 60 days.');
  }

  protected fulfill(requirement: Requirement): void {
    this.confirmAction(
      requirement,
      {
        title: 'Mark as fulfilled?',
        message: `"${requirement.title}" will be taken off your profile and can't be made live again.`,
        confirmLabel: 'Mark as fulfilled',
      },
      () => this.requirements.fulfill(requirement.id),
      'Marked as fulfilled.',
    );
  }

  protected remove(requirement: Requirement): void {
    this.confirmDelete(
      requirement,
      {
        title: 'Delete this requirement?',
        message: `"${requirement.title}" will be removed from your profile. This can't be undone.`,
        confirmLabel: 'Delete requirement',
      },
      () => this.requirements.delete(requirement.id),
      'Requirement deleted.',
    );
  }
}
