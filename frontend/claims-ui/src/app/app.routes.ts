import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'claims' },
  {
    path: 'claims',
    loadComponent: () => import('./features/claims-list/claims-list').then((m) => m.ClaimsList)
  },
  {
    path: 'claims/new',
    loadComponent: () => import('./features/fnol-intake/fnol-intake').then((m) => m.FnolIntake)
  },
  {
    path: 'claims/:id',
    loadComponent: () => import('./features/claim-detail/claim-detail').then((m) => m.ClaimDetailPage)
  },
  { path: '**', redirectTo: 'claims' }
];
