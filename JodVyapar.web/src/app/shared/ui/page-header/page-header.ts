import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** Page or card heading: one h1, optional subtitle, optional actions (`<ng-container pageHeaderActions>`). */
@Component({
  selector: 'app-page-header',
  template: `
    <div class="text">
      <h1>{{ title() }}</h1>
      @if (subtitle(); as s) {
        <p class="subtitle">{{ s }}</p>
      }
    </div>
    <div class="actions"><ng-content select="[pageHeaderActions]" /></div>
  `,
  styles: `
    :host {
      display: flex;
      flex-wrap: wrap;
      align-items: flex-end;
      justify-content: space-between;
      gap: var(--jv-space-4);
    }
    .text {
      display: flex;
      flex-direction: column;
      gap: var(--jv-space-2);
      min-width: 0;
    }
    .subtitle {
      color: var(--jv-color-text-muted);
      font-size: var(--jv-font-size-md);
      line-height: var(--jv-line-height-snug);
    }
    .actions:empty {
      display: none;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PageHeader {
  readonly title = input.required<string>();
  readonly subtitle = input<string>();
}
