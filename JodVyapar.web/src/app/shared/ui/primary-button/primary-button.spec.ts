import { TestBed } from '@angular/core/testing';

import { PrimaryButton } from './primary-button';

describe('PrimaryButton', () => {
  function setup(inputs: { loading?: boolean; disabled?: boolean } = {}) {
    const fixture = TestBed.createComponent(PrimaryButton);
    fixture.componentRef.setInput('label', 'Sign in');
    fixture.componentRef.setInput('loadingLabel', 'Signing in…');
    fixture.componentRef.setInput('type', 'submit');
    fixture.componentRef.setInput('loading', inputs.loading ?? false);
    fixture.componentRef.setInput('disabled', inputs.disabled ?? false);
    fixture.detectChanges();
    return fixture.nativeElement.querySelector('button') as HTMLButtonElement;
  }

  it('renders an enabled submit button with its label', () => {
    const button = setup();
    expect(button.type).toBe('submit');
    expect(button.disabled).toBe(false);
    expect(button.textContent).toContain('Sign in');
    expect(button.getAttribute('aria-busy')).toBeNull();
  });

  it('disables itself, shows a spinner and the loading label while loading', () => {
    const button = setup({ loading: true });
    expect(button.disabled).toBe(true);
    expect(button.getAttribute('aria-busy')).toBe('true');
    expect(button.querySelector('mat-progress-spinner')).not.toBeNull();
    expect(button.textContent).toContain('Signing in…');
  });

  it('can be disabled without loading', () => {
    const button = setup({ disabled: true });
    expect(button.disabled).toBe(true);
    expect(button.querySelector('mat-progress-spinner')).toBeNull();
  });
});
