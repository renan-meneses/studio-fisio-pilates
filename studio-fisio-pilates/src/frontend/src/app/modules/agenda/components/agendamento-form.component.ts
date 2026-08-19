import { Component, inject, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TimePickerModalComponent } from '../../../shared/components/time-picker-modal.component';
import { CriarAgendamentoRequest, ehPilates, TIPOS_AULA, TIPOS_SESSAO, TipoSessao } from '../models/agendamento.model';
import { AgendaService } from '../services/agenda.service';
import { horarioCurto, rotuloDia, Turma, TurmaHorario } from '../../turmas/models/turma.model';

type CampoHorario = 'horaInicio' | 'horaFim';

function diaSemanaDe(dataIso: string): number {
  const dia = new Date(`${dataIso}T12:00:00`).getDay();
  return ((dia + 6) % 7) + 1;
}

@Component({
  selector: 'clin-agendamento-form',
  standalone: true,
  imports: [ReactiveFormsModule, TimePickerModalComponent],
  template: `
    <form class="card form" [formGroup]="form" (ngSubmit)="salvar()">
      <h3 class="form__title">Novo agendamento</h3>
      <div class="form-grid">
        <div class="form-group">
          <label>Paciente *</label>
          <select formControlName="pacienteId">
            <option value="" disabled>Selecione…</option>
            @for (p of pacientes(); track p.id) {
              <option [value]="p.id">{{ p.nome }}</option>
            }
          </select>
        </div>

        <div class="form-group">
          <label>Profissional *</label>
          <select formControlName="profissionalId">
            <option value="" disabled>Selecione…</option>
            @for (p of profissionais(); track p.id) {
              <option [value]="p.id">{{ p.nome }} ({{ p.especialidades }})</option>
            }
          </select>
        </div>

        <div class="form-group">
          <label>Data *</label>
          <input type="date" formControlName="data" (change)="limparTurmaSeDiaMudou()" />
        </div>

        <div class="form-group">
          <label>Tipo de sessão *</label>
          <select formControlName="tipoSessao" (change)="limparTurmaSeTipoMudou()">
            @for (t of TIPOS_SESSAO; track t.valor) {
              <option [value]="t.valor">{{ t.rotulo }}</option>
            }
          </select>
        </div>

        <div class="form-group">
          <label>Tipo de aula *</label>
          <select formControlName="tipoAula">
            @for (t of TIPOS_AULA; track t.valor) {
              <option [value]="t.valor">{{ t.rotulo }}</option>
            }
          </select>
        </div>

        <div class="form-group">
          <label>Turma</label>
          <select formControlName="turmaId" [disabled]="!ehPilates(tipoSessao())">
            <option value="">Sem turma</option>
            @for (t of turmasDoTipo(); track t.id) {
              <option [value]="t.id">{{ t.nome }}</option>
            }
          </select>
        </div>

        @if (ehPilates(tipoSessao()) && turmaSelecionada()) {
          <div class="form-group form-group--full">
            <label class="turma__label">
              Horários da turma — {{ rotuloDia(diaSemana()) }} ({{ turmaSelecionada()!.nome }})
            </label>
            @if (horariosDoDia().length === 0) {
              <p class="turma__vazio">Esta turma não tem horário nesse dia.</p>
            } @else {
              <div class="chip">
                @for (h of horariosDoDia(); track h.id) {
                  <button type="button" class="chip__item" (click)="aplicarHorario(h)">
                    {{ horarioCurto(h.horaInicio) }} – {{ horarioCurto(h.horaFim) }}
                  </button>
                }
              </div>
            }
          </div>
        }

        <div class="form-group">
          <label>Início</label>
          <input
            class="time-input"
            [value]="form.value.horaInicio ?? ''"
            placeholder="00:00"
            readonly
            (click)="abrirHorario('horaInicio')"
          />
        </div>

        <div class="form-group">
          <label>Fim</label>
          <input
            class="time-input"
            [value]="form.value.horaFim ?? ''"
            placeholder="00:00"
            readonly
            (click)="abrirHorario('horaFim')"
          />
        </div>

        <div class="form-group">
          <label>Valor da sessão (R$)</label>
          <input type="number" step="0.01" min="0" formControlName="valorSessao" />
        </div>

        <div class="form-group form-group--full">
          <label>Observações</label>
          <input formControlName="observacoes" />
        </div>
      </div>

      @if (erro()) {
        <p class="form__error">{{ erro() }}</p>
      }

      <div class="form__actions">
        <button type="button" class="btn btn--outline" (click)="cancelar.emit()">Fechar</button>
        <button type="submit" class="btn btn--primary" [disabled]="form.invalid || carregando()">
          {{ carregando() ? 'Salvando…' : 'Salvar' }}
        </button>
      </div>
    </form>

    @if (horarioAberto()) {
      <clin-time-picker-modal
        [valor]="form.value[horarioAberto()!] ?? ''"
        (selecionado)="escolherHorario($event)"
        (fechado)="horarioAberto.set(null)"
      />
    }
  `,
  styles: `
    .form__title { margin-bottom: 1rem; font-size: 1.05rem; }
    .form-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 0 1rem;
    }
    .form-group--full { grid-column: 1 / -1; }
    .form__actions { display: flex; justify-content: flex-end; gap: 0.6rem; }
    .form__error { color: var(--clin-danger); font-size: 0.85rem; margin: 0.5rem 0; }
    .time-input {
      background: var(--clin-surface);
      cursor: pointer;
      font-variant-numeric: tabular-nums;
    }
    .time-input:focus { background: var(--clin-surface); }
    .turma__label { font-weight: 600; }
    .turma__vazio { margin: 0.25rem 0 0; color: var(--clin-text-muted); font-size: 0.85rem; }
    .chip { display: flex; flex-wrap: wrap; gap: 0.4rem; margin-top: 0.4rem; }
    .chip__item {
      border: none;
      cursor: pointer;
      background: var(--clin-primary-light);
      color: var(--clin-primary-dark);
      padding: 0.35rem 0.7rem;
      border-radius: 999px;
      font: inherit;
      font-size: 0.85rem;
      font-weight: 600;

      &:hover { filter: brightness(0.95); }
    }
  `,
})
export class AgendamentoFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly agenda = inject(AgendaService);

  readonly salvo = output<boolean>();
  readonly cancelar = output<void>();

  readonly pacientes = signal<{ id: string; nome: string }[]>([]);
  readonly profissionais = signal<{ id: string; nome: string; especialidades: string }[]>([]);
  readonly turmas = signal<Turma[]>([]);
  readonly carregando = signal(false);
  readonly erro = signal('');
  readonly horarioAberto = signal<CampoHorario | null>(null);

  readonly TIPOS_SESSAO = TIPOS_SESSAO;
  readonly TIPOS_AULA = TIPOS_AULA;

  readonly form = this.fb.group({
    pacienteId: ['', Validators.required],
    profissionalId: ['', Validators.required],
    data: [this.hoje(), Validators.required],
    tipoSessao: ['PilatesSolo', Validators.required],
    tipoAula: ['Plano', Validators.required],
    turmaId: [''],
    horaInicio: ['', Validators.required],
    horaFim: ['', Validators.required],
    valorSessao: [null as number | null],
    observacoes: [''],
  });

  constructor() {
    this.carregar();
  }

  private carregar(): void {
    this.agenda.listarPacientes().subscribe(lista => this.pacientes.set(lista));
    this.agenda.listarProfissionais().subscribe(lista => this.profissionais.set(lista));
    this.agenda.listarTurmas().subscribe(lista => this.turmas.set(lista));
  }

  hoje(): string {
    return new Date().toISOString().slice(0, 10);
  }

  tipoSessao(): TipoSessao {
    return this.form.value.tipoSessao as TipoSessao;
  }

  ehPilates = ehPilates;

  turmasDoTipo(): Turma[] {
    return this.turmas().filter(t => t.ativo && t.tipoSessao === this.tipoSessao());
  }

  turmaSelecionada(): Turma | undefined {
    const id = this.form.value.turmaId;
    return id ? this.turmasDoTipo().find(t => t.id === id) : undefined;
  }

  diaSemana(): number {
    return diaSemanaDe(this.form.value.data ?? this.hoje());
  }

  horariosDoDia(): TurmaHorario[] {
    const turma = this.turmaSelecionada();
    return turma ? turma.horarios.filter(h => h.diaSemana === this.diaSemana()) : [];
  }

  rotuloDia = rotuloDia;
  horarioCurto = horarioCurto;

  limparTurmaSeDiaMudou(): void {
    if (this.form.value.turmaId && this.horariosDoDia().length === 0) {
      this.form.patchValue({ turmaId: '' });
    }
  }

  limparTurmaSeTipoMudou(): void {
    this.form.patchValue({ turmaId: '' });
  }

  aplicarHorario(horario: TurmaHorario): void {
    this.form.patchValue({
      horaInicio: horarioCurto(horario.horaInicio),
      horaFim: horarioCurto(horario.horaFim),
    });
  }

  abrirHorario(campo: CampoHorario): void {
    this.horarioAberto.set(campo);
  }

  escolherHorario(horario: string): void {
    const campo = this.horarioAberto();
    if (campo) {
      this.form.patchValue({ [campo]: horario });
      if (campo === 'horaInicio') {
        this.autopreencherFim(horario);
      }
    }
    this.horarioAberto.set(null);
  }

  private autopreencherFim(inicio: string): void {
    const atual = this.form.value.horaFim;
    if (atual && atual > inicio) {
      return;
    }
    const [h, m] = inicio.split(':').map(Number);
    const total = h * 60 + m + 60;
    const fim = total >= 1440 ? '23:59' : `${String(Math.floor(total / 60)).padStart(2, '0')}:${String(total % 60).padStart(2, '0')}`;
    this.form.patchValue({ horaFim: fim });
  }

  salvar(): void {
    if (this.form.invalid) {
      return;
    }
    const v = this.form.value;
    const data = v.data!;
    const inicio = `${data}T${v.horaInicio}:00`;
    const fim = `${data}T${v.horaFim}:00`;

    if (inicio >= fim) {
      this.erro.set('O horário de fim deve ser posterior ao de início.');
      return;
    }

    const req: CriarAgendamentoRequest = {
      pacienteId: v.pacienteId!,
      profissionalId: v.profissionalId!,
      dataHoraInicio: inicio,
      dataHoraFim: fim,
      tipoSessao: v.tipoSessao as TipoSessao,
      tipoAula: v.tipoAula as CriarAgendamentoRequest['tipoAula'],
      turmaId: v.turmaId || undefined,
      valorSessao: v.valorSessao ?? undefined,
      observacoes: v.observacoes ?? undefined,
    };

    this.carregando.set(true);
    this.erro.set('');
    this.agenda.criar(req).subscribe({
      next: () => {
        this.carregando.set(false);
        this.salvo.emit(true);
        this.form.reset({
          data: this.hoje(),
          tipoSessao: 'PilatesSolo',
          tipoAula: 'Plano',
        });
      },
      error: (erro: Error) => {
        this.carregando.set(false);
        this.erro.set(erro.message);
      },
    });
  }
}