import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { Subject, catchError, map, merge, of, switchMap, tap } from 'rxjs';

import { controlError } from '../../../core/forms/form-errors';
import { City, ReferenceDataService, State } from '../../../core/reference/reference-data.service';

/** Loading pattern for a lookup list (see STYLEGUIDE.md, "Loading, empty and error"). */
export type ListStatus = 'idle' | 'loading' | 'ready' | 'empty' | 'error';

const MESSAGES = {
  state: { required: 'Please choose a state.' },
  city: { required: 'Please choose a city.' },
};

/**
 * The State and City selects side by side (stacked on phones). City depends on State: changing
 * the state clears the city and loads that state's cities. If the controls already hold values
 * (an edit form), the saved city is kept. Each list shows loading, empty and error-with-Retry hints.
 */
@Component({
  selector: 'app-state-city-fields',
  imports: [ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatProgressSpinnerModule, MatSelectModule],
  template: `
    <mat-form-field>
      <mat-label>State</mat-label>
      <mat-select [formControl]="stateControl()">
        @for (state of states(); track state.id) {
          <mat-option [value]="state.id">{{ state.name }}</mat-option>
        }
      </mat-select>
      @switch (statesStatus()) {
        @case ('loading') {
          <mat-hint class="list-hint">
            <mat-spinner diameter="14" strokeWidth="2" aria-hidden="true" />Loading states…
          </mat-hint>
        }
        @case ('empty') {
          <mat-hint>No states available right now.</mat-hint>
        }
        @case ('error') {
          <mat-hint class="list-hint is-error">
            Couldn't load states.
            <button mat-button type="button" class="retry" (click)="loadStates()">Retry</button>
          </mat-hint>
        }
      }
      <mat-error>{{ error(stateControl(), messages.state) }}</mat-error>
    </mat-form-field>

    <mat-form-field>
      <mat-label>City</mat-label>
      <mat-select [formControl]="cityControl()">
        @for (city of cities(); track city.id) {
          <mat-option [value]="city.id">{{ city.name }}</mat-option>
        }
      </mat-select>
      @switch (citiesStatus()) {
        @case ('idle') {
          <mat-hint>Choose a state first</mat-hint>
        }
        @case ('loading') {
          <mat-hint class="list-hint">
            <mat-spinner diameter="14" strokeWidth="2" aria-hidden="true" />Loading cities…
          </mat-hint>
        }
        @case ('empty') {
          <mat-hint>No cities listed for this state yet.</mat-hint>
        }
        @case ('error') {
          <mat-hint class="list-hint is-error">
            Couldn't load cities.
            <button mat-button type="button" class="retry" (click)="reloadCities()">Retry</button>
          </mat-hint>
        }
      }
      <mat-error>{{ error(cityControl(), messages.city) }}</mat-error>
    </mat-form-field>
  `,
  styleUrl: './state-city-fields.scss',
  // Default change detection: error text depends on FormControl state, which is not a signal.
  changeDetection: ChangeDetectionStrategy.Default,
})
export class StateCityFields implements OnInit {
  private readonly referenceData = inject(ReferenceDataService);
  private readonly destroyRef = inject(DestroyRef);

  readonly stateControl = input.required<FormControl<string>>();
  readonly cityControl = input.required<FormControl<string>>();

  protected readonly messages = MESSAGES;
  protected readonly states = signal<State[]>([]);
  protected readonly statesStatus = signal<ListStatus>('loading');
  protected readonly cities = signal<City[]>([]);
  protected readonly citiesStatus = signal<ListStatus>('idle');

  private readonly retryCities = new Subject<void>();

  ngOnInit(): void {
    this.loadStates();

    const state = this.stateControl();
    const city = this.cityControl();

    // Each load says which city to keep: the saved one on first load, none after a state change.
    const initial = of({ stateId: state.value, keepCityId: city.value });
    const changed = state.valueChanges.pipe(map((stateId) => ({ stateId, keepCityId: '' })));
    const retried = this.retryCities.pipe(map(() => ({ stateId: state.value, keepCityId: '' })));

    merge(initial, changed, retried)
      .pipe(
        tap(({ stateId, keepCityId }) => {
          city.reset(keepCityId, { emitEvent: false });
          city.disable({ emitEvent: false });
          this.cities.set([]);
          this.citiesStatus.set(stateId ? 'loading' : 'idle');
        }),
        switchMap(({ stateId, keepCityId }) =>
          stateId
            ? this.referenceData.getCities(stateId).pipe(
                map((cities) => ({ stateId, keepCityId, cities: cities as City[] | null })),
                catchError(() => of({ stateId, keepCityId, cities: null })),
              )
            : of({ stateId, keepCityId, cities: [] as City[] }),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(({ stateId, keepCityId, cities }) => {
        if (cities === null) {
          this.citiesStatus.set('error');
          return;
        }
        if (!stateId) {
          return;
        }
        this.cities.set(cities);
        this.citiesStatus.set(cities.length ? 'ready' : 'empty');
        if (cities.length) {
          city.enable({ emitEvent: false });
          if (keepCityId && !cities.some((c) => c.id === keepCityId)) {
            city.setValue('', { emitEvent: false });
          }
        }
      });
  }

  protected loadStates(): void {
    this.statesStatus.set('loading');
    this.referenceData
      .getStates()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (states) => {
          this.states.set(states);
          this.statesStatus.set(states.length ? 'ready' : 'empty');
        },
        error: () => this.statesStatus.set('error'),
      });
  }

  protected reloadCities(): void {
    this.retryCities.next();
  }

  protected error(control: FormControl<string>, messages: Record<string, string>): string | null {
    return controlError(control, messages);
  }
}
