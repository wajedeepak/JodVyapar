import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { UserRole } from '../../core/auth/auth.models';
import { AuthService } from '../../core/auth/auth.service';
import { storeTestSession } from '../../testing/session';
import { HomePage } from './home-page';

describe('HomePage', () => {
  function setup(roles: UserRole[]) {
    localStorage.clear();
    storeTestSession(roles);
    TestBed.configureTestingModule({ providers: [provideRouter([]), provideHttpClient()] });
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
    ]);
  });

  it('shows seller actions in Selling mode', () => {
    expect(setup(['Seller']).cards()).toEqual([
      { title: 'Add a product', href: '/products/new' },
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

  it('points Admin-only accounts to the admin page and profile instead of mode cards', () => {
    const { el, cards } = setup(['Admin']);
    expect(cards()).toEqual([]);
    expect(el.querySelector('a[href="/admin"]')).not.toBeNull();
    expect(el.querySelector('a[href="/profile"]')).not.toBeNull();
  });
});
