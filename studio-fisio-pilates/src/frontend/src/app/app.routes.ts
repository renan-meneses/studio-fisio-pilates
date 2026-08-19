import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { tenantGuard } from './core/guards/tenant.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'agenda' },
  {
    path: 'login',
    loadComponent: () => import('./modules/auth/pages/login.component').then(m => m.LoginComponent),
  },
  {
    path: '',
    canActivate: [authGuard, tenantGuard],
    loadComponent: () => import('./modules/layout/shell.component').then(m => m.ShellComponent),
    children: [
      { path: 'agenda', loadChildren: () => import('./modules/agenda/agenda.routes').then(m => m.agendaRoutes) },
      {
        path: 'prontuarios',
        loadChildren: () => import('./modules/prontuario/prontuario.routes').then(m => m.prontuarioRoutes),
      },
      {
        path: 'financeiro',
        loadChildren: () => import('./modules/financeiro/financeiro.routes').then(m => m.financeiroRoutes),
      },
      {
        path: 'rh',
        loadChildren: () => import('./modules/rh/rh.routes').then(m => m.rhRoutes),
      },
      {
        path: 'planos',
        loadChildren: () => import('./modules/planos/planos.routes').then(m => m.planosRoutes),
      },
      {
        path: 'servicos',
        loadChildren: () => import('./modules/servicos/servicos.routes').then(m => m.servicosRoutes),
      },
      {
        path: 'alunos',
        loadChildren: () => import('./modules/alunos/alunos.routes').then(m => m.alunosRoutes),
      },
      {
        path: 'turmas',
        loadChildren: () => import('./modules/turmas/turmas.routes').then(m => m.turmasRoutes),
      },
    ],
  },
  { path: '**', redirectTo: 'agenda' },
];