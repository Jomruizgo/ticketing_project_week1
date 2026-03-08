# Matriz Local Pre-Workflow — Semana 4

## Objetivo

Definir la base mínima que debe funcionar **en local** antes de formalizar el workflow de CI/CD.

Principio rector:

> El workflow no debe inventar ejecuciones nuevas; debe automatizar ejecuciones locales ya entendidas, repetibles y defendibles.

## Regla de paridad local

Para cada bloque que después viva en GitHub Actions debe existir primero una ejecución local equivalente o casi equivalente.

Eso aplica a:

- build de contenedores,
- suites de prueba por nivel,
- scripts candidatos a caja negra,
- y validaciones de soporte previas al pipeline.

## Matriz mínima actual

| Bloque futuro | Objetivo local previo | Comando local base | Estado actual | Observación |
|---|---|---|---|---|
| Build contenedores backend | confirmar que las imágenes construyen con Dockerfiles endurecidos | `docker compose build producer crud-service reservation-service payment` | Validado | build local exitoso tras hardening |
| Unit ReservationService | validar reglas de reserva y expiración en capa de aplicación | `dotnet test ReservationService/tests/ReservationService.Application.Tests/ReservationService.Application.Tests.csproj` | Validado | 9/9 verde |
| Unit Producer | validar command handlers del productor | `dotnet test producer/tests/Producer.Application.Tests/Producer.Application.Tests.csproj` | Validado | 3/3 verde |
| Unit Payment | validar TTL, idempotencia y transiciones | `dotnet test paymentService/MsPaymentService.Worker.Tests/MsPaymentService.Worker.Tests.csproj` | Validado | 25/25 verde |
| Crud Unit | validar parser, formatter, hub y consumer | `dotnet test crud_service/tests/CrudService.Infrastructure.Tests/CrudService.Infrastructure.Tests.csproj --filter "Category=Unit"` | Validado | 40/40 verde |
| Crud Component | validar pipeline SSE sin infraestructura externa completa | `dotnet test crud_service/tests/CrudService.Infrastructure.Tests/CrudService.Infrastructure.Tests.csproj --filter "Category=Component"` | Validado | 8/8 verde |
| Crud Integration | validar contrato SSE observable | `dotnet test crud_service/tests/CrudService.Infrastructure.Tests/CrudService.Infrastructure.Tests.csproj --filter "Category=Integration"` | Validado | 10/10 verde |
| Caja Negra E2E general | validar flujo observable distribuido | `bash scripts/verify-e2e.sh` | Validado | candidato principal para primera caja negra oficial |
| Caja Negra expiración | validar liberación por RabbitMQ en entorno real | `bash scripts/verify-devA-expiration.sh` | Validado | candidato secundario y regresión crítica de HU-R06 |

## Ejecuciones locales ya verificadas

### Build de contenedores

- `docker compose build producer crud-service reservation-service payment`
- Resultado: **exitoso en local**.

### Suites mínimas por nivel

| Suite | Resultado local |
|---|---|
| ReservationService Application | 9/9 verde |
| Producer Application | 3/3 verde |
| Payment Worker | 25/25 verde |
| Crud Unit | 40/40 verde |
| Crud Component | 8/8 verde |
| Crud Integration | 10/10 verde |

### Caja negra sobre entorno orquestado

| Script | Resultado local | Observación |
|---|---|---|
| `scripts/verify-e2e.sh` | `E2E SUCCESS` | cubre flujo distribuido observable reserva → pago → SSE |
| `scripts/verify-devA-expiration.sh` | `Verification SUCCESS` | cubre expiración canónica por RabbitMQ |

## Selección recomendada de caja negra inicial

### Caja negra principal recomendada

- **`scripts/verify-e2e.sh`**

### Justificación

Se recomienda como primera caja negra oficial porque:

1. atraviesa más superficie funcional del sistema distribuido,
2. ejerce HTTP real, mensajería y observación final por SSE,
3. representa mejor el flujo de negocio general defendible ante la guía,
4. y permite explicar con claridad por qué es caja negra: valida comportamiento observable sin inspeccionar detalles internos del código durante la prueba.

### Caja negra secundaria obligatoria a conservar

- **`scripts/verify-devA-expiration.sh`**

Debe conservarse como regresión crítica porque protege específicamente:

- `HU-R06 Expiración y liberación de tickets`,
- la ruta canónica `ticket.expired` por RabbitMQ,
- y la coherencia del inventario liberado.

## Lectura técnica de la matriz

### 1. Qué ya está suficientemente estable

- build local de imágenes backend,
- suites mínimas por nivel con ejecución local confirmada,
- scripts E2E candidatos a caja negra con ejecución local confirmada.

### 2. Qué todavía no debe asumirse como cerrado

- política exacta de entrada/salida de cada bloque al futuro workflow,
- herramienta exacta de escaneo de imagen,
- política final de bloqueo para PR.

### 3. Qué sí queda prohibido hacer

- crear un job en CI que no tenga equivalente local claro,
- usar el workflow como sustituto de diagnóstico manual básico,
- mezclar en un solo job suites que todavía no están identificadas conceptualmente.

## Conclusión operativa

Antes del workflow, el equipo debe poder defender esta secuencia:

1. las imágenes construyen en local,
2. las suites relevantes corren localmente,
3. existe al menos una caja negra defendible en local,
4. recién después se automatiza ese comportamiento en CI.