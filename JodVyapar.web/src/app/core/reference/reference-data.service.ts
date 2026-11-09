import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, shareReplay, throwError } from 'rxjs';

import { environment } from '../../../environments/environment';

export interface State {
  id: string;
  name: string;
  code: string;
  isUnionTerritory: boolean;
}

export interface City {
  id: string;
  stateId: string;
  name: string;
}

export interface Category {
  id: string;
  name: string;
  slug: string;
  children: Category[];
}

export interface Unit {
  id: string;
  name: string;
  symbol: string;
}

/**
 * Lookup data (states, cities, categories, units). Each response is fetched once per app session
 * and shared; a failed request is dropped from the cache so the next call retries.
 */
@Injectable({ providedIn: 'root' })
export class ReferenceDataService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/reference`;
  private readonly cache = new Map<string, Observable<unknown>>();

  getStates(): Observable<State[]> {
    return this.cached('states', () => this.http.get<State[]>(`${this.baseUrl}/states`));
  }

  getCities(stateId: string): Observable<City[]> {
    return this.cached(`cities:${stateId}`, () =>
      this.http.get<City[]>(`${this.baseUrl}/cities`, { params: { stateId } }),
    );
  }

  getCategories(): Observable<Category[]> {
    return this.cached('categories', () => this.http.get<Category[]>(`${this.baseUrl}/categories`));
  }

  getUnits(): Observable<Unit[]> {
    return this.cached('units', () => this.http.get<Unit[]>(`${this.baseUrl}/units`));
  }

  private cached<T>(key: string, request: () => Observable<T>): Observable<T> {
    let response = this.cache.get(key) as Observable<T> | undefined;
    if (!response) {
      response = request().pipe(
        catchError((error: unknown) => {
          this.cache.delete(key);
          return throwError(() => error);
        }),
        shareReplay(1),
      );
      this.cache.set(key, response);
    }
    return response;
  }
}
