# Reporte de Deuda Técnica — TicketRush

**Fecha:** 2026-02-16
**Autores:** JR + EM
**Metodología:** Mock Imposible — intentar escribir tests unitarios puros (sin Docker, sin DB, sin RabbitMQ) para cada clase con lógica de negocio. Cada fricción detectada se documenta como señal de baja calidad.

---

## 1. Smells Detectados

### 1.1 PaymentService (JR)

#### MOCK IMPOSIBLE — Clases no testeables sin infraestructura

| # | Archivo | Líneas | Smell | Principio SOLID Violado | Descripción |
|---|---------|--------|-------|------------------------|-------------|
| 1 | `paymentService/.../Services/TicketStateService.cs` | L10, L17 | Dependencia concreta de `PaymentDbContext` | **DIP** | Constructor exige `PaymentDbContext` concreto en vez de abstracción. |
| 2 | `paymentService/.../Services/TicketStateService.cs` | L32, L106 | Transacciones DB en lógica de negocio | **SRP** | Llama `_dbContext.Database.BeginTransactionAsync()` directamente. Mezcla gestión transaccional + lógica de estados + historial en un solo método. |
| 3 | `paymentService/.../Repositories/TicketRepository.cs` | L36 | SQL crudo no mockeable | **DIP** | `FromSqlRaw("SELECT ... FOR UPDATE")` — SQL específico de PostgreSQL imposible de mockear sin una DB real. |
| 4 | `paymentService/.../Repositories/TicketRepository.cs` | L46 | SQL crudo para UPDATE | **DIP** | `ExecuteSqlRawAsync` con SQL crudo para actualización con concurrencia optimista. Acoplado a dialecto PostgreSQL. |
| 5 | `paymentService/.../Messaging/TicketPaymentConsumer.cs` | L19 | Dependencia concreta de `RabbitMQConnection` | **DIP** | Depende de la clase concreta `RabbitMQConnection`, no de una interfaz. |
| 6 | `paymentService/.../Messaging/RabbitMQConnection.cs` | L7 | Clase sin interfaz | **DIP** | No expone interfaz. Crea `ConnectionFactory` internamente (L44). Imposible sustituir en tests. |

#### Otros smells

| # | Archivo | Líneas | Smell | Tipo | Descripción |
|---|---------|--------|-------|------|-------------|
| 7 | `paymentService/.../Services/PaymentValidationService.cs` | L143 | Magic Number | Code Smell | `AddMinutes(5)` hardcodeado. Existe `PaymentSettings.ReservationTtlMinutes` pero no se inyecta. |
| 8 | `paymentService/.../Configurations/RabbitMQSettings.cs` | L10-14 | Lectura de env vars en propiedades | Code Smell | `Environment.GetEnvironmentVariable()` en defaults de propiedades. Mezcla configuración con lectura de entorno. |

#### Clases testeables (tests escritos)

| Clase | Tests | Resultado |
|-------|-------|-----------|
| `PaymentValidationService` | 15 | ✅ 15/15 |
| `PaymentApprovedEventHandler` | 4 | ✅ 4/4 |
| `PaymentRejectedEventHandler` | 3 | ✅ 3/3 |
| `PaymentEventDispatcherImpl` | 3 | ✅ 3/3 |

### 1.2 CrudService (EM)

> **TODO (EM):** Documentar smells detectados en CrudService.
> Archivos a analizar:
> - `crud_service/Data/RepositoriesImplementation.cs` — God File con 4 repos
> - `crud_service/Repositories/IRepositories.cs` — Múltiples interfaces en un archivo
> - `crud_service/Data/TicketingDbContext.cs` — Dependencia concreta en repositorios
>
> Incluir: archivo, línea, smell, principio SOLID violado, descripción.

### 1.3 ReservationService (EM)

> **TODO (EM):** Documentar smells detectados en ReservationService.
> Archivos a analizar:
> - `ReservationService/.../Consumers/TicketReservationConsumer.cs` — ConnectionFactory instanciada internamente (L36-42)
>
> Incluir: archivo, línea, smell, principio SOLID violado, descripción.

### 1.4 Producer (JR)

