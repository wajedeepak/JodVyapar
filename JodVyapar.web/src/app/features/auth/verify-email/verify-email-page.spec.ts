import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { vi } from 'vitest';

import { environment } from '../../../../environments/environment';
import { AuthService } from '../../../core/auth/auth.service';
import { VerifyEmailPage } from './verify-email-page';

const api = `${environment.apiBaseUrl}/auth`;

describe('VerifyEmailPage', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
  });

  afterEach(() => TestBed.inject(HttpTestingController).verify());

  function setup(email?: string) {
    const fixture = TestBed.createComponent(VerifyEmailPage);
    if (email) {
      fixture.componentRef.setInput('email', email);
    }
    fixture.detectChanges();
    return { fixture, el: fixture.nativeElement as HTMLElement };
  }

  it('verifies the code, signs in and goes home', () => {
    const { fixture, el } = setup('asha@example.com');
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);

    const input = el.querySelector('app-code-input input') as HTMLInputElement;
    input.value = '123456';
    input.dispatchEvent(new Event('input'));
    el.querySelector('form')!.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    const request = TestBed.inject(HttpTestingController).expectOne(`${api}/verify-email`);
    expect(request.request.body).toEqual({ email: 'asha@example.com', code: '123456' });
    request.flush({
      accessToken: 'token',
      expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
      user: { id: '1', fullName: 'Asha Patel', email: 'asha@example.com', roles: ['Seller'], companyName: null },
    });

    expect(TestBed.inject(AuthService).isLoggedIn()).toBe(true);
    expect(navigate).toHaveBeenCalledWith('/');
  });

  it('starts with the resend button counting down', () => {
    const { el } = setup('asha@example.com');
    const resend = el.querySelector('app-resend-code button') as HTMLButtonElement;
    expect(resend.disabled).toBe(true);
    expect(resend.textContent).toContain('Resend code in 60s');
  });

  it('explains what to do when the email is missing', () => {
    const { el } = setup();
    expect(el.querySelector('form')).toBeNull();
    expect(el.querySelector('app-alert-banner')?.textContent).toContain('missing your email address');
  });
});
