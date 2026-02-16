# Reporte de Deuda Técnica — TicketRush

**Fecha:** 2026-02-16
**Autores:** Equipo de refactorización
**Metodología:** Mock Imposible — intentar escribir tests unitarios puros (sin Docker, sin DB, sin RabbitMQ) para cada clase con lógica de negocio. Cada fricción detectada se documenta como señal de baja calidad.

---

## 1. Tabla de Smells Detectados

### 1.1 MOCK IMPOSIBLE — Clases no testeables sin infraestructura

| # | Archivo | Líneas | Servicio | Smell | Principio SOLID Violado | Descripción |
|---|---------|--------|----------|-------|------------------------|-------------|
| 1 | `paymentService/.../Services/TicketStateService.cs` | L10, L17 | PaymentService | Dependencia concreta de `PaymentDbContext` | **DIP** | Constructor exige `PaymentDbContext` concreto en vez de abstracción. |
| 2 | `paymentService/.../Services/TicketStateService.cs` | L32, L106 | PaymentService | Transacciones DB en lógica de negocio | **SRP** | Llama `_dbContext.Database.BeginTransactionAsync()` directamente. Mezcla gestión transaccional + lógica de estados + historial en un solo método. |
| 3 | `paymentService/.../Repositories/TicketRepository.cs` | L36 | PaymentService | SQL crudo no mockeable | **DIP** | `FromSqlRaw("SELECT ... FOR UPDATE")` — SQL específico de PostgreSQL imposible de mockear sin una DB real. |
| 4 | `paymentService/.../Repositories/TicketRepository.cs` | L46 | PaymentService | SQL crudo para UPDATE | **DIP** | `ExecuteSqlRawAsync` con SQL crudo para actualización con concurrencia optimista. Acoplado a dialecto PostgreSQL. |
| 5 | `crud_service/Data/RepositoriesImplementation.cs` | L1-201 | CrudService | God File | **SRP** | 4 repositorios en un solo archivo (~200 líneas): `EventRepository`, `TicketRepository`, `PaymentRepository`, `TicketHistoryRepository`. |
| 6 | `crud_service/Data/RepositoriesImplementation.cs` | L14, L70, L133, L179 | CrudService | Dependencia concreta de DbContext | **DIP** | Los 4 repositorios dependen de `TicketingDbContext` concreto, no de abstracciones. |
| 7 | `ReservationService/.../Consumers/TicketReservationConsumer.cs` | L36-42 | ReservationService | `ConnectionFactory` instanciada internamente | **DIP** | Crea `new ConnectionFactory { ... }` dentro de `ExecuteAsync`. Sin abstracción. Imposible testear sin RabbitMQ corriendo. |
| 8 | `paymentService/.../Messaging/TicketPaymentConsumer.cs` | L19 | PaymentService | Dependencia concreta de `RabbitMQConnection` | **DIP** | Depende de la clase concreta `RabbitMQConnection`, no de una interfaz. |
| 9 | `paymentService/.../Messaging/RabbitMQConnection.cs` | L7 | PaymentService | Clase sin interfaz | **DIP** | No expone interfaz. Crea `ConnectionFactory` internamente (L44). Imposible sustituir en tests. |
| 10 | `crud_service/Repositories/IRepositories.cs` | L1-52 | CrudService | Múltiples interfaces en un archivo | **SRP** (leve) | 4 interfaces en un solo archivo. Menor gravedad pero dificulta navegación y viola convención de un tipo por archivo. |

### 1.2 Otros Smells Detectados

