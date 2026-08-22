import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { PageHeaderComponent } from '../../../shared/components/page-header.component';
import { StatCardComponent } from '../../../shared/components/stat-card.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state.component';
import { DashboardService } from '../services/dashboard.service';
import { DashboardResumo, FaturamentoItem, OcupacaoDia, TopSessao } from '../models/dashboard.model';

@Component({
  selector: 'clin-dashboard-page',
  standalone: true,
  imports: [PageHeaderComponent, StatCardComponent, EmptyStateComponent, DatePipe],
  template: `
    <clin-page-header titulo="Dashboard" subtitulo="Visão geral da clínica" />

    <div class="grid gap-4 mb-4 [grid-template-columns:repeat(auto-fit,minmax(180px,1fr))]">
      <clin-stat-card label="Pacientes ativos" [value]="resumo() ? texto(resumo()!.pacientesAtivos) : '—'" />
      <clin-stat-card label="Agendamentos hoje" [value]="resumo() ? texto(resumo()!.agendamentosHoje) : '—'" />
      <clin-stat-card label="Receita do mês" [value]="resumo() ? brl(resumo()!.receitaMes) : '—'" />
      <clin-stat-card label="Inadimplência" [value]="resumo() ? brl(resumo()!.inadimplencia) : '—'" />
    </div>

    <section class="card p-5">
      <header class="flex items-center justify-between mb-3">
        <h2 class="m-0 text-[1.05rem] font-semibold">Faturamento mensal</h2>
        <span class="flex gap-3 text-xs text-slate-500 dark:text-slate-400">
          <span class="inline-flex items-center gap-1.5"><i class="inline-block w-3 h-3 rounded-[3px] bg-teal-500"></i> Recebido</span>
          <span class="inline-flex items-center gap-1.5"><i class="inline-block w-3 h-3 rounded-[3px] bg-slate-300 dark:bg-slate-700"></i> Previsto</span>
        </span>
      </header>
      @if (faturamento().length === 0) {
        <clin-empty-state icone="📊" titulo="Sem dados de faturamento" hint="Gere mensalidades para visualizar o gráfico." />
      } @else {
        <div class="flex items-stretch gap-3 h-[220px] pt-2">
          @for (m of faturamento(); track m.competencia) {
            <div class="flex-1 flex flex-col items-center gap-1.5 min-w-0" [title]="rotuloCompetencia(m) + '\n' + brl(m.receita) + ' de ' + brl(m.previsto)">
              <div class="flex-1 w-full max-w-[56px] flex items-end justify-center gap-1">
                <div class="w-[45%] min-h-[2px] rounded-t bg-slate-300 dark:bg-slate-700 transition-[height] duration-[400ms]" [style.height.%]="altura(m.previsto)"></div>
                <div class="w-[45%] min-h-[2px] rounded-t bg-teal-500 transition-[height] duration-[400ms]" [style.height.%]="altura(m.receita)"></div>
              </div>
              <span class="text-[0.72rem] whitespace-nowrap text-slate-500 dark:text-slate-400">{{ competenciaCurta(m.competencia) }}</span>
            </div>
          }
        </div>
      }
    </section>

    <div class="grid grid-cols-1 lg:grid-cols-2 gap-4 mt-4">
      <section class="card p-5">
        <header class="mb-3">
          <h2 class="m-0 text-[1.05rem] font-semibold">Ocupação — últimos 14 dias</h2>
        </header>
        @if (ocupacaoRecente().length === 0) {
          <clin-empty-state icone="📅" titulo="Sem agendamentos" hint="A agenda dos últimos dias aparecerá aqui." />
        } @else {
          <div class="flex items-stretch gap-[3px] h-40 pt-1">
            @for (d of ocupacaoRecente(); track d.data) {
              <div
                class="flex-1 flex flex-col min-w-0"
                [title]="dataCurta(d.data) + ': ' + d.total + ' sessões, ' + d.realizados + ' realizadas, ' + d.faltas + ' faltas'"
              >
                <div class="flex-1 flex items-end bg-slate-200 dark:bg-slate-800 rounded-md overflow-hidden">
                  <div
                    class="w-full bg-teal-500/80 rounded-t transition-[height] duration-[400ms]"
                    [class.!opacity-100]="d.faltas === 0"
                    [style.height.%]="alturaOcupacao(d.total)"
                  ></div>
                </div>
                <span class="text-[0.62rem] text-center mt-0.5 text-slate-500 dark:text-slate-400">{{ dataCurta(d.data) }}</span>
              </div>
            }
          </div>
        }
      </section>

      <section class="card p-5">
        <header class="mb-3">
          <h2 class="m-0 text-[1.05rem] font-semibold">Top sessões por receita</h2>
        </header>
        @if (topSessoes().length === 0) {
          <clin-empty-state icone="🏆" titulo="Nenhuma sessão realizada" hint="Sessões realizadas serão ranqueadas aqui." />
        } @else {
          <ul class="list-none m-0 p-0 flex flex-col gap-3.5">
            @for (t of topSessoes(); track t.tipoSessao) {
              <li class="flex flex-col gap-1.5">
                <div class="flex justify-between text-sm">
                  <span>{{ rotuloSessao(t.tipoSessao) }}</span>
                  <strong>{{ brl(t.receita) }}</strong>
                </div>
                <div class="h-2.5 rounded-full bg-slate-200 dark:bg-slate-800 overflow-hidden">
                  <div class="h-full bg-teal-500 rounded-full transition-[width] duration-[400ms]" [style.width.%]="largura(t.receita)"></div>
                </div>
                <span class="text-xs text-slate-500 dark:text-slate-400">{{ t.quantidade }} sessão(ões)</span>
              </li>
            }
          </ul>
        }
      </section>
    </div>
  `,
})
export class DashboardPageComponent {
  private readonly service = inject(DashboardService);

