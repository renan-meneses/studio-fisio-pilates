import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { DashboardResumo, FaturamentoItem, OcupacaoDia, TopSessao } from '../models/dashboard.model';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/relatorios`;

  resumo(): Observable<DashboardResumo> {
    return this.http.get<DashboardResumo>(`${this.base}/resumo`);
  }

  faturamento(meses = 6): Observable<FaturamentoItem[]> {
    return this.http.get<FaturamentoItem[]>(`${this.base}/faturamento`, { params: { meses } });
  }

  ocupacao(dias = 30): Observable<OcupacaoDia[]> {
    return this.http.get<OcupacaoDia[]>(`${this.base}/ocupacao`, { params: { dias } });
  }

  topSessoes(top = 5): Observable<TopSessao[]> {
    return this.http.get<TopSessao[]>(`${this.base}/top-sessoes`, { params: { top } });
  }
}
