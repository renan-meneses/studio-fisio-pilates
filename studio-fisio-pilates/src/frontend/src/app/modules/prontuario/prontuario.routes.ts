import { Routes } from '@angular/router';
import { PacientesPageComponent } from './pages/pacientes-page.component';
import { ProntuarioDetalheComponent } from './pages/prontuario-detalhe.component';

export const prontuarioRoutes: Routes = [
  { path: '', component: PacientesPageComponent },
  { path: ':id', component: ProntuarioDetalheComponent },
];