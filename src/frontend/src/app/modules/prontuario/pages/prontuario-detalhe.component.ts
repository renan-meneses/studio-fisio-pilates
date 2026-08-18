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

    <div class="card cursor">
      <nav class="tabs">
        <button
          class="tabs__item"
          [class.tabs__item--active]="aba() === 'dados'"
          (click)="aba.set('dados')"
        >
          Dados
        </button>
        <button
          class="tabs__item"
          [class.tabs__item--active]="aba() === 'anamneses'"
          (click)="aba.set('anamneses')"
        >
          Anamneses
        </button>
        <button
          class="tabs__item"
          [class.tabs__item--active]="aba() === 'evolucoes'"
          (click)="aba.set('evolucoes')"
        >
          Evoluções
        </button>
      </nav>
    </div>

    @if (paciente(); as pac) {
      <div class="card cursor">
        @switch (aba()) {
          @case ('dados') {
            <dl class="dados">
              <dt>Nome</dt><dd>{{ pac.nome }}</dd>
              <dt>Nascimento</dt><dd>{{ pac.dataNascimento ?? '—' }}</dd>
              <dt>Telefone</dt><dd>{{ pac.telefone ?? '—' }}</dd>
              <dt>E-mail</dt><dd>{{ pac.email ?? '—' }}</dd>
              <dt>Convênio</dt><dd>{{ pac.convenio ?? '—' }}</dd>
            </dl>
          }
          @case ('anamneses') {
            <h3 class="cursor__sub">Nova anamnese</h3>
            <form [formGroup]="anamneseForm" (ngSubmit)="salvarAnamnese()">
              <div class="form-group">
                <label>Queixa principal *</label>
                <textarea formControlName="queixaPrincipal" rows="2"></textarea>
              </div>
              <div class="form-group">
                <label>Histórico médico</label>
                <textarea formControlName="historicoMedico" rows="2"></textarea>
              </div>
              <div class="form-row">
                <div class="form-group">
                  <label>Alergias</label>
                  <input formControlName="alergias" />
                </div>
                <div class="form-group">
                  <label>Medicamentos</label>
                  <input formControlName="medicamentos" />
                </div>
              </div>
              <button class="btn btn--primary" type="submit" [disabled]="anamneseForm.invalid">
                Registrar anamnese
              </button>
            </form>

            @for (a of pac.anamneses; track a.id) {
              <article class="cursor__item">
                <header>
                  <strong>{{ a.queixaPrincipal }}</strong>
                  <span class="badge badge--muted">{{ a.data }}</span>
                </header>
                @if (a.historicoMedico) { <p><b>Histórico:</b> {{ a.historicoMedico }}</p> }
                @if (a.alergias) { <p><b>Alergias:</b> {{ a.alergias }}</p> }
                @if (a.medicamentos) { <p><b>Medicamentos:</b> {{ a.medicamentos }}</p> }
              </article>
            }
          }
          @case ('evolucoes') {
            <h3 class="cursor__sub">Nova evolução clínica</h3>
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
              <article class="cursor__item">
                <header>
                  <strong>{{ e.data }}</strong>
                </header>
                <p>{{ e.descricao }}</p>
                @if (e.observacoes) { <p class="cursor__obs">{{ e.observacoes }}</p> }
              </article>
            }
          }
        }
      </div>
    }
  `,
  styles: `
    .tabs { display: flex; gap: 0.25rem; }
    .tabs__item {
      padding: 0.6rem 1rem;
      border: none;
      background: transparent;
      font-weight: 600;
      color: var(--clin-text-muted);
      cursor: pointer;
      border-bottom: 2px solid transparent;
      &--active { color: var(--clin-primary); border-bottom-color: var(--clin-primary); }
    }
    .dados {
      display: grid;
      grid-template-columns: 140px 1fr;
      row-gap: 0.6rem;
      margin: 0;
    }
    .dados dt { font-weight: 600; color: var(--clin-text-muted); }
    .dados dd { margin: 0; }
    .form-row { display: flex; gap: 1rem; }
    .form-row .form-group { flex: 1; }
    .cursor__sub { margin: 1.5rem 0 0.75rem; font-size: 1rem; }
    .cursor__item {
      border: 1px solid var(--clin-border);
      border-radius: 8px;
      padding: 0.9rem 1rem;
      margin-top: 0.75rem;
      header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.4rem; }
      p { margin: 0.25rem 0; }
    }
    .cursor__obs { color: var(--clin-text-muted); }
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