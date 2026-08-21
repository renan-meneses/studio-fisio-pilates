import { TipoSessao } from '../../agenda/models/agendamento.model';

export interface TurmaHorario {
  id: string;
  diaSemana: number;
  horaInicio: string;
  horaFim: string;
}

export interface Turma {
  id: string;
  nome: string;
  tipoSessao: TipoSessao;
  profissionalId?: string;
  profissionalNome?: string;
  ativo: boolean;
  capacidade: number;
  horarios: TurmaHorario[];
}

export interface WaitlistEntry {
  id: string;
  pacienteId: string;
  pacienteNome: string;
  entradaEm: string;
}

export interface HorarioNovo {
  diaSemana: number;
  horaInicio: string;
  horaFim: string;
}

export const DIAS_SEMANA = [
  { valor: 1, rotulo: 'Segunda-feira' },
  { valor: 2, rotulo: 'Terça-feira' },
  { valor: 3, rotulo: 'Quarta-feira' },
  { valor: 4, rotulo: 'Quinta-feira' },
  { valor: 5, rotulo: 'Sexta-feira' },
  { valor: 6, rotulo: 'Sábado' },
  { valor: 7, rotulo: 'Domingo' },
];

export function rotuloDia(dia: number): string {
  return DIAS_SEMANA.find(d => d.valor === dia)?.rotulo ?? `Dia ${dia}`;
}

export function horarioCurto(iso: string): string {
  return iso.length >= 5 ? iso.slice(0, 5) : iso;
}