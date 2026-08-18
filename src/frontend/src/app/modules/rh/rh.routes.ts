import { Routes } from '@angular/router';
import { FolhaPageComponent } from './pages/folha-page.component';
import { PontoPageComponent } from './pages/ponto-page.component';

export const rhRoutes: Routes = [
  { path: '', redirectTo: 'folha', pathMatch: 'full' },
  { path: 'folha', component: FolhaPageComponent },
  { path: 'ponto', component: PontoPageComponent },
];