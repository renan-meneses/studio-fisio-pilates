import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { CriarPlanoRequest, Plano } from '../models/plano.model';

@Injectable({ providedIn: 'root' })
export class PlanoService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/planos`;

  listar(): Observable<Plano[]> {
    return this.http.get<Plano[]>(this.base);
  }

  criar(requisicao: CriarPlanoRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.base, requisicao);
  }

  adicionarServico(planoId: string, servicoId: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${planoId}/servicos`, { servicoId });
  }

  removerServico(planoId: string, servicoId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${planoId}/servicos/${servicoId}`);
  }
}