| # | Archivo | Líneas | Servicio | Smell | Tipo | Descripción |
|---|---------|--------|----------|-------|------|-------------|
| 11 | `paymentService/.../Services/PaymentValidationService.cs` | L143 | PaymentService | Magic Number | Code Smell | `AddMinutes(5)` hardcodeado. Existe `PaymentSettings.ReservationTtlMinutes` pero no se inyecta. |
| 12 | `producer/.../Controllers/PaymentsController.cs` | L151-158 | Producer | Lógica de negocio en Controller | **SRP** | `SimulatePaymentProcessing` con `Random.Shared` vive en el controlador. No testeable, no sustituible. |
| 13 | `producer/.../Controllers/PaymentsController.cs` | L157 | Producer | Simulación no determinista | Testeabilidad | `Random.Shared.Next(0, 100) < 80` — comportamiento no predecible en tests. |
| 14 | `producer/.../Services/RabbitMQPaymentPublisher.cs` | L40-93 | Producer | Código duplicado | **DRY** | Patrón idéntico de serialize + properties + publish repetido en `PublishPaymentApprovedAsync` y `PublishPaymentRejectedAsync`. |
| 15 | `producer/.../Services/RabbitMQTicketPublisher.cs` | L36-54 | Producer | Código duplicado (cross-class) | **DRY** | Mismo patrón serialize + publish que en `RabbitMQPaymentPublisher`. 3 métodos con estructura idéntica. |
| 16 | `paymentService/.../Configurations/RabbitMQSettings.cs` | L10-14 | PaymentService | Lectura de env vars en propiedades | Code Smell | `Environment.GetEnvironmentVariable()` en defaults de propiedades. Mezcla configuración con lectura de entorno. Dificulta tests y override desde `appsettings.json`. |

---

## 2. Clasificación de Deuda Técnica

### 2.1 Por Tipo

| Tipo de Deuda | Ocurrencias | Smells # | Impacto |
|---------------|-------------|----------|---------|
| **Acoplamiento a infraestructura (DIP)** | 7 | 1, 3, 4, 6, 7, 8, 9 | Crítico — impide testeo unitario puro |
| **Violación SRP** | 4 | 2, 5, 10, 12 | Alto — dificulta mantenimiento y comprensión |
| **Magic Numbers / Config hardcodeada** | 2 | 11, 16 | Medio — dificulta operaciones y deploy |
| **Código duplicado (DRY)** | 2 | 14, 15 | Medio — aumenta superficie de error |
| **No determinismo en lógica** | 1 | 13 | Medio — impide tests predecibles |

### 2.2 Por Servicio

| Servicio | Smells | Críticos | Altos | Medios |
|----------|--------|----------|-------|--------|
| **PaymentService** | 1, 2, 3, 4, 8, 9, 11, 16 | 5 | 1 | 2 |
| **CrudService** | 5, 6, 10 | 1 | 2 | 0 |
| **ReservationService** | 7 | 1 | 0 | 0 |
| **Producer** | 12, 13, 14, 15 | 0 | 1 | 3 |

---

## 3. Impacto en Calidad

### 3.1 Testeabilidad

| Clase | Testeable sin infra? | Razón |
|-------|---------------------|-------|
| `PaymentValidationService` | **SI** | Depende solo de interfaces (ITicketRepository, IPaymentRepository, ITicketStateService) |
| `PaymentApprovedEventHandler` | **SI** | Depende de IPaymentValidationService + IOptions |
| `PaymentRejectedEventHandler` | **SI** | Depende de IPaymentValidationService + IOptions |
| `PaymentEventDispatcherImpl` | **SI** | Depende de IEnumerable\<IPaymentEventHandler\> |
| `EventService` | **SI** | Depende de IEventRepository, ITicketRepository |
| `TicketService` | **SI** | Depende de ITicketRepository, ITicketHistoryRepository |
| `ReservationServiceImpl` | **SI** | Depende de ITicketRepository (ya tiene 4 tests) |
| `TicketStateService` | **NO** | Depende de `PaymentDbContext` concreto. Llama `Database.BeginTransactionAsync()` |
| `TicketRepository` (Payment) | **NO** | SQL crudo (`FromSqlRaw`, `ExecuteSqlRawAsync`) |
| `RepositoriesImplementation` (Crud) | **NO** | Todos dependen de `TicketingDbContext` concreto |
| `TicketReservationConsumer` | **NO** | Instancia `ConnectionFactory` directamente |
| `TicketPaymentConsumer` | **NO** | Depende de `RabbitMQConnection` clase concreta |
| `RabbitMQConnection` | **NO** | Sin interfaz, crea `ConnectionFactory` internamente |
| `PaymentsController` | **NO** | Lógica de negocio + `Random.Shared` incrustados |

### 3.2 Métricas

