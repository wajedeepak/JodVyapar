import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';

import { controlError } from '../../../core/forms/form-errors';
import { Category, ReferenceDataService } from '../../../core/reference/reference-data.service';
import type { ListStatus } from '../state-city-fields/state-city-fields';

const MESSAGES = {
  parent: { required: 'Please choose a category.' },
  category: { required: 'Please choose a subcategory.' },
};

/**
 * The Category and Subcategory selects (stacked on phones). Subcategory depends on Category:
 * changing the category clears it. If the controls already hold values (an edit form), they are kept.
 */
@Component({
  selector: 'app-category-fields',
  imports: [ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatProgressSpinnerModule, MatSelectModule],
  template: `
    <mat-form-field>
      <mat-label>Category</mat-label>
      <mat-select [formControl]="parentControl()">
        @for (parent of categories(); track parent.id) {
          <mat-option [value]="parent.id">{{ parent.name }}</mat-option>
        }
      </mat-select>
      @switch (status()) {
        @case ('loading') {
          <mat-hint class="list-hint"><mat-spinner diameter="14" strokeWidth="2" aria-hidden="true" />Loading categories…</mat-hint>
        }
        @case ('empty') {
          <mat-hint>No categories available right now.</mat-hint>
        }
        @case ('error') {
          <mat-hint class="list-hint is-error">
            Couldn't load categories.
            <button mat-button type="button" class="retry" (click)="load()">Retry</button>
          </mat-hint>
        }
      }
      <mat-error>{{ error(parentControl(), messages.parent) }}</mat-error>
    </mat-form-field>

    <mat-form-field>
      <mat-label>Subcategory</mat-label>
      <mat-select [formControl]="categoryControl()">
        @for (child of subcategories(); track child.id) {
          <mat-option [value]="child.id">{{ child.name }}</mat-option>
        }
      </mat-select>
      @if (!parentControl().value) {
        <mat-hint>Choose a category first</mat-hint>
      }
      <mat-error>{{ error(categoryControl(), messages.category) }}</mat-error>
    </mat-form-field>
  `,
  styleUrl: '../state-city-fields/state-city-fields.scss',
  // Default change detection: error text depends on FormControl state, which is not a signal.
  changeDetection: ChangeDetectionStrategy.Default,
})
export class CategoryFields implements OnInit {
  private readonly referenceData = inject(ReferenceDataService);
  private readonly destroyRef = inject(DestroyRef);

  readonly parentControl = input.required<FormControl<string>>();
  readonly categoryControl = input.required<FormControl<string>>();

  protected readonly messages = MESSAGES;
  protected readonly status = signal<ListStatus>('loading');
  protected readonly categories = signal<Category[]>([]);
  protected readonly subcategories = signal<Category[]>([]);

  ngOnInit(): void {
    this.categoryControl().disable({ emitEvent: false });
    this.load();

    this.parentControl()
      .valueChanges.pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.categoryControl().reset('', { emitEvent: false });
        this.showSubcategories();
      });
  }

  protected load(): void {
    this.status.set('loading');
    this.referenceData
      .getCategories()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (categories) => {
          this.categories.set(categories);
          this.status.set(categories.length ? 'ready' : 'empty');
          this.showSubcategories();
        },
        error: () => this.status.set('error'),
      });
  }

  /** Shows the chosen category's children. A saved subcategory that is no longer offered is cleared. */
  private showSubcategories(): void {
    const children = this.categories().find((c) => c.id === this.parentControl().value)?.children ?? [];
    const control = this.categoryControl();

    this.subcategories.set(children);
    if (children.length) {
      control.enable({ emitEvent: false });
    } else {
      control.disable({ emitEvent: false });
    }
    if (control.value && !children.some((c) => c.id === control.value)) {
      control.setValue('', { emitEvent: false });
    }
  }

  protected error(control: FormControl<string>, messages: Record<string, string>): string | null {
    return controlError(control, messages);
  }
}
