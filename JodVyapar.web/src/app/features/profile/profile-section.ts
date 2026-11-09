import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * A titled card on the profile pages. Content goes in the slot; detail rows use
 * `<dl class="details">` (styled here: label above value on phones, side by side from `sm`).
 */
@Component({
  selector: 'app-profile-section',
  template: `
    <h2 class="title">{{ title() }}</h2>
    <ng-content />
  `,
  styleUrl: './profile-section.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfileSection {
  readonly title = input.required<string>();
}
