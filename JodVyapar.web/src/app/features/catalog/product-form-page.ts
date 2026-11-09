import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { RouterLink } from '@angular/router';
import { Observable, catchError, concat, finalize, map, of, switchMap, toArray } from 'rxjs';

import { controlError, errorMessage } from '../../core/forms/form-errors';
import { ListingFormBase } from '../../core/listings/listing-form-base';
import { amountValidator, notBelow, toAmount } from '../../core/listings/listing-validators';
import {
  DESCRIPTION_MAX_LENGTH,
  MAX_PRODUCT_IMAGES,
  Product,
  ProductImage,
  ProductRequest,
  TITLE_MAX_LENGTH,
} from '../../core/listings/listing.models';
import { ProductService } from '../../core/listings/product.service';
import { ProfileService } from '../../core/profile/profile.service';
import {
  AlertBanner,
  CategoryFields,
  EmptyState,
  ImagePicker,
  KeywordChipsField,
  PageHeader,
  PickerImage,
  PrimaryButton,
  Skeleton,
  StateCityFields,
  TextField,
  UnitField,
} from '../../shared/ui';

const MESSAGES: Record<string, Record<string, string>> = {
  title: { required: 'Please enter a product name.', maxlength: `Name must be ${TITLE_MAX_LENGTH} characters or fewer.` },
  priceMin: { required: 'Please enter a price.', amount: 'Please enter a price above 0, with up to 2 decimals.' },
  priceMax: {
    amount: 'Please enter a price above 0, with up to 2 decimals.',
    range: "Maximum price can't be less than the minimum price.",
  },
  minOrderQty: { required: 'Please enter a minimum order quantity.', amount: 'Please enter a quantity above 0, with up to 2 decimals.' },
  description: { maxlength: `Description must be ${DESCRIPTION_MAX_LENGTH} characters or fewer.` },
};

/** The form's image failure messages for a saved product: the API's field message when there is one. */
function imageErrorMessage(err: unknown): string {
  const problem = err instanceof HttpErrorResponse ? (err.error as { errors?: { file?: string[] } } | null) : null;
  return problem?.errors?.file?.[0] ?? errorMessage(err);
}

/** /products/new and /products/:id/edit. */
@Component({
  selector: 'app-product-form-page',
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
    ImagePicker,
    KeywordChipsField,
    PageHeader,
    PrimaryButton,
    Skeleton,
    StateCityFields,
    TextField,
    UnitField,
  ],
  templateUrl: './product-form-page.html',
  styleUrl: './listing-pages.scss',
  // Default change detection: error text depends on FormControl state, which is not a signal.
  changeDetection: ChangeDetectionStrategy.Default,
})
export class ProductFormPage extends ListingFormBase {
  private readonly products = inject(ProductService);
  private readonly profiles = inject(ProfileService);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly messages = MESSAGES;
  protected readonly titleMaxLength = TITLE_MAX_LENGTH;
  protected readonly descriptionMaxLength = DESCRIPTION_MAX_LENGTH;
  protected readonly maxImages = MAX_PRODUCT_IMAGES;

  protected readonly form = this.fb.group({
    title: ['', [Validators.required, Validators.maxLength(TITLE_MAX_LENGTH)]],
    parentCategoryId: ['', Validators.required],
    categoryId: ['', Validators.required],
    priceMin: ['', [Validators.required, amountValidator]],
    priceMax: ['', [amountValidator, notBelow('priceMin')]],
    unitId: ['', Validators.required],
    minOrderQty: ['', [Validators.required, amountValidator]],
    stateId: ['', Validators.required],
    cityId: ['', Validators.required],
    description: ['', Validators.maxLength(DESCRIPTION_MAX_LENGTH)],
    keywords: this.fb.control<string[]>([]),
  });

  protected readonly optionalControls = ['description', 'keywords'] as const;

  protected readonly images = signal<PickerImage[]>([]);
  private savedImages: ProductImage[] = [];

