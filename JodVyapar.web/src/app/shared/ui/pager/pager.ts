import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';

/** Previous / "Page 1 of 3" / Next under a list. Renders nothing when everything fits on one page. */
@Component({
  selector: 'app-pager',
  imports: [MatButtonModule],
  template: `
    @if (pageCount() > 1) {
      <nav class="pager" aria-label="Pages">
        <button mat-stroked-button type="button" [disabled]="page() <= 1" (click)="pageChange.emit(page() - 1)">Previous</button>
        <span class="position" aria-live="polite">Page {{ page() }} of {{ pageCount() }}</span>
        <button mat-stroked-button type="button" [disabled]="page() >= pageCount()" (click)="pageChange.emit(page() + 1)">Next</button>
      </nav>
    }
  `,
  styles: `
    .pager {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: var(--jv-space-3);
    }
    .position {
      color: var(--jv-color-text-muted);
      font-size: var(--jv-font-size-sm);
      white-space: nowrap;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Pager {
  readonly page = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly totalCount = input.required<number>();
  readonly pageChange = output<number>();

  protected readonly pageCount = computed(() => Math.ceil(this.totalCount() / this.pageSize()));
}
