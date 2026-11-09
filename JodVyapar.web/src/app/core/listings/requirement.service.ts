import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';

import { environment } from '../../../environments/environment';
import { LISTING_PAGE_SIZE, PagedResult, PublicRequirement, Requirement, RequirementRequest, RequirementStatus } from './listing.models';

/** /api/v1/requirements (Buyer role) and the public requirement list of a profile. */
@Injectable({ providedIn: 'root' })
export class RequirementService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/requirements`;

  getMine(page: number, pageSize = LISTING_PAGE_SIZE, status?: RequirementStatus): Observable<PagedResult<Requirement>> {
    return this.http.get<PagedResult<Requirement>>(`${this.baseUrl}/mine`, {
      params: { page, pageSize, ...(status ? { status } : {}) },
    });
  }

  /** How many of the buyer's requirements are live. */
  countActive(): Observable<number> {
    return this.getMine(1, 1, 'Active').pipe(map((page) => page.totalCount));
  }

  get(id: string): Observable<Requirement> {
    return this.http.get<Requirement>(`${this.baseUrl}/${id}`);
  }

  create(request: RequirementRequest): Observable<Requirement> {
    return this.http.post<Requirement>(this.baseUrl, request);
  }

  update(id: string, request: RequirementRequest): Observable<Requirement> {
    return this.http.put<Requirement>(`${this.baseUrl}/${id}`, request);
  }

  pause(id: string): Observable<Requirement> {
    return this.http.post<Requirement>(`${this.baseUrl}/${id}/pause`, null);
  }

  activate(id: string): Observable<Requirement> {
    return this.http.post<Requirement>(`${this.baseUrl}/${id}/activate`, null);
  }

  renew(id: string): Observable<Requirement> {
    return this.http.post<Requirement>(`${this.baseUrl}/${id}/renew`, null);
  }

  fulfill(id: string): Observable<Requirement> {
    return this.http.post<Requirement>(`${this.baseUrl}/${id}/fulfill`, null);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  /** Active, unexpired requirements on a public profile. */
  getPublic(userId: string, page: number, pageSize = LISTING_PAGE_SIZE): Observable<PagedResult<PublicRequirement>> {
    return this.http.get<PagedResult<PublicRequirement>>(
      `${environment.apiBaseUrl}/profile/${encodeURIComponent(userId)}/requirements`,
      { params: { page, pageSize } },
    );
  }
}
