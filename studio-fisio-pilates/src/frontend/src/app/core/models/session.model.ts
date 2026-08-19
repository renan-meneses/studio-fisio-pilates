export interface LoginResponse {
  accessToken: string;
  expiresAt: string;
  tenantId: string;
  tenantNome: string;
  usuarioId: string;
  nome: string;
  papel: string;
}

export interface DecodedToken {
  sub: string;
  email: string;
  tenant_id: string;
  tenant_name: string;
  exp: number;
  iat: number;
}

export interface TenantInfo {
  tenantId: string;
  tenantNome?: string;
}

const STORAGE_KEYS = {
  token: 'clinica.access_token',
  tenant: 'clinica.tenant',
  user: 'clinica.user',
} as const;

export class SessionStore {
  static save(login: LoginResponse): void {
    localStorage.setItem(STORAGE_KEYS.token, login.accessToken);
    localStorage.setItem(
      STORAGE_KEYS.tenant,
      JSON.stringify({ tenantId: login.tenantId, tenantNome: login.tenantNome }),
    );
    localStorage.setItem(
      STORAGE_KEYS.user,
      JSON.stringify({ usuarioId: login.usuarioId, nome: login.nome, papel: login.papel }),
    );
  }

  static token(): string | null {
    return localStorage.getItem(STORAGE_KEYS.token);
  }

  static tenant(): TenantInfo | null {
    const raw = localStorage.getItem(STORAGE_KEYS.tenant);
    return raw ? (JSON.parse(raw) as TenantInfo) : null;
  }

  static userName(): string | null {
    const raw = localStorage.getItem(STORAGE_KEYS.user);
    return raw ? ((JSON.parse(raw) as { nome: string }).nome) : null;
  }

  static clear(): void {
    localStorage.removeItem(STORAGE_KEYS.token);
    localStorage.removeItem(STORAGE_KEYS.tenant);
    localStorage.removeItem(STORAGE_KEYS.user);
  }
}