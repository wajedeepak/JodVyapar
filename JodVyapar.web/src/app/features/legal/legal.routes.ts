import { Routes } from '@angular/router';

export const LEGAL_ROUTES: Routes = [
  {
    path: 'privacy',
    title: 'Privacy Policy | JodVyapar',
    data: { doc: 'privacy' },
    loadComponent: () => import('./legal-page').then((m) => m.LegalPage),
  },
  {
    path: 'terms',
    title: 'Terms of Use | JodVyapar',
    data: { doc: 'terms' },
    loadComponent: () => import('./legal-page').then((m) => m.LegalPage),
  },
  { path: '', redirectTo: 'terms', pathMatch: 'full' },
];
