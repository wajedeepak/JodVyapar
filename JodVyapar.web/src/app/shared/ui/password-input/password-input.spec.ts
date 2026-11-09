import { TestBed } from '@angular/core/testing';
import { FormControl } from '@angular/forms';

import { PasswordInput } from './password-input';

describe('PasswordInput', () => {
  function setup(showRequirements = false) {
    const control = new FormControl('', { nonNullable: true });
    const fixture = TestBed.createComponent(PasswordInput);
    fixture.componentRef.setInput('control', control);
    fixture.componentRef.setInput('showRequirements', showRequirements);
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    return { fixture, control, el, input: el.querySelector('input') as HTMLInputElement };
  }

  it('toggles between hidden and visible text', () => {
    const { fixture, el, input } = setup();
    const toggle = el.querySelector('button') as HTMLButtonElement;
    expect(input.type).toBe('password');
    expect(toggle.getAttribute('aria-label')).toBe('Show password');

    toggle.click();
    fixture.detectChanges();
    expect(input.type).toBe('text');
    expect(toggle.getAttribute('aria-label')).toBe('Hide password');
    expect(toggle.getAttribute('aria-pressed')).toBe('true');
  });

  it('has no requirements list unless asked', () => {
    const { el } = setup();
    expect(el.querySelector('.checklist')).toBeNull();
  });

  it('ticks requirements and rates strength as the user types', async () => {
    const { fixture, control, el } = setup(true);
    const met = () => el.querySelectorAll('.checklist li.met').length;
    expect(met()).toBe(0);

    control.setValue('abc');
    fixture.detectChanges();
    await fixture.whenStable();
    expect(met()).toBe(1);
    expect(el.querySelector('.strength-label')?.textContent).toContain('Weak');

    control.setValue('Abcdefg1');
    fixture.detectChanges();
    await fixture.whenStable();
    expect(met()).toBe(4);
    expect(el.querySelector('.strength-label')?.textContent).toContain('Good');
  });
});
