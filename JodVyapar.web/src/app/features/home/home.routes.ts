import { Routes } from '@angular/router';

/** Rendered inside the app shell, which already requires sign-in. */
export const HOME_ROUTES: Routes = [
  {
    path: '',
    title: 'Home | JodVyapar',
    loadComponent: () => import('./home-page').then((m) => m.HomePage),
  },
];
