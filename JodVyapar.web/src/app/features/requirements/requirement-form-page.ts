import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { controlError } from '../../core/forms/form-errors';
import { ListingFormBase } from '../../core/listings/listing-form-base';
import { amountValidator, notBelow, notPastDate, toAmount, todayIso } from '../../core/listings/listing-validators';
import { DESCRIPTION_MAX_LENGTH, Requirement, RequirementRequest, TITLE_MAX_LENGTH } from '../../core/listings/listing.models';
import { RequirementService } from '../../core/listings/requirement.service';
import { ProfileService } from '../../core/profile/profile.service';
import {
  AlertBanner,
  CategoryFields,
  EmptyState,
  KeywordChipsField,
  PageHeader,
  PrimaryButton,
  Skeleton,
  StateCityFields,
  TextField,
  UnitField,
} from '../../shared/ui';

const MESSAGES: Record<string, Record<string, string>> = {
  title: { required: 'Please say what you need.', maxlength: `Title must be ${TITLE_MAX_LENGTH} characters or fewer.` },
  quantity: { required: 'Please enter a quantity.', amount: 'Please enter a quantity above 0, with up to 2 decimals.' },
  budgetMin: { amount: 'Please enter an amount above 0, with up to 2 decimals.' },
  budgetMax: {
    amount: 'Please enter an amount above 0, with up to 2 decimals.',
    range: "Maximum budget can't be less than the minimum budget.",
  },
  neededBy: { pastDate: "Needed-by date can't be in the past." },
  description: { maxlength: `Description must be ${DESCRIPTION_MAX_LENGTH} characters or fewer.` },
};

/** /requirements/new and /requirements/:id/edit. */
@Component({
  selector: 'app-requirement-form-page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatExpansionModule,
    MatFormFieldModule,
    MatInputModule,
    AlertBanner,
    CategoryFields,
    EmptyState,
    KeywordChipsField,
    PageHeader,
    PrimaryButton,
    Skeleton,
    StateCityFields,
    TextField,
    UnitField,
  ],
  templateUrl: './requirement-form-page.html',
  styleUrl: '../catalog/listing-pages.scss',
  // Default change detection: error text depends on FormControl state, which is not a signal.
  changeDetection: ChangeDetectionStrategy.Default,
})
export class RequirementFormPage extends ListingFormBase {
  private readonly requirements = inject(RequirementService);
  private readonly profiles = inject(ProfileService);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly messages = MESSAGES;
  protected readonly titleMaxLength = TITLE_MAX_LENGTH;
  protected readonly descriptionMaxLength = DESCRIPTION_MAX_LENGTH;
  protected readonly today = todayIso();

  private savedNeededBy = '';

  protected readonly form = this.fb.group({
    title: ['', [Validators.required, Validators.maxLength(TITLE_MAX_LENGTH)]],
    parentCategoryId: ['', Validators.required],
    categoryId: ['', Validators.required],
    quantity: ['', [Validators.required, amountValidator]],
    unitId: ['', Validators.required],
    stateId: ['', Validators.required],
    cityId: ['', Validators.required],
    budgetMin: ['', amountValidator],
    budgetMax: ['', [amountValidator, notBelow('budgetMin')]],
    neededBy: ['', notPastDate(() => this.savedNeededBy)],
    description: ['', Validators.maxLength(DESCRIPTION_MAX_LENGTH)],
    keywords: this.fb.control<string[]>([]),
  });

  protected readonly optionalControls = ['budgetMin', 'budgetMax', 'neededBy', 'description', 'keywords'] as const;

  constructor() {
    super();
    const { budgetMin, budgetMax } = this.form.controls;
    budgetMin.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => budgetMax.updateValueAndValidity({ emitEvent: false }));
  }

  protected get isEdit(): boolean {
    return !!this.id();
  }

  protected load(): void {
    this.status.set('loading');
    this.loadError.set(null);

    const id = this.id();
    if (id) {
      this.requirements
        .get(id)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({ next: (requirement) => this.fillFrom(requirement), error: (err: unknown) => this.loadFailed(err) });
      return;
    }

    // A new requirement starts at the buyer's own location.
    this.profiles
      .getMine()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (profile) => {
          this.form.patchValue({ stateId: profile.company?.stateId ?? '', cityId: profile.company?.cityId ?? '' });
          this.form.markAsPristine();
          this.status.set('ready');
        },
        error: (err: unknown) => this.loadFailed(err),
      });
  }

  protected error(field: 'description' | 'neededBy'): string | null {
    return controlError(this.form.controls[field], MESSAGES[field]);
  }

  protected save(): void {
    if (this.form.invalid) {
      this.showInvalid();
      return;
    }

    const id = this.id();
    const request = this.toRequest();

    this.saving.set(true);
    this.formError.set(null);
    (id ? this.requirements.update(id, request) : this.requirements.create(request))
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.saved = true;
          this.snackBar.open('Requirement saved.', 'OK', { duration: 4000 });
          this.router.navigate(['/my/requirements']);
        },
        error: (err: unknown) => this.saveFailed(err),
      });
  }

  private fillFrom(requirement: Requirement): void {
    this.savedNeededBy = requirement.neededBy ?? '';
    this.form.reset({
      title: requirement.title,
      parentCategoryId: requirement.parentCategoryId,
      categoryId: requirement.categoryId,
      quantity: String(requirement.quantity),
      unitId: requirement.unitId,
      stateId: requirement.stateId,
      cityId: requirement.cityId,
      budgetMin: requirement.budgetMin === null ? '' : String(requirement.budgetMin),
      budgetMax: requirement.budgetMax === null ? '' : String(requirement.budgetMax),
      neededBy: requirement.neededBy ?? '',
      description: requirement.description ?? '',
      keywords: requirement.keywords,
    });
    this.moreOpen.set(
      requirement.budgetMin !== null ||
        requirement.budgetMax !== null ||
        !!requirement.neededBy ||
        !!requirement.description ||
        requirement.keywords.length > 0,
    );
    this.status.set('ready');
  }

  private toRequest(): RequirementRequest {
    const value = this.form.getRawValue();
    return {
      title: value.title.trim(),
      description: value.description.trim() || null,
      categoryId: value.categoryId || null,
      budgetMin: toAmount(value.budgetMin),
      budgetMax: toAmount(value.budgetMax),
      quantity: toAmount(value.quantity),
      unitId: value.unitId,
      stateId: value.stateId || null,
      cityId: value.cityId || null,
      neededBy: value.neededBy || null,
      keywords: value.keywords,
    };
  }
}
