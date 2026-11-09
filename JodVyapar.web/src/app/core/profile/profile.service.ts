import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AuthService } from '../auth/auth.service';
import { MyProfile, PublicProfile, UpdateProfileRequest } from './profile.models';

/** /api/v1/profile. Adding a role lives on AuthService.addRole, because it replaces the session. */
@Injectable({ providedIn: 'root' })
export class ProfileService {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);
  private readonly baseUrl = `${environment.apiBaseUrl}/profile`;

  getMine(): Observable<MyProfile> {
    return this.http.get<MyProfile>(`${this.baseUrl}/me`);
  }

  /** Saves and refreshes the cached user (name, company) so the shell shows the change at once. */
  updateMine(request: UpdateProfileRequest): Observable<MyProfile> {
    return this.http.put<MyProfile>(`${this.baseUrl}/me`, request).pipe(
      tap((profile) =>
        this.auth.updateCachedUser({ fullName: profile.fullName, companyName: profile.company?.companyName ?? null }),
      ),
    );
  }

  getPublic(userId: string): Observable<PublicProfile> {
    return this.http.get<PublicProfile>(`${this.baseUrl}/${encodeURIComponent(userId)}`);
  }
}
