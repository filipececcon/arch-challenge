import { Routes } from '@angular/router'
import { authGuard, guestGuard } from './core/auth/auth.guard'

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./core/auth/pages/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'auth/callback',
    loadComponent: () =>
      import('./core/auth/pages/callback.component').then(
        (m) => m.CallbackComponent,
      ),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./layout/app-shell.component').then((m) => m.AppShellComponent),
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./pages/home.component').then((m) => m.HomeComponent),
      },
      {
        path: 'cashflow',
        loadComponent: () =>
          import('./features/cashflow/pages/cashflow-home.component').then(
            (m) => m.CashflowHomeComponent,
          ),
      },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/pages/dashboard-home.component').then(
            (m) => m.DashboardHomeComponent,
          ),
      },
    ],
  },
  { path: '**', redirectTo: '' },
]
