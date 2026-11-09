import { ChangeDetectionStrategy, Component, ElementRef, OnDestroy, computed, input, model, signal, viewChild } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';

import { MAX_IMAGE_BYTES } from '../../../core/listings/listing.models';

/** An image in the picker: already saved (`id`) or newly chosen and not uploaded yet (`file`). */
export interface PickerImage {
  key: string;
  url: string;
  id?: string;
  file?: File;
}

const ALLOWED_TYPES = ['image/jpeg', 'image/png', 'image/webp'];

/**
 * Choose images by tap or drag and drop, with previews and remove. It only tracks the selection in
 * `images`; the page uploads the new files and deletes the removed ones when the form is saved.
 */
@Component({
  selector: 'app-image-picker',
  imports: [MatButtonModule, MatIcon],
  template: `
    <div
      class="dropzone"
      [class.is-over]="dragging()"
      (dragover)="onDragOver($event)"
      (dragleave)="dragging.set(false)"
      (drop)="onDrop($event)"
    >
      <mat-icon aria-hidden="true">add_photo_alternate</mat-icon>
      <button mat-stroked-button type="button" [disabled]="isFull()" (click)="openFilePicker()">Choose images</button>
      <p class="hint">Or drag them here. JPG, PNG or WebP, up to 2 MB each, {{ max() }} at most.</p>
      <input #fileInput type="file" hidden multiple [attr.accept]="accept" (change)="onFilesChosen($event)" />
    </div>

    @if (error(); as message) {
      <p class="jv-field-error" role="alert">{{ message }}</p>
    }

    @if (images().length) {
      <ul class="previews">
        @for (image of images(); track image.key; let index = $index) {
          <li class="preview">
            <img [src]="image.url" [alt]="'Image ' + (index + 1)" />
            <button mat-icon-button type="button" class="remove" [attr.aria-label]="'Remove image ' + (index + 1)" (click)="remove(image)">
              <mat-icon aria-hidden="true">close</mat-icon>
            </button>
          </li>
        }
      </ul>
    }
  `,
  styleUrl: './image-picker.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ImagePicker implements OnDestroy {
  readonly images = model<PickerImage[]>([]);
  readonly max = input(3);

  protected readonly accept = ALLOWED_TYPES.join(',');
  protected readonly dragging = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly isFull = computed(() => this.images().length >= this.max());

  private readonly fileInput = viewChild.required<ElementRef<HTMLInputElement>>('fileInput');
  private nextKey = 0;

  ngOnDestroy(): void {
    this.images().forEach((image) => this.releasePreview(image));
  }

  protected openFilePicker(): void {
    this.fileInput().nativeElement.click();
  }

  protected onFilesChosen(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.addFiles(Array.from(input.files ?? []));
    // Allows choosing the same file again after removing it.
    input.value = '';
  }

  protected onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(true);
  }

  protected onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(false);
    this.addFiles(Array.from(event.dataTransfer?.files ?? []));
  }

  protected remove(image: PickerImage): void {
    this.releasePreview(image);
    this.images.update((images) => images.filter((i) => i !== image));
    this.error.set(null);
  }

  private addFiles(files: File[]): void {
    const accepted: PickerImage[] = [];
    let problem: string | null = null;

    for (const file of files) {
      const invalid = this.validate(file);
      if (invalid) {
        problem = invalid;
      } else if (this.images().length + accepted.length >= this.max()) {
        problem = `You can add up to ${this.max()} images.`;
      } else {
        accepted.push({ key: `new-${this.nextKey++}`, url: URL.createObjectURL(file), file });
      }
    }

    this.error.set(problem);
    if (accepted.length) {
      this.images.update((images) => [...images, ...accepted]);
    }
  }

  private validate(file: File): string | null {
    if (!ALLOWED_TYPES.includes(file.type)) {
      return 'Please choose JPG, PNG or WebP images.';
    }
    return file.size > MAX_IMAGE_BYTES ? 'Each image must be 2 MB or smaller.' : null;
  }

  private releasePreview(image: PickerImage): void {
    if (image.file) {
      URL.revokeObjectURL(image.url);
    }
  }
}
