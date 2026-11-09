import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { EMPTY, Observable, catchError, filter, finalize, map, switchMap, tap } from 'rxjs';

import { RegistrableRole } from '../auth/auth.models';
import { AuthService } from '../auth/auth.service';
import { confirm } from '../../shared/ui/confirm-dialog/confirm-dialog';
import { COMPANY_PROFILE_REQUIRED } from './profile.models';

const ROLE_COPY: Record<RegistrableRole, { title: string; message: string; done: string }> = {
  Buyer: {
    title: 'Add the Buyer role?',
    message: 'You will be able to post requirements and contact sellers. Your seller tools stay as they are.',
    done: 'Buyer role added. You are now in Buying mode.',
  },
  Seller: {
    title: 'Add the Seller role?',
    message: 'You will be able to list products and respond to buyer requirements. Your buyer tools stay as they are.',
    done: 'Seller role added. You are now in Selling mode.',
  },
};

/**
 * The "Add Buyer/Seller role" action used on Home and My Profile: a short confirmation, the API
 * call (which returns a fresh token), then a snackbar. `adding()` is the role being added, for
 * button loading states.
 */
@Injectable({ providedIn: 'root' })
export class RoleActions {
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly router = inject(Router);

  readonly adding = signal<RegistrableRole | null>(null);

  /** The other marketplace role for a single-role user, or null if they hold both (or neither). */
  readonly missingRole = computed<RegistrableRole | null>(() => {
    const roles = this.auth.roles();
    const buyer = roles.includes('Buyer');
    const seller = roles.includes('Seller');
    if (buyer === seller) {
      return null;
    }
    return buyer ? 'Seller' : 'Buyer';
  });

  /** Emits true once the role is added; completes without emitting if the user cancels or it fails. */
  addRole(role: RegistrableRole): Observable<true> {
    const copy = ROLE_COPY[role];
    return confirm(this.dialog, { title: copy.title, message: copy.message, confirmLabel: `Add ${role} role` }).pipe(
      filter(Boolean),
      tap(() => this.adding.set(role)),
      switchMap(() =>
        this.auth.addRole(role).pipe(
          finalize(() => this.adding.set(null)),
          tap(() => this.snackBar.open(copy.done, 'OK', { duration: 5000 })),
          map(() => true as const),
          catchError((err: unknown) => {
            this.showError(err);
            return EMPTY;
          }),
        ),
      ),
    );
  }

  private showError(err: unknown): void {
    const code = err instanceof HttpErrorResponse ? (err.error as { code?: string } | null)?.code : undefined;
    if (code === COMPANY_PROFILE_REQUIRED) {
      this.snackBar
        .open('Please add your company details first.', 'Add details', { duration: 8000 })
        .onAction()
        .subscribe(() => this.router.navigate(['/profile/edit']));
      return;
    }
    const offline = err instanceof HttpErrorResponse && err.status === 0;
    this.snackBar.open(
      offline ? 'Cannot reach the server. Please check your connection.' : "Couldn't add the role. Please try again.",
      'OK',
      { duration: 6000 },
    );
  }
}
