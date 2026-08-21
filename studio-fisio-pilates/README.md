# Clinica SaaS — Gestão de Estúdio de Fisioterapia e Pilates

Sistema SaaS multitenant (isolamento lógico via `ClinicaId`) para gestão de
estúdios de fisioterapia e pilates: agenda, prontuário eletrônico, financeiro
e recursos humanos.

## Arquitetura

```
┌─────────────────────────────────────────────────────────────┐
│  Angular 17+ (Standalone Components, Lazy Modules)          │
│  core/ (interceptors X-Tenant-Id + JWT)  · shared/ · modules/│
└──────────────┬──────────────────────────────────────────────┘
               │ HTTPS (JWT Bearer + X-Tenant-Id)
┌──────────────▼──────────────────────────────────────────────┐
│  API .NET 8 — Clean Architecture / DDD                      │
│  ├─ Core/Domain        Entidades, Value Objects, Enums      │
│  ├─ Core/Application   CQRS (MediatR), DTOs, Validation     │
│  ├─ Infrastructure/Persistence  EF Core 8 + Npgsql          │
│  │    TenantDbContext · Global Query Filters · Interceptors │
│  ├─ Infrastructure/CrossCutting   JWT, IoC, Logging         │
│  └─ API                Controllers, Middlewares             │
└──────────────┬──────────────────────────────────────────────┘
               │ PostgreSQL (uma instância, N tenants lógicos)
┌──────────────▼──────────────────────────────────────────────┐
│  Docker Compose (dev)  ·  Kubernetes (prod, manifestos em   │
│  k8s/ com base/ + modules/{api,web})                        │
└─────────────────────────────────────────────────────────────┘
```

### Multitenancy

- Toda entidade de negócio implementa `ITenantEntity` (`ClinicaId`).
- `TenantDbContext` aplica **Global Query Filters** por `ClinicaId`
  (leitura) e `TenantSaveChangesInterceptor` injeta o tenant em inserts
  (escrita).
- O tenant é resolvido por contrato no header `X-Tenant-Id`, autenticado
  pelo token JWT (`tenant_id` claim).

## Estrutura de pastas

```text
├── k8s/                          # Manifestos Kubernetes
│   ├── base/                     # Ingress, ConfigMaps, Secrets, Postgres
│   └── modules/                  # api/ (Deployment+Service) · web/
├── src/
│   ├── backend/                  # Clean Architecture .NET 8
│   │   ├── Core/{Domain,Application}
│   │   ├── Infrastructure/{Persistence,CrossCutting}
│   │   └── API/
│   └── frontend/                 # Angular 17+
│       └── src/app/{core,shared,modules}
└── tests/
    ├── backend/{UnitTests,IntegrationTests}
    └── frontend/e2e              # Cypress
```

## Como executar (dev)

```bash
# 1. Backend (requer .NET 8 SDK)
cd src/backend/API
dotnet restore && dotnet run

# 2. Frontend (requer Node 18+)
cd src/frontend
npm install && ng serve

# 3. Ou tudo via Docker
docker compose up --build
```

Tenant de exemplo: `X-Tenant-Id: 00000000-0000-0000-0000-000000000001` (seed
cria clínica demo, paciente e profissional no primeiro boot dev).
Usuários são criados pelo onboarding: `POST /api/clinicas/onboarding`.

## Qualidade

- Backend: xUnit + Moq + FluentAssertions, Respawn + Testcontainers para
  integração, cobertura alvo ≥ 80% (coverlet).
- Frontend: Jasmine + Karma (unit) e Cypress (E2E).

## Deploy (K8s)

```bash
kubectl apply -k k8s/            # base + módulos
kubectl apply -k k8s/modules/api
kubectl apply -k k8s/modules/web
```

Ver README dos subprojetos para detalhes de cada camada.

### Health checks

| Endpoint       | Uso                  | Verifica                          |
| -------------- | -------------------- | --------------------------------- |
| `/health/live` | livenessProbe (k8s)  | processo vivo                     |
| `/health/ready`| readinessProbe (k8s) | conectividade com Postgres        |
| `/health`      | compat               | tudo                              |

### Observabilidade (OTLP)

Métricas (`api.request.duration`, `api.request.total`) e traces são
exportadas via OTLP quando o endpoint está configurado:

```bash
export OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
```

Sem a variável, o pipeline de telemetria não é registrado (custo zero).

### Backup & restore (Postgres)

```bash
# Backup (diário, retenção 30 dias)
kubectl exec -n clinica deploy/clinica-postgres -- \
  pg_dump -U clinica -Fc clinica > backup-$(date +%F).dump

# Restore
kubectl exec -i -n clinica deploy/clinica-postgres -- \
  pg_restore -U clinica -d clinica --clean --if-exists < backup-2026-08-21.dump
```

### Rotação de secrets

Todos os segredos chegam por variáveis de ambiente (Secret `clinica-secrets`);
nenhum valor sensível vive no repositório:

| Secret                        | Variável                            |
| ----------------------------- | ----------------------------------- |
| Senha do banco                | `ConnectionStrings__Default`        |
| Chave de assinatura JWT       | `JWT__SecretKey`                    |
| Segredo HMAC dos webhooks     | `Webhooks__SecretKey`               |
| Bootstrap de admin (opcional) | `AdminBootstrap__Email` / `__Senha` |

Rotação: atualize o Secret e faça rollout restart. A troca de
`JWT__SecretKey` invalida sessões ativas (usuários refazem login);
a de `Webhooks__SecretKey` deve ser coordenada com o PSP para evitar
rejeição de webhooks em trânsito.