import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { environment } from '../../../environments/environment';
import { UserRole } from '../../core/auth/auth.models';
import { AuthService } from '../../core/auth/auth.service';
import { storeTestSession } from '../../testing/session';
import { HomePage } from './home-page';

describe('HomePage', () => {
  function setup(roles: UserRole[]) {
    localStorage.clear();
    storeTestSession(roles);
    TestBed.configureTestingModule({ providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()] });
    const fixture = TestBed.createComponent(HomePage);
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    const cards = () =>
      Array.from(el.querySelectorAll('.action-card')).map((a) => ({
        title: a.querySelector('.title')?.textContent?.trim(),
        href: a.getAttribute('href'),
      }));
    return { fixture, el, cards, auth: TestBed.inject(AuthService) };
  }

  it('greets the user by first name', () => {
    expect(setup(['Buyer']).el.querySelector('h1')?.textContent).toContain('Asha');
  });

  it('shows buyer actions in Buying mode, each linking to a page', () => {
    expect(setup(['Buyer']).cards()).toEqual([
      { title: 'Search products', href: '/search' },
      { title: 'Post a requirement', href: '/requirements/new' },
      { title: 'My requirements', href: '/my/requirements' },
    ]);
  });

  it('shows seller actions in Selling mode', () => {
    expect(setup(['Seller']).cards()).toEqual([
      { title: 'Add a product', href: '/products/new' },
      { title: 'My products', href: '/my/products' },
      { title: 'Browse buyer requirements', href: '/requirements' },
    ]);
  });

  it('follows the mode switch for Buyer + Seller users, with no add-role prompt', () => {
    const { fixture, el, cards, auth } = setup(['Buyer', 'Seller']);
    expect(cards()[0].title).toBe('Search products');
    expect(el.querySelector('#prompt-heading')).toBeNull();

    auth.setMode('Seller');
    fixture.detectChanges();

    expect(cards()[0].title).toBe('Add a product');
  });

  it('offers the missing role to single-role users', () => {
    const { el } = setup(['Buyer']);
    expect(el.querySelector('#prompt-heading')?.textContent).toContain('Selling too?');
    expect(el.querySelector('app-primary-button')?.textContent).toContain('Add Seller role');
  });

  it('shows how many listings are live for the active mode, following the mode switch', () => {
    const { fixture, el, auth } = setup(['Buyer', 'Seller']);
    const http = TestBed.inject(HttpTestingController);
    const api = environment.apiBaseUrl;

    TestBed.tick();
    const buyerCount = http.expectOne((r) => r.url === `${api}/requirements/mine`);
    expect(buyerCount.request.params.get('status')).toBe('Active');
    buyerCount.flush({ items: [], page: 1, pageSize: 1, totalCount: 4 });
    fixture.detectChanges();
    expect(el.querySelector('.count')?.textContent).toContain('4 active');

    auth.setMode('Seller');
    TestBed.tick();
    http.expectOne((r) => r.url === `${api}/products/mine`).flush({ items: [], page: 1, pageSize: 1, totalCount: 2 });
    fixture.detectChanges();
    expect(el.querySelector('.count')?.textContent).toContain('2 active');
  });

  it('hides the count when it cannot be loaded', () => {
    const { fixture, el } = setup(['Seller']);
    TestBed.tick();
    TestBed.inject(HttpTestingController)
      .expectOne((r) => r.url.endsWith('/products/mine'))
      .flush({}, { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();
    expect(el.querySelector('.count')).toBeNull();
  });

  it('points Admin-only accounts to the admin page and profile instead of mode cards', () => {
    const { el, cards } = setup(['Admin']);
    expect(cards()).toEqual([]);
    expect(el.querySelector('a[href="/admin"]')).not.toBeNull();
    expect(el.querySelector('a[href="/profile"]')).not.toBeNull();
  });
});
