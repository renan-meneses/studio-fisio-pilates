import { Component, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PageHeaderComponent } from '../../../shared/components/page-header.component';
import { ProntuarioService } from '../services/prontuario.service';
import { PacienteCompleto } from '../models/prontuario.model';

type Aba = 'dados' | 'anamneses' | 'evolucoes';

@Component({
  selector: 'clin-prontuario-detalhe',
  standalone: true,
  imports: [PageHeaderComponent, ReactiveFormsModule],
  template: `
    <clin-page-header
      [titulo]="paciente()?.nome ?? 'Prontuário'"
      subtitulo="Prontuário eletrônico do paciente"
    >
      <a class="btn btn--outline" routerLink="/prontuarios">← Voltar</a>
    </clin-page-header>

    <div class="card">
      <nav class="flex gap-1 overflow-x-auto">
        <button
          type="button"
          class="cursor-pointer whitespace-nowrap rounded-lg px-4 py-2.5 font-semibold transition-colors"
          [class]="aba() === 'dados'
            ? 'bg-teal-600/10 text-teal-700 dark:bg-teal-400/10 dark:text-teal-300'
            : 'text-slate-500 hover:text-slate-800 dark:text-slate-400 dark:hover:text-slate-100'"
          (click)="aba.set('dados')"
        >
          Dados
        </button>
        <button
          type="button"
          class="cursor-pointer whitespace-nowrap rounded-lg px-4 py-2.5 font-semibold transition-colors"
          [class]="aba() === 'anamneses'
            ? 'bg-teal-600/10 text-teal-700 dark:bg-teal-400/10 dark:text-teal-300'
            : 'text-slate-500 hover:text-slate-800 dark:text-slate-400 dark:hover:text-slate-100'"
          (click)="aba.set('anamneses')"
        >
          Anamneses
        </button>
        <button
          type="button"
          class="cursor-pointer whitespace-nowrap rounded-lg px-4 py-2.5 font-semibold transition-colors"
          [class]="aba() === 'evolucoes'
            ? 'bg-teal-600/10 text-teal-700 dark:bg-teal-400/10 dark:text-teal-300'
            : 'text-slate-500 hover:text-slate-800 dark:text-slate-400 dark:hover:text-slate-100'"
          (click)="aba.set('evolucoes')"
        >
          Evoluções
        </button>
      </nav>
    </div>

    @if (paciente(); as pac) {
      <div class="card">
        @switch (aba()) {
          @case ('dados') {
            <dl class="m-0 grid grid-cols-[140px_1fr] gap-y-2.5">
              <dt class="font-semibold text-slate-500 dark:text-slate-400">Nome</dt>
              <dd class="m-0">{{ pac.nome }}</dd>
              <dt class="font-semibold text-slate-500 dark:text-slate-400">Nascimento</dt>
              <dd class="m-0">{{ pac.dataNascimento ?? '—' }}</dd>
              <dt class="font-semibold text-slate-500 dark:text-slate-400">Telefone</dt>
              <dd class="m-0">{{ pac.telefone ?? '—' }}</dd>
              <dt class="font-semibold text-slate-500 dark:text-slate-400">E-mail</dt>
              <dd class="m-0">{{ pac.email ?? '—' }}</dd>
              <dt class="font-semibold text-slate-500 dark:text-slate-400">Convênio</dt>
              <dd class="m-0">{{ pac.convenio ?? '—' }}</dd>
            </dl>
          }
          @case ('anamneses') {
            <h3 class="mb-3 mt-6 text-base font-semibold first:mt-0">Nova anamnese</h3>
            <form [formGroup]="anamneseForm" (ngSubmit)="salvarAnamnese()">
              <div class="form-group">
                <label>Queixa principal *</label>
                <textarea formControlName="queixaPrincipal" rows="2"></textarea>
              </div>
              <div class="form-group">
                <label>Histórico médico</label>
                <textarea formControlName="historicoMedico" rows="2"></textarea>
              </div>
              <div class="flex flex-col gap-4 sm:flex-row">
                <div class="form-group flex-1">
                  <label>Alergias</label>
                  <input formControlName="alergias" />
                </div>
                <div class="form-group flex-1">
                  <label>Medicamentos</label>
                  <input formControlName="medicamentos" />
                </div>
              </div>
              <button class="btn btn--primary" type="submit" [disabled]="anamneseForm.invalid">
                Registrar anamnese
              </button>
            </form>

            @for (a of pac.anamneses; track a.id) {
              <article
                class="mt-3 rounded-xl border border-slate-200 p-4 dark:border-slate-800"
              >
                <header class="mb-1.5 flex items-center justify-between gap-2">
                  <strong>{{ a.queixaPrincipal }}</strong>
                  <span class="badge badge--muted">{{ a.data }}</span>
                </header>
                @if (a.historicoMedico) { <p class="my-1 text-sm"><b>Histórico:</b> {{ a.historicoMedico }}</p> }
                @if (a.alergias) { <p class="my-1 text-sm"><b>Alergias:</b> {{ a.alergias }}</p> }
                @if (a.medicamentos) { <p class="my-1 text-sm"><b>Medicamentos:</b> {{ a.medicamentos }}</p> }
              </article>
            }
          }
          @case ('evolucoes') {
            <h3 class="mb-3 mt-6 text-base font-semibold first:mt-0">Nova evolução clínica</h3>
            <form [formGroup]="evolucaoForm" (ngSubmit)="salvarEvolucao()">
              <div class="form-group">
                <label>Descrição *</label>
                <textarea formControlName="descricao" rows="3"></textarea>
              </div>
              <div class="form-group">
                <label>Observações</label>
                <textarea formControlName="observacoes" rows="2"></textarea>
              </div>
              <button class="btn btn--primary" type="submit" [disabled]="evolucaoForm.invalid">
                Registrar evolução
              </button>
            </form>

            @for (e of pac.evolucoes; track e.id) {
              <article
                class="mt-3 rounded-xl border border-slate-200 p-4 dark:border-slate-800"
              >
                <header class="mb-1.5 flex items-center justify-between gap-2">
                  <strong>{{ e.data }}</strong>
                </header>
                <p class="my-1 text-sm">{{ e.descricao }}</p>
                @if (e.observacoes) { <p class="my-1 text-sm text-slate-500 dark:text-slate-400">{{ e.observacoes }}</p> }
              </article>
            }
          }
        }
      </div>
    }
  `,
})
export class ProntuarioDetalheComponent {
  private readonly fb = inject(FormBuilder);
  private readonly prontuario = inject(ProntuarioService);

  readonly pacienteId = input.required<string>();
  readonly paciente = signal<PacienteCompleto | null>(null);
  readonly aba = signal<Aba>('dados');

  readonly anamneseForm = this.fb.group({
    queixaPrincipal: ['', Validators.required],
    historicoMedico: [''],
    alergias: [''],
    medicamentos: [''],
  });

  readonly evolucaoForm = this.fb.group({
    descricao: ['', Validators.required],
    observacoes: [''],
  });

  constructor() {
    this.recarregar();
  }

  recarregar(): void {
    this.prontuario.obterPaciente(this.pacienteId()).subscribe({
      next: paciente => this.paciente.set(paciente),
      error: () => alert('Falha ao carregar o prontuário.'),
    });
  }

  salvarAnamnese(): void {
    this.prontuario.criarAnamnese(this.pacienteId(), this.anamneseForm.getRawValue() as never)
      .subscribe(() => {
        this.anamneseForm.reset();
        this.recarregar();
      });
  }

  salvarEvolucao(): void {
    this.prontuario.criarEvolucao(this.pacienteId(), this.evolucaoForm.getRawValue() as never)
      .subscribe(() => {
        this.evolucaoForm.reset();
        this.recarregar();
      });
  }
}