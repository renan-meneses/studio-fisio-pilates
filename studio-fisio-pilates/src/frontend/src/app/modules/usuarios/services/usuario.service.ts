import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { Papel, Usuario } from '../models/usuario.model';

@Injectable({ providedIn: 'root' })
export class UsuarioService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/usuarios`;

  listar(): Observable<Usuario[]> {
    return this.http.get<Usuario[]>(this.base);
  }

  criar(requisicao: { nome: string; email: string; senha: string; papel: Papel }): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.base, requisicao);
  }

  alterarStatus(id: string, ativo: boolean): Observable<void> {
    return this.http.patch<void>(`${this.base}/${id}/status`, { ativo });
  }

  redefinirSenha(id: string, novaSenha: string): Observable<void> {
    return this.http.patch<void>(`${this.base}/${id}/senha`, { novaSenha });
  }

  alterarSenhaPropria(senhaAtual: string, novaSenha: string): Observable<void> {
    return this.http.patch<void>(`${environment.apiUrl}/auth/senha`, { senhaAtual, novaSenha });
  }
}
