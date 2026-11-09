import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';

import { environment } from '../../../environments/environment';
import { normaliseRoles, readStoredMode, resolveActiveMode, storeMode } from './active-mode';
import {
  ActiveMode,
  AuthSession,
  AuthUser,
  CodeRequestedResponse,
  LoginRequest,
  MeResponse,
  RegisterRequest,
  RegisterResponse,
  RegistrableRole,
  ResetPasswordRequest,
  UserRole,
} from './auth.models';

const STORAGE_KEY = 'jv.session';

/**
 * Holds the signed-in user as signals and persists the session (token + user) in localStorage.
 *
 * Roles and mode: `roles()` / `hasRole()` are the roles the user holds (the API enforces the same
 * roles from the JWT). `activeMode()` is only a UI preference (which side's tools to show) and is
 * never sent to the API. See STYLEGUIDE.md, "Roles and active mode".
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly baseUrl = `${environment.apiBaseUrl}/auth`;
  private readonly session = signal<AuthSession | null>(readStoredSession());
  private expiryTimer?: ReturnType<typeof setTimeout>;

  /** Mode chosen during this visit, per user id (the stored value is read once per user). */
  private readonly chosenModes = signal<Record<string, ActiveMode>>({});

  readonly currentUser = computed(() => this.session()?.user ?? null);
  readonly isLoggedIn = computed(() => this.session() !== null);
  readonly accessToken = computed(() => this.session()?.accessToken ?? null);

  /** Every role the signed-in user holds, in display order. Empty when signed out. */
  readonly roles = computed(() => this.currentUser()?.roles ?? []);

  /** True when the user holds both Buyer and Seller, so a mode switcher should be shown. */
  readonly canSwitchMode = computed(() => this.roles().includes('Buyer') && this.roles().includes('Seller'));

  /**
   * Buyer or Seller: which tools the UI shows. Always a role the user holds; defaults to Buyer for
   * users with both; null for Admin-only accounts.
   */
  readonly activeMode = computed<ActiveMode | null>(() => {
    const user = this.currentUser();
    if (!user) {
      return null;
    }
    const preferred = this.chosenModes()[user.id] ?? readStoredMode(user.id);
    return resolveActiveMode(user.roles, preferred);
  });

  constructor() {
    const session = this.session();
    if (session) {
      this.scheduleExpiry(session);
    }
  }

  login(request: LoginRequest): Observable<AuthSession> {
    return this.http
      .post<AuthSession>(`${this.baseUrl}/login`, request)
      .pipe(tap((session) => this.setSession(session)));
  }

  /** Creates the account and emails a verification code. Does not sign in (see verifyEmail). */
  register(request: RegisterRequest): Observable<RegisterResponse> {
    return this.http.post<RegisterResponse>(`${this.baseUrl}/register`, request);
  }

  /** Verifies the email with the emailed code and signs the user in. */
  verifyEmail(email: string, code: string): Observable<AuthSession> {
    return this.http
      .post<AuthSession>(`${this.baseUrl}/verify-email`, { email, code })
      .pipe(tap((session) => this.setSession(session)));
  }

  /** Emails a new verification code if the account exists and is unverified (the response never says). */
  resendVerification(email: string): Observable<CodeRequestedResponse> {
    return this.http.post<CodeRequestedResponse>(`${this.baseUrl}/resend-verification`, { email });
  }

  /** Emails a sign-in code if the account exists (the response never says). */
  requestLoginCode(email: string): Observable<CodeRequestedResponse> {
    return this.http.post<CodeRequestedResponse>(`${this.baseUrl}/login/otp/request`, { email });
  }

  /** Signs in with the emailed code. Also verifies the email. */
  verifyLoginCode(email: string, code: string): Observable<AuthSession> {
    return this.http
      .post<AuthSession>(`${this.baseUrl}/login/otp/verify`, { email, code })
      .pipe(tap((session) => this.setSession(session)));
  }

  /** Emails a password reset code if the account exists (the response never says). */
  forgotPassword(email: string): Observable<CodeRequestedResponse> {
    return this.http.post<CodeRequestedResponse>(`${this.baseUrl}/forgot-password`, { email });
  }

  /** Sets a new password with the emailed code. Does not sign in; the user goes to Login next. */
  resetPassword(request: ResetPasswordRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/reset-password`, request);
  }

  /**
   * Adds Buyer or Seller to the signed-in user. The API returns a fresh token that carries the new
   * role, which replaces the session so the role takes effect immediately. The UI switches to the
   * new mode, since the user just asked for it.
   */
  addRole(role: RegistrableRole): Observable<AuthSession> {
    return this.http.post<AuthSession>(`${environment.apiBaseUrl}/profile/me/roles`, { role }).pipe(
      tap((session) => {
        this.setSession(session);
        this.setMode(role);
      }),
    );
  }

  /** Remembers the mode on this device. Ignored unless the user holds that role. */
  setMode(mode: ActiveMode): void {
    const user = this.currentUser();
    if (!user || !user.roles.includes(mode)) {
      return;
    }
    storeMode(user.id, mode);
    this.chosenModes.update((modes) => ({ ...modes, [user.id]: mode }));
  }

  /** Updates the cached user after a profile edit (name, company), so the shell shows it without a reload. */
  updateCachedUser(changes: Partial<Pick<AuthUser, 'fullName' | 'companyName'>>): void {
    const session = this.session();
    if (session) {
      this.setSession({ ...session, user: { ...session.user, ...changes } });
    }
  }

  /**
   * Checks a restored session against the API on startup. A 401 (revoked/invalid token) is handled
   * by the interceptor, which logs out and redirects; on success the cached user details are refreshed.
   */
  verifySession(): void {
    const session = this.session();
    if (!session) {
      return;
    }

    this.http.get<MeResponse>(`${this.baseUrl}/me`).subscribe({
      next: (me) =>
        this.setSession({
          ...session,
          user: {
            id: me.id,
            fullName: me.fullName,
            email: me.email,
            roles: normaliseRoles(me.roles),
            companyName: me.company?.companyName ?? null,
          },
        }),
      error: () => {
        // 401 is handled by the interceptor; other failures (e.g. offline) keep the cached session.
      },
    });
  }

  /** True if the user holds at least one of the given roles (held roles, never the active mode). */
  hasRole(...roles: UserRole[]): boolean {
    const held = this.roles();
    return roles.some((role) => held.includes(role));
  }

  logout(returnUrl?: string): void {
    clearTimeout(this.expiryTimer);
    localStorage.removeItem(STORAGE_KEY);
    this.session.set(null);
    this.router.navigate(['/auth/login'], returnUrl ? { queryParams: { returnUrl } } : {});
  }

  private setSession(session: AuthSession): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
    this.session.set(session);
    this.scheduleExpiry(session);
  }

  /** Signs the user out when the token expires, instead of waiting for the next 401. */
  private scheduleExpiry(session: AuthSession): void {
    clearTimeout(this.expiryTimer);
    const msLeft = new Date(session.expiresAt).getTime() - Date.now();
    this.expiryTimer = setTimeout(() => this.logout(this.router.url), Math.max(msLeft, 0));
  }
}

/** Restores a saved session, discarding it if it is unreadable or the token has expired. */
function readStoredSession(): AuthSession | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return null;
    }

    // Sessions saved before dual roles have `user.role` (one value) instead of `user.roles`.
    const session = JSON.parse(raw) as AuthSession & { user: { role?: string; roles?: string[] } };
    if (new Date(session.expiresAt).getTime() > Date.now()) {
      const { role, roles, ...user } = session.user;
      return { ...session, user: { ...user, roles: normaliseRoles(roles ?? (role ? [role] : [])) } };
    }
  } catch {
    // Fall through and clear the corrupt value.
  }

  localStorage.removeItem(STORAGE_KEY);
  return null;
}
