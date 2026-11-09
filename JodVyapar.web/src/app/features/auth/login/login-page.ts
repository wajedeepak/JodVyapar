import { HttpErrorResponse } from '@angular/common/http';
import { Component, ElementRef, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButton } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { EMAIL_NOT_VERIFIED } from '../../../core/auth/auth.models';
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
  TextField,
} from '../../../shared/ui';
import { safeReturnUrl } from '../return-url';

/** "password": email + password. "code": email, then a 6-digit code we email. */
type LoginMethod = 'password' | 'code';

@Component({
  selector: 'app-login-page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButton,
    MatButtonToggleModule,
    AlertBanner,
    AuthLayout,
    CodeInput,
    PageHeader,
    PasswordInput,
    PrimaryButton,
    ResendCode,
    TextField,
  ],
  templateUrl: './login-page.html',
  styleUrl: './login-page.scss',
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  /** Bound from the ?returnUrl= query parameter set by the guard / 401 interceptor. */
  readonly returnUrl = input<string>();

  // Controls not used by the current method/step are disabled, so they don't block submit.
  protected readonly form = inject(NonNullableFormBuilder).group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
    code: [
      { value: '', disabled: true },
      [Validators.required, Validators.pattern(new RegExp(`^\\d{${CODE_LENGTH}}$`))],
    ],
  });

  protected readonly method = signal<LoginMethod>('password');
  /** Set once a sign-in code has been requested; the page then asks for the code. */
  protected readonly codeSentTo = signal<string | null>(null);

  protected readonly submitting = signal(false);
  protected readonly resending = signal(false);
  protected readonly formError = signal<string | null>(null);
  protected readonly notice = signal<string | null>(null);
  protected readonly cooldown = resendCooldown();

  protected setMethod(method: LoginMethod): void {
    this.method.set(method);
    this.codeSentTo.set(null);
    this.clearMessages();
    this.syncControls();
  }

  /** Back to the email step of "Email code" (e.g. to fix a typo). */
  protected changeEmail(): void {
    this.codeSentTo.set(null);
    this.clearMessages();
    this.syncControls();
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      focusFirstInvalid(this.host.nativeElement);
      return;
    }

    if (this.method() === 'password') {
      this.signInWithPassword();
    } else if (this.codeSentTo()) {
      this.signInWithCode();
    } else {
      this.sendCode();
    }
  }

  protected resend(): void {
    const email = this.codeSentTo();
    if (!email) {
      return;
    }

    this.resending.set(true);
    this.clearMessages();
    this.auth
      .requestLoginCode(email)
      .pipe(finalize(() => this.resending.set(false)))
      .subscribe({
        next: () => {
          this.notice.set(`We've sent a new code to ${email}. Only the newest code works.`);
          this.form.controls.code.reset();
          this.cooldown.start();
        },
        error: (err: unknown) => this.handleError(err),
      });
  }

  private signInWithPassword(): void {
    const { email, password } = this.form.getRawValue();
    this.submitting.set(true);
    this.clearMessages();
    this.auth
      .login({ email, password })
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: () => this.router.navigateByUrl(safeReturnUrl(this.returnUrl())),
        error: (err: unknown) => {
          // Right password, unverified email: the API has just emailed a verification code.
          if (err instanceof HttpErrorResponse && err.status === 403 && err.error?.code === EMAIL_NOT_VERIFIED) {
            this.router.navigate(['/auth/verify-email'], {
              queryParams: { email: email.trim(), returnUrl: this.returnUrl() || null },
            });
            return;
          }
          this.formError.set(applyServerErrors(this.form, err));
        },
      });
  }

  private sendCode(): void {
    const email = this.form.controls.email.value.trim();
    this.submitting.set(true);
    this.clearMessages();
    this.auth
      .requestLoginCode(email)
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: () => {
          this.codeSentTo.set(email);
          this.form.controls.code.reset();
          this.syncControls();
          this.cooldown.start();
        },
        error: (err: unknown) => this.handleError(err),
      });
  }

  private signInWithCode(): void {
    this.submitting.set(true);
    this.clearMessages();
    this.auth
      .verifyLoginCode(this.codeSentTo()!, this.form.controls.code.value)
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: () => this.router.navigateByUrl(safeReturnUrl(this.returnUrl())),
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

  private syncControls(): void {
    const { email, password, code } = this.form.controls;
    const usingPassword = this.method() === 'password';
    const awaitingCode = !usingPassword && this.codeSentTo() !== null;

    if (usingPassword) {
      password.enable();
    } else {
      password.disable();
    }
    // The email is fixed while entering the code ("Change" goes back a step).
    if (awaitingCode) {
      email.disable();
      code.enable();
    } else {
      email.enable();
      code.disable();
    }
  }

  private clearMessages(): void {
    this.formError.set(null);
    this.notice.set(null);
  }
}