| Métrica | Valor |
|---------|-------|
| Total clases con lógica | 14 |
| Clases testeables (mock posible) | **7 (50%)** |
| Clases no testeables | **7 (50%)** |
| Tests unitarios existentes (pre-fase) | 4 (ReservationServiceImpl) |
| Tests unitarios nuevos (fase 1) | ~28 (PaymentService: ~17, CrudService: ~11) |
| Cobertura estimada post-fase 1 | ~50% de clases con lógica cubiertas |

**Cobertura por servicio:**

| Servicio | Clases con lógica | Testeables | Cobertura actual |
|----------|-------------------|------------|------------------|
| PaymentService | 7 | 4 (57%) | 0% → ~57% con tests nuevos |
| CrudService | 6 | 2 (33%) | 0% → ~33% con tests nuevos |
| ReservationService | 2 | 1 (50%) | 50% (ya existía) |
| Producer | 3 | 0 (0%) | 0% |

---

## 4. Priorización de Resolución

### P0 — Crítico (desbloquea testeabilidad)

| ID | Refactoring | Archivos afectados | Justificación |
|----|------------|-------------------|---------------|
| R1 | Extraer `IUnitOfWork` de `TicketStateService` | TicketStateService.cs, nuevo IUnitOfWork.cs, nuevo EfUnitOfWork.cs | Desbloquea ~6 tests nuevos para la clase más compleja del sistema. `TicketStateService` orquesta transiciones de estado con transacciones — es el core del dominio de pagos. |
| R2 | Extraer interfaz `IRabbitMQConnection` | RabbitMQConnection.cs, TicketPaymentConsumer.cs | Desbloquea testeabilidad del consumer. Permite verificar lógica de ACK/NACK sin RabbitMQ. |
| R3 | Separar God File `RepositoriesImplementation.cs` | RepositoriesImplementation.cs → 4 archivos individuales | Cambio estructural sin cambio de lógica. Reduce confusión, mejora navegación, facilita ownership. |

### P1 — Alto (mejora calidad)

| ID | Refactoring | Archivos afectados | Justificación |
|----|------------|-------------------|---------------|
| R4 | Extraer lógica de `PaymentsController` a servicio | PaymentsController.cs, nuevo IPaymentProcessingService.cs, nuevo IPaymentGateway.cs | Strategy pattern reemplaza `SimulatePaymentProcessing` con `Random.Shared`. Permite testear el flujo de decisión approved/rejected. |
| R5 | Abstraer `ConnectionFactory` en ReservationService | TicketReservationConsumer.cs, nuevo IRabbitMQConnectionFactory.cs | Elimina `new ConnectionFactory` hardcodeado. Misma lógica que R2 pero para ReservationService. |
| R6 | TTL configurable en `PaymentValidationService` | PaymentValidationService.cs, PaymentSettings.cs | Inyectar `IOptions<PaymentSettings>` y usar `ReservationTtlMinutes` en vez del magic number `5`. Ya existe la config, solo falta conectarla. |

### P2 — Medio (limpieza)

| ID | Refactoring | Archivos afectados | Justificación |
|----|------------|-------------------|---------------|
| R7 | DRY en Publishers del Producer | RabbitMQPaymentPublisher.cs, RabbitMQTicketPublisher.cs | 3 métodos con patrón idéntico (serialize + properties + publish). Extraer helper o clase base. |
| R8 | Separar interfaces de `IRepositories.cs` | IRepositories.cs → 4 archivos individuales | Convención un-tipo-por-archivo. Menor impacto pero mejora organización. |

---

## 5. Test del CTO

> **"Si mañana cambiamos RabbitMQ por Kafka o AWS SQS, ¿hay que reescribir lógica de negocio?"**

### Evaluación por clase

