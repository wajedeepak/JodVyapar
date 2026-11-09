import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/** Up to 10 digits and 2 decimals: what the API's decimal(12,2) columns hold. */
const AMOUNT_PATTERN = /^\d{1,10}(\.\d{1,2})?$/;

/** Blank is fine (combine with required). Otherwise a number above zero. Error key: `amount`. */
export function amountValidator(control: AbstractControl<string>): ValidationErrors | null {
  const value = control.value.trim();
  return !value || (AMOUNT_PATTERN.test(value) && Number(value) > 0) ? null : { amount: true };
}

/**
 * This control must not be below a sibling (maximum vs minimum). Error key: `range`. The sibling's
 * changes don't re-run it automatically, so the form calls updateValueAndValidity when it changes.
 */
export function notBelow(siblingName: string): ValidatorFn {
  return (control: AbstractControl<string>) => {
    const sibling = (control.parent?.get(siblingName)?.value as string | undefined)?.trim();
    const value = control.value.trim();
    return value && sibling && Number(value) < Number(sibling) ? { range: true } : null;
  };
}

/** Today in the viewer's time zone as yyyy-MM-dd (the format of a date input). */
export function todayIso(now = new Date()): string {
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${now.getFullYear()}-${month}-${day}`;
}

/**
 * The date can't be in the past, unless it is the date already saved (so an older listing can still
 * be edited). Error key: `pastDate`.
 */
export function notPastDate(savedDate: () => string): ValidatorFn {
  return (control: AbstractControl<string>) => {
    const value = control.value;
    return value && value !== savedDate() && value < todayIso() ? { pastDate: true } : null;
  };
}

/** A form's text amount as a number for the API, or null when blank. */
export function toAmount(value: string): number | null {
  return value.trim() ? Number(value) : null;
}