  constructor() {
    super();
    const { priceMin, priceMax } = this.form.controls;
    priceMin.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => priceMax.updateValueAndValidity({ emitEvent: false }));
  }

  protected get isEdit(): boolean {
    return !!this.id();
  }

  protected load(): void {
    this.status.set('loading');
    this.loadError.set(null);

    const id = this.id();
    if (id) {
      this.products
        .get(id)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({ next: (product) => this.fillFrom(product), error: (err: unknown) => this.loadFailed(err) });
      return;
    }

    // A new product starts at the seller's own location.
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

  protected error(field: 'description'): string | null {
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
    (id ? this.products.update(id, request) : this.products.create(request))
      .pipe(
        switchMap((product) => this.syncImages(product.id).pipe(map((failures) => ({ product, failures })))),
        finalize(() => this.saving.set(false)),
      )
      .subscribe({
        next: ({ product, failures }) => this.finish(product.id, failures),
        error: (err: unknown) => this.saveFailed(err),
      });
  }

  protected override hasOtherChanges(): boolean {
    const kept = this.images().filter((image) => image.id).length;
    return kept !== this.savedImages.length || this.images().some((image) => image.file);
  }

  private fillFrom(product: Product): void {
    this.form.reset({
      title: product.title,
      parentCategoryId: product.parentCategoryId,
      categoryId: product.categoryId,
      priceMin: String(product.priceMin),
      priceMax: product.priceMax === null ? '' : String(product.priceMax),
      unitId: product.unitId,
      minOrderQty: String(product.minOrderQty),
      stateId: product.stateId,
      cityId: product.cityId,
      description: product.description ?? '',
      keywords: product.keywords,
    });
    this.savedImages = product.images;
    this.images.set(product.images.map((image) => ({ key: image.id, id: image.id, url: image.url })));
    this.moreOpen.set(!!product.description || product.keywords.length > 0 || product.images.length > 0);
    this.status.set('ready');
  }

  private toRequest(): ProductRequest {
    const value = this.form.getRawValue();
    return {
      title: value.title.trim(),
      description: value.description.trim() || null,
      categoryId: value.categoryId || null,
      priceMin: toAmount(value.priceMin),
      priceMax: toAmount(value.priceMax),
      unitId: value.unitId,
      minOrderQty: toAmount(value.minOrderQty),
      stateId: value.stateId || null,
      cityId: value.cityId || null,
      keywords: value.keywords,
    };
  }

  /**
   * Deletes removed images first (so the limit of 3 has room), then uploads new ones one at a time.
   * The product itself is already saved, so a failed image is reported, not treated as a failed save.
   */
  private syncImages(productId: string): Observable<string[]> {
    const keptIds = new Set(this.images().map((image) => image.id));
    const removals = this.savedImages
      .filter((image) => !keptIds.has(image.id))
      .map((image) => this.products.removeImage(productId, image.id));
    const uploads = this.images()
      .filter((image) => image.file)
      .map((image) => this.products.addImage(productId, image.file!));

    const steps = [...removals, ...uploads].map((step) =>
      step.pipe(
        map(() => null as string | null),
        catchError((err: unknown) => of(imageErrorMessage(err))),
      ),
    );

    return steps.length
      ? concat(...steps).pipe(
          toArray(),
          map((results) => results.filter((message): message is string => message !== null)),
        )
      : of([]);
  }

  private finish(productId: string, imageFailures: string[]): void {
    this.saved = true;
    if (imageFailures.length) {
      this.snackBar
        .open(`Product saved, but an image change failed: ${imageFailures[0]}`, 'Fix images', { duration: 10000 })
        .onAction()
        .subscribe(() => this.router.navigate(['/products', productId, 'edit']));
    } else {
      this.snackBar.open('Product saved.', 'OK', { duration: 4000 });
    }
    this.router.navigate(['/my/products']);
  }
}
