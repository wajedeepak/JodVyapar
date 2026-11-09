import { HttpErrorResponse } from '@angular/common/http';
import { AbstractControl, FormGroup } from '@angular/forms';

/** RFC 7807 body returned by the API for every error. */
interface ProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

const GENERIC_ERROR = 'Something went wrong. Please try again.';

const DEFAULT_MESSAGES: Record<string, string> = {
  required: 'This field is required.',
  email: 'Please enter a valid email address.',
};

/** A friendly message for a failed request that isn't tied to a form (page loads, actions). */
export function errorMessage(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) {
    return GENERIC_ERROR;
  }
  if (error.status === 0) {
    return 'Cannot reach the server. Please check your connection and try again.';
  }
  const problem = (error.error ?? {}) as ProblemDetails;
  return problem.detail ?? problem.title ?? GENERIC_ERROR;
}

/**
 * Puts API validation errors (400 ValidationProblemDetails) on the matching form controls
 * and returns any message that belongs to the whole form (or null if every error was placed).
 */
export function applyServerErrors(form: FormGroup, error: unknown): string | null {
  const problem = (error instanceof HttpErrorResponse ? (error.error ?? {}) : {}) as ProblemDetails;
  if (error instanceof HttpErrorResponse && error.status === 400 && problem.errors) {
    const unplaced: string[] = [];
    for (const [field, messages] of Object.entries(problem.errors)) {
      const control = findControl(form, field);
      if (control) {
        control.setErrors({ ...control.errors, server: messages[0] });
        control.markAsTouched();
      } else {
        unplaced.push(...messages);
      }
    }
    return unplaced.length ? unplaced.join(' ') : null;
  }

  return errorMessage(error);
}

/** First error message for a control: server message first, then the given or default messages. */
export function controlError(control: AbstractControl, messages: Record<string, string> = {}): string | null {
  const errors = control.errors;
  if (!errors) {
    return null;
  }
  if (typeof errors['server'] === 'string') {
    return errors['server'];
  }
  const key = Object.keys(errors)[0];
  return messages[key] ?? DEFAULT_MESSAGES[key] ?? 'This value is not valid.';
}

/** API field names are camelCase; match them to form controls case-insensitively. */
function findControl(form: FormGroup, field: string): AbstractControl | null {
  const name = Object.keys(form.controls).find((key) => key.toLowerCase() === field.toLowerCase());
  return name ? form.controls[name] : null;
}
