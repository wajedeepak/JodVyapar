import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FormControl, ReactiveFormsModule } from '@angular/forms';

import { ChoiceCards, ChoiceOption } from './choice-cards';

@Component({
  imports: [ReactiveFormsModule, ChoiceCards],
  template: `<app-choice-cards [formControl]="control" [options]="options" aria-label="Account type" />`,
})
class Host {
  readonly control = new FormControl('Buyer');
  readonly options: ChoiceOption[] = [
    { value: 'Buyer', title: 'Buying' },
    { value: 'Seller', title: 'Selling' },
  ];
}

describe('ChoiceCards', () => {
  function setup() {
    const fixture = TestBed.createComponent(Host);
    fixture.detectChanges();
    const cards = Array.from(fixture.nativeElement.querySelectorAll('[role="radio"]')) as HTMLElement[];
    return { fixture, host: fixture.componentInstance, cards };
  }

  it('is a labelled radiogroup reflecting the control value', () => {
    const { fixture, cards } = setup();
    const group = fixture.nativeElement.querySelector('app-choice-cards') as HTMLElement;
    expect(group.getAttribute('role')).toBe('radiogroup');
    expect(group.getAttribute('aria-label')).toBe('Account type');
    expect(cards.map((c) => c.getAttribute('aria-checked'))).toEqual(['true', 'false']);
    // Roving tabindex: only the selected card is tabbable.
    expect(cards.map((c) => c.tabIndex)).toEqual([0, -1]);
  });

  it('selects on click and updates the form control', () => {
    const { fixture, host, cards } = setup();
    cards[1].click();
    fixture.detectChanges();
    expect(host.control.value).toBe('Seller');
    expect(cards[1].getAttribute('aria-checked')).toBe('true');
  });

  it('moves and selects with arrow keys, wrapping around', () => {
    const { fixture, host, cards } = setup();
    cards[0].dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight', bubbles: true }));
    fixture.detectChanges();
    expect(host.control.value).toBe('Seller');
    expect(document.activeElement).toBe(cards[1]);

    cards[1].dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight', bubbles: true }));
    fixture.detectChanges();
    expect(host.control.value).toBe('Buyer');
  });

  it('marks the control touched on blur', () => {
    const { host, cards } = setup();
    cards[0].dispatchEvent(new FocusEvent('blur'));
    expect(host.control.touched).toBe(true);
  });
});

@Component({
  imports: [ReactiveFormsModule, ChoiceCards],
  template: `<app-choice-cards [formControl]="control" [options]="options" [multiple]="true" aria-label="I want to" />`,
})
class MultiHost {
  readonly control = new FormControl<string[]>([]);
  readonly options: ChoiceOption[] = [
    { value: 'Buyer', title: 'Buy' },
    { value: 'Seller', title: 'Sell' },
  ];
}

describe('ChoiceCards (multiple)', () => {
  function setup() {
    const fixture = TestBed.createComponent(MultiHost);
    fixture.detectChanges();
    const cards = Array.from(fixture.nativeElement.querySelectorAll('[role="checkbox"]')) as HTMLElement[];
    return { fixture, host: fixture.componentInstance, cards };
  }

  it('is a labelled group of checkboxes, each a tab stop', () => {
    const { fixture, cards } = setup();
    const group = fixture.nativeElement.querySelector('app-choice-cards') as HTMLElement;
    expect(group.getAttribute('role')).toBe('group');
    expect(cards.length).toBe(2);
    expect(cards.map((c) => c.tabIndex)).toEqual([0, 0]);
    expect(cards.map((c) => c.getAttribute('aria-checked'))).toEqual(['false', 'false']);
  });

  it('toggles each card and keeps the value in option order', () => {
    const { fixture, host, cards } = setup();
    cards[1].click();
    cards[0].click();
    fixture.detectChanges();
    expect(host.control.value).toEqual(['Buyer', 'Seller']);
    expect(cards.map((c) => c.getAttribute('aria-checked'))).toEqual(['true', 'true']);

    cards[0].dispatchEvent(new KeyboardEvent('keydown', { key: ' ', bubbles: true }));
    fixture.detectChanges();
    expect(host.control.value).toEqual(['Seller']);
  });

  it('reflects a value written by the form', () => {
    const { fixture, host, cards } = setup();
    host.control.setValue(['Seller']);
    fixture.detectChanges();
    expect(cards.map((c) => c.getAttribute('aria-checked'))).toEqual(['false', 'true']);
  });
});
