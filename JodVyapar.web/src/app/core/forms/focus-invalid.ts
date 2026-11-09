/** Focusable parts of invalid controls: inputs, textareas, Material selects, checkboxes, choice cards. */
const INVALID_SELECTOR = [
  'input.ng-invalid',
  'textarea.ng-invalid',
  'mat-select.ng-invalid',
  'mat-checkbox.ng-invalid input',
  'app-choice-cards.ng-invalid [tabindex="0"]',
].join(', ');

/**
 * After a failed submit, move focus to the first invalid field so keyboard and screen-reader
 * users land on it. Returns false if there was no invalid field to focus.
 */
export function focusFirstInvalid(container: HTMLElement): boolean {
  const target = container.querySelector<HTMLElement>(INVALID_SELECTOR);
  target?.focus();
  return !!target;
}
