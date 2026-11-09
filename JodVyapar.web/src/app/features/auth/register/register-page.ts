import { Component, ElementRef, Injector, afterNextRender, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { RegistrableRole } from '../../../core/auth/auth.models';
import { AuthService } from '../../../core/auth/auth.service';
import { applyServerErrors, controlError } from '../../../core/forms/form-errors';
import { focusFirstInvalid } from '../../../core/forms/focus-invalid';
import {
  AlertBanner,
  AuthLayout,
  ChoiceCards,
  ChoiceOption,
  FormSection,
  PageHeader,
  PasswordInput,
  PrimaryButton,
  StateCityFields,
  TextField,
} from '../../../shared/ui';

const MESSAGES: Record<string, Record<string, string>> = {
  roles: { required: 'Please choose whether you want to buy, sell or both.' },
  fullName: { required: 'Please enter your full name.' },
  companyName: { required: 'Please enter your company name.' },
  email: { required: 'Please enter your email address.' },
  mobile: {
    required: 'Please enter your mobile number.',
    pattern: 'Please enter a valid 10-digit Indian mobile number.',
  },
  password: {
    required: 'Please enter a password.',
    minlength: 'Password must be at least 8 characters.',
    pattern: 'Password must contain an uppercase letter, a lowercase letter and a number.',
  },
  consentAccepted: { required: 'Please accept the terms and privacy policy to continue.' },
};

const ROLE_OPTIONS: readonly ChoiceOption<RegistrableRole>[] = [
  { value: 'Buyer', title: 'I want to buy', description: 'Post requirements and find suppliers.', icon: 'shopping_bag' },
  { value: 'Seller', title: 'I want to sell', description: 'List products and reach new buyers.', icon: 'storefront' },
];

@Component({
  selector: 'app-register-page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatCheckboxModule,
    AlertBanner,
    AuthLayout,
    ChoiceCards,
    FormSection,
    PageHeader,
    PasswordInput,
    PrimaryButton,
    StateCityFields,
    TextField,
  ],
  templateUrl: './register-page.html',
  styleUrl: './register-page.scss',
})
export class RegisterPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly injector = inject(Injector);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  protected readonly messages = MESSAGES;
  protected readonly roleOptions = ROLE_OPTIONS;

  protected readonly submitting = signal(false);
  protected readonly formError = signal<string | null>(null);

  protected readonly form = this.fb.group({
    // Either or both; none is selected at first so the choice is deliberate.
    roles: this.fb.control<RegistrableRole[]>([], Validators.required),
    fullName: ['', [Validators.required, Validators.maxLength(100)]],
    companyName: ['', [Validators.required, Validators.maxLength(150)]],
    stateId: ['', Validators.required],
    cityId: [{ value: '', disabled: true }, Validators.required],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    mobile: ['', [Validators.required, Validators.pattern(/^[6-9]\d{9}$/)]],
    password: [
      '',
      [Validators.required, Validators.minLength(8), Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).*$/)],
    ],
    consentAccepted: [false, Validators.requiredTrue],
  });

  protected error(field: keyof typeof this.form.controls): string | null {
    return controlError(this.form.controls[field], MESSAGES[field]);
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      focusFirstInvalid(this.host.nativeElement);
      return;
    }

    this.submitting.set(true);
    this.formError.set(null);
    this.auth
      .register(this.form.getRawValue())
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        // No session yet: the account is created and a code is on its way to the email.
        next: (response) => this.router.navigate(['/auth/verify-email'], { queryParams: { email: response.email } }),
        error: (err: unknown) => {
          this.formError.set(applyServerErrors(this.form, err));
          this.revealErrors();
        },
      });
  }

  /** Once errors have rendered: focus the first field with a server error, else bring the banner into view. */
  private revealErrors(): void {
    afterNextRender(
      () => {
        const host = this.host.nativeElement;
        if (!focusFirstInvalid(host)) {
          host.querySelector('app-alert-banner')?.scrollIntoView({ block: 'center' });
        }
      },
      { injector: this.injector },
    );
  }
}
