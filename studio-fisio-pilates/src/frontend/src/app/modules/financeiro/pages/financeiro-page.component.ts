import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PageHeaderComponent } from '../../../shared/components/page-header.component';
import { StatCardComponent } from '../../../shared/components/stat-card.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state.component';
import { FinanceiroService } from '../services/financeiro.service';
import { FinanceiroResumo, Mensalidade } from '../models/financeiro.model';

function brl(valor: number): string {
  return valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

@Component({
  selector: 'clin-financeiro-page',
  standalone: true,
  imports: [PageHeaderComponent, StatCardComponent, EmptyStateComponent, ReactiveFormsModule],
  template: `
    <clin-page-header titulo="Financeiro" subtitulo="Acompanhamento mensal de receitas e despesas">
      <input
        type="month"
        class="filter"
        [value]="competencia()"
        (change)="mudarCompetencia($event)"
      />
    </clin-page-header>

    @if (resumo(); as r) {
      <div class="stats">
        <clin-stat-card label="Receita esperada" [value]="brl(r.receitaEsperada)" />
        <clin-stat-card label="Receita recebida" [value]="brl(r.receitaRecebida)" />
        <clin-stat-card label="Despesas pagas" [value]="brl(r.despesaPaga)" />
        <clin-stat-card
          label="Resultado do mês"
          [value]="brl(r.resultado)"
          [meta]="r.resultado >= 0 ? 'Positivo 🎯' : 'Atenção!'"
        />
      </div>
    }

    <section class="card">
      <header class="section">
        <h2 class="section__title">Cobrar mensalidade</h2>
      </header>
      <form [formGroup]="cobrancaForm" (ngSubmit)="cobrar()" class="cobranca">
        <div class="form-group">
          <label>Paciente</label>
          <input formControlName="paciente" placeholder="Nome do paciente" />
        </div>
        <div class="form-group">
          <label>Valor (R$)</label>
          <input formControlName="valor" type="number" step="0.01" min="0" />
        </div>
        <div class="form-group">
          <label>Vencimento</label>
          <input formControlName="vencimento" type="date" />
        </div>
        <button class="btn btn--primary" type="submit" [disabled]="cobrancaForm.invalid">
          Cobrar
        </button>
      </form>
    </section>

    <section class="card">
      <header class="section">
        <h2 class="section__title">Mensalidades — {{ competencia() }}</h2>
      </header>
      @if (mensalidades().length === 0) {
        <clin-empty-state
          icone="💳"
          titulo="Nenhuma mensalidade lançada"
          hint="Cobre mensalidades para acompanhar a receita do mês."
        />
      } @else {
        <table class="data-table">
          <thead>
            <tr>
              <th>Paciente</th>
              <th>Vencimento</th>
              <th>Valor</th>
              <th>Status</th>
              <th>Ações</th>
            </tr>
          </thead>
          <tbody>
            @for (m of mensalidades(); track m.id) {
              <tr>
                <td>{{ m.pacienteNome }}</td>
                <td>{{ m.vencimento }}</td>
                <td>{{ brl(m.valor) }}</td>
                <td>
                  <span class="badge {{ m.paga ? 'badge--success' : 'badge--warning' }}">
                    {{ m.paga ? 'Paga' : 'Pendente' }}
                  </span>
                </td>
                <td>
                  <div class="acoes">
                    @if (!m.paga) {
                      <button class="btn btn--primary" (click)="receber(m)">Receber</button>
                      <button class="btn btn--danger" (click)="cancelar(m)">Cancelar</button>
                    }
                  </div>
                </td>
              </tr>
            }
          </tbody>
        </table>
      }
    </section>
  `,
  styles: `
    .filter {
      padding: 0.55rem 0.75rem;
      border: 1px solid var(--clin-border);
      border-radius: 8px;
      font: inherit;
      background: var(--clin-surface);
    }
    .stats { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 1rem; margin-bottom: 1.25rem; }
    .section { display: flex; align-items: center; justify-content: space-between; margin-bottom: 0.75rem; }
    .section__title { font-size: 1.05rem; }
    .cobranca { display: grid; grid-template-columns: 2fr 1fr 1fr auto; gap: 1rem; align-items: end; }
    .acoes { display: flex; gap: 0.4rem; }
  `,
})
export class FinanceiroPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly financeiro = inject(FinanceiroService);

  readonly brl = brl;
  readonly competencia = signal(this.mesAtual());
  readonly resumo = signal<FinanceiroResumo | null>(null);
  readonly mensalidades = signal<Mensalidade[]>([]);

  readonly cobrancaForm = this.fb.group({
    paciente: ['', Validators.required],
    valor: [0, [Validators.required, Validators.min(0.01)]],
    vencimento: ['', Validators.required],
  });

  constructor() {
    this.recarregar();
  }

  mesAtual(): string {
    return new Date().toISOString().slice(0, 7);
  }

  mudarCompetencia(evento: Event): void {
    this.competencia.set((evento.target as HTMLInputElement).value);
    this.recarregar();
  }

  recarregar(): void {
    const mes = this.competencia();
    const dia = `${mes}-01`;
    this.financeiro.resumo(mes).subscribe(r => this.resumo.set(r));
    this.financeiro.listarMensalidades(mes).subscribe(lista => this.mensalidades.set(lista));
    this.financeiro.listarContas(mes).subscribe(() => undefined);
    void dia;
  }

  cobrar(): void {
    this.financeiro
      .cobrarMensalidade({
        pacienteId: crypto.randomUUID(),
        competencia: this.competencia(),
        valor: this.cobrancaForm.value.valor!,
        vencimento: this.cobrancaForm.value.vencimento!,
      })
      .subscribe(() => {
        this.cobrancaForm.reset({ valor: 0 });
        this.recarregar();
      });
  }

  receber(m: Mensalidade): void {
    this.financeiro.receberMensalidade(m.id).subscribe(() => this.recarregar());
  }

  cancelar(m: Mensalidade): void {
    this.financeiro.cancelarMensalidade(m.id).subscribe(() => this.recarregar());
  }
}