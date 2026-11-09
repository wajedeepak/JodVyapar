import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/** Up to two initials from a name: "Asha Patel" -> "AP", "asha" -> "A". */
export function initialsOf(name: string): string {
  const words = name.trim().split(/\s+/).filter(Boolean);
  const picked = words.length > 1 ? [words[0], words[words.length - 1]] : words;
  return picked.map((word) => word.charAt(0).toLocaleUpperCase()).join('');
}

/** A round avatar with the person's initials. Decorative: always show the name as text nearby. */
@Component({
  selector: 'app-avatar',
  template: `{{ initials() }}`,
  styles: `
    :host {
      display: inline-flex;
      flex: none;
      align-items: center;
      justify-content: center;
      width: var(--jv-avatar-size-md);
      height: var(--jv-avatar-size-md);
      border-radius: var(--jv-radius-pill);
      background: var(--jv-color-primary-soft);
      color: var(--jv-color-primary);
      font-size: var(--jv-font-size-sm);
      font-weight: var(--jv-font-weight-semibold);
      line-height: 1;
      user-select: none;
    }
    :host(.size-sm) {
      width: var(--jv-avatar-size-sm);
      height: var(--jv-avatar-size-sm);
      font-size: var(--jv-font-size-xs);
    }
    :host(.size-lg) {
      width: var(--jv-avatar-size-lg);
      height: var(--jv-avatar-size-lg);
      font-size: var(--jv-font-size-2xl);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    'aria-hidden': 'true',
    '[class]': '"size-" + size()',
  },
})
export class Avatar {
  readonly name = input.required<string>();
  readonly size = input<'sm' | 'md' | 'lg'>('md');

  protected readonly initials = computed(() => initialsOf(this.name()) || '?');
}
