import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  forwardRef,
  input,
  signal,
  viewChildren,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { MatIcon } from '@angular/material/icon';

export interface ChoiceOption<T extends string = string> {
  value: T;
  title: string;
  description?: string;
  /** Material Icons ligature, e.g. "storefront". */
  icon?: string;
}

let nextId = 0;

/**
 * Large selectable cards (a form control: use formControlName).
 * - Default: one choice, like a radio group. The value is the option value. Tab focuses the
 *   selected card, arrow keys move and select, Space/Enter select.
 * - `multiple`: any number of choices, like a checkbox group. The value is an array of option
 *   values in option order. Every card is a tab stop; Space/Enter toggle.
 */
@Component({
  selector: 'app-choice-cards',
  imports: [MatIcon],
  templateUrl: './choice-cards.html',
  styleUrl: './choice-cards.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => ChoiceCards), multi: true }],
  host: {
    '[attr.role]': 'multiple() ? "group" : "radiogroup"',
    '[attr.aria-label]': 'ariaLabel() ?? null',
    '[attr.aria-labelledby]': 'ariaLabelledby() ?? null',
    '[attr.aria-describedby]': 'ariaDescribedby() || null',
    '[attr.aria-disabled]': 'disabled() || null',
  },
})
export class ChoiceCards<T extends string = string> implements ControlValueAccessor {
  readonly options = input.required<readonly ChoiceOption<T>[]>();
  readonly multiple = input(false);
  readonly ariaLabel = input<string>(undefined, { alias: 'aria-label' });
  readonly ariaLabelledby = input<string>(undefined, { alias: 'aria-labelledby' });
  readonly ariaDescribedby = input<string>(undefined, { alias: 'aria-describedby' });

  protected readonly id = `jv-choice-${nextId++}`;
  /** Selected values (at most one unless `multiple`). */
  protected readonly selected = signal<readonly T[]>([]);
  protected readonly disabled = signal(false);
  protected readonly cardRole = computed(() => (this.multiple() ? 'checkbox' : 'radio'));

  private readonly cards = viewChildren<ElementRef<HTMLElement>>('card');
  private onChange: (value: T | T[] | null) => void = () => {};
  private onTouched: () => void = () => {};

  protected isSelected(option: ChoiceOption<T>): boolean {
    return this.selected().includes(option.value);
  }

  /** Radio mode uses a roving tabindex (only the selected card, or the first); checkbox mode tabs to every card. */
  protected tabIndexFor(index: number): number {
    if (this.disabled()) {
      return -1;
    }
    if (this.multiple()) {
      return 0;
    }
    const selected = this.options().findIndex((o) => this.isSelected(o));
    return index === (selected === -1 ? 0 : selected) ? 0 : -1;
  }

  protected select(option: ChoiceOption<T>): void {
    if (this.disabled()) {
      return;
    }

    if (this.multiple()) {
      const next = this.isSelected(option)
        ? this.selected().filter((v) => v !== option.value)
        : [...this.selected(), option.value];
      // Keep option order, so ['Seller', 'Buyer'] and ['Buyer', 'Seller'] never both appear.
      const ordered = this.options().map((o) => o.value).filter((v) => next.includes(v));
      this.selected.set(ordered);
      this.onChange(ordered);
      return;
    }

    if (!this.isSelected(option)) {
      this.selected.set([option.value]);
      this.onChange(option.value);
    }
  }

  protected onKeydown(event: KeyboardEvent, index: number): void {
    const step = { ArrowRight: 1, ArrowDown: 1, ArrowLeft: -1, ArrowUp: -1 }[event.key];
    if (step && !this.multiple()) {
      event.preventDefault();
      const count = this.options().length;
      const next = (index + step + count) % count;
      this.select(this.options()[next]);
      this.cards()[next]?.nativeElement.focus();
    } else if (event.key === ' ' || event.key === 'Enter') {
      event.preventDefault();
      this.select(this.options()[index]);
    }
  }

  protected onBlur(): void {
    this.onTouched();
  }

  writeValue(value: T | readonly T[] | null): void {
    if (Array.isArray(value)) {
      this.selected.set([...value]);
    } else {
      this.selected.set(value ? [value as T] : []);
    }
  }

  registerOnChange(fn: (value: T | T[] | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled.set(isDisabled);
  }
}
