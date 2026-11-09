import { HttpErrorResponse } from '@angular/common/http';
import { DestroyRef, inject, signal } from '@angular/core';

import { INVALID_OR_EXPIRED_CODE } from './auth.models';

/** Matches the API's Otp:ResendCooldownSeconds; a resend sooner than this is ignored by the API. */
export const RESEND_COOLDOWN_SECONDS = 60;

/**
 * Countdown until "Resend code" is allowed again. Create it in a field initializer (it needs an
 * injection context) and call `start()` whenever a code has been sent.
 */
export function resendCooldown() {
  const secondsLeft = signal(0);
  let timer: ReturnType<typeof setInterval> | undefined;
  inject(DestroyRef).onDestroy(() => clearInterval(timer));

  return {
    secondsLeft: secondsLeft.asReadonly(),
    start(): void {
      clearInterval(timer);
      secondsLeft.set(RESEND_COOLDOWN_SECONDS);
      timer = setInterval(() => {
        secondsLeft.update((s) => Math.max(0, s - 1));
        if (secondsLeft() === 0) {
          clearInterval(timer);
        }
      }, 1000);
    },
  };
}

export const WRONG_CODE_MESSAGE =
  'That code is wrong or has expired. Check the latest email from us, or tap "Resend code" to get a new one.';
export const RATE_LIMITED_MESSAGE = 'Too many attempts. Please wait a minute and try again.';

/**
 * Friendly message for the failures every code screen handles the same way (rate limited, or a
 * wrong/expired/used code), else null. `wrongCode` tells the screen to clear the code field.
 */
export function emailCodeErrorMessage(err: unknown): { message: string; wrongCode: boolean } | null {
  if (!(err instanceof HttpErrorResponse)) {
    return null;
  }
  if (err.status === 429) {
    return { message: RATE_LIMITED_MESSAGE, wrongCode: false };
  }
  if (err.error?.code === INVALID_OR_EXPIRED_CODE) {
    return { message: WRONG_CODE_MESSAGE, wrongCode: true };
  }
  return null;
}
