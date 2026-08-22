import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { tenantGuard } from './core/guards/tenant.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  {
    path: 'login',
    loadComponent: () => import('./modules/auth/pages/login.component').then(m => m.LoginComponent),
  },
  {
    path: 'recuperar-senha',
    loadComponent: () =>
      import('./modules/auth/pages/recuperar-senha.component').then(m => m.RecuperarSenhaComponent),
  },
  {
    path: 'redefinir-senha',
    loadComponent: () =>
      import('./modules/auth/pages/redefinir-senha.component').then(m => m.RedefinirSenhaComponent),
  },
  {
    path: '',
    canActivate: [authGuard, tenantGuard],
    loadComponent: () => import('./modules/layout/shell.component').then(m => m.ShellComponent),
    children: [
      { path: 'dashboard', loadChildren: () => import('./modules/dashboard/dashboard.routes').then(m => m.dashboardRoutes) },
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
      {
        path: 'usuarios',
        loadChildren: () => import('./modules/usuarios/usuarios.routes').then(m => m.usuariosRoutes),
      },
    ],
  },
  { path: '**', redirectTo: 'dashboard' },
];