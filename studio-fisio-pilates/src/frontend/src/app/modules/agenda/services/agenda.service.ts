import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  Agendamento,
  CriarAgendamentoRequest,
  PacienteResumo,
  ProfissionalResumo,
} from '../models/agendamento.model';
import { Turma } from '../../turmas/models/turma.model';

@Injectable({ providedIn: 'root' })
export class AgendaService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/agendamentos`;

  listar(dataInicio: string, dataFim: string): Observable<Agendamento[]> {
    return this.http.get<Agendamento[]>(this.base, {
      params: { de: dataInicio, ate: dataFim },
    });
  }

  criar(requisicao: CriarAgendamentoRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.base, requisicao);
  }

  confirmar(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/confirmar`, {});
  }

  registrarPresenca(id: string): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.base}/${id}/presenca`, { resultado: 'Realizado' });
  }

  cancelar(id: string, motivo: string): Observable<void> {
    return this.http.patch<void>(`${this.base}/${id}/cancelar`, { motivo });
  }

  listarPacientes(termo?: string): Observable<PacienteResumo[]> {
    return this.http.get<PacienteResumo[]>(`${environment.apiUrl}/prontuarios/pacientes`, {
      params: termo ? { termo } : {},
    });
  }

  criarPaciente(dados: { nome: string; telefone?: string }): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${environment.apiUrl}/prontuarios/pacientes`, dados);
  }

  listarProfissionais(termo?: string): Observable<ProfissionalResumo[]> {
    return this.http.get<ProfissionalResumo[]>(`${environment.apiUrl}/rh/profissionais`, {
      params: termo ? { termo } : {},
    });
  }

  criarProfissional(dados: { nome: string; especialidades: string }): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${environment.apiUrl}/rh/profissionais`, dados);
  }

  listarTurmas(): Observable<Turma[]> {
    return this.http.get<Turma[]>(`${environment.apiUrl}/turmas`);
  }
}
