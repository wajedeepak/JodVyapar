import { Routes } from '@angular/router';

import { authGuard, roleGuard, unsavedChangesGuard } from './core/auth/auth.guards';
import type { ComingSoonData } from './features/placeholders/coming-soon-page';

/** Route data for the shared "Coming soon" placeholder page. */
function comingSoon(data: ComingSoonData) {
  return {
    data,
    loadComponent: () => import('./features/placeholders/coming-soon-page').then((m) => m.ComingSoonPage),
  };
}

export const routes: Routes = [
  {
    // Full-screen sign-in pages (each uses AuthLayout).
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes').then((m) => m.AUTH_ROUTES),
  },
  {
    path: 'legal',
    loadComponent: () => import('./layout/public-layout/public-layout').then((m) => m.PublicLayout),
    loadChildren: () => import('./features/legal/legal.routes').then((m) => m.LEGAL_ROUTES),
  },
  {
    // Every signed-in page lives inside the app shell (top bar, bottom tabs on phones).
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/app-shell/app-shell').then((m) => m.AppShell),
    children: [
      {
        path: '',
        pathMatch: 'full',
        loadChildren: () => import('./features/home/home.routes').then((m) => m.HOME_ROUTES),
      },
      {
        path: 'search',
        title: 'Search | JodVyapar',
        ...comingSoon({
          icon: 'search',
          heading: 'Search is coming soon',
          message: 'Soon you will search products and suppliers across India by category, city and state.',
        }),
      },
      {
        path: 'messages',
        title: 'Messages | JodVyapar',
        ...comingSoon({
          icon: 'chat_bubble_outline',
          heading: 'No messages yet',
          message: 'When you contact a buyer or seller, your conversations will appear here. Messaging is coming soon.',
        }),
      },
      {
        path: 'profile',
        loadChildren: () => import('./features/profile/profile.routes').then((m) => m.PROFILE_ROUTES),
      },
      // Listings. Guarded by the roles the user holds (never by the active mode).
      {
        path: 'my/products',
        canActivate: [roleGuard('Seller')],
        title: 'My products | JodVyapar',
        loadComponent: () => import('./features/catalog/my-products-page').then((m) => m.MyProductsPage),
      },
      {
        path: 'products/new',
        canActivate: [roleGuard('Seller')],
        canDeactivate: [unsavedChangesGuard],
        title: 'Add a product | JodVyapar',
        loadComponent: () => import('./features/catalog/product-form-page').then((m) => m.ProductFormPage),
      },
      {
        path: 'products/:id/edit',
        canActivate: [roleGuard('Seller')],
        canDeactivate: [unsavedChangesGuard],
        title: 'Edit product | JodVyapar',
        loadComponent: () => import('./features/catalog/product-form-page').then((m) => m.ProductFormPage),
      },
      {
        path: 'my/requirements',
        canActivate: [roleGuard('Buyer')],
        title: 'My requirements | JodVyapar',
        loadComponent: () => import('./features/requirements/my-requirements-page').then((m) => m.MyRequirementsPage),
      },
      {
        path: 'requirements/new',
        canActivate: [roleGuard('Buyer')],
        canDeactivate: [unsavedChangesGuard],
        title: 'Post a requirement | JodVyapar',
        loadComponent: () => import('./features/requirements/requirement-form-page').then((m) => m.RequirementFormPage),
      },
      {
        path: 'requirements/:id/edit',
        canActivate: [roleGuard('Buyer')],
        canDeactivate: [unsavedChangesGuard],
        title: 'Edit requirement | JodVyapar',
        loadComponent: () => import('./features/requirements/requirement-form-page').then((m) => m.RequirementFormPage),
      },
      {
        path: 'requirements',
        pathMatch: 'full',
        canActivate: [roleGuard('Seller')],
        title: 'Buyer requirements | JodVyapar',
        ...comingSoon({
          icon: 'assignment',
          heading: 'Buyer requirements are coming soon',
          message: 'You will see what buyers are looking for and respond to the ones that fit your products.',
        }),
      },
      {
        path: 'admin',
        canActivate: [roleGuard('Admin')],
        title: 'Admin | JodVyapar',
        ...comingSoon({
          icon: 'admin_panel_settings',
          heading: 'Admin tools are coming soon',
          message: 'User management and reports will live here.',
        }),
      },
      {
        path: 'forbidden',
        title: 'No access | JodVyapar',
        loadComponent: () => import('./features/errors/forbidden-page').then((m) => m.ForbiddenPage),
      },
      {
        path: '**',
        title: 'Page not found | JodVyapar',
        loadComponent: () => import('./features/errors/not-found-page').then((m) => m.NotFoundPage),
      },
    ],
  },
];
