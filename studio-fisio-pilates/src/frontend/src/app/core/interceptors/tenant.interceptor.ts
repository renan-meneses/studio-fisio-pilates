import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { environment } from '../../../environments/environment';
import { TenantService } from '../services/tenant.service';

/** Injeta o cabeçalho X-Tenant-Id em toda requisição para a API. */
export const tenantInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith(environment.apiUrl)) {
    return next(req);
  }
  const tenant = inject(TenantService).tenant();
  if (!tenant?.tenantId) {
    return next(req);
  }
  const clonada = req.clone({
    setHeaders: { [environment.tenantHeader]: tenant.tenantId },
  });
  return next(clonada);
};