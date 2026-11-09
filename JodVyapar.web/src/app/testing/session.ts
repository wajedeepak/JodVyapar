import { AuthSession, UserRole } from '../core/auth/auth.models';

export const TEST_USER_ID = 'user-1';

export function testSession(roles: UserRole[], overrides: Partial<AuthSession['user']> = {}): AuthSession {
  return {
    accessToken: 'token',
    expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
    user: {
      id: TEST_USER_ID,
      fullName: 'Asha Patel',
      email: 'asha@example.com',
      roles,
      companyName: 'Patel Textiles',
      ...overrides,
    },
  };
}

/** Saves a session the way AuthService does, so a freshly created AuthService starts signed in. */
export function storeTestSession(roles: UserRole[], overrides: Partial<AuthSession['user']> = {}): AuthSession {
  const session = testSession(roles, overrides);
  localStorage.setItem('jv.session', JSON.stringify(session));
  return session;
}
