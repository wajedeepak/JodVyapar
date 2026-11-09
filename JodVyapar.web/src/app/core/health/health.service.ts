import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';

import { environment } from '../../../environments/environment';

export interface HealthStatus {
  status: string;
  database: string;
  serverTimeUtc: string;
}

@Injectable({ providedIn: 'root' })
export class HealthService {
  private readonly http = inject(HttpClient);

  check() {
    return this.http.get<HealthStatus>(`${environment.apiBaseUrl}/health`);
  }
}
