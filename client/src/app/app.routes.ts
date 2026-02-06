import { Routes } from '@angular/router';
import { authGuard, adminGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/dashboard',
    pathMatch: 'full'
  },
  {
    path: 'login',
    loadComponent: () =>
      import('./features/login/login.page').then(m => m.LoginPage)
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./features/register/register.page').then(m => m.RegisterPage)
  },
  {
    path: 'entra-provision',
    loadComponent: () =>
      import('./features/entra-provision/entra-provision.page').then(m => m.EntraProvisionPage)
  },
  {
    path: 'onboarding',
    loadComponent: () =>
      import('./features/onboarding/onboarding.page').then(m => m.OnboardingPage),
    canActivate: [authGuard]
  },
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./features/dashboard/dashboard.page').then(m => m.DashboardPage),
    canActivate: [authGuard]
  },
  {
    path: 'projects',
    loadComponent: () =>
      import('./features/projects/projects.page').then(m => m.ProjectsPage),
    canActivate: [authGuard]
  },
  {
    path: 'teams',
    loadComponent: () =>
      import('./features/teams/teams.page').then(m => m.TeamsPage),
    canActivate: [authGuard]
  },
  {
    path: 'settings',
    loadComponent: () =>
      import('./features/settings/settings.page').then(m => m.SettingsPage),
    canActivate: [authGuard, adminGuard]
  },
  {
    path: '**',
    redirectTo: '/dashboard'
  }
];
