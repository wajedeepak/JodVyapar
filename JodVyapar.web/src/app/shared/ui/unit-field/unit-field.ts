import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';

import { controlError } from '../../../core/forms/form-errors';
import { ReferenceDataService, Unit } from '../../../core/reference/reference-data.service';
import type { ListStatus } from '../state-city-fields/state-city-fields';

/** The unit of measure select, with loading, empty and error-with-Retry hints. */
@Component({
  selector: 'app-unit-field',
  imports: [ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatProgressSpinnerModule, MatSelectModule],
  template: `
    <mat-form-field>
      <mat-label>Unit</mat-label>
      <mat-select [formControl]="control()">
        @for (unit of units(); track unit.id) {
          <mat-option [value]="unit.id">{{ unit.name }} ({{ unit.symbol }})</mat-option>
        }
      </mat-select>
      @switch (status()) {
        @case ('loading') {
          <mat-hint class="list-hint"><mat-spinner diameter="14" strokeWidth="2" aria-hidden="true" />Loading units…</mat-hint>
        }
        @case ('empty') {
          <mat-hint>No units available right now.</mat-hint>
        }
        @case ('error') {
          <mat-hint class="list-hint is-error">
            Couldn't load units.
            <button mat-button type="button" class="retry" (click)="load()">Retry</button>
          </mat-hint>
        }
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
    .list-hint {
      display: inline-flex;
      align-items: center;
      gap: var(--jv-space-2);
    }
    .is-error {
      color: var(--jv-color-error);
    }
    .retry {
      --mat-button-text-container-height: auto;
      --mat-button-text-horizontal-padding: var(--jv-space-1);
      min-width: 0;
      font-size: inherit;
    }
  `,
  // Default change detection: error text depends on FormControl state, which is not a signal.
  changeDetection: ChangeDetectionStrategy.Default,
})
export class UnitField implements OnInit {
  private readonly referenceData = inject(ReferenceDataService);
  private readonly destroyRef = inject(DestroyRef);

  readonly control = input.required<FormControl<string>>();

  protected readonly status = signal<ListStatus>('loading');
  protected readonly units = signal<Unit[]>([]);

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.status.set('loading');
    this.referenceData
      .getUnits()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (units) => {
          this.units.set(units);
          this.status.set(units.length ? 'ready' : 'empty');
        },
        error: () => this.status.set('error'),
      });
  }

  protected error(): string | null {
    return controlError(this.control(), { required: 'Please choose a unit.' });
  }
}
