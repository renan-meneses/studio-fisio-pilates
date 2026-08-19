export interface Funcionario {
  id: string;
  nome: string;
  cargo: string;
  tipoContrato: string;
  ativo: boolean;
}

export interface RegistroPonto {
  id: string;
  funcionarioId: string;
  data: string;
  horaEntrada: string;
  horaSaida?: string;
}

export interface FolhaSalarial {
  id: string;
  funcionarioId: string;
  funcionarioNome: string;
  competencia: string;
  salarioBruto: number;
  descontos: number;
  salarioLiquido: number;
  paga: boolean;
  dataPagamento?: string;
}

export interface ProcessarFolhaRequest {
  competencia: string;
  descontos: number;
}

export interface LancarPontoRequest {
  data: string;
  horaEntrada: string;
  horaSaida?: string;
}