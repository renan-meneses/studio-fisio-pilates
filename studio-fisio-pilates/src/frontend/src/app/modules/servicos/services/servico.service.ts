import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { CriarServicoRequest, Servico } from '../models/servico.model';

@Injectable({ providedIn: 'root' })
export class ServicoService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/servicos`;

  listar(): Observable<Servico[]> {
    return this.http.get<Servico[]>(this.base);
  }

  criar(requisicao: CriarServicoRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.base, requisicao);
  }
}