import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatIcon } from '@angular/material/icon';
import { RouterLink } from '@angular/router';

import { LogoMark } from '../logo-mark/logo-mark';

const BENEFITS = [
  { icon: 'verified_user', title: 'Verified businesses', text: 'Deal with real companies that have complete profiles.' },
  { icon: 'hub', title: 'Smart requirement matching', text: 'Post what you need and see sellers who fit.' },
  { icon: 'forum', title: 'Talk directly, no middlemen', text: 'Message buyers and sellers one to one.' },
] as const;

/**
 * Full-screen layout for sign-in style pages: brand panel + form card. Split from `md`,
 * single column with a compact brand header on smaller screens. The page content
 * (PageHeader + form) goes in the default slot. Used by the /auth/* pages, which sit outside
 * the signed-in AppShell.
 */
@Component({
  selector: 'app-auth-layout',
  imports: [MatIcon, RouterLink, LogoMark],
  templateUrl: './auth-layout.html',
  styleUrl: './auth-layout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class.wide]': 'wide()',
  },
})
export class AuthLayout {
  /** Wider card for longer forms (e.g. register). */
  readonly wide = input(false);

  protected readonly benefits = BENEFITS;
  protected readonly year = new Date().getFullYear();
}
