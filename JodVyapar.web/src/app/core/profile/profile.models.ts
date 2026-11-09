import { UserRole } from '../auth/auth.models';

/** GET/PUT /profile/me: the signed-in user's own profile, including private contact details. */
export interface MyProfile {
  id: string;
  fullName: string;
  email: string;
  roles: UserRole[];
  isEmailVerified: boolean;
  /** ISO timestamp (UTC). */
  memberSince: string;
  /** Null for Admin-only accounts that haven't added company details yet. */
  company: MyCompany | null;
}

export interface MyCompany {
  companyName: string;
  stateId: string;
  stateName: string;
  cityId: string;
  cityName: string;
  mobile: string;
  gstin: string | null;
  about: string | null;
  isMobileVerified: boolean;
  isGstinVerified: boolean;
}

/** Body of PUT /profile/me. Email and roles are read-only. */
export interface UpdateProfileRequest {
  fullName: string;
  companyName: string;
  mobile: string;
  stateId: string;
  cityId: string;
  gstin: string | null;
  about: string | null;
}

/** GET /profile/{userId}: what other users see. Never has email, mobile or GSTIN. */
export interface PublicProfile {
  id: string;
  fullName: string;
  companyName: string;
  cityName: string;
  stateName: string;
  roles: UserRole[];
  about: string | null;
  isEmailVerified: boolean;
  isGstinVerified: boolean;
  memberSince: string;
}

/** ProblemDetails `code` when adding a role before company details exist. */
export const COMPANY_PROFILE_REQUIRED = 'COMPANY_PROFILE_REQUIRED';

/** Mirrors the API's format check (no checksum): state code, PAN, entity number, Z, check character. */
export const GSTIN_PATTERN = /^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$/;

export const MOBILE_PATTERN = /^[6-9]\d{9}$/;

export const ABOUT_MAX_LENGTH = 500;
