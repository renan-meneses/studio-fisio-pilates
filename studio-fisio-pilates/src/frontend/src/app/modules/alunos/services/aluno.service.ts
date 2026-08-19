import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { Aluno, CriarAlunoRequest } from '../models/aluno.model';

@Injectable({ providedIn: 'root' })
export class AlunoService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/alunos`;

  listar(): Observable<Aluno[]> {
    return this.http.get<Aluno[]>(this.base);
  }

  criar(requisicao: CriarAlunoRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.base, requisicao);
  }
}