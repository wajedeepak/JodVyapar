import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  afterNextRender,
  computed,
  input,
  signal,
  viewChild,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatIconButton } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';
import { startWith, switchMap } from 'rxjs';

import { controlError } from '../../../core/forms/form-errors';

interface Requirement {
  label: string;
  test: (value: string) => boolean;
}

/** Mirrors the API password policy (and the register form validators). */
export const PASSWORD_REQUIREMENTS: readonly Requirement[] = [
  { label: 'At least 8 characters', test: (v) => v.length >= 8 },
  { label: 'An uppercase letter', test: (v) => /[A-Z]/.test(v) },
  { label: 'A lowercase letter', test: (v) => /[a-z]/.test(v) },
  { label: 'A number', test: (v) => /\d/.test(v) },
];

const STRENGTH_LABELS = ['', 'Weak', 'Fair', 'Good', 'Strong'] as const;

let nextId = 0;

/**
 * Password field with a show/hide toggle. With `showRequirements` it also renders a live
 * strength meter and the policy checklist (use for new passwords, not on sign-in).
 */
@Component({
  selector: 'app-password-input',
  imports: [ReactiveFormsModule, MatFormFieldModule, MatIcon, MatIconButton, MatInput],
  templateUrl: './password-input.html',
  styleUrl: './password-input.scss',
  // Default change detection: the error text depends on FormControl state, which is not a signal.
  changeDetection: ChangeDetectionStrategy.Default,
})
export class PasswordInput {
  readonly control = input.required<FormControl<string>>();
  readonly label = input('Password');
  readonly autocomplete = input<'current-password' | 'new-password'>('current-password');
  readonly errorMessages = input<Record<string, string>>({});
  readonly showRequirements = input(false);
  readonly autofocus = input(false);

  protected readonly visible = signal(false);
  protected readonly requirementsId = `jv-password-req-${nextId++}`;

  private readonly inputRef = viewChild.required<ElementRef<HTMLInputElement>>('input');

  private readonly value = toSignal(
    toObservable(this.control).pipe(switchMap((c) => c.valueChanges.pipe(startWith(c.value)))),
    { initialValue: '' },
  );

  protected readonly checks = computed(() =>
    PASSWORD_REQUIREMENTS.map((r) => ({ label: r.label, met: r.test(this.value() ?? '') })),
  );

  /** 0 (empty) to 4: one point per met requirement, minus one if any requirement is unmet. */
  protected readonly strength = computed(() => {
    const value = this.value() ?? '';
    if (!value) {
      return 0;
    }
    const met = this.checks().filter((c) => c.met).length;
    const allMet = met === PASSWORD_REQUIREMENTS.length;
    const bonus = value.length >= 12 || /[^A-Za-z0-9]/.test(value) ? 1 : 0;
    return allMet ? Math.min(4, 3 + bonus) : Math.max(1, met - 1);
  });

  protected readonly strengthLabel = computed(() => STRENGTH_LABELS[this.strength()]);

  constructor() {
    afterNextRender(() => {
      if (this.autofocus()) {
        this.inputRef().nativeElement.focus();
      }
    });
  }

  protected error(): string | null {
    return controlError(this.control(), this.errorMessages());
  }

  protected toggle(): void {
    this.visible.update((v) => !v);
  }
}
