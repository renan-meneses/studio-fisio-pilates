import { Component, inject } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { TenantService } from '../../core/services/tenant.service';

interface NavItem {
  rota: string;
  rotulo: string;
  icone: string;
}

const NAV: NavItem[] = [
  { rota: '/agenda', rotulo: 'Agenda', icone: '📅' },
  { rota: '/prontuarios', rotulo: 'Prontuários', icone: '📋' },
  { rota: '/financeiro', rotulo: 'Financeiro', icone: '💰' },
  { rota: '/rh', rotulo: 'RH', icone: '👥' },
];

@Component({
  selector: 'clin-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="shell">
      <aside class="shell__sidebar">
        <div class="shell__brand">Clínica<span>SaaS</span></div>
        <nav class="shell__nav">
          @for (item of nav; track item.rota) {
            <a
              class="shell__link"
              [routerLink]="item.rota"
              routerLinkActive="shell__link--active"
            >
              <span aria-hidden="true">{{ item.icone }}</span>
              {{ item.rotulo }}
            </a>
          }
        </nav>
      </aside>

      <div class="shell__main">
        <header class="shell__topbar">
          <div class="shell__tenant">
            {{ tenant()?.tenantNome ?? 'Sem tenant' }}
          </div>
          <div class="shell__user">
            <span>{{ usuario }}</span>
            <button class="btn btn--outline" (click)="sair()">Sair</button>
          </div>
        </header>
        <main class="shell__content">
          <router-outlet />
        </main>
      </div>
    </div>
  `,
  styles: `
    .shell { display: flex; min-height: 100vh; }
    .shell__sidebar {
      width: 230px;
      background: #0f172a;
      color: #e2e8f0;
      padding: 1.25rem 1rem;
      display: flex;
      flex-direction: column;
      gap: 1.5rem;
      position: sticky;
      top: 0;
      height: 100vh;
    }
    .shell__brand { font-size: 1.1rem; font-weight: 800; color: #fff; padding: 0 0.5rem; }
    .shell__brand span { color: var(--clin-accent); }
    .shell__nav { display: flex; flex-direction: column; gap: 0.25rem; }
    .shell__link {
      display: flex;
      align-items: center;
      gap: 0.6rem;
      padding: 0.6rem 0.75rem;
      border-radius: 8px;
      color: #cbd5e1;
      font-weight: 500;
      transition: background 0.15s ease;

      &:hover { background: rgba(255, 255, 255, 0.06); color: #fff; }
      &--active { background: var(--clin-primary); color: #fff; }
    }
    .shell__main { flex: 1; display: flex; flex-direction: column; min-width: 0; }
    .shell__topbar {
      height: 56px;
      background: var(--clin-surface);
      border-bottom: 1px solid var(--clin-border);
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 0 1.5rem;
    }
    .shell__tenant { font-weight: 700; color: var(--clin-primary-dark); }
    .shell__user { display: flex; align-items: center; gap: 0.75rem; font-size: 0.9rem; }
    .shell__content { padding: 1.5rem; flex: 1; }
  `,
})
export class ShellComponent {
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly tenantService = inject(TenantService);

  readonly nav = NAV;
  readonly tenant = this.tenantService.tenant;

  usuario = '';

  constructor() {
    const nome = localStorage.getItem('clinica.user');
    this.usuario = nome ? JSON.parse(nome).nome : '';
    this.router.events
      .pipe(filter(evento => evento instanceof NavigationEnd))
      .subscribe(() => {
        const atual = localStorage.getItem('clinica.user');
        this.usuario = atual ? JSON.parse(atual).nome : '';
      });
  }

  sair(): void {
    this.auth.logout();
    void this.router.navigate(['/login']);
  }
}