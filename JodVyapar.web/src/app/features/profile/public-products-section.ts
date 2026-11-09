import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { Observable } from 'rxjs';

import { ListingListBase } from '../../core/listings/listing-list-base';
import { productFacts } from '../../core/listings/listing-format';
import { PagedResult, PublicProduct } from '../../core/listings/listing.models';
import { ProductService } from '../../core/listings/product.service';
import { AlertBanner, EmptyState, ListingCard, Pager, Skeleton } from '../../shared/ui';
import { ProfileSection } from './profile-section';

/** The Products section of a public profile: Active products only (the API filters them), paged. */
@Component({
  selector: 'app-public-products-section',
  imports: [MatButtonModule, AlertBanner, EmptyState, ListingCard, Pager, Skeleton, ProfileSection],
  template: `
    <app-profile-section title="Products">
      @switch (status()) {
        @case ('loading') {
          <div aria-busy="true">
            <span class="jv-sr-only" role="status">Loading products…</span>
            <app-skeleton shape="block" />
          </div>
        }
        @case ('error') {
          <app-alert-banner type="error" title="Couldn't load products">
            <p>{{ loadError() }}</p>
            <button mat-button type="button" (click)="reload()">Retry</button>
          </app-alert-banner>
        }
        @case ('empty') {
          <app-empty-state icon="inventory_2" title="No products to show yet" [message]="companyName() + ' has no products listed right now.'" [compact]="true" />
        }
        @case ('ready') {
          <ul class="listing-grid">
            @for (product of items(); track product.id) {
              <li>
                <app-listing-card
                  [title]="product.title"
                  [category]="product.parentCategoryName + ' › ' + product.categoryName"
                  [description]="product.description"
                  [facts]="facts(product)"
                  [showImage]="true"
                  [imageUrl]="product.images[0]?.url ?? null"
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
export class PublicProductsSection extends ListingListBase<PublicProduct> {
  private readonly products = inject(ProductService);

  readonly userId = input.required<string>();
  readonly companyName = input.required<string>();

  protected readonly facts = productFacts;

  protected fetch(page: number): Observable<PagedResult<PublicProduct>> {
    return this.products.getPublic(this.userId(), page, this.pageSize);
  }
}
