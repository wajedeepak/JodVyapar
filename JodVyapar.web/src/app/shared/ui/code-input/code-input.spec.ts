import { TestBed } from '@angular/core/testing';
import { FormControl } from '@angular/forms';

import { CodeInput } from './code-input';

describe('CodeInput', () => {
  function setup() {
    const control = new FormControl('', { nonNullable: true });
    const fixture = TestBed.createComponent(CodeInput);
    fixture.componentRef.setInput('control', control);
    fixture.detectChanges();
    const input = (fixture.nativeElement as HTMLElement).querySelector('input') as HTMLInputElement;
    return { control, input };
  }

  function type(input: HTMLInputElement, value: string) {
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  it('is a numeric one-time-code field', () => {
    const { input } = setup();
    expect(input.getAttribute('inputmode')).toBe('numeric');
    expect(input.getAttribute('autocomplete')).toBe('one-time-code');
  });

  it('keeps only the first six digits of pasted text', () => {
    const { control, input } = setup();
    type(input, ' 12-34 56 78');
    expect(control.value).toBe('123456');
  });
});
