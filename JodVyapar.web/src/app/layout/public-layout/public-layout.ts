import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink, RouterOutlet } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';
import { LogoMark } from '../../shared/ui/logo-mark/logo-mark';

/** Minimal layout for public pages that anyone can open (Terms, Privacy): logo and a way back in. */
@Component({
  selector: 'app-public-layout',
  imports: [RouterLink, RouterOutlet, MatButtonModule, LogoMark],
  template: `
    <header class="bar">
      <div class="bar-inner">
        <a routerLink="/" class="brand" aria-label="JodVyapar home"><app-logo-mark size="sm" /></a>
        @if (auth.isLoggedIn()) {
          <a mat-button routerLink="/">Back to JodVyapar</a>
        } @else {
          <a mat-button routerLink="/auth/login">Sign in</a>
        }
      </div>
    </header>
    <main class="content"><router-outlet /></main>
  `,
  styleUrl: './public-layout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PublicLayout {
  protected readonly auth = inject(AuthService);
}
