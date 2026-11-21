import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', loadComponent: () => import('./pages/home').then(m => m.Home) },
  { path: 'property/:id', loadComponent: () => import('./pages/property-detail').then(m => m.PropertyDetail) },
  { path: 'login', loadComponent: () => import('./pages/login').then(m => m.Login) },
  { path: 'post', loadComponent: () => import('./pages/property-form').then(m => m.PropertyForm) },
  { path: 'leads', loadComponent: () => import('./pages/leads').then(m => m.Leads) },
  { path: 'dashboard', loadComponent: () => import('./pages/dashboard').then(m => m.Dashboard) },
  { path: 'map', loadComponent: () => import('./pages/map').then(m => m.MapPage) },
  { path: 'tools', loadComponent: () => import('./pages/tools').then(m => m.ToolsPage) },
  { path: 'projects', loadComponent: () => import('./pages/projects').then(m => m.ProjectsPage) },
  { path: 'agencies', loadComponent: () => import('./pages/agencies').then(m => m.AgenciesPage) },
  { path: 'saved', loadComponent: () => import('./pages/saved-searches').then(m => m.SavedSearches) },
  { path: 'agent/:id', loadComponent: () => import('./pages/agent').then(m => m.AgentPage) },
  { path: 'admin/reports', loadComponent: () => import('./pages/admin-reports').then(m => m.AdminReports) },
  { path: '**', redirectTo: '' }
];
