import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { FormGroup } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { Router, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { MyProfile, PublicProfile } from '../../core/profile/profile.models';
import { storeTestSession, TEST_USER_ID } from '../../testing/session';
import { EditProfilePage } from './edit-profile-page';
import { PublicProfilePage } from './public-profile-page';

const api = environment.apiBaseUrl;

const myProfile: MyProfile = {
  id: TEST_USER_ID,
  fullName: 'Asha Patel',
  email: 'asha@example.com',
  roles: ['Seller'],
  isEmailVerified: true,
  memberSince: '2026-01-10T00:00:00Z',
  company: {
    companyName: 'Patel Textiles',
    stateId: 's1',
    stateName: 'Maharashtra',
    cityId: 'c1',
    cityName: 'Pune',
    mobile: '9876543210',
    gstin: null,
    about: null,
    isMobileVerified: false,
    isGstinVerified: false,
  },
};

function configure() {
  localStorage.clear();
  storeTestSession(['Seller']);
  TestBed.configureTestingModule({
    providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
  });
}

describe('EditProfilePage', () => {
  beforeEach(configure);
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  function setup() {
    const fixture = TestBed.createComponent(EditProfilePage);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne(`${api}/profile/me`).flush(myProfile);
    fixture.detectChanges();
    http.expectOne(`${api}/reference/states`).flush([{ id: 's1', name: 'Maharashtra', code: 'MH', isUnionTerritory: false }]);
    http.expectOne((r) => r.url === `${api}/reference/cities`).flush([{ id: 'c1', stateId: 's1', name: 'Pune' }]);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    const page = fixture.componentInstance;
    const form = (page as unknown as { form: FormGroup }).form;
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    const submit = () => {
      el.querySelector('form')!.dispatchEvent(new Event('submit'));
      fixture.detectChanges();
    };
    return { fixture, el, http, page, form, navigate, submit };
  }

  it('prefills the form, keeping the saved city', () => {
    const { form } = setup();
    expect(form.getRawValue()).toEqual({
      fullName: 'Asha Patel',
      mobile: '9876543210',
      companyName: 'Patel Textiles',
      stateId: 's1',
      cityId: 'c1',
      gstin: '',
      about: '',
    });
    expect(form.dirty).toBe(false);
  });

  it('validates mobile and GSTIN inline without calling the API', () => {
    const { fixture, el, form, submit } = setup();
    form.patchValue({ mobile: '12345', gstin: 'NOT-A-GSTIN' });
    fixture.detectChanges();

    submit();

    expect(el.textContent).toContain('Please enter a valid 10-digit Indian mobile number.');
    expect(el.textContent).toContain('Please enter a valid 15-character GSTIN');
  });

  it('saves trimmed values with an upper-case GSTIN, then returns to the profile', () => {
    const { http, form, navigate, submit } = setup();
    form.patchValue({ fullName: '  Asha P. Patel ', gstin: ' 27abcde1234f1z5 ', about: '  Cotton fabric.  ' });
    form.markAsDirty();

    submit();

    const request = http.expectOne(`${api}/profile/me`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({
      fullName: 'Asha P. Patel',
      companyName: 'Patel Textiles',
      mobile: '9876543210',
      stateId: 's1',
      cityId: 'c1',
      gstin: '27ABCDE1234F1Z5',
      about: 'Cotton fabric.',
    });
    request.flush({ ...myProfile, fullName: 'Asha P. Patel' });

    expect(navigate).toHaveBeenCalledWith(['/profile']);
  });

  it('shows server validation errors on the matching fields', () => {
    const { fixture, el, http, submit } = setup();

    submit();
    http
      .expectOne(`${api}/profile/me`)
      .flush({ errors: { cityId: ['Please choose a city in the selected state.'] } }, { status: 400, statusText: 'Bad Request' });
    fixture.detectChanges();

    expect(el.textContent).toContain('Please choose a city in the selected state.');
  });

  it('asks before leaving with unsaved changes', async () => {
    const { form, page } = setup();
    expect(await page.confirmLeave()).toBe(true);

    form.patchValue({ about: 'Changed' });
    form.markAsDirty();
    const dialog = TestBed.inject(MatDialog);
    const open = vi.spyOn(dialog, 'open').mockReturnValue({ afterClosed: () => of(false) } as never);

    expect(await page.confirmLeave()).toBe(false);
    expect(open).toHaveBeenCalled();
  });
});

describe('PublicProfilePage', () => {
  beforeEach(configure);
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  const publicProfile: PublicProfile = {
    id: 'other-user',
    fullName: 'Ravi Kumar',
    companyName: 'Kumar Traders',
    cityName: 'Pune',
    stateName: 'Maharashtra',
    roles: ['Buyer'],
    about: 'We buy cotton yarn.',
    isEmailVerified: true,
    isGstinVerified: false,
    memberSince: '2026-03-01T00:00:00Z',
  };

  function setup(userId: string) {
    const fixture = TestBed.createComponent(PublicProfilePage);
    fixture.componentRef.setInput('userId', userId);
    fixture.detectChanges();
    return { fixture, el: fixture.nativeElement as HTMLElement, http: TestBed.inject(HttpTestingController) };
  }

  it('shows business details, a disabled Message button, and only the sections for held roles', () => {
    const { fixture, el, http } = setup('other-user');
    http.expectOne(`${api}/profile/other-user`).flush(publicProfile);
    fixture.detectChanges();
    http
      .expectOne((r) => r.url === `${api}/profile/other-user/requirements`)
      .flush({ items: [], page: 1, pageSize: 12, totalCount: 0 });
    fixture.detectChanges();

    expect(el.querySelector('h1')?.textContent).toContain('Ravi Kumar');
    expect(el.textContent).toContain('Kumar Traders');
    expect(el.textContent).toContain('Pune, Maharashtra');
    const message = Array.from(el.querySelectorAll('button')).find((b) => b.textContent?.includes('Message'));
    expect(message?.disabled).toBe(true);
    expect(el.textContent).toContain('Coming soon');
    expect(el.textContent).toContain('Requirements');
    expect(el.textContent).not.toContain('Products');
  });

  it('shows a friendly not-found state', () => {
    const { fixture, el, http } = setup('missing');
    http.expectOne(`${api}/profile/missing`).flush({ title: 'Profile not found.' }, { status: 404, statusText: 'Not Found' });
    fixture.detectChanges();

    expect(el.querySelector('h1')?.textContent).toContain('Profile not found');
    expect(el.querySelector('a[href="/"]')).not.toBeNull();
  });

  it('tells owners this is their public view instead of offering to message themselves', () => {
    const { fixture, el, http } = setup(TEST_USER_ID);
    http.expectOne(`${api}/profile/${TEST_USER_ID}`).flush({ ...publicProfile, id: TEST_USER_ID, roles: ['Seller'] });
    fixture.detectChanges();
    http
      .expectOne((r) => r.url === `${api}/profile/${TEST_USER_ID}/products`)
      .flush({ items: [], page: 1, pageSize: 12, totalCount: 0 });
    fixture.detectChanges();

    expect(el.textContent).toContain('This is how other businesses see your profile.');
    expect(Array.from(el.querySelectorAll('button')).some((b) => b.textContent?.includes('Message'))).toBe(false);
    expect(el.textContent).toContain('Products');
  });
});
