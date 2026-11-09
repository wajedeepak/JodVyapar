export type UserRole = 'Buyer' | 'Seller' | 'Admin';

/** Roles a user can give themselves (at registration or later). Admin is never self-assignable. */
export type RegistrableRole = Extract<UserRole, 'Buyer' | 'Seller'>;

/**
 * Which side of the marketplace the UI is showing. A UI preference only: the API never sees it
 * and always authorises on the roles the user holds.
 */
export type ActiveMode = RegistrableRole;

export interface AuthUser {
  id: string;
  fullName: string;
  email: string;
  /** Every role the user holds, in display order (Buyer, Seller, Admin). */
  roles: UserRole[];
  companyName: string | null;
}

/** Response of POST /auth/login, /auth/verify-email and /auth/login/otp/verify; stored as the session. */
export interface AuthSession {
  accessToken: string;
  /** ISO timestamp (UTC) when the access token expires. */
  expiresAt: string;
  user: AuthUser;
}

/** Response of GET /auth/me (only the fields the app uses today). */
export interface MeResponse {
  id: string;
  fullName: string;
  email: string;
  roles: UserRole[];
  company: { companyName: string } | null;
}

export interface LoginRequest {
  email: string;
  password: string;
}

/** Response of POST /auth/register: no token yet, the email must be verified first. */
export interface RegisterResponse {
  requiresEmailVerification: boolean;
  email: string;
}

/** ProblemDetails `code` when a correct password is used on an unverified email (403). */
export const EMAIL_NOT_VERIFIED = 'EMAIL_NOT_VERIFIED';

/** Same message whether or not the email has an account. */
export interface CodeRequestedResponse {
  message: string;
}

export interface ResetPasswordRequest {
  email: string;
  code: string;
  newPassword: string;
}

/** ProblemDetails `code` for a wrong, expired, used or burnt-out emailed code. */
export const INVALID_OR_EXPIRED_CODE = 'INVALID_OR_EXPIRED_CODE';

export interface RegisterRequest {
  fullName: string;
  companyName: string;
  email: string;
  mobile: string;
  password: string;
  stateId: string;
  cityId: string;
  /** Buyer, Seller or both; at least one. */
  roles: RegistrableRole[];
  consentAccepted: boolean;
}
