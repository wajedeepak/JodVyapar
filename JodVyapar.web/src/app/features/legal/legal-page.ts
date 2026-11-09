import { Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';

import { AlertBanner, PageHeader } from '../../shared/ui';

const DOCS = {
  privacy: {
    title: 'Privacy Policy',
    intro:
      'JodVyapar uses the details you give us (your name, company, location, email and mobile number) ' +
      'to create your business profile and to connect you with other businesses on the platform.',
  },
  terms: {
    title: 'Terms of Use',
    intro:
      'JodVyapar is a platform for Indian businesses to find and contact each other. By creating an account ' +
      'you agree to provide accurate business information and to use the platform for genuine trade enquiries.',
  },
} as const;

/** Placeholder for the Privacy Policy and Terms of Use until the final legal text is ready. */
@Component({
  selector: 'app-legal-page',
  imports: [RouterLink, AlertBanner, PageHeader],
  template: `
    <article class="legal">
      <app-page-header [title]="content().title" subtitle="Last updated: draft" />
      <app-alert-banner type="info">
        This is a placeholder. The full {{ content().title }} will be published before launch.
      </app-alert-banner>
      <p>{{ content().intro }}</p>
      <p>
        See also the
        @if (doc() === 'privacy') {
          <a routerLink="/legal/terms">Terms of Use</a>.
        } @else {
          <a routerLink="/legal/privacy">Privacy Policy</a>.
        }
      </p>
    </article>
  `,
  styles: `
    .legal {
      display: flex;
      flex-direction: column;
      gap: var(--jv-space-6);
      max-width: var(--jv-prose-width);
    }
  `,
})
export class LegalPage {
  /** Bound from route data. */
  readonly doc = input<keyof typeof DOCS>('terms');

  protected readonly content = computed(() => DOCS[this.doc()]);
}
