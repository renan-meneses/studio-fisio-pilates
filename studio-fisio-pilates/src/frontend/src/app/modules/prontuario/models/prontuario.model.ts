export interface PacienteResumo {
  id: string;
  nome: string;
  telefone?: string;
  ativo: boolean;
}

export interface Anamnese {
  id: string;
  data: string;
  queixaPrincipal: string;
  historicoMedico?: string;
  alergias?: string;
  medicamentos?: string;
  observacoes?: string;
}

export interface EvolucaoClinica {
  id: string;
  data: string;
  descricao: string;
  observacoes?: string;
}

export interface PacienteCompleto {
  id: string;
  nome: string;
  dataNascimento?: string;
  telefone?: string;
  email?: string;
  convenio?: string;
  ativo: boolean;
  anamneses: Anamnese[];
  evolucoes: EvolucaoClinica[];
}

export interface CriarAnamneseRequest {
  queixaPrincipal: string;
  historicoMedico?: string;
  alergias?: string;
  medicamentos?: string;
  observacoes?: string;
}

export interface CriarEvolucaoRequest {
  descricao: string;
  observacoes?: string;
}