| Clase | ¿Se ve afectada? | ¿Contiene lógica de negocio mezclada? |
|-------|-------------------|--------------------------------------|
| `PaymentValidationService` | **NO** | No depende de RabbitMQ. Pura lógica de validación. |
| `PaymentApprovedEventHandler` | **Parcialmente** | Deserialization de JSON es agnóstica. Pero la clase es instanciada por el consumer de RabbitMQ. Requiere re-wireado de DI, no reescritura. |
| `PaymentRejectedEventHandler` | **Parcialmente** | Mismo caso que ApprovedEventHandler. |
| `PaymentEventDispatcherImpl` | **Mínimo** | Usa `QueueName` como concepto de routing. Con Kafka serían topics pero el dispatcher funciona igual. |
| `TicketStateService` | **NO** | No depende de messaging. Solo de DB. |
| `TicketPaymentConsumer` | **SI — REESCRITURA** | Acoplado a `RabbitMQConnection`, `IModel`, `BasicDeliverEventArgs`, `AsyncEventingBasicConsumer`. Se reescribe entero. |
| `RabbitMQConnection` | **SI — REEMPLAZO** | Se elimina y se crea `KafkaConnection` o similar. |
| `TicketReservationConsumer` | **SI — REESCRITURA** | Instancia `ConnectionFactory` de RabbitMQ directamente. Se reescribe entero. |
| `RabbitMQPaymentPublisher` | **SI — REEMPLAZO** | Se crea `KafkaPaymentPublisher`. La interfaz `IPaymentPublisher` se mantiene. |
| `RabbitMQTicketPublisher` | **SI — REEMPLAZO** | Se crea `KafkaTicketPublisher`. La interfaz `ITicketPublisher` se mantiene. |
| `EventService` | **NO** | No tiene relación con messaging. |
| `TicketService` | **NO** | No tiene relación con messaging. |
| `ReservationServiceImpl` | **NO** | Recibe datos, no sabe de dónde vienen. |

### Veredicto

- **Lógica de negocio protegida:** `PaymentValidationService`, `TicketStateService`, `EventService`, `TicketService`, `ReservationServiceImpl` — **NO se reescriben**. El dominio está correctamente aislado de messaging gracias a las interfaces `IPaymentPublisher`/`ITicketPublisher`.
- **Lógica de infraestructura a reescribir:** 5 clases (consumers + publishers + connection). Esto es **esperado** — son adaptadores de infraestructura.
- **Problema real:** Los consumers (`TicketPaymentConsumer`, `TicketReservationConsumer`) mezclan lógica de ACK/NACK con el wireado de RabbitMQ. Si existiera una abstracción `IMessageConsumer`, solo se cambiaría la implementación.

**Indicador de acoplamiento:** 5/14 clases (36%) requieren cambio al migrar de broker. De esas 5, solo los consumers mezclan lógica que podría abstraerse. Los publishers ya están detrás de interfaz (`IPaymentPublisher`, `ITicketPublisher`), lo cual es correcto.

**Score:** 6/10 — El lado del producer está bien aislado. Los consumers son el punto débil.

---

## 6. Estrategia de Branching (Gitflow)

```
main (fork)
 └── develop
      ├── feature/mock-imposible/payment-tests         (JR)
      ├── feature/mock-imposible/crud-tests             (EM)
      ├── feature/mock-imposible/report                 (JR + EM)
      │
      ├── refactor/payment/unit-of-work                 (JR — R1)
      ├── refactor/crud/split-repositories              (EM — R3)
      ├── refactor/payment/rabbitmq-interface            (JR — R2)
      ├── refactor/reservation/connection-factory        (EM — R5)
      ├── refactor/producer/extract-payment-service      (JR — R4)
      └── refactor/producer/ttl-config-and-dry           (EM — R6+R7+R8)
```

### Reglas

1. `feature/mock-imposible/*` se mergea primero a `develop` (son tests, no rompen nada)
2. Los `refactor/*` van en orden de prioridad (P0 → P1 → P2)
3. PR review cruzado obligatorio: JR revisa EM, EM revisa JR
4. Cada PR incluye: tests que pasan + descripción del smell que resuelve
5. Merge a `main` solo desde `develop` después de validar que todos los servicios compilan y los tests pasan

### División de trabajo

| JR (PaymentService) | EM (CrudService + ReservationService) |
|---------------------|---------------------------------------|
| `MsPaymentService.Worker.Tests` (~25 tests) | `CrudService.Tests` (~19 tests) |
| Documentar Mock Imposible de PaymentService | Documentar Mock Imposible de CrudService + ReservationService |
| R1: UnitOfWork + tests TicketStateService | R3: Separar God File |
| R2: IRabbitMQConnection | R5: Abstraer ConnectionFactory |
| R4: Extraer lógica PaymentsController | R8: Separar interfaces |
| R7: DRY publishers | R6: TTL configurable |
