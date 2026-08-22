import { Component, inject } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { TenantService } from '../../core/services/tenant.service';
import { ThemeService } from '../../core/services/theme.service';

interface NavItem {
  rota: string;
  rotulo: string;
  icone: string;
}

const NAV: NavItem[] = [
  { rota: '/dashboard', rotulo: 'Dashboard', icone: '📊' },
  { rota: '/agenda', rotulo: 'Agenda', icone: '📅' },
  { rota: '/alunos', rotulo: 'Alunos', icone: '🎓' },
  { rota: '/turmas', rotulo: 'Turmas', icone: '🧘' },
  { rota: '/planos', rotulo: 'Planos', icone: '💳' },
  { rota: '/servicos', rotulo: 'Serviços', icone: '🧩' },
  { rota: '/prontuarios', rotulo: 'Prontuários', icone: '📋' },
  { rota: '/financeiro', rotulo: 'Financeiro', icone: '💰' },
  { rota: '/financeiro/contas', rotulo: 'Contas a pagar', icone: '🧾' },
  { rota: '/rh', rotulo: 'Folha', icone: '👥' },
  { rota: '/rh/ponto', rotulo: 'Ponto', icone: '🕐' },
  { rota: '/usuarios', rotulo: 'Usuários', icone: '🔐' },
];

@Component({
  selector: 'clin-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="flex min-h-screen">
      <aside
        class="sticky top-0 flex h-screen w-60 shrink-0 flex-col gap-6 overflow-y-auto border-r border-slate-800/60 bg-slate-900 px-3 py-5"
      >
        <div class="flex items-center gap-2 px-2">
          <div
            class="flex size-8 items-center justify-center rounded-xl bg-gradient-to-br from-teal-400 to-teal-600 text-base shadow-md shadow-teal-500/20"
          >
            ✚
          </div>
          <span class="text-[15px] font-extrabold tracking-tight text-white">
            Clínica<span class="text-teal-400">SaaS</span>
          </span>
        </div>

        <nav class="flex flex-col gap-1" aria-label="Navegação principal">
          @for (item of nav; track item.rota) {
            <a
              class="flex items-center gap-2.5 rounded-lg px-3 py-2 text-sm font-medium text-slate-300 transition-colors hover:bg-white/5 hover:text-white"
              [routerLink]="item.rota"
              routerLinkActive="!bg-teal-400/10 !text-teal-300 ring-1 ring-inset ring-teal-400/25"
            >
              <span aria-hidden="true" class="w-5 text-center text-[15px]">{{ item.icone }}</span>
              {{ item.rotulo }}
            </a>
          }
        </nav>

        <div class="mt-auto rounded-xl bg-slate-800/50 p-3 text-xs leading-relaxed text-slate-400">
          Multitenant · Fisio & Pilates
        </div>
      </aside>

      <div class="flex min-w-0 flex-1 flex-col">
        <header
          class="sticky top-0 z-20 flex h-14 items-center justify-between border-b border-slate-200/70 bg-white/80 px-6 backdrop-blur dark:border-slate-800 dark:bg-slate-900/80"
        >
          <div class="flex items-center gap-2 text-sm font-bold text-teal-700 dark:text-teal-300">
            <span class="inline-block size-2 rounded-full bg-teal-500"></span>
            {{ tenant()?.tenantNome ?? 'Sem tenant' }}
          </div>
          <div class="flex items-center gap-3 text-sm">
            <button
              class="btn btn--ghost !px-2.5"
              (click)="alternarTema()"
              [title]="theme.tema() === 'Escuro' ? 'Modo claro' : 'Modo escuro'"
              aria-label="Alternar tema"
            >
              {{ theme.tema() === 'Escuro' ? '☀️' : '🌙' }}
            </button>
            <div class="flex items-center gap-2">
              <div
                class="flex size-8 items-center justify-center rounded-full bg-gradient-to-br from-slate-600 to-slate-800 text-xs font-bold uppercase text-white"
                aria-hidden="true"
              >
                {{ iniciais() }}
              </div>
              <span class="hidden font-medium sm:inline">{{ usuario }}</span>
            </div>
            <button class="btn btn--outline btn--sm" (click)="sair()">Sair</button>
          </div>
        </header>
        <main class="flex-1 p-6 animate-fade-in">
          <router-outlet />
        </main>
      </div>
    </div>
  `,
})
export class ShellComponent {
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly tenantService = inject(TenantService);
  readonly theme = inject(ThemeService);

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

  iniciais(): string {
    return this.usuario
      .split(/\s+/)
      .slice(0, 2)
      .map(parte => parte[0] ?? '')
      .join('');
  }

  sair(): void {
    this.auth.logout();
    void this.router.navigate(['/login']);
  }

  alternarTema(): void {
    const novo = this.theme.alternar();
    this.auth.atualizarTema(novo).subscribe({
      error: () => console.warn('Não foi possível persistir o tema no usuário.'),
    });
  }
}
