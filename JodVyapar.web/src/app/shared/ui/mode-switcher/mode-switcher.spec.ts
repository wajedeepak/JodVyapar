import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { AuthService } from '../../../core/auth/auth.service';
import { UserRole } from '../../../core/auth/auth.models';
import { storeTestSession } from '../../../testing/session';
import { ModeSwitcher } from './mode-switcher';

describe('ModeSwitcher', () => {
  function setup(roles: UserRole[]) {
    localStorage.clear();
    storeTestSession(roles);
    TestBed.configureTestingModule({ providers: [provideRouter([]), provideHttpClient()] });
    const fixture = TestBed.createComponent(ModeSwitcher);
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    return { fixture, el, auth: TestBed.inject(AuthService) };
  }

  it('renders nothing for single-role users', () => {
    expect(setup(['Seller']).el.querySelector('mat-button-toggle-group')).toBeNull();
    TestBed.resetTestingModule();
    expect(setup(['Buyer', 'Admin']).el.querySelector('mat-button-toggle-group')).toBeNull();
  });

  it('shows Buying selected by default and switches the mode', () => {
    const { fixture, el, auth } = setup(['Buyer', 'Seller']);
    const buttons = Array.from(el.querySelectorAll('mat-button-toggle button')) as HTMLButtonElement[];
    expect(buttons.map((b) => b.textContent?.trim())).toEqual(['shopping_bag Buying', 'storefront Selling']);
    expect(buttons[0].getAttribute('aria-checked')).toBe('true');

    buttons[1].click();
    fixture.detectChanges();

    expect(auth.activeMode()).toBe('Seller');
    expect(buttons[1].getAttribute('aria-checked')).toBe('true');
  });
});
