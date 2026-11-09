import { Component, ElementRef, computed, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButton } from '@angular/material/button';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { AuthService } from '../../../core/auth/auth.service';
import { emailCodeErrorMessage, resendCooldown } from '../../../core/auth/email-code';
import { applyServerErrors } from '../../../core/forms/form-errors';
import { focusFirstInvalid } from '../../../core/forms/focus-invalid';
import { AlertBanner, AuthLayout, CODE_LENGTH, CodeInput, PageHeader, PrimaryButton, ResendCode } from '../../../shared/ui';
import { safeReturnUrl } from '../return-url';

/**
 * Enter the emailed verification code. Reached after registering, or after a password sign-in
 * on an unverified email (the API has just sent a code in both cases). Success signs the user in.
 */
@Component({
  selector: 'app-verify-email-page',
  imports: [ReactiveFormsModule, RouterLink, MatButton, AlertBanner, AuthLayout, CodeInput, PageHeader, PrimaryButton, ResendCode],
  templateUrl: './verify-email-page.html',
})
export class VerifyEmailPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  /** From ?email=, set by Register / Login. */
  readonly email = input<string>();
  /** Where to go after signing in (passed on from Login). */
  readonly returnUrl = input<string>();

  protected readonly targetEmail = computed(() => this.email()?.trim() ?? '');
  protected readonly subtitle = computed(
    () => `We've emailed a 6-digit code to ${this.targetEmail()}. Enter it below to verify your email and sign in.`,
  );

  protected readonly form = inject(NonNullableFormBuilder).group({
    code: ['', [Validators.required, Validators.pattern(new RegExp(`^\\d{${CODE_LENGTH}}$`))]],
  });

  protected readonly submitting = signal(false);
  protected readonly resending = signal(false);
  protected readonly formError = signal<string | null>(null);
  protected readonly notice = signal<string | null>(null);
  protected readonly cooldown = resendCooldown();

  constructor() {
    // A code was sent just before this page opened.
    this.cooldown.start();
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      focusFirstInvalid(this.host.nativeElement);
      return;
    }

    this.submitting.set(true);
    this.formError.set(null);
    this.notice.set(null);
    this.auth
      .verifyEmail(this.targetEmail(), this.form.controls.code.value)
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: (session) => {
          const firstName = session.user.fullName.trim().split(/\s+/)[0];
          this.router.navigateByUrl(safeReturnUrl(this.returnUrl()));
          this.snackBar.open(`Welcome to JodVyapar, ${firstName}! Your email is verified.`, 'Got it', { duration: 6000 });
        },
        error: (err: unknown) => this.handleError(err),
      });
  }

  protected resend(): void {
    this.resending.set(true);
    this.formError.set(null);
    this.notice.set(null);
    this.auth
      .resendVerification(this.targetEmail())
      .pipe(finalize(() => this.resending.set(false)))
      .subscribe({
        next: () => {
          this.notice.set(`We've sent a new code to ${this.targetEmail()}. Only the newest code works.`);
          this.form.controls.code.reset();
          this.cooldown.start();
        },
        error: (err: unknown) => this.handleError(err),
      });
  }

  private handleError(err: unknown): void {
    const codeError = emailCodeErrorMessage(err);
    if (codeError) {
      this.formError.set(codeError.message);
      if (codeError.wrongCode) {
        this.form.controls.code.reset();
        this.host.nativeElement.querySelector<HTMLInputElement>('app-code-input input')?.focus();
      }
      return;
    }

    this.formError.set(applyServerErrors(this.form, err));
  }
}
