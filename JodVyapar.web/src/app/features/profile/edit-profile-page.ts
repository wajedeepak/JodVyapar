import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  Injector,
  afterNextRender,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { AbstractControl, NonNullableFormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router, RouterLink } from '@angular/router';
import { finalize, firstValueFrom, map, startWith } from 'rxjs';

import { HasUnsavedChanges } from '../../core/auth/auth.guards';
import { focusFirstInvalid } from '../../core/forms/focus-invalid';
import { applyServerErrors, controlError, errorMessage } from '../../core/forms/form-errors';
import {
  ABOUT_MAX_LENGTH,
  GSTIN_PATTERN,
  MOBILE_PATTERN,
  MyProfile,
  UpdateProfileRequest,
} from '../../core/profile/profile.models';
import { ProfileService } from '../../core/profile/profile.service';
import {
  AlertBanner,
  FormSection,
  PageHeader,
  PrimaryButton,
  Skeleton,
  StateCityFields,
  TextField,
  confirm,
} from '../../shared/ui';

type LoadStatus = 'loading' | 'ready' | 'error';

const MESSAGES: Record<string, Record<string, string>> = {
  fullName: { required: 'Please enter your full name.', maxlength: 'Full name must be 100 characters or fewer.' },
  companyName: {
    required: 'Please enter your company name.',
    maxlength: 'Company name must be 150 characters or fewer.',
  },
  mobile: {
    required: 'Please enter your mobile number.',
    pattern: 'Please enter a valid 10-digit Indian mobile number.',
  },
  gstin: { gstin: 'Please enter a valid 15-character GSTIN, for example 27ABCDE1234F1Z5.' },
  about: { maxlength: `About must be ${ABOUT_MAX_LENGTH} characters or fewer.` },
};

/** Optional GSTIN: blank is fine; otherwise the format check, ignoring case and spaces around it. */
function gstinValidator(control: AbstractControl<string>): ValidationErrors | null {
  const value = control.value.trim().toUpperCase();
  return !value || GSTIN_PATTERN.test(value) ? null : { gstin: true };
}

/** /profile/edit: name and company details. Warns before leaving with unsaved changes. */
@Component({
  selector: 'app-edit-profile-page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    AlertBanner,
    FormSection,
    PageHeader,
    PrimaryButton,
    Skeleton,
    StateCityFields,
    TextField,
  ],
  templateUrl: './edit-profile-page.html',
  styleUrl: './profile-pages.scss',
  // Default change detection: error text depends on FormControl state, which is not a signal.
  changeDetection: ChangeDetectionStrategy.Default,
  host: {
    '(window:beforeunload)': 'onBeforeUnload($event)',
  },
})
export class EditProfilePage implements HasUnsavedChanges {
  private readonly profileService = inject(ProfileService);
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  protected readonly messages = MESSAGES;
  protected readonly aboutMaxLength = ABOUT_MAX_LENGTH;

  protected readonly status = signal<LoadStatus>('loading');
  protected readonly loadError = signal<string | null>(null);
  protected readonly profile = signal<MyProfile | null>(null);
  protected readonly saving = signal(false);
  protected readonly formError = signal<string | null>(null);

  protected readonly form = this.fb.group({
    fullName: ['', [Validators.required, Validators.maxLength(100)]],
    mobile: ['', [Validators.required, Validators.pattern(MOBILE_PATTERN)]],
    companyName: ['', [Validators.required, Validators.maxLength(150)]],
    stateId: ['', Validators.required],
    cityId: ['', Validators.required],
    gstin: ['', gstinValidator],
    about: ['', Validators.maxLength(ABOUT_MAX_LENGTH)],
  });

  protected readonly aboutLength = toSignal(
    this.form.controls.about.valueChanges.pipe(
      startWith(this.form.controls.about.value),
      map((v) => v.length),
    ),
    { initialValue: 0 },
  );

  private readonly gstinValue = toSignal(this.form.controls.gstin.valueChanges, { initialValue: '' });

  /** A verified GSTIN that is being changed will need verifying again. */
  protected readonly gstinWillNeedVerifying = computed(() => {
    const company = this.profile()?.company;
    return !!company?.isGstinVerified && this.gstinValue().trim().toUpperCase() !== (company.gstin ?? '');
  });

  /** Set once the save succeeded, so the guard lets the navigation through. */
  private saved = false;

  constructor() {
    this.load();
  }

  protected load(): void {
    this.status.set('loading');
    this.loadError.set(null);
    this.profileService
      .getMine()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (profile) => {
          this.profile.set(profile);
          const company = profile.company;
          this.form.reset({
            fullName: profile.fullName,
            mobile: company?.mobile ?? '',
            companyName: company?.companyName ?? '',
            stateId: company?.stateId ?? '',
            cityId: company?.cityId ?? '',
            gstin: company?.gstin ?? '',
            about: company?.about ?? '',
          });
          this.status.set('ready');
        },
        error: (err: unknown) => {
          this.loadError.set(errorMessage(err));
          this.status.set('error');
        },
      });
  }

  protected error(field: keyof typeof this.form.controls): string | null {
    return controlError(this.form.controls[field], MESSAGES[field]);
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      focusFirstInvalid(this.host.nativeElement);
      return;
    }

    const value = this.form.getRawValue();
    const request: UpdateProfileRequest = {
      fullName: value.fullName.trim(),
      companyName: value.companyName.trim(),
      mobile: value.mobile.trim(),
      stateId: value.stateId,
      cityId: value.cityId,
      gstin: value.gstin.trim().toUpperCase() || null,
      about: value.about.trim() || null,
    };

    this.saving.set(true);
    this.formError.set(null);
    this.profileService
      .updateMine(request)
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.saved = true;
          this.snackBar.open('Profile saved.', 'OK', { duration: 4000 });
          this.router.navigate(['/profile']);
        },
        error: (err: unknown) => {
          this.formError.set(applyServerErrors(this.form, err));
          this.revealErrors();
        },
      });
  }

  confirmLeave(): boolean | Promise<boolean> {
    if (this.saved || !this.form.dirty) {
      return true;
    }
    return firstValueFrom(
      confirm(this.dialog, {
        title: 'Discard your changes?',
        message: "You have edits that haven't been saved. If you leave now, they will be lost.",
        confirmLabel: 'Discard changes',
        cancelLabel: 'Keep editing',
      }),
    );
  }

  /** Browser-level warning when closing or reloading the tab with unsaved edits. */
  protected onBeforeUnload(event: BeforeUnloadEvent): void {
    if (this.form.dirty && !this.saved) {
      event.preventDefault();
    }
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
