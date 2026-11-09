import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** JodVyapar logo: two linked rings ("jod" = join) plus the wordmark. */
@Component({
  selector: 'app-logo-mark',
  template: `
    <svg class="mark" viewBox="0 0 32 32" aria-hidden="true" focusable="false">
      <rect class="tile" width="32" height="32" rx="9" />
      <circle class="ring" cx="12.5" cy="16" r="5.75" />
      <circle class="ring ring-2" cx="19.5" cy="16" r="5.75" />
    </svg>
    @if (showWordmark()) {
      <span class="wordmark">Jod<span class="accent">Vyapar</span></span>
    } @else {
      <span class="jv-sr-only">JodVyapar</span>
    }
  `,
  styleUrl: './logo-mark.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': '"size-" + size() + " variant-" + variant()',
  },
})
export class LogoMark {
  readonly size = input<'sm' | 'md' | 'lg'>('md');
  /** `inverse` is for blue / dark backgrounds. */
  readonly variant = input<'default' | 'inverse'>('default');
  readonly showWordmark = input(true);
}
