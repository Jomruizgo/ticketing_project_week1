# 📋 Informe de Exposición — Semana 3
## "El Pipeline Inquebrantable y Multinivel"

> **Proyecto:** TicketRush — Sistema distribuido de venta de tickets  
> **Release:** v3.0.0  
> **Equipo:** Sofka Technologies  

---

## 🧭 ¿Qué se hizo esta semana? (Para todos los públicos)

Imagina que tu equipo construyó un carro. La semana pasada le pusieron el motor y le dieron forma. Esta semana instalaron el **sistema de control de calidad automático**: cada vez que alguien toca el código, una serie de pruebas se ejecuta sola, como si el carro se probara en una pista de pruebas antes de salir a la calle.

Además, empacaron el carro en una caja estándar (Docker) para que **funcione igual en cualquier computadora**, eliminando para siempre el problema de *"en mi máquina sí funciona"*.

---

## 1. 🏗️ Infraestructura Inmutable — Los contenedores seguros

### ¿Qué es Docker y por qué importa?

Docker es como un **contenedor de carga marítimo**: el software y todo lo que necesita para funcionar viaja dentro, sin importar si el barco es un barco de carga, un velero o un portaaviones. El resultado es siempre el mismo.

### ¿Qué se mejoró?

Todos los servicios del proyecto (CrudService, Producer, PaymentService, ReservationService) fueron actualizados con Dockerfiles optimizados:

| Mejora | Descripción técnica | ¿Qué significa? |
|---|---|---|
| **Multi-stage build** | El compilador y el ejecutable se separan en etapas | La imagen final es más pequeña y limpia |
| **Usuario no-root** | El proceso corre como `appuser`, no como administrador | Si alguien hackea el contenedor, tiene permisos mínimos |
| **HEALTHCHECK** | El contenedor se auto-examina periódicamente | Docker reinicia el servicio si detecta que murió |
| **ARG BUILD_CONFIGURATION** | El modo de compilación es configurable | Se puede compilar en modo Debug o Release sin editar el Dockerfile |
| **`--no-restore` y `UseAppHost=false`** | Optimizaciones en la compilación | Builds más rápidos y sin archivos innecesarios |

**Analogía:** Es como construir un auto en una fábrica limpia (multi-stage), entregarlo con el volante bloqueado para que el conductor no pueda hacer maniobras peligrosas (no-root), y con un sensor que enciende una luz de advertencia si algo falla (HEALTHCHECK).

---

## 2. 🔁 Pipeline CI/CD — La pista de pruebas automática

### ¿Qué es un pipeline CI/CD?

Es una **línea de producción automática** que se activa cada vez que alguien sube código al repositorio. Si algo está roto, el pipeline lo detecta y **bloquea** que ese código llegue a producción.

**Archivo creado:** `.github/workflows/ci.yml`

### Los 6 pasos (Jobs) del pipeline

```
┌──────────────┐
│  1. 🔨 BUILD │  ← Compila los 4 servicios
└──────┬───────┘
       │
       ├──────────────────────────────────────────────┐
       │                                              │
┌──────▼──────────┐  ┌──────────────────┐  ┌─────────▼──────────┐  ┌──────────────────────┐
│ 2. 🧪 UNIT      │  │ 3. 🔗 COMPONENT  │  │ 4. 🌐 INTEGRATION  │  │ 5. 📦 BLACK-BOX      │
│ Lógica interna  │  │ Módulos juntos   │  │ Contratos API/RMQ  │  │ API HTTP desde afuera│
└──────┬──────────┘  └────────┬─────────┘  └──────────┬─────────┘  └──────────┬───────────┘
       │                      │                        │                        │
       └──────────────────────┴────────────────────────┴────────────────────────┘
                                                       │
                                          ┌────────────▼────────────┐
                                          │ 6. 🐳 DOCKER + TRIVY    │
                                          │ Build imágenes +        │
                                          │ Escaneo vulnerabilidades│
                                          └─────────────────────────┘
```

