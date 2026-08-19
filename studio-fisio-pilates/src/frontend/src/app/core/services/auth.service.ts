import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LoginResponse, SessionStore } from '../models/session.model';

export interface Credenciais {
  email: string;
  senha: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  login(credenciais: Credenciais): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${environment.apiUrl}/auth/login`, credenciais)
      .pipe(
        tap(SessionStore.save),
        map(resposta => resposta),
      );
  }

  logout(): void {
    SessionStore.clear();
  }

  atualizarTema(tema: 'Claro' | 'Escuro'): Observable<void> {
    return this.http.patch<void>(`${environment.apiUrl}/auth/tema`, { tema });
  }

  isAuthenticated(): boolean {
    return SessionStore.token() !== null;
  }
}