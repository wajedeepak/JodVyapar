import { ChangeDetectionStrategy, Component, ElementRef, afterNextRender, input, viewChild } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';

import { controlError } from '../../../core/forms/form-errors';

/**
 * Labelled text input bound to a reactive FormControl, with hint and inline error.
 * Errors appear after blur or submit (Material's default) and are announced politely.
 * The message for each validator key comes from `errorMessages`; server errors win.
 */
@Component({
  selector: 'app-text-field',
  imports: [ReactiveFormsModule, MatFormFieldModule, MatInput],
  template: `
    <mat-form-field>
      <mat-label>{{ label() }}</mat-label>
      @if (prefix(); as p) {
        <span matTextPrefix class="prefix">{{ p }}&nbsp;</span>
      }
      <input
        #input
        matInput
        [formControl]="control()"
        [type]="type()"
        [attr.autocomplete]="autocomplete() ?? null"
        [attr.inputmode]="inputmode() ?? null"
        [attr.maxlength]="maxlength() ?? null"
        [placeholder]="placeholder() ?? ''"
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
    .prefix {
      color: var(--jv-color-text-muted);
    }
  `,
  // Default change detection: the error text depends on FormControl state, which is not a signal.
  changeDetection: ChangeDetectionStrategy.Default,
})
export class TextField {
  readonly control = input.required<FormControl<string>>();
  readonly label = input.required<string>();
  readonly type = input<'text' | 'email' | 'tel' | 'url' | 'number' | 'search'>('text');
  readonly autocomplete = input<string>();
  readonly inputmode = input<'text' | 'email' | 'tel' | 'numeric' | 'decimal' | 'url' | 'search'>();
  readonly maxlength = input<number>();
  readonly placeholder = input<string>();
  readonly prefix = input<string>();
  readonly hint = input<string>();
  readonly errorMessages = input<Record<string, string>>({});
  /** Focus this field when it first renders (use on the first field of a page). */
  readonly autofocus = input(false);

  private readonly inputRef = viewChild.required<ElementRef<HTMLInputElement>>('input');

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
}
