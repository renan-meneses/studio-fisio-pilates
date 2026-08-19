import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

const MENSAGENS_PADRAO: Record<number, string> = {
  400: 'Requisição inválida.',
  401: 'Sessão expirada. Faça login novamente.',
  403: 'Sem permissão para esta operação.',
  404: 'Recurso não encontrado.',
  409: 'Conflito com dados existentes.',
  500: 'Erro interno do servidor.',
};

/** Normaliza erros de API e desloga em 401. */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const auth = inject(AuthService);

  return next(req).pipe(
    catchError((erro: HttpErrorResponse) => {
      if (erro.status === 401 && !req.url.endsWith('/auth/login')) {
        auth.logout();
        void router.navigate(['/login']);
        return throwError(() => erro);
      }
      const mensagem =
        (erro.error as { mensagem?: string; title?: string } | null)?.mensagem ??
        (erro.error as { title?: string } | null)?.title ??
        erro.message ??
        MENSAGENS_PADRAO[erro.status] ??
        'Falha de comunicação com o servidor.';
      return throwError(() => new Error(mensagem));
    }),
  );
};