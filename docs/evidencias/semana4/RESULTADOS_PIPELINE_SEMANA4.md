# Resultados y Evidencias de Pipeline — Semana 4

## Estado actual

> ✅ **Pipeline ejecutado y verificado** — Release v3.0.1 mergeado a `main` con 9/9 jobs verdes.  
> Fecha de ejecución: **2026-03-09**

## 1. Registro de ejecuciones

| Fecha | Run # | Workflow / job | Trigger | Resultado | Enlace |
|---|---|---|---|---|---|
| 2026-03-09 | #6 | CI completo (PR #23) | `pull_request` | ✅ 9/9 verde | [Run #6](https://github.com/Jomruizgo/ticketing_project_week1/actions/runs/22839101202) |
| 2026-03-09 | #8 | CI completo (PR #24) | `pull_request` | ❌ failure | [Run #8](https://github.com/Jomruizgo/ticketing_project_week1/actions/runs/22839521421) |
| 2026-03-09 | #9 | CI completo (PR #24) | `pull_request` | ❌ failure | [Run #9](https://github.com/Jomruizgo/ticketing_project_week1/actions/runs/22839559072) |
| 2026-03-09 | #10 | CI completo (PR #24) | `pull_request` | ❌ failure | [Run #10](https://github.com/Jomruizgo/ticketing_project_week1/actions/runs/22839812863) |
| 2026-03-09 | **#11** | **CI completo (PR #24)** | `pull_request` | ✅ **9/9 verde** | [**Run #11**](https://github.com/Jomruizgo/ticketing_project_week1/actions/runs/22839897387) |
| 2026-03-09 | #12 | CI post-merge | `push` a `main` | ✅ success | [Run #12](https://github.com/Jomruizgo/ticketing_project_week1/actions/runs/22839962322) |
| 2026-03-09 | #13 | CI post-merge | `push` a `develop` | ✅ success | [Run #13](https://github.com/Jomruizgo/ticketing_project_week1/actions/runs/22839984867) |

> Los runs #8, #9, #10 son evidencia de que el CI **bloqueó efectivamente** la integración de código defectuoso. Sólo el run #11 (con todos los problemas corregidos) fue permitido para merge.

---

## 2. Resultados por job — Run #11 (PR #24, el definitivo)

**URL**: https://github.com/Jomruizgo/ticketing_project_week1/actions/runs/22839897387

### 2.1 Build — Todos los servicios ✅

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

Servicios compilados:
- `MsPaymentService.Domain` → bin/Release/net8.0
- `MsPaymentService.Application` → bin/Release/net8.0
- `MsPaymentService.Infrastructure` → bin/Release/net8.0
- `MsPaymentService.Worker` → bin/Release/net8.0
- `CrudService.*`, `ReservationService.*`, `Producer.*`

### 2.2 Unit Tests — Caja Blanca ✅

| Suite | Passed | Failed | Total | Duración | Artifact |
|---|---|---|---|---|---|
| CrudService.Application.Tests | 28 | 0 | **28** | 166 ms | `unit-crud-app.trx` |
| ReservationService.Domain.Tests | 9 | 0 | **9** | 14 ms | `unit-reservation-domain.trx` |
| ReservationService.Infrastructure.Tests | 7 | 0 | **7** | — | `unit-reservation.trx` |
| MsPaymentService.Application.Tests | 25 | 0 | **25** | — | `unit-payment.trx` |
| Producer.Tests | — | 0 | — | — | `unit-producer.trx` |

**Artifacts en**: `artifacts/unit-tests/`

### 2.3 Component Tests — Integración Interna ✅

Pruebas de componente del CrudService (integración interna de capas hexagonales).

**Artifact**: `artifacts/component/component-crud.trx`

### 2.4 Integration Tests — Contratos entre Servicios ✅

Pruebas de integración que verifican contratos entre servicios.

**Artifacts**: `artifacts/integration/integration-crud-sse.trx`, `integration-reservation.trx`

### 2.5 Black-Box Tests — API HTTP Real ✅

9 pruebas HTTP reales sobre `CrudService.Api` usando `WebApplicationFactory<Program>`.  
Sin mocks de la capa HTTP — las peticiones traversan toda la cadena de middleware real.

**Por qué es Caja Negra**: el test no conoce la implementación interna de los handlers, repositorios ni contexto de BD. Solo envía peticiones HTTP y verifica respuestas observables (códigos de estado, cuerpos JSON).

**Artifact**: `artifacts/blackbox/blackbox-crud-api.trx`

### 2.6 Escaneo de imagen — Trivy ✅

| Servicio | OS Base | Vulnerabilidades OS | Vulnerabilidades App | Secretos |
|---|---|---|---|---|
| producer | Debian 12.13 | 0 | 0 | - |
| crud-service | Debian 12.13 | 0 | 0 | - |
| reservation-service | Debian 12.13 | 0 | 0 | - |
| payment-service | Debian 12.13 | 0 | **1 HIGH** | - |

**Hallazgo en payment-service**:
- Librería: `Microsoft.Extensions.Caching.Memory 8.0.0`
- CVE: **CVE-2024-43483** (hash flooding, susceptibilidad a DoS)
- Severidad: HIGH — Status: **fixed** (disponible en 8.0.1+)
- Decisión: documentado, requiere actualización de dependencia en próximo sprint

**Artifacts Trivy**: `artifacts/trivy/trivy-*.txt` (4 reportes, uno por servicio)

---

## 3. Evidencia de PR y release GitFlow

| Evidencia | Estado | Ubicación |
|---|---|---|
| PR #23: feature → develop | ✅ Merged (2026-03-09) | [PR #23](https://github.com/Jomruizgo/ticketing_project_week1/pull/23) · `capturas/PR23_feature_a_develop.md` |
| Pipeline PR #23 | ✅ 9/9 verde | [Run #6](https://github.com/Jomruizgo/ticketing_project_week1/actions/runs/22839101202) |
| PR #24: release → main | ✅ Merged (2026-03-09) | [PR #24](https://github.com/Jomruizgo/ticketing_project_week1/pull/24) · `capturas/PR24_release_a_main.md` |
| Pipeline PR #24 | ✅ 9/9 verde | [Run #11](https://github.com/Jomruizgo/ticketing_project_week1/actions/runs/22839897387) · `capturas/pipeline_verde_PR24_resumen.md` |
| Tag v3.0.1 | ✅ Publicado | [v3.0.1](https://github.com/Jomruizgo/ticketing_project_week1/releases/tag/v3.0.1) |
| Back-merge main → develop | ✅ Completado | Run #13 en develop |

---

## 4. Notas para defensa

- **Separación real de jobs**: El pipeline tiene 6 tipos de jobs diferenciados: Build, Unit (caja blanca), Component, Integration, Black-Box, Docker+Trivy. Cada uno tiene su propia finalidad y puede fallar independientemente.
- **Caja Negra defendible**: `WebApplicationFactory<Program>` con `Microsoft.AspNetCore.Mvc.Testing` levanta el host real de ASP.NET Core. No hay mocks de HTTP. Los tests verifican comportamiento observable desde el exterior.
- **Bloqueo efectivo del CI**: Los runs #8, #9, #10 fallaron por errores reales de compilación y tests rotos. El merge sólo se permitió después del run #11 (verde).
- **Trivy activo**: El escaneo de imagen se ejecuta en cada PR. El hallazgo en payment-service (CVE-2024-43483) fue detectado automáticamente y queda registrado en `artifacts/trivy/trivy-payment-service.txt`.
