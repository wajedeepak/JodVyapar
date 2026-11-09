import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { vi } from 'vitest';

import { environment } from '../../../../environments/environment';
import { AuthSession } from '../../../core/auth/auth.models';
import { LoginPage } from './login-page';

const api = `${environment.apiBaseUrl}/auth`;

const session: AuthSession = {
  accessToken: 'token',
  expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
  user: { id: '1', fullName: 'Asha Patel', email: 'asha@example.com', roles: ['Buyer'], companyName: 'Patel Co' },
};

describe('LoginPage', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
  });

  afterEach(() => TestBed.inject(HttpTestingController).verify());

  function setup() {
    const fixture = TestBed.createComponent(LoginPage);
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    const http = TestBed.inject(HttpTestingController);
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    const navigateTo = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    const type = (selector: string, value: string) => {
      const input = el.querySelector(selector) as HTMLInputElement;
      input.value = value;
      input.dispatchEvent(new Event('input'));
    };
    const submit = () => {
      el.querySelector('form')!.dispatchEvent(new Event('submit'));
      fixture.detectChanges();
    };
    const chooseEmailCode = () => {
      (el.querySelectorAll('mat-button-toggle button')[1] as HTMLButtonElement).click();
      fixture.detectChanges();
    };
    return { fixture, el, http, navigate, navigateTo, type, submit, chooseEmailCode };
  }

  it('signs in with an emailed code', () => {
    const { fixture, el, http, navigate, type, submit, chooseEmailCode } = setup();

    chooseEmailCode();
    expect(el.querySelector('app-password-input')).toBeNull();
    type('app-text-field input', 'asha@example.com');
    submit();
    http.expectOne(`${api}/login/otp/request`).flush({ message: 'sent' });
    fixture.detectChanges();

    expect(el.textContent).toContain('asha@example.com');
    type('app-code-input input', '123456');
    submit();
    const verify = http.expectOne(`${api}/login/otp/verify`);
    expect(verify.request.body).toEqual({ email: 'asha@example.com', code: '123456' });
    verify.flush(session);

    expect(navigate).toHaveBeenCalledWith('/');
  });

  it('shows a friendly message and clears the code when it is wrong', () => {
    const { fixture, el, http, type, submit, chooseEmailCode } = setup();
    chooseEmailCode();
    type('app-text-field input', 'asha@example.com');
    submit();
    http.expectOne(`${api}/login/otp/request`).flush({ message: 'sent' });
    fixture.detectChanges();

    type('app-code-input input', '000000');
    submit();
    http
      .expectOne(`${api}/login/otp/verify`)
      .flush({ code: 'INVALID_OR_EXPIRED_CODE' }, { status: 400, statusText: 'Bad Request' });
    fixture.detectChanges();

    expect(el.querySelector('app-alert-banner')?.textContent).toContain('wrong or has expired');
    expect((el.querySelector('app-code-input input') as HTMLInputElement).value).toBe('');
  });

  it('sends an unverified user to Verify Email after a correct password', () => {
    const { http, navigateTo, type, submit } = setup();
    type('app-text-field input', 'asha@example.com');
    type('app-password-input input', 'Passw0rd1');
    submit();

    http
      .expectOne(`${api}/login`)
      .flush({ code: 'EMAIL_NOT_VERIFIED' }, { status: 403, statusText: 'Forbidden' });

    expect(navigateTo).toHaveBeenCalledWith(['/auth/verify-email'], {
      queryParams: { email: 'asha@example.com', returnUrl: null },
    });
  });
});