**Clave:** Los jobs de pruebas corren **en paralelo** después del build. Solo si todos pasan, se construyen y escanean las imágenes Docker.

---

## 3. 🧪 Estrategia de Testing Multinivel

### Los 7 Principios del Testing aplicados al proyecto

| Principio | Descripción | Cómo se aplica en TicketRush |
|---|---|---|
| **1. Las pruebas muestran defectos, no su ausencia** | Pasar todas las pruebas no garantiza que el software sea perfecto | Usamos múltiples niveles para maximizar la detección |
| **2. El testing exhaustivo es imposible** | No se pueden probar todas las combinaciones posibles | Se usan particiones equivalentes (casos representativos) |
| **3. Testing temprano** | Detectar bugs antes cuesta menos | Las pruebas de dominio son las más baratas y las primeras en correr |
| **4. Clustering de defectos** | Los bugs se acumulan en partes específicas | Los tests se concentran en la lógica de reserva y pago |
| **5. Paradoja del pesticida** | Las mismas pruebas siempre pierden efectividad | Se mezclan tipos: unitarias, componente, integración y caja negra |
| **6. El testing depende del contexto** | No existe un enfoque único | El foco es backend distribuido: RabbitMQ, SSE y concurrencia |
| **7. La falacia de la ausencia de errores** | Un software sin bugs conocidos no es necesariamente útil | Los tests validan el comportamiento esperado del negocio |

### Los 4 niveles de prueba y cuántos tests hay

```
                          🔺 PIRÁMIDE DE TESTING
                         
                    ╔══════════════════════╗
                    ║   📦 CAJA NEGRA      ║  ← 9 tests — API HTTP real
                    ║   (Black-box)        ║     (nuevo esta semana)
                    ╠══════════════════════╣
                    ║   🌐 INTEGRACIÓN     ║  ← Tests contrato SSE + RabbitMQ
                    ║   (Integration)      ║
                    ╠══════════════════════╣
                    ║   🔗 COMPONENTE      ║  ← Tests pipeline mensajería
                    ║   (Component)        ║
                    ╠══════════════════════╣
                    ║   🧪 UNITARIAS       ║  ← 34+ tests lógica de negocio
                    ║   (Unit / Caja       ║     (ReservationService,
                    ║    Blanca)           ║      PaymentService, Producer,
                    ╚══════════════════════╝      CrudService)
```

#### Resumen de pruebas por servicio

| Servicio | Tipo | Tests | Descripción |
|---|---|---|---|
| `CrudService.Api.Tests` | **Caja Negra** 📦 | 9 | Llama a la API HTTP sin conocer su implementación |
| `CrudService.Application.Tests` | Caja Blanca 🧪 | 9 | Lógica del TicketService (alta, baja, liberación) |
| `CrudService.Infrastructure.Tests` | Componente + Integración | 5+ | Pipeline SSE y contrato de mensajes |
| `ReservationService.Application.Tests` | Caja Blanca 🧪 | Tests handlers | Reserva y expiración de tickets |
| `ReservationService.Domain.Tests` | Dominio 🧪 | 9 | Entidad Ticket: estados, expiración, campos |
| `ReservationService.Infrastructure.Tests` | Infraestructura 🔧 | 7 | Configuración RabbitMQ y contratos |
| `PaymentService.Tests` | Caja Blanca 🧪 | 4 | Validación y manejo de pagos |
| `Producer.Application.Tests` | Caja Blanca 🧪 | 2 | Comandos de reserva y pago |

---

## 4. 📦 La Prueba de Caja Negra — El cliente externo simulado

### ¿Qué es una prueba de Caja Negra?

Es una prueba donde el evaluador **no sabe ni le importa** cómo está hecho el software por dentro. Solo interactúa con lo que ve: las entradas y las salidas.

