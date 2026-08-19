import { Routes } from '@angular/router';
import { FinanceiroPageComponent } from './pages/financeiro-page.component';
import { ContasPageComponent } from './pages/contas-page.component';

export const financeiroRoutes: Routes = [
  { path: '', component: FinanceiroPageComponent },
  { path: 'contas', component: ContasPageComponent },
];