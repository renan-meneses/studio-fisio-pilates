import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PageHeaderComponent } from '../../../shared/components/page-header.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state.component';
import { FinanceiroService } from '../services/financeiro.service';
import { ContaPagar } from '../models/financeiro.model';

function brl(valor: number): string {
  return valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

@Component({
  selector: 'clin-contas-page',
  standalone: true,
  imports: [PageHeaderComponent, EmptyStateComponent, ReactiveFormsModule],
  template: `
    <clin-page-header titulo="Contas a pagar" subtitulo="Despesas operacionais do estúdio">
      <a class="btn btn--outline" routerLink="/financeiro">← Resumo</a>
    </clin-page-header>

    <section class="card">
      <h2 class="mb-3 text-lg font-bold tracking-tight">Nova conta</h2>
      <form [formGroup]="form" (ngSubmit)="criar()" class="grid grid-cols-1 items-end gap-4 sm:grid-cols-2 lg:grid-cols-[2fr_1fr_1fr_1fr_auto]">
        <div class="form-group">
          <label>Descrição *</label>
          <input formControlName="descricao" placeholder="Ex.: Aluguel do espaço" />
        </div>
        <div class="form-group">
          <label>Competência</label>
          <input type="month" formControlName="competencia" />
        </div>
        <div class="form-group">
          <label>Valor (R$) *</label>
          <input formControlName="valor" type="number" step="0.01" min="0" />
        </div>
        <div class="form-group">
          <label>Vencimento *</label>
          <input formControlName="vencimento" type="date" />
        </div>
        <button class="btn btn--primary" type="submit" [disabled]="form.invalid">Lançar</button>
      </form>
    </section>

    <section class="card">
      <h2 class="mb-3 text-lg font-bold tracking-tight">Contas de {{ competencia() }}</h2>
      @if (contas().length === 0) {
        <clin-empty-state
          icone="🧾"
          titulo="Nenhuma conta lançada"
          hint="Registre despesas como aluguel, internet e energia."
        />
      } @else {
        <table class="data-table">
          <thead>
            <tr>
              <th>Descrição</th>
              <th>Vencimento</th>
              <th>Valor</th>
              <th>Status</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            @for (c of contas(); track c.id) {
              <tr>
                <td>{{ c.descricao }}</td>
                <td>{{ c.vencimento }}</td>
                <td>{{ brl(c.valor) }}</td>
                <td>
                  <span class="badge {{ c.paga ? 'badge--success' : 'badge--warning' }}">
                    {{ c.paga ? 'Paga' : 'Pendente' }}
                  </span>
                </td>
                <td>
                  @if (!c.paga) {
                    <button class="btn btn--primary btn--sm" (click)="pagar(c)">Pagar</button>
                  }
                </td>
              </tr>
            }
          </tbody>
        </table>
      }
    </section>
  `,
})
export class ContasPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly financeiro = inject(FinanceiroService);

  readonly brl = brl;
  readonly competencia = signal(new Date().toISOString().slice(0, 7));
  readonly contas = signal<ContaPagar[]>([]);

  readonly form = this.fb.group({
    descricao: ['', Validators.required],
    competencia: ['', Validators.required],
    valor: [0, [Validators.required, Validators.min(0.01)]],
    vencimento: ['', Validators.required],
  });

  constructor() {
    this.recarregar();
  }

  recarregar(): void {
    this.financeiro.listarContas(this.competencia()).subscribe(contas => this.contas.set(contas));
  }

  criar(): void {
    const valor = this.form.value;
    this.financeiro
      .criarConta({
        descricao: valor.descricao!,
        competencia: valor.competencia!,
        valor: valor.valor!,
        vencimento: valor.vencimento!,
      })
      .subscribe(() => {
        this.form.reset({ competencia: this.competencia(), valor: 0 });
        this.recarregar();
      });
  }

  pagar(c: ContaPagar): void {
    this.financeiro.pagarConta(c.id).subscribe(() => this.recarregar());
  }
}