import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PageHeaderComponent } from '../../../shared/components/page-header.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state.component';
import { ProntuarioService } from '../services/prontuario.service';
import { PacienteResumo } from '../models/prontuario.model';

@Component({
  selector: 'clin-pacientes-page',
  standalone: true,
  imports: [PageHeaderComponent, EmptyStateComponent, RouterLink],
  template: `
    <clin-page-header
      titulo="Prontuários"
      subtitulo="Pacientes cadastrados e seus prontuários eletrônicos"
    />

    <div class="mb-4 flex items-center gap-3">
      <input
        class="w-full max-w-[360px] rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-800
          placeholder:text-slate-400 transition-colors focus:border-teal-500 focus:outline-none focus:ring-2
          focus:ring-teal-500/25 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100 dark:focus:border-teal-400"
        placeholder="Buscar paciente…"
        (input)="buscar($event)"
      />
    </div>

    @if (pacientes().length === 0) {
      <div class="card">
        <clin-empty-state
          icone="🧑‍⚕️"
          titulo="Nenhum paciente encontrado"
          hint="Ajuste o termo de busca ou adicione pacientes pelo cadastro."
        />
      </div>
    } @else {
      <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        @for (pac of pacientes(); track pac.id) {
          <article
            class="flex flex-col gap-2 rounded-2xl border border-slate-200/80 bg-white p-5 shadow-card
              transition-shadow hover:shadow-lg dark:border-slate-800 dark:bg-slate-900"
          >
            <div class="flex items-start justify-between gap-2">
              <h3 class="text-base font-bold text-slate-800 dark:text-slate-100">{{ pac.nome }}</h3>
              <span class="badge {{ pac.ativo ? 'badge--success' : 'badge--danger' }}">
                {{ pac.ativo ? 'Ativo' : 'Inativo' }}
              </span>
            </div>
            <p class="text-sm text-slate-500 dark:text-slate-400">Telefone: {{ pac.telefone ?? '—' }}</p>
            <div class="mt-auto pt-1">
              <a class="btn btn--outline btn--sm" [routerLink]="['/prontuarios', pac.id]">
                Abrir prontuário
              </a>
            </div>
          </article>
        }
      </div>
    }
  `,
})
export class PacientesPageComponent {
  private readonly prontuario = inject(ProntuarioService);

  readonly pacientes = signal<PacienteResumo[]>([]);

  constructor() {
    this.recarregar();
  }

  recarregar(termo?: string): void {
    this.prontuario.listarPacientes(termo).subscribe({
      next: lista => this.pacientes.set(lista),
      error: () => alert('Falha ao carregar pacientes.'),
    });
  }

  buscar(evento: Event): void {
    const termo = (evento.target as HTMLInputElement).value;
    this.recarregar(termo || undefined);
  }
}