import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { Observable } from 'rxjs';

import { ListingListBase } from '../../core/listings/listing-list-base';
import { productFacts } from '../../core/listings/listing-format';
import { PagedResult, Product } from '../../core/listings/listing.models';
import { ProductService } from '../../core/listings/product.service';
import { AlertBanner, EmptyState, ListingCard, PageHeader, Pager, Skeleton, StatusBadge } from '../../shared/ui';

/** /my/products: the seller's own products, with edit, pause/activate and delete. */
@Component({
  selector: 'app-my-products-page',
  imports: [RouterLink, MatButtonModule, MatIcon, AlertBanner, EmptyState, ListingCard, PageHeader, Pager, Skeleton, StatusBadge],
  templateUrl: './my-products-page.html',
  styleUrl: './listing-pages.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyProductsPage extends ListingListBase<Product> {
  private readonly products = inject(ProductService);

  protected readonly facts = productFacts;

  protected fetch(page: number): Observable<PagedResult<Product>> {
    return this.products.getMine(page, this.pageSize);
  }

  protected pause(product: Product): void {
    this.runAction(product, this.products.pause(product.id), "Product paused. Buyers won't see it for now.");
  }

  protected activate(product: Product): void {
    this.runAction(product, this.products.activate(product.id), 'Product is live again.');
  }

  protected remove(product: Product): void {
    this.confirmDelete(
      product,
      {
        title: 'Delete this product?',
        message: `"${product.title}" will be removed from your profile. This can't be undone.`,
        confirmLabel: 'Delete product',
      },
      () => this.products.delete(product.id),
      'Product deleted.',
    );
  }
}
