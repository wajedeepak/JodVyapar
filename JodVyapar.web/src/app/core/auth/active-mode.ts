import { ActiveMode, UserRole } from './auth.models';

/** Order roles are shown in everywhere (matches the API). */
export const ROLE_ORDER: readonly UserRole[] = ['Buyer', 'Seller', 'Admin'];

const MODE_KEY_PREFIX = 'jv.mode.';

/**
 * The mode the UI should show, from the roles the user holds and their stored preference:
 * - the preference, if it is a role they hold;
 * - otherwise Buyer if they hold it (the default for Buyer + Seller), then Seller;
 * - null if they hold neither (an Admin-only account).
 */
export function resolveActiveMode(roles: readonly UserRole[], preferred: ActiveMode | null): ActiveMode | null {
  if (preferred && roles.includes(preferred)) {
    return preferred;
  }
  if (roles.includes('Buyer')) {
    return 'Buyer';
  }
  return roles.includes('Seller') ? 'Seller' : null;
}

/** Roles in display order without duplicates or unknown values (e.g. from an old cached session). */
export function normaliseRoles(roles: readonly string[]): UserRole[] {
  return ROLE_ORDER.filter((role) => roles.includes(role));
}

/** The saved mode for one user on this device, or null if none / unreadable. Kept per user so a shared browser doesn't mix them. */
export function readStoredMode(userId: string): ActiveMode | null {
  try {
    const value = localStorage.getItem(MODE_KEY_PREFIX + userId);
    return value === 'Buyer' || value === 'Seller' ? value : null;
  } catch {
    return null;
  }
}

export function storeMode(userId: string, mode: ActiveMode): void {
  try {
    localStorage.setItem(MODE_KEY_PREFIX + userId, mode);
  } catch {
    // Storage full or blocked (private mode): the choice still applies for this visit.
  }
}
