import { Injectable, signal } from '@angular/core';
import { SessionStore, TenantInfo } from '../models/session.model';

@Injectable({ providedIn: 'root' })
export class TenantService {
  readonly tenant = signal<TenantInfo | null>(SessionStore.tenant());

  /** Define o tenant ativo (login/onboarding) e persiste a sessão. */
  setTenant(tenant: TenantInfo): void {
    SessionStore.save.bind(SessionStore);
    localStorage.setItem(
      'clinica.tenant',
      JSON.stringify({ tenantId: tenant.tenantId, tenantNome: tenant.tenantNome }),
    );
    this.tenant.set(tenant);
  }
}