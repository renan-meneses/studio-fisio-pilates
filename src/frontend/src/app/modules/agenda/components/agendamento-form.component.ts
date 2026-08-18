import { Component, inject, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CriarAgendamentoRequest } from '../models/agendamento.model';
import { AgendaService } from '../services/agenda.service';

@Component({
  selector: 'clin-agendamento-form',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <form class="card form" [formGroup]="form" (ngSubmit)="salvar()">
      <h3 class="form__title">Novo agendamento</h3>
      <div class="form-grid">
        <div class="form-group">
          <label>Paciente</label>
          <input formControlName="paciente" placeholder="Nome do paciente" />
        </div>
        <div class="form-group">
          <label>Data</label>
          <input type="date" formControlName="data" />
        </div>
        <div class="form-group">
          <label>Início</label>
          <input type="time" formControlName="horaInicio" />
        </div>
        <div class="form-group">
          <label>Fim</label>
          <input type="time" formControlName="horaFim" />
        </div>
        <div class="form-group">
          <label>Serviço</label>
          <input formControlName="servico" placeholder="Ex.: Fisioterapia / Pilates" />
        </div>
        <div class="form-group">
          <label>Observações</label>
          <input formControlName="observacoes" />
        </div>
      </div>
      <div class="form__actions">
        <button type="button" class="btn btn--outline" (click)="cancelar.emit()">Fechar</button>
        <button type="submit" class="btn btn--primary" [disabled]="form.invalid">Salvar</button>
      </div>
    </form>
  `,
  styles: `
    .form__title { margin-bottom: 1rem; font-size: 1.05rem; }
    .form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 0 1rem; }
    .form__actions { display: flex; justify-content: flex-end; gap: 0.6rem; }
  `,
})
export class AgendamentoFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly agenda = inject(AgendaService);

  readonly salvo = output<boolean>();
  readonly cancelar = output<void>();

  readonly form = this.fb.group({
    paciente: ['', Validators.required],
    data: ['', Validators.required],
    horaInicio: ['', Validators.required],
    horaFim: ['', Validators.required],
    servico: [''],
    observacoes: [''],
  });

  salvar(): void {
    if (this.form.invalid) {
      return;
    }
    const req: CriarAgendamentoRequest = {
      data: this.form.value.data!,
      horaInicio: this.form.value.horaInicio!,
      horaFim: this.form.value.horaFim!,
      pacienteId: crypto.randomUUID(),
      servicoId: this.form.value.servico ? crypto.randomUUID() : undefined,
      observacoes: this.form.value.observacoes ?? undefined,
    };
    this.agenda.criar(req).subscribe({
      next: () => {
        this.salvo.emit(true);
        this.form.reset();
      },
      error: () => alert('Erro ao salvar agendamento'),
    });
  }
}