**Analogía:** Cuando compras un café en una máquina expendedora, presionas el botón y esperas el café. No sabes si es un robot o un humano quien lo prepara. Eso es una prueba de caja negra.

### ¿Qué se probó?

**Archivo:** `crud_service/tests/CrudService.Api.Tests/TicketsApiBlackBoxTests.cs`

El test levanta el servidor HTTP real de CrudService (con mocks en lugar de la base de datos real) y llama a los endpoints como lo haría cualquier cliente:

| Test | Endpoint | Qué valida |
|---|---|---|
| `GET /health` | → 200 OK | El servicio está vivo |
| `GET /api/tickets/health` | → 200 OK | El controlador responde |
| `POST /api/tickets/bulk` con cantidad 0 | → 400 Bad Request | No acepta datos inválidos |
| `POST /api/tickets/bulk` con cantidad 9999 | → 400 Bad Request | Límite de cantidad respetado |
| `POST /api/tickets/bulk` con EventId 0 | → 400 Bad Request | No acepta evento inválido |
| `PUT /api/tickets/{id}/status` con estado vacío | → 400 Bad Request | Estado requerido |
| `GET /api/tickets/99999` | → 404 Not Found | Ticket inexistente detectado |
| `GET /api/tickets/42` (mock) | → 200 OK con JSON | Respuesta correcta para ticket existente |
| `GET /api/tickets/event/5` (mock) | → 200 OK con lista | Lista de tickets por evento |

---

## 5. 🔒 Escaneo de Vulnerabilidades — Trivy

### ¿Qué es Trivy?

Trivy es como un **escáner antivirus para imágenes Docker**. Analiza todos los paquetes instalados en el contenedor y detecta vulnerabilidades de seguridad conocidas (CVEs).

### ¿Cómo se integra?

En el pipeline, después de construir cada imagen Docker, Trivy la escanea automáticamente:

```yaml
- name: Run Trivy vulnerability scanner
  uses: aquasecurity/trivy-action@master
  with:
    image-ref: "ticketrush/crud-service:abc123"
    severity: "CRITICAL,HIGH"
```

El reporte queda disponible como artefacto descargable en cada ejecución del pipeline.

---

## 6. 🌊 GitFlow y Release — La estrategia de ramificación

### ¿Qué es GitFlow?

GitFlow es como un **protocolo de tránsito aéreo** para el código:
- `feature/*` — Los aviones en construcción (nuevas funcionalidades)
- `develop` — La pista de aterrizaje (código listo pero aún no en producción)
- `main` — El aeropuerto internacional (producción)

Solo los aviones que pasaron todas las inspecciones (pruebas) pueden llegar a `main`.

### Evidencia del flujo seguido

```
feature/semana4-plan-pruebas-evidencias ──┐
                                          ▼
                                    PR #21 (aprobado + merged)
                                          │
                                          ▼
develop ◄────────────────────────────────── (CI pipeline verde)
  │
  │  + commits: CI workflow, black-box tests, tests vacíos
  │
  ▼
PR #22: develop → main
  │
  ▼
main ◄──── merge release
  │
  ▼
🏷️  tag v3.0.0
```

---

## 7. 🏗️ Decisión Arquitectónica — Eliminación del Cron

### ¿Qué fue eliminado y por qué?

El sistema tenía un contenedor Alpine que cada minuto ejecutaba un script SQL para liberar tickets vencidos. Este mecanismo fue **reemplazado** por una solución basada en RabbitMQ que ya existía en el `ReservationService`.

| Aspecto | Mecanismo anterior (Cron) | Mecanismo actual (RabbitMQ) |
|---|---|---|
| **Forma** | Script SQL directo a BD | Mensaje con TTL → consumidor |
| **Precisión** | ±1 minuto | Al segundo exacto |
| **Acoplamiento** | Script conoce esquema de BD | Solo el servicio conoce su BD |
| **Consistencia** | Viola arquitectura hexagonal | Sigue el patrón del proyecto |
| **Observabilidad** | Logs difíciles de rastrear | Trazabilidad en RabbitMQ |

