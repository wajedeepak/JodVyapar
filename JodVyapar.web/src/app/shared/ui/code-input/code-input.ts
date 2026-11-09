import { ChangeDetectionStrategy, Component, ElementRef, afterNextRender, input, viewChild } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';

import { controlError } from '../../../core/forms/form-errors';

/** Length of the codes the API emails (verification, sign-in, password reset). */
export const CODE_LENGTH = 6;

/**
 * Entry for a 6-digit emailed code. One input (not six boxes), so paste, autofill from the
 * email (autocomplete="one-time-code") and screen readers just work. Anything that isn't a
 * digit is dropped as the user types or pastes. Pair the control with
 * `Validators.required` and `Validators.pattern(/^\d{6}$/)`.
 */
@Component({
  selector: 'app-code-input',
  imports: [ReactiveFormsModule, MatFormFieldModule, MatInput],
  template: `
    <mat-form-field>
      <mat-label>{{ label() }}</mat-label>
      <input
        #input
        matInput
        class="code"
        [formControl]="control()"
        (input)="sanitize()"
        type="text"
        inputmode="numeric"
        autocomplete="one-time-code"
        [attr.maxlength]="length"
        autocapitalize="off"
        spellcheck="false"
      />
      @if (hint(); as h) {
        <mat-hint>{{ h }}</mat-hint>
      }
      <mat-error>{{ error() }}</mat-error>
    </mat-form-field>
  `,
  styles: `
    :host {
      display: block;
    }
    mat-form-field {
      width: 100%;
    }
    .code {
      font-size: var(--jv-font-size-xl);
      font-weight: var(--jv-font-weight-semibold);
      font-variant-numeric: tabular-nums;
      letter-spacing: var(--jv-space-2);
    }
  `,
  // Default change detection: the error text depends on FormControl state, which is not a signal.
  changeDetection: ChangeDetectionStrategy.Default,
})
export class CodeInput {
  readonly control = input.required<FormControl<string>>();
  readonly label = input('6-digit code');
  readonly hint = input<string>();
  readonly errorMessages = input<Record<string, string>>({
    required: 'Please enter the 6-digit code.',
    pattern: 'The code is 6 digits.',
  });
  readonly autofocus = input(false);

  protected readonly length = CODE_LENGTH;
  private readonly inputRef = viewChild.required<ElementRef<HTMLInputElement>>('input');

  constructor() {
    afterNextRender(() => {
      if (this.autofocus()) {
        this.inputRef().nativeElement.focus();
      }
    });
  }

  protected sanitize(): void {
    const control = this.control();
    const digits = control.value.replace(/\D/g, '').slice(0, CODE_LENGTH);
    if (digits !== control.value) {
      control.setValue(digits);
    }
  }

  protected error(): string | null {
    return controlError(this.control(), this.errorMessages());
  }
}
