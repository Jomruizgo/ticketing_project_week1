# Propuesta de Quality Gates para PR — TicketRush

## 1. Objetivo

Definir qué debe verificarse automáticamente en Pull Requests para reducir regresiones sin convertir la CI en un cuello de botella inviable.

---

## 2. Principio rector

Los checks de PR deben ser:

- rápidos,
- confiables,
- trazables al riesgo,
- más baratos que una E2E completa,
- suficientes para bloquear errores evidentes antes del merge.

---

## 3. Gates obligatorios en PR

| Gate | Tipo | Bloqueante | Motivo |
|---|---|---|---|
| Build por servicio impactado | Compilación | Sí | evitar roturas básicas |
| Tests unitarios por servicio impactado | Unit | Sí | proteger lógica crítica |
| Tests de contrato SSE / parsing / pipeline relevantes | Integración rápida | Sí | proteger contrato inter-servicio/frontend |
| Lint/chequeos frontend si cambia UI/TS | Calidad estática | Sí | detectar regresiones tempranas |
| Validación de scripts/config si cambia RabbitMQ o esquema | Consistencia | Sí | evitar drift operacional |

---

## 4. Gates condicionales por tipo de cambio

### Cambio en `producer`
- tests de handlers de reserva/pago,
- build del servicio,
- validación de contratos devueltos al frontend.

### Cambio en `ReservationService`
- tests de command handlers relevantes,
- tests de concurrencia/idempotencia aplicables,
- verificación de contratos/eventos si toca RabbitMQ.

### Cambio en `paymentService`
- tests de validación de pago,
- tests de handlers approved/rejected,
- validación de TTL/idempotencia.

### Cambio en `crud_service`
- tests de SSE contract,
- tests del pipeline consumer → hub,
- build del servicio.

### Cambio en frontend
- lint/typecheck,
- tests de hooks/componentes relevantes,
- validación del contrato consumido si cambia integración.

---

## 5. Gates fuera de PR pero obligatorios antes de liberar

| Gate | Momento recomendado |
|---|---|
| `scripts/verify-e2e.sh` | post-merge, nightly o manual previo a release |
| `scripts/verify-devA-expiration.sh` | post-merge o pipeline programado como validación de la ruta canónica de expiración RabbitMQ |
| integración completa de servicios impactados | post-merge |

---

## 6. Criterios para activar E2E reforzada

Ejecutar E2E reforzada si el PR toca alguno de estos ejes:

- estados de ticket o payment,
- routing keys, colas o topología RabbitMQ,
- lógica de expiración,
- contrato SSE,
- integración entre frontend y cambio de estado final.

---

## 7. Recomendación de implementación incremental en GitHub Actions

### Fase 1
- build por servicio,
- `dotnet test` suites rápidas,
- frontend typecheck/lint,
- smoke mínimo.

### Fase 2
- matrices por paths impactados,
- integración rápida de SSE/consumers,
- artefactos de resultados de prueba.

### Fase 3
- Docker Compose E2E en pipeline dedicado,
- ejecución programada,
- evidencias y reportes consolidados.

---

## 8. Juicio final

El repositorio ya tiene base suficiente para quality gates reales en PR. La clave no es ejecutar todo siempre, sino ejecutar lo correcto según el impacto del cambio.
