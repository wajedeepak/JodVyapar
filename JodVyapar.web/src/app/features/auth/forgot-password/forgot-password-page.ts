import { Component, ElementRef, OnInit, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { AuthService } from '../../../core/auth/auth.service';
import { applyServerErrors } from '../../../core/forms/form-errors';
import { focusFirstInvalid } from '../../../core/forms/focus-invalid';
import { AlertBanner, AuthLayout, PageHeader, PrimaryButton, TextField } from '../../../shared/ui';

/** Step 1 of the reset: ask for the email, then go to Reset Password to enter the code. */
@Component({
  selector: 'app-forgot-password-page',
  imports: [ReactiveFormsModule, RouterLink, AlertBanner, AuthLayout, PageHeader, PrimaryButton, TextField],
  templateUrl: './forgot-password-page.html',
})
export class ForgotPasswordPage implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  /** Prefilled from ?email= (the Login page passes what the user already typed). */
  readonly email = input<string>();

  protected readonly form = inject(NonNullableFormBuilder).group({
    email: ['', [Validators.required, Validators.email]],
  });

  protected readonly submitting = signal(false);
  protected readonly formError = signal<string | null>(null);

  ngOnInit(): void {
    const email = this.email()?.trim();
    if (email) {
      this.form.controls.email.setValue(email);
    }
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      focusFirstInvalid(this.host.nativeElement);
      return;
    }

    const email = this.form.controls.email.value.trim();
    this.submitting.set(true);
    this.formError.set(null);
    this.auth
      .forgotPassword(email)
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: () => this.router.navigate(['/auth/reset-password'], { queryParams: { email } }),
        error: (err: unknown) => this.formError.set(applyServerErrors(this.form, err)),
      });
  }
}
