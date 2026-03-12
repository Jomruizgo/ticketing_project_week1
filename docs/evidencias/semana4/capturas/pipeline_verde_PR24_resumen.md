# Evidencia: Pipeline Verde — PR #24 (release/v3.0.1 → main)

**Fecha**: 2026-03-09T05:35–05:37 UTC  
**Run URL**: https://github.com/Jomruizgo/ticketing_project_week1/actions/runs/22839897387  
**Conclusión**: ✅ **success** — 9/9 jobs verdes

---

## Resumen de ejecución

```
Workflow: CI — Pipeline Multinivel TicketRush
Run #11 | PR #24 | release/v3.0.1 → main
Trigger: pull_request
SHA: eb445ccbeabf5b4dd7a94cd94a477e31511ffe9e
Duración total: ~2 min 4 seg
```

---

## Resultados por nivel de testing

### 🧪 Unit Tests — Caja Blanca (Job: 66243874321)

| Suite | Resultado | Passed | Failed | Total | Duración |
|---|---|---|---|---|---|
| CrudService.Application.Tests | ✅ Passed | 28 | 0 | 28 | 166 ms |
| ReservationService.Domain.Tests | ✅ Passed | 9 | 0 | 9 | 14 ms |
| ReservationService.Infrastructure.Tests | ✅ Passed | 7 | 0 | 7 | — |
| MsPaymentService.Application.Tests | ✅ Passed | 25 | 0 | 25 | — |
| Producer unit tests | ✅ Passed | — | 0 | — | — |

**Artifacts**: `unit-test-results/` → `unit-crud-app.trx`, `unit-reservation-domain.trx`, `unit-reservation.trx`, `unit-payment.trx`, `unit-producer.trx`

### 🔗 Component Tests — Integración Interna

**Artifact**: `component-test-results/component-crud.trx`

### 🌐 Integration Tests — Contratos entre Servicios

**Artifacts**: `integration-test-results/integration-crud-sse.trx`, `integration-reservation.trx`

### 📦 Black-Box Tests — API HTTP Real

**Artifact**: `blackbox-test-results/blackbox-crud-api.trx`  
Tipo: Pruebas HTTP reales sobre `CrudService.Api` usando `WebApplicationFactory` (sin mocks de infraestructura HTTP)

---

## 🐳 Docker Build & Trivy Scan

| Servicio | Build | Vulnerabilidades detectadas |
|---|---|---|
| producer | ✅ | 0 |
| crud-service | ✅ | 0 |
| reservation-service | ✅ | 0 |
| payment-service | ✅ | 1 HIGH (CVE-2024-43483, ya tiene fix disponible) |

**Detalle payment-service**:
- Librería: `Microsoft.Extensions.Caching.Memory 8.0.0`
- CVE: CVE-2024-43483 (hash flooding)
- Severidad: HIGH
- Fix disponible: versión 8.0.1+
- Decisión: documentada, requiere actualización de dependencia

---

## Historia de ejecuciones del PR #24

| Run | Conclusión | Motivo del fallo |
|---|---|---|
| #8 (22839521421) | ❌ failure | PaymentService path incorrecto en ci.yml |
| #9 (22839559072) | ❌ failure | Duplicate DTOs (DTOs/ vs Dtos/) + TicketingDbContext duplicado |
| #10 (22839812863) | ❌ failure | Conflicto en ProcessApprovedPaymentCommandHandlerTests.cs |
| **#11 (22839897387)** | ✅ **success** | **9/9 jobs verdes** |

> Los runs fallidos son evidencia de que el CI efectivamente **bloqueó** la integración de código defectuoso antes del merge.
