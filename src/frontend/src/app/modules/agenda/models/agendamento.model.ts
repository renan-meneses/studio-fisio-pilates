export type StatusAgendamento = 'Agendado' | 'Confirmado' | 'Concluido' | 'Cancelado';

export interface Agendamento {
  id: string;
  data: string;
  horaInicio: string;
  horaFim: string;
  pacienteId: string;
  pacienteNome: string;
  servicoId?: string;
  servicoNome?: string;
  status: StatusAgendamento;
  observacoes?: string;
  presencaRegistrada: boolean;
}

export interface CriarAgendamentoRequest {
  data: string;
  horaInicio: string;
  horaFim: string;
  pacienteId: string;
  servicoId?: string;
  observacoes?: string;
}

export interface CancelarAgendamentoRequest {
  motivo: string;
}