  readonly resumo = signal<DashboardResumo | null>(null);
  readonly faturamento = signal<FaturamentoItem[]>([]);
  readonly ocupacao = signal<OcupacaoDia[]>([]);
  readonly topSessoes = signal<TopSessao[]>([]);

  readonly ocupacaoRecente = computed(() => this.ocupacao().slice(-14));

  private readonly rotulosSessao: Record<string, string> = {
    Avaliacao: 'Avaliação',
    PilatesSolo: 'Pilates Solo',
    PilatesDupla: 'Pilates Dupla',
    PilatesGrupo: 'Pilates Grupo',
    Fisioterapia: 'Fisioterapia',
    Domiciliar: 'Domiciliar',
  };

  constructor() {
    this.service.resumo().subscribe(r => this.resumo.set(r));
    this.service.faturamento(6).subscribe(f => this.faturamento.set(f));
    this.service.ocupacao(30).subscribe(o => this.ocupacao.set(o));
    this.service.topSessoes(5).subscribe(t => this.topSessoes.set(t));
  }

  texto(valor: number): string {
    return valor.toLocaleString('pt-BR');
  }

  brl(valor: number): string {
    return valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
  }

  altura(valor: number): number {
    const maiorPrevisto = Math.max(...this.faturamento().map(m => m.previsto), 1);
    return Math.max((valor / maiorPrevisto) * 100, valor > 0 ? 4 : 0);
  }

  alturaOcupacao(total: number): number {
    const maior = Math.max(...this.ocupacaoRecente().map(d => d.total), 1);
    return Math.max((total / maior) * 100, total > 0 ? 8 : 0);
  }

  largura(receita: number): number {
    const maior = Math.max(...this.topSessoes().map(t => t.receita), 1);
    return Math.max((receita / maior) * 100, 4);
  }

  competenciaCurta(competencia: string): string {
    const [ano, mes] = competencia.split('-');
    return `${mes}/${ano.slice(2)}`;
  }

  rotuloCompetencia(m: FaturamentoItem): string {
    return this.competenciaCurta(m.competencia);
  }

  dataCurta(data: string): string {
    const [, mes, dia] = data.split('-');
    return `${dia}/${mes}`;
  }

  rotuloSessao(tipo: string): string {
    return this.rotulosSessao[tipo] ?? tipo;
  }
}
