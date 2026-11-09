import { Routes } from '@angular/router';

import { guestGuard } from '../../core/auth/auth.guards';

export const AUTH_ROUTES: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    title: 'Sign in | JodVyapar',
    loadComponent: () => import('./login/login-page').then((m) => m.LoginPage),
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    title: 'Create account | JodVyapar',
    loadComponent: () => import('./register/register-page').then((m) => m.RegisterPage),
  },
  {
    path: 'verify-email',
    canActivate: [guestGuard],
    title: 'Verify email | JodVyapar',
    loadComponent: () => import('./verify-email/verify-email-page').then((m) => m.VerifyEmailPage),
  },
  {
    path: 'forgot-password',
    canActivate: [guestGuard],
    title: 'Forgot password | JodVyapar',
    loadComponent: () => import('./forgot-password/forgot-password-page').then((m) => m.ForgotPasswordPage),
  },
  {
    path: 'reset-password',
    canActivate: [guestGuard],
    title: 'Reset password | JodVyapar',
    loadComponent: () => import('./reset-password/reset-password-page').then((m) => m.ResetPasswordPage),
  },
  { path: '', redirectTo: 'login', pathMatch: 'full' },
];
