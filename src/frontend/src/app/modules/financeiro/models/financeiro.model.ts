export interface FinanceiroResumo {
  competencia: string;
  receitaEsperada: number;
  receitaRecebida: number;
  despesaPrevista: number;
  despesaPaga: number;
  resultado: number;
}

export interface Mensalidade {
  id: string;
  pacienteId: string;
  pacienteNome: string;
  competencia: string;
  valor: number;
  vencimento: string;
  paga: boolean;
  dataPagamento?: string;
}

export interface ContaPagar {
  id: string;
  descricao: string;
  competencia: string;
  valor: number;
  vencimento: string;
  paga: boolean;
  dataPagamento?: string;
}

export interface CobrarMensalidadeRequest {
  pacienteId: string;
  competencia: string;
  valor: number;
  vencimento: string;
}

export interface ReceberMensalidadeRequest {
  valor?: number;
  vencimento?: string;
}

export interface CriarContaPagarRequest {
  descricao: string;
  competencia: string;
  valor: number;
  vencimento: string;
}