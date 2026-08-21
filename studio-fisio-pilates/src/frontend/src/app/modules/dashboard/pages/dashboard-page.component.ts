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

    <div class="stats">
      <clin-stat-card label="Pacientes ativos" [value]="resumo() ? texto(resumo()!.pacientesAtivos) : '—'" />
      <clin-stat-card label="Agendamentos hoje" [value]="resumo() ? texto(resumo()!.agendamentosHoje) : '—'" />
      <clin-stat-card label="Receita do mês" [value]="resumo() ? brl(resumo()!.receitaMes) : '—'" />
      <clin-stat-card label="Inadimplência" [value]="resumo() ? brl(resumo()!.inadimplencia) : '—'" />
    </div>

    <section class="card">
      <header class="section">
        <h2 class="section__title">Faturamento mensal</h2>
        <span class="legenda">
          <span class="legenda__item"><i class="barra barra--receita"></i> Recebido</span>
          <span class="legenda__item"><i class="barra barra--previsto"></i> Previsto</span>
        </span>
      </header>
      @if (faturamento().length === 0) {
        <clin-empty-state icone="📊" titulo="Sem dados de faturamento" hint="Gere mensalidades para visualizar o gráfico." />
      } @else {
        <div class="grafico">
          @for (m of faturamento(); track m.competencia) {
            <div class="grafico__coluna" [title]="rotuloCompetencia(m) + '\n' + brl(m.receita) + ' de ' + brl(m.previsto)">
              <div class="grafico__barras">
                <div class="barra-vertical barra--previsto" [style.height.%]="altura(m.previsto)"></div>
                <div class="barra-vertical barra--receita" [style.height.%]="altura(m.receita)"></div>
              </div>
              <span class="grafico__rotulo">{{ competenciaCurta(m.competencia) }}</span>
            </div>
          }
        </div>
      }
    </section>

    <div class="duas-colunas">
      <section class="card">
        <header class="section">
          <h2 class="section__title">Ocupação — últimos 14 dias</h2>
        </header>
        @if (ocupacaoRecente().length === 0) {
          <clin-empty-state icone="📅" titulo="Sem agendamentos" hint="A agenda dos últimos dias aparecerá aqui." />
        } @else {
          <div class="ocupacao">
            @for (d of ocupacaoRecente(); track d.data) {
              <div
                class="ocupacao__dia"
                [title]="dataCurta(d.data) + ': ' + d.total + ' sessões, ' + d.realizados + ' realizadas, ' + d.faltas + ' faltas'"
              >
                <div class="ocupacao__barra-wrap">
                  <div
                    class="ocupacao__barra"
                    [class.ocupacao__barra--cheia]="d.faltas === 0"
                    [style.height.%]="alturaOcupacao(d.total)"
                  ></div>
                </div>
                <span class="ocupacao__label">{{ dataCurta(d.data) }}</span>
              </div>
            }
          </div>
        }
      </section>

      <section class="card">
        <header class="section">
          <h2 class="section__title">Top sessões por receita</h2>
        </header>
        @if (topSessoes().length === 0) {
          <clin-empty-state icone="🏆" titulo="Nenhuma sessão realizada" hint="Sessões realizadas serão ranqueadas aqui." />
        } @else {
          <ul class="top-lista">
            @for (t of topSessoes(); track t.tipoSessao) {
              <li class="top-item">
                <div class="top-item__linha">
                  <span>{{ rotuloSessao(t.tipoSessao) }}</span>
                  <strong>{{ brl(t.receita) }}</strong>
                </div>
                <div class="top-item__barra-wrap">
                  <div class="top-item__barra" [style.width.%]="largura(t.receita)"></div>
                </div>
                <span class="top-item__meta">{{ t.quantidade }} sessão(ões)</span>
              </li>
            }
          </ul>
        }
      </section>
    </div>
  `,
  styles: `
    .stats { display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 1rem; margin-bottom: 1rem; }
    .section { display: flex; align-items: center; justify-content: space-between; margin-bottom: 0.75rem; }
    .section__title { margin: 0; font-size: 1.05rem; }
    .legenda { display: flex; gap: 0.9rem; font-size: 0.8rem; color: var(--clin-text-muted); }
    .legenda__item { display: inline-flex; align-items: center; gap: 0.35rem; }
    .barra { display: inline-block; width: 12px; height: 12px; border-radius: 3px; }
    .barra--receita { background: var(--clin-primary, #4f6ef7); }
    .barra--previsto { background: var(--clin-border, #d5dbe6); }

    .grafico {
      display: flex;
      align-items: stretch;
      gap: 0.75rem;
      height: 220px;
      padding-top: 0.5rem;
    }
    .grafico__coluna {
      flex: 1;
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 0.35rem;
      min-width: 0;
    }
    .grafico__barras {
      flex: 1;
      width: 100%;
      max-width: 56px;
      display: flex;
      align-items: flex-end;
      justify-content: center;
      gap: 4px;
    }
    .barra-vertical { width: 45%; min-height: 2px; border-radius: 4px 4px 0 0; transition: height 0.4s ease; }
    .grafico__rotulo { font-size: 0.72rem; color: var(--clin-text-muted); white-space: nowrap; }

    .duas-colunas { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; margin-top: 1rem; }
    @media (max-width: 900px) { .duas-colunas { grid-template-columns: 1fr; } }

    .ocupacao { display: flex; align-items: stretch; gap: 3px; height: 160px; padding-top: 0.25rem; }
    .ocupacao__dia { flex: 1; display: flex; flex-direction: column; min-width: 0; }
    .ocupacao__barra-wrap { flex: 1; display: flex; align-items: flex-end; background: var(--clin-surface-alt); border-radius: 4px; overflow: hidden; }
    .ocupacao__barra { width: 100%; background: var(--clin-primary, #4f6ef7); opacity: 0.85; border-radius: 4px 4px 0 0; transition: height 0.4s ease; }
    .ocupacao__barra--cheia { opacity: 1; }
    .ocupacao__label { font-size: 0.62rem; color: var(--clin-text-muted); text-align: center; margin-top: 2px; }

    .top-lista { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 0.9rem; }
    .top-item { display: flex; flex-direction: column; gap: 0.3rem; }
    .top-item__linha { display: flex; justify-content: space-between; font-size: 0.92rem; }
    .top-item__barra-wrap { height: 8px; background: var(--clin-surface-alt); border-radius: 4px; overflow: hidden; }
    .top-item__barra { height: 100%; background: var(--clin-primary, #4f6ef7); border-radius: 4px; transition: width 0.4s ease; }
    .top-item__meta { font-size: 0.78rem; color: var(--clin-text-muted); }
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
