import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PageHeaderComponent } from '../../../shared/components/page-header.component';
import { StatCardComponent } from '../../../shared/components/stat-card.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state.component';
import { FinanceiroService } from '../services/financeiro.service';
import { FinanceiroResumo, Inadimplencia, Mensalidade } from '../models/financeiro.model';

function brl(valor: number): string {
  return valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

@Component({
  selector: 'clin-financeiro-page',
  standalone: true,
  imports: [PageHeaderComponent, StatCardComponent, EmptyStateComponent, ReactiveFormsModule, DatePipe],
  template: `
    <clin-page-header titulo="Financeiro" subtitulo="Acompanhamento mensal de receitas e despesas">
      <input
        type="month"
        class="rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-800 transition-colors focus:border-teal-500 focus:outline-none focus:ring-2 focus:ring-teal-500/25 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
        [value]="competencia()"
        (change)="mudarCompetencia($event)"
      />
    </clin-page-header>

    @if (resumo(); as r) {
      <div class="mb-5 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
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
      <header class="mb-3 flex flex-wrap items-center justify-between gap-3">
        <h2 class="text-lg font-bold tracking-tight">Cobrar mensalidade</h2>
      </header>
      <form [formGroup]="cobrancaForm" (ngSubmit)="cobrar()" class="grid grid-cols-1 items-end gap-4 sm:grid-cols-2 lg:grid-cols-[2fr_1fr_1fr_auto]">
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
      <header class="mb-3 flex flex-wrap items-center justify-between gap-3">
        <h2 class="text-lg font-bold tracking-tight">Mensalidades — {{ competencia() }}</h2>
        <div class="flex flex-wrap items-center gap-3">
          @if (mensagemFaturamento()) {
            <span class="text-sm text-slate-500 dark:text-slate-400">{{ mensagemFaturamento() }}</span>
          }
          <button
            class="btn btn--outline"
            type="button"
            [disabled]="faturando()"
            (click)="gerarFaturamento()"
          >
            {{ faturando() ? 'Gerando…' : '⚡ Gerar faturamento do mês' }}
          </button>
        </div>
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
                  <div class="flex flex-wrap gap-1.5">
                    @if (!m.paga) {
                      <button class="btn btn--outline btn--sm" (click)="emitirCobranca(m, 'Pix')" title="Emitir cobrança Pix">Pix</button>
                      <button class="btn btn--outline btn--sm" (click)="emitirCobranca(m, 'Boleto')" title="Emitir boleto">Boleto</button>
                      <button class="btn btn--primary btn--sm" (click)="receber(m)">Receber</button>
                      <button class="btn btn--danger btn--sm" (click)="cancelar(m)">Cancelar</button>
                    }
                  </div>
                </td>
              </tr>
            }
          </tbody>
        </table>
      }
    </section>

    <section class="card">
      <header class="mb-3 flex flex-wrap items-center justify-between gap-3">
        <h2 class="text-lg font-bold tracking-tight">Inadimplência (vencidas em aberto)</h2>
      </header>
      @if (inadimplencia(); as i) {
        @if (i.itens.length === 0) {
          <clin-empty-state icone="✅" titulo="Nenhum vencido" hint="Todas as mensalidades estão em dia." />
        } @else {
          <div class="mb-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            @for (faixa of faixasOrdenadas(i); track faixa.nome) {
              <clin-stat-card [label]="'Atraso ' + faixa.nome + ' dias'" [value]="brl(faixa.valor)" />
            }
          </div>
          <table class="data-table">
            <thead>
              <tr>
                <th>Paciente</th>
                <th>Competência</th>
                <th>Valor</th>
                <th>Vencimento</th>
                <th>Atraso</th>
              </tr>
            </thead>
            <tbody>
              @for (item of i.itens; track item.mensalidadeId) {
                <tr>
                  <td>{{ item.pacienteNome }}</td>
                  <td>{{ item.competencia }}</td>
                  <td>{{ brl(item.valor) }}</td>
                  <td>{{ item.dataVencimento | date: 'dd/MM/yyyy' }}</td>
                  <td>
                    <span class="badge {{ item.diasAtraso > 60 ? 'badge--danger' : 'badge--warning' }}">
                      {{ item.diasAtraso }} dias
                    </span>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        }
      }
    </section>
  `,
})
export class FinanceiroPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly financeiro = inject(FinanceiroService);

  readonly brl = brl;
  readonly competencia = signal(this.mesAtual());
  readonly resumo = signal<FinanceiroResumo | null>(null);
  readonly mensalidades = signal<Mensalidade[]>([]);
  readonly inadimplencia = signal<Inadimplencia | null>(null);
  readonly faturando = signal(false);
  readonly mensagemFaturamento = signal('');

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

  faixasOrdenadas(i: Inadimplencia): { nome: string; valor: number }[] {
    const ordem = ['1-30', '31-60', '61-90', '90+'];
    return Object.entries(i.porFaixa)
      .map(([nome, valor]) => ({ nome, valor }))
      .sort((a, b) => ordem.indexOf(a.nome) - ordem.indexOf(b.nome));
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
    this.financeiro.inadimplencia().subscribe(i => this.inadimplencia.set(i));
    void dia;
  }

  gerarFaturamento(): void {
    this.faturando.set(true);
    this.mensagemFaturamento.set('');
    this.financeiro.faturarRecorrente(this.competencia()).subscribe({
      next: resultado => {
        this.faturando.set(false);
        this.mensagemFaturamento.set(
          `${resultado.geradas} mensalidade(s) gerada(s), ${resultado.jaExistentes} já existente(s).`,
        );
        this.recarregar();
      },
      error: () => this.faturando.set(false),
    });
  }

  emitirCobranca(m: Mensalidade, tipo: 'Pix' | 'Boleto'): void {
    this.financeiro.emitirCobranca(m.id, tipo).subscribe({
      next: cobranca => {
        const codigo = cobranca.pixCopiaECola ?? cobranca.boletoLinhaDigitavel ?? '';
        const rotulo = tipo === 'Pix' ? 'Copia e cola' : 'Linha digitável';
        if (navigator.clipboard) {
          void navigator.clipboard.writeText(codigo);
          alert(`${rotulo} copiado para a área de transferência:\n\n${codigo}`);
        } else {
          prompt(`${rotulo}:`, codigo);
        }
      },
      error: (erro: Error) => alert(erro.message),
    });
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