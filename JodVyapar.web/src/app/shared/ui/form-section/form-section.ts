import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** A titled group of related fields, rendered as a fieldset so screen readers announce the group. */
@Component({
  selector: 'app-form-section',
  template: `
    <fieldset>
      <legend>
        <span class="title">{{ title() }}</span>
        @if (description(); as d) {
          <span class="description">{{ d }}</span>
        }
      </legend>
      <div class="content"><ng-content /></div>
    </fieldset>
  `,
  styles: `
    :host {
      display: block;
    }
    fieldset {
      margin: 0;
      padding: 0;
      border: 0;
      min-width: 0;
    }
    legend {
      display: flex;
      flex-direction: column;
      gap: var(--jv-space-1);
      padding: 0;
      margin-bottom: var(--jv-space-4);
    }
    .title {
      font-size: var(--jv-font-size-md);
      font-weight: var(--jv-font-weight-semibold);
      color: var(--jv-color-text);
    }
    .description {
      font-size: var(--jv-font-size-sm);
      color: var(--jv-color-text-muted);
    }
    .content {
      display: flex;
      flex-direction: column;
      gap: var(--jv-space-4);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FormSection {
  readonly title = input.required<string>();
  readonly description = input<string>();
}
