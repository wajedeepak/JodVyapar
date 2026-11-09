import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, TestRequest, provideHttpClientTesting } from '@angular/common/http/testing';
import { Type } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormControl, FormGroup } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { Router, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { environment } from '../../../environments/environment';
import { formatMoneyRange, formatQuantity, requirementFacts } from '../../core/listings/listing-format';
import { amountValidator, notBelow, notPastDate } from '../../core/listings/listing-validators';
import { PagedResult, Product, Requirement } from '../../core/listings/listing.models';
import { PublicProductsSection } from '../profile/public-products-section';
import { PublicRequirementsSection } from '../profile/public-requirements-section';
import { MyRequirementsPage } from '../requirements/my-requirements-page';
import { RequirementFormPage } from '../requirements/requirement-form-page';
import { ImagePicker, PickerImage, normalizeKeywords } from '../../shared/ui';
import { storeTestSession } from '../../testing/session';
import { MyProductsPage } from './my-products-page';
import { ProductFormPage } from './product-form-page';

const api = environment.apiBaseUrl;

const product: Product = {
  id: 'p1',
  title: 'Cotton yarn',
  description: null,
  parentCategoryId: 'cat',
  parentCategoryName: 'Textiles',
  categoryId: 'sub',
  categoryName: 'Yarn',
  priceMin: 100,
  priceMax: 150,
  unitId: 'kg',
  unitName: 'Kilogram',
  unitSymbol: 'kg',
  minOrderQty: 50,
  stateId: 's1',
  stateName: 'Maharashtra',
  cityId: 'c1',
  cityName: 'Pune',
  keywords: [],
  status: 'Active',
  images: [],
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: '2026-01-01T00:00:00Z',
};

const requirement: Requirement = {
  id: 'r1',
  title: 'Need yarn',
  description: null,
  parentCategoryId: 'cat',
  parentCategoryName: 'Textiles',
  categoryId: 'sub',
  categoryName: 'Yarn',
  budgetMin: null,
  budgetMax: null,
  quantity: 500,
  unitId: 'kg',
  unitName: 'Kilogram',
  unitSymbol: 'kg',
  stateId: 's1',
  stateName: 'Maharashtra',
  cityId: 'c1',
  cityName: 'Pune',
  keywords: [],
  neededBy: null,
  status: 'Active',
  expiresAt: '2027-01-09T00:00:00Z',
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: '2026-01-01T00:00:00Z',
};

function pageOf<T>(items: T[], totalCount = items.length): PagedResult<T> {
  return { items, page: 1, pageSize: 12, totalCount };
}

function configure() {
  localStorage.clear();
  storeTestSession(['Buyer', 'Seller']);
  TestBed.configureTestingModule({
    providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
  });
}

function http(): HttpTestingController {
  return TestBed.inject(HttpTestingController);
}

function urlIs(url: string) {
  return (request: { url: string }) => request.url === url;
}

function buttonNamed(el: HTMLElement, name: string): HTMLButtonElement {
  const button = Array.from(el.querySelectorAll('button')).find((b) => b.textContent?.trim() === name);
  if (!button) {
    throw new Error(`No button named ${name}`);
  }
  return button;
}

function answerConfirm(answer: boolean) {
  return vi.spyOn(TestBed.inject(MatDialog), 'open').mockReturnValue({ afterClosed: () => of(answer) } as never);
}

describe('listing helpers', () => {
  it('formats money ranges and quantities', () => {
    expect(formatMoneyRange(100, 150)).toBe('₹100 – ₹150');
    expect(formatMoneyRange(100, null)).toBe('₹100');
    expect(formatMoneyRange(null, null)).toBeNull();
    expect(formatQuantity(1500.5, 'kg')).toBe('1,500.5 kg');
  });

  it('leaves optional facts out of a requirement', () => {
    expect(requirementFacts(requirement).map((f) => f.text)).toEqual(['Needs 500 kg', 'Pune, Maharashtra']);
  });

  it('accepts amounts above zero with up to 2 decimals', () => {
    const check = (value: string) => amountValidator(new FormControl(value, { nonNullable: true }));
    expect(check('')).toBeNull();
    expect(check('10.25')).toBeNull();
    expect(check('0')).toEqual({ amount: true });
    expect(check('1.234')).toEqual({ amount: true });
    expect(check('abc')).toEqual({ amount: true });
  });

  it('rejects a maximum below its minimum', () => {
    const form = new FormGroup({
      min: new FormControl('100', { nonNullable: true }),
      max: new FormControl('50', { nonNullable: true, validators: notBelow('min') }),
    });
    form.controls.max.updateValueAndValidity();
    expect(form.controls.max.errors).toEqual({ range: true });
    form.controls.max.setValue('100');
    expect(form.controls.max.errors).toBeNull();
  });

  it('rejects a past date unless it is the saved one', () => {
    const validator = notPastDate(() => '2020-01-01');
    expect(validator(new FormControl('2020-01-01'))).toBeNull();
    expect(validator(new FormControl('2020-01-02'))).toEqual({ pastDate: true });
    expect(validator(new FormControl(''))).toBeNull();
  });

  it('normalizes keywords the same way as the API', () => {
    expect(normalizeKeywords('  Cotton,  COMBED   yarn ,,')).toEqual(['cotton', 'combed yarn']);
  });
});

describe('ImagePicker', () => {
  beforeEach(() => {
    configure();
    URL.createObjectURL = vi.fn(() => 'blob:preview');
    URL.revokeObjectURL = vi.fn();
  });

  function setup(existing: PickerImage[] = []) {
    const fixture = TestBed.createComponent(ImagePicker);
    fixture.componentRef.setInput('images', existing);
    fixture.detectChanges();
    const choose = (...files: File[]) => {
      const input = fixture.nativeElement.querySelector('input[type=file]') as HTMLInputElement;
      Object.defineProperty(input, 'files', { value: files, configurable: true });
      input.dispatchEvent(new Event('change'));
      fixture.detectChanges();
    };
    return { fixture, el: fixture.nativeElement as HTMLElement, choose };
  }

  const png = (name = 'a.png', size = 10) => new File([new Uint8Array(size)], name, { type: 'image/png' });

  it('adds valid images as previews', () => {
    const { fixture, el, choose } = setup();
    choose(png());
    expect(fixture.componentInstance.images().length).toBe(1);
    expect(el.querySelectorAll('.preview').length).toBe(1);
  });

  it('rejects other file types and files over 2 MB', () => {
    const { fixture, el, choose } = setup();

    choose(new File(['x'], 'a.pdf', { type: 'application/pdf' }));
    expect(el.textContent).toContain('Please choose JPG, PNG or WebP images.');

    choose(png('big.png', 2 * 1024 * 1024 + 1));
    expect(el.textContent).toContain('Each image must be 2 MB or smaller.');
    expect(fixture.componentInstance.images()).toEqual([]);
  });

  it('stops at 3 images, and frees the button again after a removal', () => {
    const { fixture, el, choose } = setup();
    choose(png('1.png'), png('2.png'), png('3.png'), png('4.png'));

    expect(fixture.componentInstance.images().length).toBe(3);
    expect(el.textContent).toContain('You can add up to 3 images.');
    expect(buttonNamed(el, 'Choose images').disabled).toBe(true);

    (el.querySelector('.remove') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(fixture.componentInstance.images().length).toBe(2);
    expect(buttonNamed(el, 'Choose images').disabled).toBe(false);
  });
});

describe('MyProductsPage', () => {
  beforeEach(configure);
  afterEach(() => http().verify());

  const mine = urlIs(`${api}/products/mine`);

  function setup(): { fixture: ComponentFixture<MyProductsPage>; el: HTMLElement } {
    const fixture = TestBed.createComponent(MyProductsPage);
    fixture.detectChanges();
    return { fixture, el: fixture.nativeElement as HTMLElement };
  }

  function loaded(items: Product[] = [product]) {
    const page = setup();
    http().expectOne(mine).flush(pageOf(items));
    page.fixture.detectChanges();
    return page;
  }

  it('shows a loading state, then the product with its price, MOQ, location and status', () => {
    const { fixture, el } = setup();
    expect(el.querySelector('[aria-busy="true"]')).not.toBeNull();

    http().expectOne(mine).flush(pageOf([product]));
    fixture.detectChanges();

    expect(el.textContent).toContain('Cotton yarn');
    expect(el.textContent).toContain('Textiles › Yarn');
    expect(el.textContent).toContain('₹100 – ₹150 / kg');
    expect(el.textContent).toContain('Minimum order 50 kg');
    expect(el.textContent).toContain('Pune, Maharashtra');
    expect(el.querySelector('app-status-badge')?.textContent).toContain('Active');
  });

  it('shows the empty state with a way to add a product', () => {
    const { el } = loaded([]);
    expect(el.textContent).toContain('No products yet');
    expect(el.querySelector('a[href="/products/new"]')).not.toBeNull();
  });

  it('shows an error with a working Retry', () => {
    const { fixture, el } = setup();
    http().expectOne(mine).flush({ title: 'Nope' }, { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();
    expect(el.textContent).toContain("Couldn't load your products");

    buttonNamed(el, 'Retry').click();
    http().expectOne(mine).flush(pageOf([product]));
    fixture.detectChanges();

    expect(el.textContent).toContain('Cotton yarn');
  });

  it('pauses a product and updates its badge', () => {
    const { fixture, el } = loaded();

    buttonNamed(el, 'Pause').click();
    http().expectOne(`${api}/products/p1/pause`).flush({ ...product, status: 'Paused' });
    fixture.detectChanges();

    expect(el.querySelector('app-status-badge')?.textContent).toContain('Paused');
    expect(buttonNamed(el, 'Activate')).toBeTruthy();
  });

  it('deletes only after confirmation, then reloads', () => {
    const { fixture, el } = loaded();

    answerConfirm(false);
    buttonNamed(el, 'Delete').click();
    http().expectNone(`${api}/products/p1`);

    answerConfirm(true);
    buttonNamed(el, 'Delete').click();
    const request = http().expectOne(`${api}/products/p1`);
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
    http().expectOne(mine).flush(pageOf([]));
    fixture.detectChanges();

    expect(el.textContent).toContain('No products yet');
  });

  it('pages through results', () => {
    const { fixture, el } = setup();
    http().expectOne(mine).flush(pageOf([product], 13));
    fixture.detectChanges();
    expect(el.textContent).toContain('Page 1 of 2');

    buttonNamed(el, 'Next').click();
    const request: TestRequest = http().expectOne(mine);
    expect(request.request.params.get('page')).toBe('2');
    request.flush({ ...pageOf([product], 13), page: 2 });
    fixture.detectChanges();

    expect(el.textContent).toContain('Page 2 of 2');
  });
});

/** Answers the reference lookups every listing form makes. */
function flushLookups(fixture: ComponentFixture<unknown>) {
  fixture.detectChanges();
  http().expectOne(`${api}/reference/states`).flush([{ id: 's1', name: 'Maharashtra', code: 'MH', isUnionTerritory: false }]);
  http().expectOne(urlIs(`${api}/reference/cities`)).flush([{ id: 'c1', stateId: 's1', name: 'Pune' }]);
  http().expectOne(`${api}/reference/categories`).flush([
    { id: 'cat', name: 'Textiles', slug: 'textiles', children: [{ id: 'sub', name: 'Yarn', slug: 'yarn', children: [] }] },
  ]);
  http().expectOne(`${api}/reference/units`).flush([{ id: 'kg', name: 'Kilogram', symbol: 'kg' }]);
  fixture.detectChanges();
}

const myProfile = { id: 'user-1', company: { stateId: 's1', cityId: 'c1' } };

function formSetup<T>(component: Type<T>, id?: string) {
  const fixture = TestBed.createComponent(component);
  if (id) {
    fixture.componentRef.setInput('id', id);
  }
  fixture.detectChanges();
  const el = fixture.nativeElement as HTMLElement;
  const page = fixture.componentInstance as T & { confirmLeave(): boolean | Promise<boolean> };
  const internals = page as unknown as { form: FormGroup; images?: { set(images: PickerImage[]): void } };
  const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
  const submit = () => {
    el.querySelector('form')!.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  };
  return { fixture, el, page, form: internals.form, images: internals.images, navigate, submit };
}

describe('ProductFormPage', () => {
  beforeEach(configure);
  afterEach(() => http().verify());

  const valid = {
    title: '  Cotton yarn ',
    parentCategoryId: 'cat',
    categoryId: 'sub',
    priceMin: '100',
    priceMax: '150.50',
    unitId: 'kg',
    minOrderQty: '50',
  };

  function newForm() {
    const page = formSetup(ProductFormPage);
    http().expectOne(`${api}/profile/me`).flush(myProfile);
    flushLookups(page.fixture);
    return page;
  }

  it('prefills the location from the profile and starts clean', async () => {
    const { form, page } = newForm();
    expect(form.getRawValue()).toMatchObject({ stateId: 's1', cityId: 'c1' });
    expect(await page.confirmLeave()).toBe(true);
  });

  it('validates inline without calling the API', () => {
    const { fixture, el, form, submit } = newForm();
    form.patchValue({ ...valid, priceMin: '200', priceMax: '100' });
    fixture.detectChanges();

    submit();

    expect(el.textContent).toContain("Maximum price can't be less than the minimum price.");
  });

  it('re-checks the maximum when the minimum changes', () => {
    const { form } = newForm();
    form.patchValue({ priceMin: '100', priceMax: '150' });
    expect(form.controls['priceMax'].valid).toBe(true);

    form.controls['priceMin'].setValue('200');

    expect(form.controls['priceMax'].errors).toEqual({ range: true });
  });

  it('creates the product, uploads chosen images one by one, then goes to the list', () => {
    const { form, images, navigate, submit } = newForm();
    form.patchValue(valid);
    const file = new File(['x'], 'a.png', { type: 'image/png' });
    images!.set([{ key: 'k', url: 'blob:x', file }]);

    submit();

    const create = http().expectOne(`${api}/products`);
    expect(create.request.body).toEqual({
      title: 'Cotton yarn',
      description: null,
      categoryId: 'sub',
      priceMin: 100,
      priceMax: 150.5,
      unitId: 'kg',
      minOrderQty: 50,
      stateId: 's1',
      cityId: 'c1',
      keywords: [],
    });
    create.flush(product);
    expect(navigate).not.toHaveBeenCalled();

    const upload = http().expectOne(`${api}/products/p1/images`);
    expect((upload.request.body as FormData).get('file')).toBeInstanceOf(File);
    upload.flush(product);

    expect(navigate).toHaveBeenCalledWith(['/my/products']);
  });

  it('still finishes when an image fails, because the product is saved', () => {
    const { form, images, navigate, submit } = newForm();
    form.patchValue(valid);
    images!.set([{ key: 'k', url: 'blob:x', file: new File(['x'], 'a.png', { type: 'image/png' }) }]);

    submit();
    http().expectOne(`${api}/products`).flush(product);
    http().expectOne(`${api}/products/p1/images`).flush({ errors: { file: ['Bad image.'] } }, { status: 400, statusText: 'Bad Request' });

    expect(navigate).toHaveBeenCalledWith(['/my/products']);
  });

  it('shows server errors on the matching fields and stays on the page', () => {
    const { fixture, el, form, navigate, submit } = newForm();
    form.patchValue(valid);

    submit();
    http().expectOne(`${api}/products`).flush({ errors: { minOrderQty: ['Too small.'] } }, { status: 400, statusText: 'Bad Request' });
    fixture.detectChanges();

    expect(el.textContent).toContain('Too small.');
    expect(navigate).not.toHaveBeenCalled();
  });

  it('opens More details when an optional field has a server error', () => {
    const { fixture, form, page, submit } = newForm();
    form.patchValue(valid);

    submit();
    http().expectOne(`${api}/products`).flush({ errors: { description: ['Too long.'] } }, { status: 400, statusText: 'Bad Request' });
    fixture.detectChanges();

    expect(page.moreOpen()).toBe(true);
  });

  it('asks before leaving with chosen images', async () => {
    const { images, page } = newForm();
    images!.set([{ key: 'k', url: 'blob:x', file: new File(['x'], 'a.png') }]);
    const open = answerConfirm(false);

    expect(await page.confirmLeave()).toBe(false);
    expect(open).toHaveBeenCalled();
  });

  it('loads an existing product, and deletes a removed image before uploading on save', () => {
    const saved = { ...product, images: [{ id: 'i1', url: '/api/v1/media/product-images/i1', sortOrder: 0 }] };
    const { images, form, navigate, submit, fixture } = formSetup(ProductFormPage, 'p1');
    http().expectOne(`${api}/products/p1`).flush(saved);
    flushLookups(fixture);
    expect(form.getRawValue()).toMatchObject({ title: 'Cotton yarn', priceMin: '100', priceMax: '150', categoryId: 'sub' });

    images!.set([{ key: 'k', url: 'blob:x', file: new File(['x'], 'b.png', { type: 'image/png' }) }]);
    submit();

    http().expectOne(`${api}/products/p1`).flush(saved);
    const removal = http().expectOne(`${api}/products/p1/images/i1`);
    expect(removal.request.method).toBe('DELETE');
    http().expectNone(`${api}/products/p1/images`);
    removal.flush(saved);
    http().expectOne(`${api}/products/p1/images`).flush(saved);

    expect(navigate).toHaveBeenCalledWith(['/my/products']);
  });

  it("shows not-found for a product that isn't the seller's", () => {
    const { fixture, el } = formSetup(ProductFormPage, 'other');
    http().expectOne(`${api}/products/other`).flush({ title: 'Product not found.' }, { status: 404, statusText: 'Not Found' });
    fixture.detectChanges();

    expect(el.querySelector('h1')?.textContent).toContain('Product not found');
    expect(el.querySelector('a[href="/my/products"]')).not.toBeNull();
  });
});

describe('MyRequirementsPage', () => {
  beforeEach(configure);
  afterEach(() => http().verify());

  const mine = urlIs(`${api}/requirements/mine`);

  function loaded(items: Requirement[]) {
    const fixture = TestBed.createComponent(MyRequirementsPage);
    fixture.detectChanges();
    http().expectOne(mine).flush(pageOf(items));
    fixture.detectChanges();
    return { fixture, el: fixture.nativeElement as HTMLElement };
  }

  it('shows the expiry date and renews', () => {
    const { fixture, el } = loaded([requirement]);
    expect(el.textContent).toContain('Expires on 9 Jan 2027');

    buttonNamed(el, 'Renew').click();
    http().expectOne(`${api}/requirements/r1/renew`).flush({ ...requirement, expiresAt: '2027-03-01T00:00:00Z' });
    fixture.detectChanges();

    expect(el.textContent).toContain('Expires on 1 Mar 2027');
  });

  it('offers Renew but not Pause on an expired requirement', () => {
    const { el } = loaded([{ ...requirement, status: 'Expired' }]);
    expect(el.textContent).toContain('Expired on');
    expect(buttonNamed(el, 'Renew')).toBeTruthy();
    expect(() => buttonNamed(el, 'Pause')).toThrow();
  });

  it('marks as fulfilled after confirmation, leaving only Delete', () => {
    const { fixture, el } = loaded([requirement]);
    answerConfirm(true);

    buttonNamed(el, 'Mark fulfilled').click();
    http().expectOne(`${api}/requirements/r1/fulfill`).flush({ ...requirement, status: 'Fulfilled' });
    fixture.detectChanges();

    expect(el.querySelector('app-status-badge')?.textContent).toContain('Fulfilled');
    expect(() => buttonNamed(el, 'Renew')).toThrow();
    expect(buttonNamed(el, 'Delete')).toBeTruthy();
  });

  it('shows the reason when the API refuses an action', () => {
    const { el } = loaded([requirement]);

    buttonNamed(el, 'Renew').click();
    http()
      .expectOne(`${api}/requirements/r1/renew`)
      .flush({ errors: { status: ["A fulfilled requirement can't be renewed."] } }, { status: 400, statusText: 'Bad Request' });

    expect(document.body.textContent).toContain("A fulfilled requirement can't be renewed.");
  });
});

describe('RequirementFormPage', () => {
  beforeEach(configure);
  afterEach(() => http().verify());

  it('posts the requirement with blank optional fields as null', () => {
    const { fixture, form, navigate, submit } = formSetup(RequirementFormPage);
    http().expectOne(`${api}/profile/me`).flush(myProfile);
    flushLookups(fixture);
    form.patchValue({ title: 'Need yarn', parentCategoryId: 'cat', categoryId: 'sub', quantity: '500', unitId: 'kg' });

    submit();

    const request = http().expectOne(`${api}/requirements`);
    expect(request.request.body).toEqual({
      title: 'Need yarn',
      description: null,
      categoryId: 'sub',
      budgetMin: null,
      budgetMax: null,
      quantity: 500,
      unitId: 'kg',
      stateId: 's1',
      cityId: 'c1',
      neededBy: null,
      keywords: [],
    });
    request.flush(requirement);

    expect(navigate).toHaveBeenCalledWith(['/my/requirements']);
  });

  it('opens More details when an optional field is invalid', () => {
    const { fixture, form, page, submit } = formSetup(RequirementFormPage);
    http().expectOne(`${api}/profile/me`).flush(myProfile);
    flushLookups(fixture);
    form.patchValue({ title: 'Need yarn', parentCategoryId: 'cat', categoryId: 'sub', quantity: '500', unitId: 'kg', neededBy: '2020-01-01' });

    submit();

    expect(page.moreOpen()).toBe(true);
    http().expectNone(`${api}/requirements`);
  });

  it('keeps a past needed-by date that was already saved', () => {
    const { fixture, form } = formSetup(RequirementFormPage, 'r1');
    http().expectOne(`${api}/requirements/r1`).flush({ ...requirement, neededBy: '2020-01-01' });
    flushLookups(fixture);

    expect(form.controls['neededBy'].valid).toBe(true);
  });
});

describe('public profile sections', () => {
  beforeEach(configure);
  afterEach(() => http().verify());

  function section<T extends PublicProductsSection | PublicRequirementsSection>(component: Type<T>) {
    const fixture = TestBed.createComponent(component);
    fixture.componentRef.setInput('userId', 'u1');
    fixture.componentRef.setInput('companyName', 'Kumar Traders');
    fixture.detectChanges();
    return { fixture, el: fixture.nativeElement as HTMLElement };
  }

  it('lists products without any owner actions', () => {
    const { fixture, el } = section(PublicProductsSection);
    http().expectOne(urlIs(`${api}/profile/u1/products`)).flush(pageOf([{ ...product, description: 'Soft.' }]));
    fixture.detectChanges();

    expect(el.textContent).toContain('Cotton yarn');
    expect(el.textContent).toContain('Soft.');
    expect(el.querySelector('button')).toBeNull();
  });

  it('shows an empty state', () => {
    const { fixture, el } = section(PublicRequirementsSection);
    http().expectOne(urlIs(`${api}/profile/u1/requirements`)).flush(pageOf([]));
    fixture.detectChanges();

    expect(el.textContent).toContain('Kumar Traders has no open requirements right now.');
  });

  it('shows an error with Retry', () => {
    const { fixture, el } = section(PublicProductsSection);
    http().expectOne(urlIs(`${api}/profile/u1/products`)).flush({}, { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();
    expect(el.textContent).toContain("Couldn't load products");

    buttonNamed(el, 'Retry').click();
    http().expectOne(urlIs(`${api}/profile/u1/products`)).flush(pageOf([]));
  });
});
