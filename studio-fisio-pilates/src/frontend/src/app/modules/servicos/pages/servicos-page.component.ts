import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PageHeaderComponent } from '../../../shared/components/page-header.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state.component';
import { ServicoService } from '../services/servico.service';
import { Servico } from '../models/servico.model';

@Component({
  selector: 'clin-servicos-page',
  standalone: true,
  imports: [PageHeaderComponent, EmptyStateComponent, ReactiveFormsModule],
  template: `
    <clin-page-header titulo="Serviços" subtitulo="Cadastro de serviços oferecidos pela clínica" />

    <form class="card form" [formGroup]="form" (ngSubmit)="salvar()">
      <h3 class="form__title">Novo serviço</h3>
      <div class="form-grid">
        <div class="form-group">
          <label>Nome</label>
          <input formControlName="nome" placeholder="Ex.: Pilates em grupo" />
        </div>
        <div class="form-group">
          <label>Valor (R$)</label>
          <input type="number" step="0.01" min="0" formControlName="valor" />
        </div>
        <div class="form-group form-group--full">
          <label>Descrição</label>
          <input formControlName="descricao" placeholder="Descrição opcional do serviço" />
        </div>
      </div>
      @if (erro()) {
        <p class="form__error">{{ erro() }}</p>
      }
      <div class="form__actions">
        <button type="submit" class="btn btn--primary" [disabled]="form.invalid || carregando()">
          {{ carregando() ? 'Salvando…' : 'Salvar serviço' }}
        </button>
      </div>
    </form>

    <div class="card">
      @if (carregando()) {
        <p class="hint">Carregando…</p>
      } @else if (servicos().length === 0) {
        <clin-empty-state icone="🧩" titulo="Nenhum serviço cadastrado" hint="Cadastre serviços para compor os planos." />
      } @else {
        <table class="data-table">
          <thead>
            <tr>
              <th>Serviço</th>
              <th>Descrição</th>
              <th>Valor</th>
            </tr>
          </thead>
          <tbody>
            @for (s of servicos(); track s.id) {
              <tr>
                <td>{{ s.nome }}</td>
                <td>{{ s.descricao ?? '—' }}</td>
                <td>{{ s.valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' }) }}</td>
              </tr>
            }
          </tbody>
        </table>
      }
    </div>
  `,
  styles: `
    .form__title { margin-bottom: 1rem; font-size: 1.05rem; }
    .form-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 0 1rem; }
    .form-group--full { grid-column: 1 / -1; }
    .form__actions { display: flex; justify-content: flex-end; }
    .form__error { color: var(--clin-danger); font-size: 0.85rem; margin: 0.5rem 0; }
    .hint { color: var(--clin-text-muted); text-align: center; padding: 1rem 0; }
  `,
})
export class ServicosPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(ServicoService);

  readonly servicos = signal<Servico[]>([]);
  readonly carregando = signal(false);
  readonly erro = signal('');

  readonly form = this.fb.group({
    nome: ['', Validators.required],
    valor: [0, [Validators.required, Validators.min(0)]],
    descricao: [''],
  });

  constructor() {
    this.recarregar();
  }

  recarregar(): void {
    this.carregando.set(true);
    this.service.listar().subscribe({
      next: lista => {
        this.servicos.set(lista);
        this.carregando.set(false);
      },
      error: () => this.carregando.set(false),
    });
  }

  salvar(): void {
    if (this.form.invalid) {
      return;
    }
    this.carregando.set(true);
    this.erro.set('');
    this.service.criar({
      nome: this.form.value.nome!,
      valor: this.form.value.valor ?? 0,
      descricao: this.form.value.descricao ?? undefined,
    }).subscribe({
      next: () => {
        this.carregando.set(false);
        this.form.reset({ valor: 0 });
        this.recarregar();
      },
      error: (erro: Error) => {
        this.carregando.set(false);
        this.erro.set(erro.message);
      },
    });
  }
}