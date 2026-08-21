import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  CobrarMensalidadeRequest,
  ContaPagar,
  CriarContaPagarRequest,
  FinanceiroResumo,
  Inadimplencia,
  Mensalidade,
  ReceberMensalidadeRequest,
} from '../models/financeiro.model';
import { Cobranca } from '../models/financeiro.model';

@Injectable({ providedIn: 'root' })
export class FinanceiroService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/financeiro`;

  resumo(competencia: string): Observable<FinanceiroResumo> {
    return this.http.get<FinanceiroResumo>(`${this.base}/resumo`, {
      params: { competencia },
    });
  }

  listarMensalidades(competencia: string): Observable<Mensalidade[]> {
    return this.http.get<Mensalidade[]>(`${this.base}/mensalidades`, {
      params: { competencia },
    });
  }

  cobrarMensalidade(requisicao: CobrarMensalidadeRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.base}/mensalidades`, requisicao);
  }

  receberMensalidade(id: string, requisicao?: ReceberMensalidadeRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/mensalidades/${id}/receber`, requisicao ?? {});
  }

  cancelarMensalidade(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/mensalidades/${id}/cancelar`, {});
  }

  listarContas(competencia: string): Observable<ContaPagar[]> {
    return this.http.get<ContaPagar[]>(`${this.base}/contas-pagar`, {
      params: { competencia },
    });
  }

  criarConta(requisicao: CriarContaPagarRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.base}/contas-pagar`, requisicao);
  }

  pagarConta(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/contas-pagar/${id}/pagar`, {});
  }

  // ===== Fase 3: cobranças, inadimplência e faturamento recorrente =====

  private readonly mensalidadesBase = `${environment.apiUrl}/mensalidades`;

  faturarRecorrente(competencia: string): Observable<{ geradas: number; jaExistentes: number }> {
    return this.http.post<{ geradas: number; jaExistentes: number }>(
      `${this.base}/faturamento-recorrente`,
      { competencia },
    );
  }

  inadimplencia(): Observable<Inadimplencia> {
    return this.http.get<Inadimplencia>(`${this.base}/inadimplencia`);
  }

  emitirCobranca(mensalidadeId: string, tipo: 'Pix' | 'Boleto'): Observable<Cobranca> {
    const valorTipo = tipo === 'Pix' ? 1 : 2;
    return this.http.post<Cobranca>(
      `${this.mensalidadesBase}/${mensalidadeId}/cobrancas`,
      { tipo: valorTipo },
    );
  }

  listarCobrancas(mensalidadeId: string): Observable<Cobranca[]> {
    return this.http.get<Cobranca[]>(
      `${this.mensalidadesBase}/${mensalidadeId}/cobrancas`,
    );
  }
}