export type StatusAgendamento = 'Agendado' | 'Confirmado' | 'Realizado' | 'Cancelado' | 'Faltou';
export type TipoSessao = 'Avaliacao' | 'PilatesSolo' | 'PilatesDupla' | 'PilatesGrupo' | 'Fisioterapia' | 'Domiciliar';
export type TipoAula = 'Experimental' | 'Plano' | 'Individual';

export interface Agendamento {
  id: string;
  pacienteId: string;
  pacienteNome: string;
  profissionalId: string;
  profissionalNome: string;
  dataHoraInicio: string;
  dataHoraFim: string;
  tipoSessao: TipoSessao;
  tipoAula: TipoAula;
  turmaId?: string;
  turmaNome?: string;
  status: StatusAgendamento;
  valorSessao: number;
  observacoes?: string;
}

export interface CriarAgendamentoRequest {
  pacienteId: string;
  profissionalId: string;
  dataHoraInicio: string;
  dataHoraFim: string;
  tipoSessao: TipoSessao;
  tipoAula: TipoAula;
  turmaId?: string;
  valorSessao?: number;
  observacoes?: string;
}

export interface PacienteResumo {
  id: string;
  nome: string;
  telefone?: string;
  ativo: boolean;
}

export interface ProfissionalResumo {
  id: string;
  nome: string;
  especialidades: string;
  ativo: boolean;
}

export const TIPOS_SESSAO: { valor: TipoSessao; rotulo: string }[] = [
  { valor: 'Avaliacao', rotulo: 'Avaliação' },
  { valor: 'PilatesSolo', rotulo: 'Pilates Solo' },
  { valor: 'PilatesDupla', rotulo: 'Pilates Dupla' },
  { valor: 'PilatesGrupo', rotulo: 'Pilates Grupo' },
  { valor: 'Fisioterapia', rotulo: 'Fisioterapia' },
  { valor: 'Domiciliar', rotulo: 'Domiciliar' },
];

export const TIPOS_AULA: { valor: TipoAula; rotulo: string }[] = [
  { valor: 'Experimental', rotulo: 'Aula experimental' },
  { valor: 'Plano', rotulo: 'Plano' },
  { valor: 'Individual', rotulo: 'Aula individual' },
];

export function rotuloTipoSessao(tipo: TipoSessao): string {
  return TIPOS_SESSAO.find(t => t.valor === tipo)?.rotulo ?? tipo;
}

export function rotuloTipoAula(tipo: TipoAula): string {
  return TIPOS_AULA.find(t => t.valor === tipo)?.rotulo ?? tipo;
}

export function ehPilates(tipo: TipoSessao): boolean {
  return tipo.startsWith('Pilates');
}
