import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { TenantService } from '../services/tenant.service';

/** Exige tenant ativo antes de carregar os módulos de negócio. */
export const tenantGuard: CanActivateFn = () => {
  const tenant = inject(TenantService).tenant();
  return tenant?.tenantId ? true : inject(Router).createUrlTree(['/login']);
};