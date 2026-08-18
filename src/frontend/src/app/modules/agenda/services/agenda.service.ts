import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  Agendamento,
  CancelarAgendamentoRequest,
  CriarAgendamentoRequest,
} from '../models/agendamento.model';

@Injectable({ providedIn: 'root' })
export class AgendaService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/agenda`;

  listar(dataInicio: string, dataFim: string): Observable<Agendamento[]> {
    return this.http.get<Agendamento[]>(this.base, {
      params: { dataInicio, dataFim },
    });
  }

  criar(requisicao: CriarAgendamentoRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.base, requisicao);
  }

  confirmar(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/confirmar`, {});
  }

  registrarPresenca(id: string): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.base}/${id}/presenca`, {});
  }

  cancelar(id: string, motivo: CancelarAgendamentoRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/cancelar`, motivo);
  }
}