import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PageHeaderComponent } from '../../../shared/components/page-header.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state.component';
import { TurmaService } from '../services/turma.service';
import { DIAS_SEMANA, horarioCurto, rotuloDia, Turma, TurmaHorario, WaitlistEntry } from '../models/turma.model';
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

    <form class="card" [formGroup]="form" (ngSubmit)="salvar()">
      <h3 class="mb-4 text-base font-semibold text-slate-800 dark:text-slate-100">Nova turma</h3>
      <div class="grid gap-x-4 sm:grid-cols-2">
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
        <div class="form-group">
          <label>Capacidade por horário</label>
          <input formControlName="capacidade" type="number" min="1" max="50" />
        </div>
        <div class="form-group sm:col-span-2">
          <label>Profissional</label>
          <select formControlName="profissionalId">
            <option value="">Sem profissional fixo</option>
            @for (p of profissionais(); track p.id) {
              <option [value]="p.id">{{ p.nome }} ({{ p.especialidades }})</option>
            }
          </select>
        </div>

        <div class="form-group sm:col-span-2">
          <label>Horários da turma</label>
          <div class="mb-2 flex flex-col gap-1.5">
            @for (linha of linhasHorarios; track linha; let i = $index) {
              <div class="grid grid-cols-2 items-center gap-1.5 sm:grid-cols-[1fr_1fr_1fr_auto]">
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
          <button type="button" class="btn btn--outline btn--sm self-start" (click)="adicionarLinha()">
            + Adicionar horário
          </button>
        </div>
      </div>

      @if (erro()) {
        <p class="field-error mt-2">{{ erro() }}</p>
      }
      <div class="mt-2 flex justify-end">
        <button type="submit" class="btn btn--primary" [disabled]="form.invalid || carregando()">
          {{ carregando() ? 'Salvando…' : 'Salvar turma' }}
        </button>
      </div>
    </form>

    @if (carregando()) {
      <p class="py-4 text-center text-sm text-slate-500 dark:text-slate-400">Carregando…</p>
    } @else if (turmas().length === 0) {
      <clin-empty-state icone="🧘" titulo="Nenhuma turma cadastrada" hint="Crie uma turma e defina os horários semanais." />
    } @else {
      <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        @for (t of turmas(); track t.id) {
          <div class="card flex flex-col gap-2.5 transition-shadow hover:shadow-lg">
            <div class="flex items-start justify-between gap-4">
              <div>
                <h3 class="text-base font-bold text-slate-800 dark:text-slate-100">{{ t.nome }}</h3>
                <p class="m-0 mt-0.5 text-sm text-slate-500 dark:text-slate-400">
                  {{ rotuloSessao(t.tipoSessao) }} · {{ t.profissionalNome ?? 'Sem profissional fixo' }}
                </p>
              </div>
              <span class="badge badge--info">{{ t.capacidade }} vagas · {{ t.horarios.length }} horário(s)</span>
            </div>
            <ul class="m-0 flex list-none flex-col gap-1.5 p-0 text-sm">
              @for (h of t.horarios; track h.id) {
                <li class="flex items-center justify-between gap-2 rounded-lg bg-slate-100 px-2.5 py-1.5 dark:bg-slate-800/70">
                  <span>{{ rotuloDia(h.diaSemana) }} — {{ horarioCurto(h.horaInicio) }} às {{ horarioCurto(h.horaFim) }}</span>
                  <button
                    class="cursor-pointer border-none bg-transparent p-0 text-xs text-red-600 opacity-70 transition-opacity hover:opacity-100 dark:text-red-400"
                    title="Remover horário"
                    (click)="removerHorario(t, h)"
                  >✕</button>
                </li>
              }
            </ul>
            <div class="mt-auto flex flex-col gap-1.5 border-t border-slate-200 pt-2.5 dark:border-slate-800">
              <h4 class="m-0 text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">Lista de espera</h4>
              @if (waitlistDe(t.id).length === 0) {
                <p class="m-0 text-sm text-slate-500 dark:text-slate-400">Nenhum aluno na fila.</p>
              } @else {
                <ul class="m-0 flex list-none flex-col gap-1.5 p-0 text-sm">
                  @for (entrada of waitlistDe(t.id); track entrada.id) {
                    <li class="flex items-center justify-between gap-2 rounded-lg bg-slate-100 px-2.5 py-1.5 dark:bg-slate-800/70">
                      <span>{{ $index + 1 }}. {{ entrada.pacienteNome }}</span>
                      <button
                        class="cursor-pointer border-none bg-transparent p-0 text-xs text-red-600 opacity-70 transition-opacity hover:opacity-100 dark:text-red-400"
                        title="Remover da fila"
                        (click)="sairDaFila(t.id, entrada.id)"
                      >✕</button>
                    </li>
                  }
                </ul>
              }
              <div class="flex gap-1.5">
                <select
                  #seletorAluno (change)="null"
                  class="w-full min-w-0 flex-1 rounded-md border border-slate-300 bg-white px-2 py-1.5 text-sm text-slate-800 focus:border-teal-500 focus:outline-none focus:ring-2 focus:ring-teal-500/25 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
                >
                  <option value="">Selecionar aluno…</option>
                  @for (p of pacientes(); track p.id) {
                    <option [value]="p.id">{{ p.nome }}</option>
                  }
                </select>
                <button
                  type="button"
                  class="btn btn--outline btn--sm"
                  [disabled]="!seletorAluno.value"
                  (click)="entrarNaFila(t.id, seletorAluno.value); seletorAluno.value = ''"
                >
                  Entrar na fila
                </button>
              </div>
            </div>
          </div>
        }
      </div>
    }
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
  readonly pacientes = signal<{ id: string; nome: string }[]>([]);
  readonly waitlists = signal<Record<string, WaitlistEntry[]>>({});
  readonly carregando = signal(false);
  readonly erro = signal('');

  readonly linhasHorarios: LinhaHorario[] = [];

  readonly form = this.fb.group({
    nome: ['', Validators.required],
    tipoSessao: ['PilatesSolo', Validators.required],
    profissionalId: [''],
    capacidade: [8],
  });

  constructor() {
    this.recarregar();
  }

  recarregar(): void {
    this.carregando.set(true);
    this.service.listar().subscribe(lista => {
      this.turmas.set(lista);
      lista.forEach(t =>
        this.service.waitlist(t.id).subscribe(entradas => this.carregarWaitlist(t.id, entradas)),
      );
    });
    this.agenda.listarProfissionais().subscribe(lista => this.profissionais.set(lista));
    this.agenda.listarPacientes().subscribe(lista => this.pacientes.set(lista));
    this.carregando.set(false);
  }

  carregarWaitlist(turmaId: string, entradas: WaitlistEntry[]): void {
    this.waitlists.update(atual => ({ ...atual, [turmaId]: entradas }));
  }

  waitlistDe(turmaId: string): WaitlistEntry[] {
    return this.waitlists()[turmaId] ?? [];
  }

  entrarNaFila(turmaId: string, pacienteId: string): void {
    if (!pacienteId) {
      return;
    }
    this.service.entrarWaitlist(turmaId, pacienteId).subscribe({
      next: () => this.recarregar(),
      error: (erro: Error) => alert(erro.message),
    });
  }

  sairDaFila(turmaId: string, entradaId: string): void {
    this.service.sairWaitlist(turmaId, entradaId).subscribe({
      next: () => this.recarregar(),
      error: (erro: Error) => alert(erro.message),
    });
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
      capacidade: this.form.value.capacidade ?? 8,
      horarios: horarios.length ? horarios : undefined,
    }).subscribe({
      next: () => {
        this.carregando.set(false);
        this.form.reset({ tipoSessao: 'PilatesSolo', capacidade: 8 });
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
