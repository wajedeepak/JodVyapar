import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatIcon } from '@angular/material/icon';

import { UserRole } from '../../core/auth/auth.models';
import { Avatar, Badge, RoleBadges } from '../../shared/ui';

/**
 * Identity card at the top of My Profile and Public Profile: avatar, name, company, location,
 * role and verification badges, member-since. Actions go in the slot (`<div profileActions>`).
 */
@Component({
  selector: 'app-profile-header-card',
  imports: [DatePipe, MatIcon, Avatar, Badge, RoleBadges],
  template: `
    <app-avatar [name]="fullName()" size="lg" />
    <div class="identity">
      <ng-content select="[profileTitle]" />
      @if (companyName(); as company) {
        <p class="company">{{ company }}</p>
      }
      @if (location(); as place) {
        <p class="meta"><mat-icon aria-hidden="true">place</mat-icon>{{ place }}</p>
      }
      @if (memberSince(); as since) {
        <p class="meta"><mat-icon aria-hidden="true">event</mat-icon>Member since {{ since | date: 'MMMM y' }}</p>
      }
      <div class="badges">
        <app-role-badges [roles]="roles()" />
        @if (isEmailVerified()) {
          <app-badge variant="success" icon="verified">Email verified</app-badge>
        } @else {
          <app-badge icon="mail_outline">Email not verified</app-badge>
        }
        @if (isGstinVerified()) {
          <app-badge variant="success" icon="verified">GSTIN verified</app-badge>
        } @else {
          <app-badge icon="receipt_long">GSTIN not verified</app-badge>
        }
      </div>
    </div>
    <div class="actions"><ng-content select="[profileActions]" /></div>
  `,
  styleUrl: './profile-header-card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfileHeaderCard {
  readonly fullName = input.required<string>();
  readonly companyName = input<string | null>(null);
  /** "Pune, Maharashtra". */
  readonly location = input<string | null>(null);
  readonly memberSince = input<string | null>(null);
  readonly roles = input.required<readonly UserRole[]>();
  readonly isEmailVerified = input(false);
  readonly isGstinVerified = input(false);
}
