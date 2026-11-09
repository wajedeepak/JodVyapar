import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

/** Attaches the JWT to API calls and sends the user to login when the API answers 401. */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const isApiRequest = req.url.startsWith(environment.apiBaseUrl);
  const token = auth.accessToken();
  const request =
    isApiRequest && token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(request).pipe(
    catchError((error: unknown) => {
      // A 401 from the login call means "wrong credentials" and is shown on the form instead.
      const isLoginCall = req.url.endsWith('/auth/login');
      if (error instanceof HttpErrorResponse && error.status === 401 && isApiRequest && !isLoginCall) {
        auth.logout(router.url);
      }
      return throwError(() => error);
    }),
  );
};
