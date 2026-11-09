import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatIcon } from '@angular/material/icon';

import type { ListingFact } from '../../../core/listings/listing-format';

/**
 * One product or requirement in a list. Put a status badge in `[listingBadge]` and buttons in
 * `[listingActions]`. `showImage` reserves the picture area, with a placeholder when there is no image.
 */
@Component({
  selector: 'app-listing-card',
  imports: [MatIcon],
  template: `
    @if (showImage()) {
      <div class="media">
        @if (imageUrl(); as url) {
          <img [src]="url" alt="" loading="lazy" />
        } @else {
          <mat-icon aria-hidden="true">image</mat-icon>
        }
      </div>
    }
    <div class="body">
      <div class="heading">
        @if (headingLevel() === 3) {
          <h3 class="title">{{ title() }}</h3>
        } @else {
          <h2 class="title">{{ title() }}</h2>
        }
        <ng-content select="[listingBadge]" />
      </div>
      <p class="category">{{ category() }}</p>
      @if (description(); as text) {
        <p class="description">{{ text }}</p>
      }
      <ul class="facts">
        @for (fact of facts(); track fact.text) {
          <li>
            <mat-icon aria-hidden="true">{{ fact.icon }}</mat-icon>
            <span>{{ fact.text }}</span>
          </li>
        }
      </ul>
      <div class="actions"><ng-content select="[listingActions]" /></div>
    </div>
  `,
  styleUrl: './listing-card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ListingCard {
  readonly title = input.required<string>();
  /** For example "Textiles › Fabrics". */
  readonly category = input.required<string>();
  readonly facts = input.required<ListingFact[]>();
  readonly description = input<string | null>(null);
  readonly showImage = input(false);
  readonly imageUrl = input<string | null>(null);
  readonly headingLevel = input<2 | 3>(2);
}
