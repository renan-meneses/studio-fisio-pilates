import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  FolhaSalarial,
  Funcionario,
  LancarPontoRequest,
  ProcessarFolhaRequest,
  RegistroPonto,
} from '../models/rh.model';

@Injectable({ providedIn: 'root' })
export class RhService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/rh`;

  listarFuncionarios(): Observable<Funcionario[]> {
    return this.http.get<Funcionario[]>(`${this.base}/funcionarios`);
  }

  registrarFuncionario(requisicao: { nome: string; cargo: string }): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.base}/funcionarios`, requisicao);
  }

  lancarPonto(funcionarioId: string, requisicao: LancarPontoRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.base}/funcionarios/${funcionarioId}/ponto`, requisicao);
  }

  listarPonto(funcionarioId: string, dataInicio: string, dataFim: string): Observable<RegistroPonto[]> {
    return this.http.get<RegistroPonto[]>(`${this.base}/ponto`, {
      params: { funcionarioId, dataInicio, dataFim },
    });
  }

  listarFolha(competencia: string): Observable<FolhaSalarial[]> {
    return this.http.get<FolhaSalarial[]>(`${this.base}/folha`, {
      params: { competencia },
    });
  }

  processarFolha(funcionarioId: string, requisicao: ProcessarFolhaRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.base}/folha/processar`, {
      funcionarioId,
      ...requisicao,
    });
  }

  pagarFolha(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/folha/${id}/pagar`, {});
  }
}