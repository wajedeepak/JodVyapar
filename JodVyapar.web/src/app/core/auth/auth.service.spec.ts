import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { environment } from '../../../environments/environment';
import { TEST_USER_ID, storeTestSession, testSession } from '../../testing/session';
import { normaliseRoles, resolveActiveMode } from './active-mode';
import { AuthService } from './auth.service';

describe('resolveActiveMode', () => {
  it('uses the preference only when the role is held', () => {
    expect(resolveActiveMode(['Buyer', 'Seller'], 'Seller')).toBe('Seller');
    expect(resolveActiveMode(['Buyer'], 'Seller')).toBe('Buyer');
    expect(resolveActiveMode(['Seller'], 'Buyer')).toBe('Seller');
  });

  it('defaults to Buyer for Buyer + Seller, and is null for Admin-only accounts', () => {
    expect(resolveActiveMode(['Buyer', 'Seller'], null)).toBe('Buyer');
    expect(resolveActiveMode(['Admin'], null)).toBeNull();
    expect(resolveActiveMode([], 'Buyer')).toBeNull();
  });

  it('normalises roles to display order and drops unknown values', () => {
    expect(normaliseRoles(['Seller', 'Admin', 'Buyer', 'Seller', 'Owner'])).toEqual(['Buyer', 'Seller', 'Admin']);
  });
});

describe('AuthService roles and mode', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
  });

  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('upgrades a session saved before dual roles (single "role") without signing the user out', () => {
    const legacy = { ...testSession([]), user: { ...testSession([]).user, roles: undefined, role: 'Seller' } };
    localStorage.setItem('jv.session', JSON.stringify(legacy));

    const auth = TestBed.inject(AuthService);

    expect(auth.isLoggedIn()).toBe(true);
    expect(auth.roles()).toEqual(['Seller']);
    expect(auth.activeMode()).toBe('Seller');
    expect(auth.canSwitchMode()).toBe(false);
  });

  it('checks held roles with hasRole', () => {
    storeTestSession(['Buyer', 'Admin']);
    const auth = TestBed.inject(AuthService);

    expect(auth.hasRole('Buyer')).toBe(true);
    expect(auth.hasRole('Seller')).toBe(false);
    expect(auth.hasRole('Seller', 'Admin')).toBe(true);
  });

  it('defaults Buyer + Seller users to Buyer and remembers their choice per user', () => {
    storeTestSession(['Buyer', 'Seller']);
    const auth = TestBed.inject(AuthService);
    expect(auth.canSwitchMode()).toBe(true);
    expect(auth.activeMode()).toBe('Buyer');

    auth.setMode('Seller');

    expect(auth.activeMode()).toBe('Seller');
    expect(localStorage.getItem(`jv.mode.${TEST_USER_ID}`)).toBe('Seller');
  });

  it('falls back safely when the stored mode is not a held role, and ignores setMode for it', () => {
    localStorage.setItem(`jv.mode.${TEST_USER_ID}`, 'Seller');
    storeTestSession(['Buyer']);
    const auth = TestBed.inject(AuthService);

    expect(auth.activeMode()).toBe('Buyer');
    auth.setMode('Seller');
    expect(auth.activeMode()).toBe('Buyer');

    localStorage.setItem(`jv.mode.${TEST_USER_ID}`, 'garbage');
    expect(auth.activeMode()).toBe('Buyer');
  });

  it('addRole replaces the session with the fresh token and switches to the new mode', () => {
    storeTestSession(['Buyer']);
    const auth = TestBed.inject(AuthService);
    const http = TestBed.inject(HttpTestingController);

    auth.addRole('Seller').subscribe();
    const request = http.expectOne(`${environment.apiBaseUrl}/profile/me/roles`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ role: 'Seller' });
    request.flush({ ...testSession(['Buyer', 'Seller']), accessToken: 'fresh-token' });

    expect(auth.accessToken()).toBe('fresh-token');
    expect(auth.roles()).toEqual(['Buyer', 'Seller']);
    expect(auth.activeMode()).toBe('Seller');
    expect(JSON.parse(localStorage.getItem('jv.session')!).accessToken).toBe('fresh-token');
  });
});
