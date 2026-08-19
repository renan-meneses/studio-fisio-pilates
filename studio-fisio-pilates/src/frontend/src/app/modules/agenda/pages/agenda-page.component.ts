import { Component, inject, signal } from '@angular/core';
import { PageHeaderComponent } from '../../../shared/components/page-header.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state.component';
import { AgendamentoFormComponent } from '../components/agendamento-form.component';
import { AgendaService } from '../services/agenda.service';
import { Agendamento, rotuloTipoAula, rotuloTipoSessao } from '../models/agendamento.model';

const STATUS_BADGE: Record<string, string> = {
  Agendado: 'badge--muted',
  Confirmado: 'badge--info',
  Realizado: 'badge--success',
  Faltou: 'badge--danger',
  Cancelado: 'badge--danger',
};

@Component({
  selector: 'clin-agenda-page',
  standalone: true,
  imports: [PageHeaderComponent, EmptyStateComponent, AgendamentoFormComponent],
  template: `
    <clin-page-header titulo="Agenda" subtitulo="Agendamentos do período selecionado">
      <input
        type="date"
        class="agenda__filter"
        [value]="data()"
        (change)="mudarData($event)"
      />
      <button class="btn btn--primary" (click)="mostrarForm.set(true)">+ Novo agendamento</button>
    </clin-page-header>

    @if (mostrarForm()) {
      <clin-agendamento-form
        class="agenda__form"
        (salvo)="recarregar()"
        (cancelar)="mostrarForm.set(false)"
      />
    }

    <div class="card">
      @if (carregando()) {
        <p class="agenda__hint">Carregando…</p>
      } @else if (agendamentos().length === 0) {
        <clin-empty-state
          icone="🗓️"
          titulo="Nenhum agendamento neste dia"
          hint="Clique em “Novo agendamento” para criar horários."
        />
      } @else {
        <table class="data-table">
          <thead>
            <tr>
              <th>Horário</th>
              <th>Paciente</th>
              <th>Profissional</th>
              <th>Sessão</th>
              <th>Aula</th>
              <th>Turma</th>
              <th>Status</th>
              <th>Ações</th>
            </tr>
          </thead>
          <tbody>
            @for (ag of agendamentos(); track ag.id) {
              <tr>
                <td>{{ horarioDe(ag) }}</td>
                <td>{{ ag.pacienteNome }}</td>
                <td>{{ ag.profissionalNome }}</td>
                <td>{{ rotuloTipoSessao(ag.tipoSessao) }}</td>
                <td>{{ rotuloTipoAula(ag.tipoAula) }}</td>
                <td>
                  @if (ag.turmaNome) {
                    <span class="badge badge--info">{{ ag.turmaNome }}</span>
                  } @else {
                    <span>—</span>
                  }
                </td>
                <td><span class="badge {{ badgeDe(ag.status) }}">{{ ag.status }}</span></td>
                <td>
                  <div class="agenda__actions">
                    @if (ag.status === 'Agendado' || ag.status === 'Confirmado') {
                      @if (ag.status === 'Agendado') {
                        <button class="btn btn--outline" (click)="confirmar(ag)">Confirmar</button>
                      }
                      <button class="btn btn--primary" (click)="presenca(ag)">Presença</button>
                      <button class="btn btn--danger" (click)="cancelar(ag)">Cancelar</button>
                    } @else {
                      <span class="agenda__hint">—</span>
                    }
                  </div>
                </td>
              </tr>
            }
          </tbody>
        </table>
      }
    </div>
  `,
  styles: `
    .agenda__filter {
      padding: 0.55rem 0.75rem;
      border: 1px solid var(--clin-border);
      border-radius: 8px;
      font: inherit;
      background: var(--clin-surface);
      color: var(--clin-text);
    }
    .agenda__form { margin-bottom: 1rem; }
    .agenda__actions { display: flex; gap: 0.4rem; flex-wrap: wrap; }
    .agenda__hint { color: var(--clin-text-muted); text-align: center; padding: 1rem 0; }
  `,
})
export class AgendaPageComponent {
  private readonly agenda = inject(AgendaService);

  readonly data = signal(this.hoje());
  readonly agendamentos = signal<Agendamento[]>([]);
  readonly carregando = signal(false);
  readonly mostrarForm = signal(false);

  readonly rotuloTipoSessao = rotuloTipoSessao;
  readonly rotuloTipoAula = rotuloTipoAula;

  constructor() {
    this.recarregar();
  }

  hoje(): string {
    return new Date().toISOString().slice(0, 10);
  }

  horarioDe(ag: Agendamento): string {
    const inicio = new Date(ag.dataHoraInicio).toLocaleTimeString('pt-BR', {
      hour: '2-digit',
      minute: '2-digit',
      hour12: false,
    });
    const fim = new Date(ag.dataHoraFim).toLocaleTimeString('pt-BR', {
      hour: '2-digit',
      minute: '2-digit',
      hour12: false,
    });
    return `${inicio} – ${fim}`;
  }

  mudarData(evento: Event): void {
    this.data.set((evento.target as HTMLInputElement).value);
    this.recarregar();
  }

  recarregar(): void {
    this.carregando.set(true);
    this.agenda.listar(this.data(), this.data()).subscribe({
      next: lista => {
        this.agendamentos.set(lista);
        this.carregando.set(false);
        this.mostrarForm.set(false);
      },
      error: () => {
        this.carregando.set(false);
        alert('Falha ao carregar a agenda.');
      },
    });
  }

  badgeDe(status: string): string {
    return STATUS_BADGE[status] ?? 'badge--muted';
  }

  confirmar(ag: Agendamento): void {
    this.agenda.confirmar(ag.id).subscribe(() => this.recarregar());
  }

  presenca(ag: Agendamento): void {
    this.agenda.registrarPresenca(ag.id).subscribe(() => this.recarregar());
  }

  cancelar(ag: Agendamento): void {
    const motivo = prompt('Motivo do cancelamento:');
    if (motivo === null) {
      return;
    }
    this.agenda.cancelar(ag.id, motivo).subscribe(() => this.recarregar());
  }
}
