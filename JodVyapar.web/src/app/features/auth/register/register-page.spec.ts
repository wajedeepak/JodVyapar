import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { FormGroup } from '@angular/forms';
import { Router, provideRouter } from '@angular/router';
import { vi } from 'vitest';

import { environment } from '../../../../environments/environment';
import { RegisterPage } from './register-page';

const api = environment.apiBaseUrl;

describe('RegisterPage roles', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
  });

  afterEach(() => TestBed.inject(HttpTestingController).verify());

  function setup() {
    const fixture = TestBed.createComponent(RegisterPage);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`${api}/reference/states`).flush([{ id: 's1', name: 'Maharashtra', code: 'MH', isUnionTerritory: false }]);
    fixture.detectChanges();
    vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    const el = fixture.nativeElement as HTMLElement;
    const form = (fixture.componentInstance as unknown as { form: FormGroup }).form;
    const cards = () => Array.from(el.querySelectorAll('app-choice-cards [role="checkbox"]')) as HTMLElement[];
    const submit = () => {
      el.querySelector('form')!.dispatchEvent(new Event('submit'));
      fixture.detectChanges();
    };

    /** Everything valid except the roles, which each test chooses. */
    const fillDetails = () => {
      form.patchValue({
        fullName: 'Asha Patel',
        companyName: 'Patel Textiles',
        email: 'asha@example.com',
        mobile: '9876543210',
        password: 'Passw0rd1',
        consentAccepted: true,
        stateId: 's1',
      });
      http.expectOne((r) => r.url === `${api}/reference/cities`).flush([{ id: 'c1', stateId: 's1', name: 'Pune' }]);
      form.patchValue({ cityId: 'c1' });
      fixture.detectChanges();
    };

    return { fixture, el, http, cards, submit, fillDetails };
  }

  it('offers "I want to buy" and "I want to sell", with neither preselected', () => {
    const { cards } = setup();
    expect(cards().map((c) => c.textContent)).toEqual([
      expect.stringContaining('I want to buy'),
      expect.stringContaining('I want to sell'),
    ]);
    expect(cards().map((c) => c.getAttribute('aria-checked'))).toEqual(['false', 'false']);
  });

  it('requires at least one role', () => {
    const { el, fillDetails, submit } = setup();
    fillDetails();

    submit();

    expect(el.querySelector('#roles-error')?.textContent).toContain('buy, sell or both');
    // No register request: verify() in afterEach fails if one was sent.
  });

  it.each([
    { pick: [0], expected: ['Buyer'] },
    { pick: [1], expected: ['Seller'] },
    { pick: [1, 0], expected: ['Buyer', 'Seller'] },
  ])('sends roles $expected', ({ pick, expected }) => {
    const { fixture, http, cards, fillDetails, submit } = setup();
    fillDetails();
    pick.forEach((i) => cards()[i].click());
    fixture.detectChanges();

    submit();

    const request = http.expectOne(`${api}/auth/register`);
    expect(request.request.body.roles).toEqual(expected);
    expect(request.request.body.role).toBeUndefined();
    request.flush({ requiresEmailVerification: true, email: 'asha@example.com' });
  });
});
