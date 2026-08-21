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
export interface Cobranca {
  id: string;
  mensalidadeId: string;
  tipo: 'Pix' | 'Boleto';
  provedor: string;
  provedorCobrancaId: string;
  valor: number;
  status: 'Pendente' | 'Paga' | 'Expirada' | 'Cancelada';
  pixCopiaECola?: string;
  boletoLinhaDigitavel?: string;
  expiraEmUtc: string;
  pagaEmUtc?: string;
}

export interface ItemInadimplencia {
  mensalidadeId: string;
  pacienteId: string;
  pacienteNome: string;
  competencia: string;
  valor: number;
  dataVencimento: string;
  diasAtraso: number;
}

export interface Inadimplencia {
  totalVencido: number;
  totalPacientes: number;
  porFaixa: Record<string, number>;
  itens: ItemInadimplencia[];
}
