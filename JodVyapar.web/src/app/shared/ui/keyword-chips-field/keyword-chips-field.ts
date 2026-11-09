import { COMMA, ENTER } from '@angular/cdk/keycodes';
import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatChipInputEvent, MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIcon } from '@angular/material/icon';

import { controlError } from '../../../core/forms/form-errors';
import { KEYWORD_MAX_LENGTH, MAX_KEYWORDS } from '../../../core/listings/listing.models';

/** Same result as the API's KeywordNormalizer, so what the user sees is what gets saved. */
export function normalizeKeywords(text: string): string[] {
  return text
    .split(',')
    .map((part) => part.trim().replace(/\s+/g, ' ').toLowerCase())
    .filter(Boolean);
}

/** Keywords as chips: Enter, a comma or leaving the field adds one. The control holds a string array. */
@Component({
  selector: 'app-keyword-chips-field',
  imports: [ReactiveFormsModule, MatChipsModule, MatFormFieldModule, MatIcon],
  template: `
    <mat-form-field>
      <mat-label>Keywords</mat-label>
      <mat-chip-grid #grid [formControl]="control()" aria-label="Keywords">
        @for (keyword of control().value; track keyword) {
          <mat-chip-row (removed)="remove(keyword)">
            {{ keyword }}
            <button matChipRemove type="button" [attr.aria-label]="'Remove ' + keyword">
              <mat-icon aria-hidden="true">cancel</mat-icon>
            </button>
          </mat-chip-row>
        }
        <input
          [matChipInputFor]="grid"
          [matChipInputSeparatorKeyCodes]="separators"
          [matChipInputAddOnBlur]="true"
          [attr.maxlength]="maxLength"
          (matChipInputTokenEnd)="add($event)"
        />
      </mat-chip-grid>
      <mat-hint>{{ limitMessage() ?? hint }}</mat-hint>
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
  `,
  // Default change detection: the chips and error text depend on FormControl state, which is not a signal.
  changeDetection: ChangeDetectionStrategy.Default,
})
export class KeywordChipsField {
  readonly control = input.required<FormControl<string[]>>();

  protected readonly separators = [ENTER, COMMA];
  protected readonly maxLength = KEYWORD_MAX_LENGTH;
  protected readonly hint = `Words buyers and sellers would search for. Press Enter after each, up to ${MAX_KEYWORDS}.`;

  private readonly limitReached = signal(false);
  protected readonly limitMessage = computed(() => (this.limitReached() ? `You can add up to ${MAX_KEYWORDS} keywords.` : null));

  protected add(event: MatChipInputEvent): void {
    const typed = normalizeKeywords(event.value);
    event.chipInput.clear();
    if (!typed.length) {
      return;
    }

    const merged = [...new Set([...this.control().value, ...typed])];
    this.limitReached.set(merged.length > MAX_KEYWORDS);
    this.setKeywords(merged.slice(0, MAX_KEYWORDS));
  }

  protected remove(keyword: string): void {
    this.limitReached.set(false);
    this.setKeywords(this.control().value.filter((k) => k !== keyword));
  }

  protected error(): string | null {
    return controlError(this.control());
  }

  private setKeywords(keywords: string[]): void {
    this.control().setValue(keywords);
    this.control().markAsDirty();
  }
}
