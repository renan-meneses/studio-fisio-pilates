import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  CriarAnamneseRequest,
  CriarEvolucaoRequest,
  PacienteCompleto,
  PacienteResumo,
} from '../models/prontuario.model';

@Injectable({ providedIn: 'root' })
export class ProntuarioService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/prontuarios`;

  listarPacientes(termo?: string): Observable<PacienteResumo[]> {
    return this.http.get<PacienteResumo[]>(`${this.base}/pacientes`, {
      params: termo ? { termo } : {},
    });
  }

  obterPaciente(id: string): Observable<PacienteCompleto> {
    return this.http.get<PacienteCompleto>(`${this.base}/${id}`);
  }

  criarAnamnese(pacienteId: string, requisicao: CriarAnamneseRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.base}/${pacienteId}/anamneses`, requisicao);
  }

  criarEvolucao(pacienteId: string, requisicao: CriarEvolucaoRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.base}/${pacienteId}/evolucoes`, requisicao);
  }
}