import { Routes } from '@angular/router';

import { unsavedChangesGuard } from '../../core/auth/auth.guards';

/** Rendered inside the app shell, which already requires sign-in. */
export const PROFILE_ROUTES: Routes = [
  {
    path: '',
    title: 'My profile | JodVyapar',
    loadComponent: () => import('./my-profile-page').then((m) => m.MyProfilePage),
  },
  {
    // Before ':userId', so "edit" is never read as a user id.
    path: 'edit',
    title: 'Edit profile | JodVyapar',
    canDeactivate: [unsavedChangesGuard],
    loadComponent: () => import('./edit-profile-page').then((m) => m.EditProfilePage),
  },
  {
    path: ':userId',
    title: 'Business profile | JodVyapar',
    loadComponent: () => import('./public-profile-page').then((m) => m.PublicProfilePage),
  },
];
