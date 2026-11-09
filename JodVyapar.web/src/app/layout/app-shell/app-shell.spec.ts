import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { UserRole } from '../../core/auth/auth.models';
import { storeTestSession } from '../../testing/session';
import { AppShell } from './app-shell';

describe('AppShell', () => {
  function setup(roles: UserRole[]) {
    localStorage.clear();
    storeTestSession(roles);
    TestBed.configureTestingModule({ providers: [provideRouter([]), provideHttpClient()] });
    const fixture = TestBed.createComponent(AppShell);
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    /** Link texts without the icon ligature. */
    const labels = (selector: string) =>
      Array.from(el.querySelectorAll(selector)).map((a) =>
        Array.from(a.childNodes)
          .filter((n) => n.nodeName !== 'MAT-ICON')
          .map((n) => n.textContent)
          .join('')
          .trim(),
      );
    return { el, labels };
  }

  it('has the four main items in both the top bar and the bottom tab bar', () => {
    const { labels } = setup(['Buyer']);
    expect(labels('.top-nav a')).toEqual(['Home', 'Search', 'Messages', 'Profile']);
    expect(labels('.tab-bar a')).toEqual(['Home', 'Search', 'Messages', 'Profile']);
  });

  it('adds Admin only for users who hold the Admin role', () => {
    expect(setup(['Buyer', 'Seller']).labels('.tab-bar a')).not.toContain('Admin');
    TestBed.resetTestingModule();
    const admin = setup(['Admin']);
    expect(admin.labels('.tab-bar a')).toContain('Admin');
    expect(admin.labels('.top-nav a')).toContain('Admin');
  });

  it('shows the mode switcher only for Buyer + Seller users', () => {
    expect(setup(['Seller']).el.querySelector('.top-mode mat-button-toggle-group')).toBeNull();
    TestBed.resetTestingModule();
    expect(setup(['Buyer', 'Seller']).el.querySelector('.top-mode mat-button-toggle-group')).not.toBeNull();
  });
});
