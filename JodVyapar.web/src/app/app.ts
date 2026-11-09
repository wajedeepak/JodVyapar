import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

/**
 * Root component. Layout belongs to the routes: AuthLayout (sign-in pages), PublicLayout (legal
 * pages) and AppShell (every signed-in page). See app.routes.ts.
 */
@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  template: '<router-outlet />',
  styleUrl: './app.scss',
})
export class App {}
