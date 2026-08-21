export interface DashboardResumo {
  pacientesAtivos: number;
  agendamentosHoje: number;
  receitaMes: number;
  inadimplencia: number;
}

export interface FaturamentoItem {
  competencia: string;
  receita: number;
  previsto: number;
}

export interface OcupacaoDia {
  data: string;
  total: number;
  realizados: number;
  faltas: number;
}

export interface TopSessao {
  tipoSessao: string;
  quantidade: number;
  receita: number;
}
