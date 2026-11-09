import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';

import { storeTestSession } from '../../testing/session';
import { roleGuard } from './auth.guards';
import { AuthService } from './auth.service';

describe('roleGuard', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({ providers: [provideRouter([]), provideHttpClient()] });
  });

  function run(...roles: Parameters<typeof roleGuard>) {
    const state = { url: '/products/new' } as RouterStateSnapshot;
    return TestBed.runInInjectionContext(() => roleGuard(...roles)({} as ActivatedRouteSnapshot, state));
  }

  it('lets users through on a held role', () => {
    storeTestSession(['Seller']);
    expect(run('Seller')).toBe(true);
  });

  it('checks held roles, not the active mode', () => {
    storeTestSession(['Buyer', 'Seller']);
    TestBed.inject(AuthService).setMode('Buyer');
    expect(run('Seller')).toBe(true);
  });

  it('sends users without the role to the 403 page', () => {
    storeTestSession(['Buyer']);
    const result = run('Seller') as UrlTree;
    expect(TestBed.inject(Router).serializeUrl(result)).toBe('/forbidden');
  });

  it('sends signed-out users to login with a return URL', () => {
    const result = run('Seller') as UrlTree;
    expect(TestBed.inject(Router).serializeUrl(result)).toBe('/auth/login?returnUrl=%2Fproducts%2Fnew');
  });
});
