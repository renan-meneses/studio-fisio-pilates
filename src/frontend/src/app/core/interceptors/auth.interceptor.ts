import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { SessionStore } from '../models/session.model';

/** Anexa o Bearer token quando a sessão existe. */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = SessionStore.token();
  if (!token || !req.url.startsWith(environment.apiUrl)) {
    return next(req);
  }
  const clonada = req.clone({
    setHeaders: { Authorization: `Bearer ${token}` },
  });
  return next(clonada);
};