| # | Archivo | Líneas | Smell | Tipo | Descripción |
|---|---------|--------|-------|------|-------------|
| 9 | `producer/.../Controllers/PaymentsController.cs` | L151-158 | Lógica de negocio en Controller | **SRP** | `SimulatePaymentProcessing` con `Random.Shared` vive en el controlador. No testeable, no sustituible. |
| 10 | `producer/.../Controllers/PaymentsController.cs` | L157 | Simulación no determinista | Testeabilidad | `Random.Shared.Next(0, 100) < 80` — comportamiento no predecible en tests. |
| 11 | `producer/.../Services/RabbitMQPaymentPublisher.cs` | L40-93 | Código duplicado | **DRY** | Patrón idéntico de serialize + properties + publish repetido en `PublishPaymentApprovedAsync` y `PublishPaymentRejectedAsync`. |
| 12 | `producer/.../Services/RabbitMQTicketPublisher.cs` | L36-54 | Código duplicado (cross-class) | **DRY** | Mismo patrón serialize + publish que en `RabbitMQPaymentPublisher`. 3 métodos con estructura idéntica. |

---

## 2. Clasificación de Deuda Técnica

> **TODO (EM):** Completar con los smells de CrudService y ReservationService una vez documentados.

### Por tipo (parcial — solo PaymentService + Producer)

| Tipo de Deuda | Ocurrencias | Smells # |
|---------------|-------------|----------|
| **Acoplamiento a infraestructura (DIP)** | 4 | 1, 3, 4, 5, 6 |
| **Violación SRP** | 2 | 2, 9 |
| **Magic Numbers / Config hardcodeada** | 2 | 7, 8 |
| **Código duplicado (DRY)** | 2 | 11, 12 |
| **No determinismo en lógica** | 1 | 10 |

---

## 3. Métricas (parcial)

| Métrica | Valor |
|---------|-------|
| Tests unitarios PaymentService | 25/25 ✅ |
| Tests unitarios CrudService | Pendiente (EM) |
| Tests unitarios ReservationService | 4/4 ✅ (preexistentes) |

---

## 4. Test del CTO

> **"Si mañana cambiamos RabbitMQ por Kafka o AWS SQS, ¿hay que reescribir lógica de negocio?"**

### PaymentService (JR)

| Clase | ¿Se ve afectada? | ¿Contiene lógica de negocio mezclada? |
|-------|-------------------|--------------------------------------|
| `PaymentValidationService` | **NO** | Pura lógica de validación. |
| `PaymentApprovedEventHandler` | **Parcialmente** | Deserialización JSON agnóstica. Requiere re-wireado de DI, no reescritura. |
| `PaymentRejectedEventHandler` | **Parcialmente** | Mismo caso. |
| `PaymentEventDispatcherImpl` | **Mínimo** | Usa `QueueName` como concepto de routing. Con Kafka serían topics pero el dispatcher funciona igual. |
| `TicketStateService` | **NO** | No depende de messaging. Solo de DB. |
| `TicketPaymentConsumer` | **SI — REESCRITURA** | Acoplado a `RabbitMQConnection`, `IModel`, `BasicDeliverEventArgs`. |
| `RabbitMQConnection` | **SI — REEMPLAZO** | Se elimina y se crea equivalente Kafka. |

### Producer (JR)

| Clase | ¿Se ve afectada? | ¿Contiene lógica de negocio mezclada? |
|-------|-------------------|--------------------------------------|
| `RabbitMQPaymentPublisher` | **SI — REEMPLAZO** | Se crea `KafkaPaymentPublisher`. La interfaz `IPaymentPublisher` se mantiene. |
| `RabbitMQTicketPublisher` | **SI — REEMPLAZO** | Se crea `KafkaTicketPublisher`. La interfaz `ITicketPublisher` se mantiene. |
| `PaymentsController` | **NO** | Depende de `IPaymentPublisher` (interfaz). |

### CrudService y ReservationService (EM)

> **TODO (EM):** Evaluar impacto de migración de broker en:
> - `TicketReservationConsumer`
> - `ReservationServiceImpl`
> - `EventService`, `TicketService`

---

## 5. Priorización de Resolución

> Ver `docs/REFACTORING_PLAN.md` para el backlog completo (P0/P1/P2).
> La Fase 2 está bloqueada hasta definir la arquitectura hexagonal.