---

## 8. ✅ Checklist de entregables de la rúbrica

| Entregable | Estado | Evidencia |
|---|---|---|
| Dockerfile optimizado y seguro | ✅ Completo | Todos los 4 servicios actualizados |
| Pipeline YAML en `.github/workflows/` | ✅ Completo | `ci.yml` con 6 jobs diferenciados |
| Jobs separados: Componente vs Integración | ✅ Completo | `test-component` y `test-integration` |
| Pruebas de Caja Blanca | ✅ Completo | 34+ tests en Application, Domain |
| Prueba de Caja Negra (API real) | ✅ Completo | `CrudService.Api.Tests` — 9 tests |
| Escaneo de vulnerabilidades (Trivy) | ✅ Completo | Job `docker-security` en pipeline |
| TEST_PLAN.md como informe técnico | ✅ Completo | `docs/evidencias/semana4/TEST_PLAN.md` |
| GitFlow con PR documentado | ✅ Completo | PR #21 y PR #22 en GitHub |
| Release formal con tag semántico | ✅ Completo | Tag `v3.0.0` en `main` |

---

## 9. 🧠 Human Check — ¿Cómo se auditó la IA?

La rúbrica exige demostrar criterio humano sobre el código generado por IA. Estos son los puntos de auditoría:

### 🔍 ¿Qué delegamos a la IA?

- Estructura inicial de los Dockerfiles optimizados
- Plantilla del workflow CI/CD
- Esqueleto de los archivos de test

### ✅ ¿Cómo auditamos las respuestas?

| Elemento | Auditoría aplicada |
|---|---|
| `WebApplicationFactory` en tests | Verificamos que los mocks reemplazaran correctamente los servicios de infraestructura sin depender de BD real |
| Job `docker-security` con Trivy | Confirmamos que `exit-code: "0"` es intencional — no bloquea por ahora, para no frenar el pipeline en imágenes base con CVEs conocidos pero no parchables |
| Filtros `Category=Component` / `Category=Integration` | Verificamos que los traits existieran en el código (`[Trait("Category", "...")]`) antes de referenciarlos en el workflow |
| Merge strategy `-X theirs` | Decisión humana consciente: `develop` es la fuente de verdad post-refactoring |

### ⚠️ Alucinaciones detectadas y corregidas

1. **Faltaba `using Xunit;`** — La IA generó los test files asumiendo `ImplicitUsings` para xunit, lo cual no es correcto. Corregido manualmente en los 4 archivos.
2. **`TicketHistory` y `Payments` en ReservationService** — La IA asumió que la entidad `Ticket` del `ReservationService` tenía las mismas colecciones que la del `CrudService`. No es así: son entidades de bounded contexts distintos. Corregido.
3. **`.csproj` eliminado accidentalmente** — El archivo `CrudService.Application.Tests.csproj` estaba en `.gitignore` o había sido eliminado. Recuperado desde el historial de git (`git show b62c2c6:...`).

---

## 📎 Referencias rápidas

| Recurso | Ubicación |
|---|---|
| Pipeline CI/CD | `.github/workflows/ci.yml` |
| Tests Caja Negra | `crud_service/tests/CrudService.Api.Tests/` |
| Plan de Pruebas (TEST_PLAN.md) | `docs/evidencias/semana4/TEST_PLAN.md` |
| Checklist Rúbrica | `docs/evidencias/semana4/CHECKLIST_RUBRICA_SEMANA4.md` |
| Release en GitHub | [v3.0.0](https://github.com/Jomruizgo/ticketing_project_week1/releases/tag/v3.0.0) |
| PR Release | [PR #22](https://github.com/Jomruizgo/ticketing_project_week1/pull/22) |
