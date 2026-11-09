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
import {
  AlertBanner,
  AuthLayout,
  CODE_LENGTH,
  CodeInput,
  PageHeader,
  PasswordInput,
  PrimaryButton,
  ResendCode,
} from '../../../shared/ui';

/** Step 2 of the reset: the emailed code plus a new password. Success sends the user to Login. */
@Component({
  selector: 'app-reset-password-page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButton,
    AlertBanner,
    AuthLayout,
    CodeInput,
    PageHeader,
    PasswordInput,
    PrimaryButton,
    ResendCode,
  ],
  templateUrl: './reset-password-page.html',
})
export class ResetPasswordPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  /** From ?email=, set by the Forgot Password page. */
  readonly email = input<string>();
  protected readonly targetEmail = computed(() => this.email()?.trim() ?? '');
  protected readonly subtitle = computed(
    () => `If an account exists for ${this.targetEmail()}, we've emailed it a 6-digit code. Enter it below with your new password.`,
  );

  protected readonly form = inject(NonNullableFormBuilder).group({
    code: ['', [Validators.required, Validators.pattern(new RegExp(`^\\d{${CODE_LENGTH}}$`))]],
    newPassword: ['', Validators.required],
  });

  protected readonly submitting = signal(false);
  protected readonly resending = signal(false);
  protected readonly formError = signal<string | null>(null);
  protected readonly notice = signal<string | null>(null);

  protected readonly cooldown = resendCooldown();

  constructor() {
    // The first code was sent just before this page opened.
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
      .resetPassword({ email: this.targetEmail(), ...this.form.getRawValue() })
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: () => {
          this.snackBar.open('Password changed. Please sign in with your new password.', 'Got it', { duration: 6000 });
          this.router.navigate(['/auth/login']);
        },
        error: (err: unknown) => this.handleError(err),
      });
  }

  protected resend(): void {
    this.resending.set(true);
    this.formError.set(null);
    this.notice.set(null);
    this.auth
      .forgotPassword(this.targetEmail())
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
