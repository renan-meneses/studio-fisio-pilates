import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PageHeaderComponent } from '../../../shared/components/page-header.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state.component';
import { TurmaService } from '../services/turma.service';
import { DIAS_SEMANA, horarioCurto, rotuloDia, Turma, TurmaHorario } from '../models/turma.model';
import { AgendaService } from '../../agenda/services/agenda.service';
import { TIPOS_SESSAO } from '../../agenda/models/agendamento.model';

interface LinhaHorario {
  diaSemana: number;
  horaInicio: string;
  horaFim: string;
}

@Component({
  selector: 'clin-turmas-page',
  standalone: true,
  imports: [PageHeaderComponent, EmptyStateComponent, ReactiveFormsModule],
  template: `
    <clin-page-header titulo="Turmas" subtitulo="Turmas de Pilates e horários semanais" />

    <form class="card form" [formGroup]="form" (ngSubmit)="salvar()">
      <h3 class="form__title">Nova turma</h3>
      <div class="form-grid">
        <div class="form-group">
          <label>Nome *</label>
          <input formControlName="nome" placeholder="Ex.: Turma Segunda e Quarta 18h" />
        </div>
        <div class="form-group">
          <label>Tipo de sessão *</label>
          <select formControlName="tipoSessao">
            @for (t of TIPOS_SESSAO; track t.valor) {
              <option [value]="t.valor">{{ t.rotulo }}</option>
            }
          </select>
        </div>
        <div class="form-group form-group--full">
          <label>Profissional</label>
          <select formControlName="profissionalId">
            <option value="">Sem profissional fixo</option>
            @for (p of profissionais(); track p.id) {
              <option [value]="p.id">{{ p.nome }} ({{ p.especialidades }})</option>
            }
          </select>
        </div>

        <div class="form-group form-group--full">
          <label>Horários da turma</label>
          <div class="horarios">
            @for (linha of linhasHorarios; track linha; let i = $index) {
              <div class="horario-linha">
                <select [value]="linha.diaSemana" (change)="linha.diaSemana = diaDe($event)">
                  @for (d of DIAS_SEMANA; track d.valor) {
                    <option [value]="d.valor">{{ d.rotulo }}</option>
                  }
                </select>
                <input type="time" [value]="linha.horaInicio" (change)="linha.horaInicio = horaDe($event)" />
                <input type="time" [value]="linha.horaFim" (change)="linha.horaFim = horaDe($event)" />
                <button type="button" class="btn btn--danger btn--sm" (click)="removerLinha(i)">✕</button>
              </div>
            }
          </div>
          <button type="button" class="btn btn--outline btn--sm" (click)="adicionarLinha()">
            + Adicionar horário
          </button>
        </div>
      </div>

      @if (erro()) {
        <p class="form__error">{{ erro() }}</p>
      }
      <div class="form__actions">
        <button type="submit" class="btn btn--primary" [disabled]="form.invalid || carregando()">
          {{ carregando() ? 'Salvando…' : 'Salvar turma' }}
        </button>
      </div>
    </form>

    @if (carregando()) {
      <p class="hint">Carregando…</p>
    } @else if (turmas().length === 0) {
      <clin-empty-state icone="🧘" titulo="Nenhuma turma cadastrada" hint="Crie uma turma e defina os horários semanais." />
    } @else {
      <div class="turmas">
        @for (t of turmas(); track t.id) {
          <div class="card turma">
            <div class="turma__head">
              <div>
                <h3 class="turma__nome">{{ t.nome }}</h3>
                <p class="turma__meta">
                  {{ rotuloSessao(t.tipoSessao) }} · {{ t.profissionalNome ?? 'Sem profissional fixo' }}
                </p>
              </div>
              <span class="badge badge--info">{{ t.horarios.length }} horário(s)</span>
            </div>
            <ul class="turma__horarios">
              @for (h of t.horarios; track h.id) {
                <li>
                  <span>{{ rotuloDia(h.diaSemana) }} — {{ horarioCurto(h.horaInicio) }} às {{ horarioCurto(h.horaFim) }}</span>
                  <button class="chip__remover" title="Remover horário" (click)="removerHorario(t, h)">✕</button>
                </li>
              }
            </ul>
          </div>
        }
      </div>
    }
  `,
  styles: `
    .form__title { margin-bottom: 1rem; font-size: 1.05rem; }
    .form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 0 1rem; }
    .form-group--full { grid-column: 1 / -1; }
    .form__actions { display: flex; justify-content: flex-end; }
    .form__error { color: var(--clin-danger); font-size: 0.85rem; margin: 0.5rem 0; }
    .hint { color: var(--clin-text-muted); text-align: center; padding: 1rem 0; }
    .horarios { display: flex; flex-direction: column; gap: 0.4rem; margin-bottom: 0.5rem; }
    .horario-linha {
      display: grid;
      grid-template-columns: 1fr 1fr 1fr auto;
      gap: 0.4rem;
      align-items: center;
    }
    .turmas { display: grid; grid-template-columns: repeat(auto-fill, minmax(320px, 1fr)); gap: 1rem; }
    .turma { display: flex; flex-direction: column; gap: 0.6rem; }
    .turma__nome { font-size: 1.05rem; }
    .turma__meta { margin: 0.25rem 0 0; color: var(--clin-text-muted); font-size: 0.85rem; }
    .turma__head { display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem; }
    .turma__horarios { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 0.3rem; }
    .turma__horarios li {
      display: flex;
      align-items: center;
      justify-content: space-between;
      background: var(--clin-surface-alt);
      border-radius: 8px;
      padding: 0.45rem 0.7rem;
      font-size: 0.85rem;
    }
    .chip__remover {
      border: none;
      background: transparent;
      color: var(--clin-danger);
      cursor: pointer;
      font-size: 0.8rem;
      padding: 0;
      opacity: 0.7;
    }
    .chip__remover:hover { opacity: 1; }
  `,
})
export class TurmasPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(TurmaService);
  private readonly agenda = inject(AgendaService);

  readonly DIAS_SEMANA = DIAS_SEMANA;
  readonly TIPOS_SESSAO = TIPOS_SESSAO;

  readonly turmas = signal<Turma[]>([]);
  readonly profissionais = signal<{ id: string; nome: string; especialidades: string }[]>([]);
  readonly carregando = signal(false);
  readonly erro = signal('');

  readonly linhasHorarios: LinhaHorario[] = [];

  readonly form = this.fb.group({
    nome: ['', Validators.required],
    tipoSessao: ['PilatesSolo', Validators.required],
    profissionalId: [''],
  });

  constructor() {
    this.recarregar();
  }

  recarregar(): void {
    this.carregando.set(true);
    this.service.listar().subscribe(lista => this.turmas.set(lista));
    this.agenda.listarProfissionais().subscribe(lista => this.profissionais.set(lista));
    this.carregando.set(false);
  }

  rotuloSessao(tipo: string): string {
    return TIPOS_SESSAO.find(t => t.valor === tipo)?.rotulo ?? tipo;
  }

  rotuloDia = rotuloDia;
  horarioCurto = horarioCurto;

  adicionarLinha(): void {
    this.linhasHorarios.push({ diaSemana: 1, horaInicio: '18:00', horaFim: '19:00' });
  }

  removerLinha(indice: number): void {
    this.linhasHorarios.splice(indice, 1);
  }

  diaDe(evento: Event): number {
    return Number((evento.target as HTMLSelectElement).value);
  }

  horaDe(evento: Event): string {
    return (evento.target as HTMLInputElement).value;
  }

  salvar(): void {
    if (this.form.invalid) {
      return;
    }
    const horarios = this.linhasHorarios
      .filter(l => l.horaInicio && l.horaFim && l.horaInicio < l.horaFim)
      .map(l => ({ diaSemana: l.diaSemana, horaInicio: `${l.horaInicio}:00`, horaFim: `${l.horaFim}:00` }));

    this.carregando.set(true);
    this.erro.set('');
    this.service.criar({
      nome: this.form.value.nome!,
      tipoSessao: this.form.value.tipoSessao as 'PilatesSolo',
      profissionalId: this.form.value.profissionalId || undefined,
      horarios: horarios.length ? horarios : undefined,
    }).subscribe({
      next: () => {
        this.carregando.set(false);
        this.form.reset({ tipoSessao: 'PilatesSolo' });
        this.linhasHorarios.length = 0;
        this.recarregar();
      },
      error: (erro: Error) => {
        this.carregando.set(false);
        this.erro.set(erro.message);
      },
    });
  }

  removerHorario(turma: Turma, horario: TurmaHorario): void {
    this.service.removerHorario(turma.id, horario.id).subscribe({
      next: () => this.recarregar(),
      error: (erro: Error) => alert(erro.message),
    });
  }
}