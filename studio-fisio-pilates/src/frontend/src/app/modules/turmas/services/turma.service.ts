import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { HorarioNovo, Turma } from '../models/turma.model';
import { TipoSessao } from '../../agenda/models/agendamento.model';

@Injectable({ providedIn: 'root' })
export class TurmaService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/turmas`;

  listar(): Observable<Turma[]> {
    return this.http.get<Turma[]>(this.base);
  }

  criar(requisicao: {
    nome: string;
    tipoSessao: TipoSessao;
    profissionalId?: string;
    horarios?: HorarioNovo[];
  }): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.base, requisicao);
  }

  adicionarHorario(turmaId: string, horario: HorarioNovo): Observable<void> {
    return this.http.post<void>(`${this.base}/${turmaId}/horarios`, horario);
  }

  removerHorario(turmaId: string, horarioId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${turmaId}/horarios/${horarioId}`);
  }
}