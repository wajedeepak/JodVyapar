import { inject } from '@angular/core';
import { CanActivateFn, CanDeactivateFn, Router } from '@angular/router';

import { UserRole } from './auth.models';
import { AuthService } from './auth.service';

/** Only signed-in users; others go to login and come back afterwards. */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.isLoggedIn()
    ? true
    : router.createUrlTree(['/auth/login'], { queryParams: { returnUrl: state.url } });
};

/** Only signed-out users (login/register); signed-in users go home. */
export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.isLoggedIn() ? router.createUrlTree(['/']) : true;
};

/**
 * Only signed-in users holding one of the given roles (held roles, never the active mode).
 * Others see the 403 page. Use after authGuard or on its own.
 */
export function roleGuard(...roles: UserRole[]): CanActivateFn {
  return (_route, state) => {
    const auth = inject(AuthService);
    const router = inject(Router);
    if (!auth.isLoggedIn()) {
      return router.createUrlTree(['/auth/login'], { queryParams: { returnUrl: state.url } });
    }
    return auth.hasRole(...roles) ? true : router.createUrlTree(['/forbidden']);
  };
}

/** A page that can hold unsaved edits. */
export interface HasUnsavedChanges {
  /** Resolves true when it is fine to leave (nothing to lose, or the user agreed to discard). */
  confirmLeave(): boolean | Promise<boolean>;
}

/** Asks the page before navigating away, so unsaved edits are not lost by accident. */
export const unsavedChangesGuard: CanDeactivateFn<HasUnsavedChanges> = (component) => component.confirmLeave();
