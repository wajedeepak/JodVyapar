import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * A grey placeholder shaped like the content that is loading. Skeletons are hidden from screen
 * readers: put `aria-busy="true"` and a visually hidden "Loading …" text on the container.
 */
@Component({
  selector: 'app-skeleton',
  template: '',
  styles: `
    :host {
      display: block;
      width: 100%;
      height: var(--jv-font-size-md);
      border-radius: var(--jv-radius-sm);
      background: linear-gradient(
        90deg,
        var(--jv-color-skeleton) 0%,
        var(--jv-color-skeleton-shine) 50%,
        var(--jv-color-skeleton) 100%
      );
      background-size: 200% 100%;
      animation: jv-skeleton-shimmer var(--jv-duration-slow) ease-in-out infinite;
    }
    :host(.shape-title) {
      height: var(--jv-font-size-2xl);
    }
    :host(.shape-circle) {
      flex: none;
      width: var(--jv-avatar-size-lg);
      height: var(--jv-avatar-size-lg);
      border-radius: var(--jv-radius-pill);
    }
    :host(.shape-block) {
      height: var(--jv-space-16);
      border-radius: var(--jv-radius-lg);
    }
    @keyframes jv-skeleton-shimmer {
      from {
        background-position: 100% 0;
      }
      to {
        background-position: -100% 0;
      }
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    'aria-hidden': 'true',
    '[class]': '"shape-" + shape()',
    '[style.max-width]': 'width() ?? null',
  },
})
export class Skeleton {
  readonly shape = input<'text' | 'title' | 'circle' | 'block'>('text');
  /** Any CSS length, applied as max-width (e.g. '60%'). */
  readonly width = input<string>();
}
