# AI Workflow - TicketRush MVP

Estrategia de interaccion con herramientas de IA para el desarrollo del sistema de ticketing TicketRush.

## 1. Metodologia

### 1.1 Enfoque AI-First

La IA actua como **Developer** que genera codigo y propone soluciones. El equipo humano actua como **Arquitectos y Revisores con autoridad final**: define la arquitectura, aprueba o rechaza propuestas, y valida que el codigo generado sea correcto y seguro.

| Rol | Responsabilidad |
|-----|-----------------|
| IA | Generar codigo, proponer soluciones, ejecutar debugging, escribir tests |
| Humano | Definir arquitectura, tomar decisiones finales, aprobar/rechazar propuestas, validar seguridad, revisar PRs |

### 1.2 Reglas de Oro

1. **Nunca aceptar codigo sin entenderlo** - Si no entiendes que hace, no lo integres. Pedir explicaciones hasta que quede claro.
2. **Prohibido boilerplate manual** - La IA genera estructuras repetitivas (DTOs, configuraciones, mappings).
3. **Validacion obligatoria** - Todo codigo critico lleva comentario `// HUMAN CHECK` con la razon de la revision.
4. **Sin secretos en codigo** - Variables de entorno para credenciales. La IA intento hardcodear credenciales de RabbitMQ; se rechazo.
5. **Probar antes de integrar** - Toda correccion se verifica end-to-end (Docker Compose + BD + RabbitMQ) antes de hacer PR.

### 1.3 Ciclo de Trabajo

```
[Definir tarea] --> [Prompt a IA] --> [Revisar output] --> [Probar en local] --> [Corregir si falla] --> [Commit + PR]
```

En la practica, el ciclo de debugging fue iterativo: la IA proponia un fix, se probaba con Docker Compose, se descubria un nuevo error, y se volvia a iterar. Un ejemplo concreto fue el Payment Service donde se corrigieron 6 bugs encadenados antes de lograr un flujo exitoso.

## 2. Herramientas de IA Utilizadas

| Herramienta | Modelo | Uso Principal | Responsable |
|-------------|--------|---------------|-------------|
| Claude Code (CLI) | Claude Opus 4.6 | Desarrollo backend (.NET), debugging, testing, analisis de arquitectura, git operations | Jorge |
| GitHub Copilot | Claude Sonnet 4.5 | Desarrollo del Producer, Frontend, configuracion inicial (RabbitMQ, PostgreSQL, Docker) | Jostin |
| GitHub Copilot | Claude Sonnet 4 / Opus 4.5 | Desarrollo del Payment Service y CRUD Service | Guillermo |

### 2.1 Configuracion del agente (`agent.md`)

Cada herramienta de IA permite definir instrucciones persistentes que se cargan automaticamente al inicio de cada sesion (ej: `CLAUDE.md` en Claude Code, instrucciones personalizadas en GitHub Copilot). Generalizamos este concepto como **`agent.md`**.

**Capacidades del agente (Claude Code CLI):**
- Lectura de codigo fuente y schema SQL para entender contexto completo
- Ejecucion de `docker compose build/up`, `dotnet build`, `dotnet test`
- Consultas directas a PostgreSQL via `docker exec psql`
- Publicacion de mensajes a RabbitMQ para testing end-to-end
- Operaciones git (commit, push, PR via `gh`)

**Instrucciones persistentes configuradas:**
- Conversacion en espanol, codigo en ingles
- Nunca incluir firmas automaticas de la IA en commits
- Actuar como experto critico, no complaciente
- Usar `scripts/schema.sql`, `compose.yml` y `scripts/rabbitmq-definitions.json` como fuentes de verdad
- Alcance del trabajo: se le indico que microservicio(s) le correspondia al desarrollador y que el alcance era MVP (funcionalidad minima, sin idempotencia, sin health checks propios, sin tests de integracion). Esto evito que el agente propusiera funcionalidades fuera de scope, aunque en ocasiones igual lo hizo y se tuvo que rechazar.

**Arquitectura base:**

La arquitectura evoluciono en dos etapas:

**Etapa 1 (MVP - estructura plana):** Se inicio con una estructura simple sin DDD donde todo vivia en un solo proyecto:

```
/MicroService
├── MicroService.sln
├── Dockerfile
├── src/
│   └── MicroService.Worker/
│       ├── Controllers/         # Controladores HTTP (o Consumers/ en Workers)
│       ├── Models/              # DTOs de entrada/salida
│       ├── Services/            # Logica de negocio
│       ├── Repositories/        # Interfaces de acceso a datos
│       ├── Data/                # Implementaciones (EF Core, DbContext)
│       ├── Configurations/      # Configuracion de servicios y middlewares
│       ├── Program.cs           # Punto de entrada
│       └── appsettings.json
├── tests/
│   └── MicroService.Worker.Tests/
└── docs/
```

**Etapa 2 (Refactorizacion - arquitectura hexagonal):** Tras el ejercicio "Mock Imposible" (intentar tests unitarios puros sin Docker, DB ni RabbitMQ), se identificaron violaciones de DIP y SRP que impedian testear la logica de negocio aisladamente. Se migro hacia arquitectura hexagonal con separacion en 4 capas, cada una en su propio proyecto (.csproj = assembly independiente):

```
/MicroService
├── MicroService.sln
├── Dockerfile
├── src/
│   ├── MicroService.Domain/              # Entidades puras, interfaces (puertos)
│   │   ├── Entities/                     # Entidades de dominio (sin dependencias externas)
│   │   ├── Interfaces/                   # Puertos de salida (ITicketRepository, etc.)
│   │   └── Exceptions/                   # Excepciones de dominio
│   ├── MicroService.Application/         # Casos de uso (logica de negocio)
│   │   ├── UseCases/
│   │   │   └── NombreCasoDeUso/
│   │   │       ├── Command.cs            # Input del caso de uso
│   │   │       ├── CommandHandler.cs     # Logica (depende solo de Domain)
│   │   │       └── Response.cs           # Output del caso de uso
│   │   └── Interfaces/                   # Puertos de entrada (IMessageConsumer, etc.)
│   ├── MicroService.Infrastructure/      # Adaptadores (EF Core, RabbitMQ, etc.)
│   │   ├── Persistence/                  # DbContext, implementaciones de repositorios
│   │   ├── Messaging/                    # Consumer RabbitMQ, configuracion
│   │   └── DependencyInjection.cs        # Registro de servicios de infraestructura
│   └── MicroService.Worker/              # Composition Root (solo Program.cs + config)
│       ├── Program.cs                    # Solo DI y bootstrap
│       └── appsettings.json
├── tests/
│   ├── MicroService.Domain.Tests/
│   ├── MicroService.Application.Tests/   # Tests unitarios puros (sin DB, sin RabbitMQ)
│   └── MicroService.Infrastructure.Tests/
└── docs/
```

**Regla de dependencias (el compilador la enforce):**
```
Domain ← no depende de nada externo
Application ← depende solo de Domain
Infrastructure ← depende de Domain + Application + paquetes NuGet (EF, RabbitMQ)
Worker ← depende de Infrastructure (composition root)
```

Cada capa es un .csproj separado que compila como DLL independiente. Si alguien importa RabbitMQ en Domain, el codigo no compila. Esto garantiza que un cambio de infraestructura (ej: RabbitMQ → Kafka) solo afecta la capa Infrastructure, sin tocar Domain ni Application.

**Estado de migracion:**
| Microservicio | Arquitectura | Estado |
|---------------|-------------|--------|
| ReservationService | Hexagonal | Migrado |
| PaymentService | Hexagonal | Migrado |
| CrudService | Hexagonal | Migrado |
| Producer | Hexagonal | Migrado |

## 3. Interacciones Clave

### 3.1 Generacion de Codigo

- **Contexto obligatorio**: Antes de generar codigo, se compartio `scripts/schema.sql`, `compose.yml`, y la estructura del proyecto para que la IA entendiera los tipos de datos reales (enums nativos de PostgreSQL, column names en snake_case).
- **Iteracion**: Se pidio primero la estructura/esqueleto del consumer, luego la logica de negocio, y finalmente los tests.
- **Fragmentacion**: El ReservationService se desarrollo en etapas: Consumer -> Service -> Repository -> Tests.

**Ejemplo real**: Para el ReservationService, la IA genero el `ReservationServiceImpl` completo pero con un modelo `Ticket` que incluia una propiedad `SectionId` que no existia en el schema de PostgreSQL. Al probar con Docker, fallo inmediatamente. Se corrigio eliminando la propiedad y ajustando el DbContext.

### 3.2 Debugging

El debugging fue la interaccion mas intensiva. El flujo siempre fue:
1. Ejecutar el sistema completo (todos los contenedores)
2. Provocar el flujo desde el Producer (reserva + pago)
3. Revisar logs de los contenedores (`docker logs`)
4. Consultar la BD directamente (`psql`)
5. La IA analizaba el stack trace y proponia un fix
6. Se aplicaba, rebuild, y se volvia a probar

**Ejemplo real - Payment Service (6 bugs encadenados)**:

Cada fix revelaba el siguiente error. La IA propuso cada correccion, pero el humano valido cada una probando el flujo completo:

| Bug | Causa raiz | Fix |
|-----|-----------|-----|
| Dispatcher no routeaba mensajes | Comparaba queue name completo (`q.ticket.payments.approved`) contra routing key (`ticket.payments.approved`) con match exacto | Cambiar a `EndsWith` |
| DTO no deserializaba | `PaymentApprovedEvent` tenia campos distintos a lo que el Producer enviaba | Alinear campos con el Producer |
| "Invalid payment status" | El servicio esperaba un payment `pending` que nadie creaba | Crear el payment si no existe |
| Error `25P02` en PostgreSQL | `ToString().ToLower()` enviaba `text` a columna de tipo `ticket_status` (enum nativo) | Pasar el enum directo a Npgsql |
| `DbUpdateConcurrencyException` (0 rows) | `Version++` antes de `UpdateAsync` desincronizaba el WHERE clause | Eliminar pre-incremento |
| `DbUpdateConcurrencyException` (history) | Raw SQL actualizaba la BD pero el change tracker de EF Core seguia con la entidad dirty | Detach entity despues del raw SQL |

### 3.3 Testing

- **Unit tests**: Se uso xUnit + NSubstitute para tests del `ReservationServiceImpl` (4 tests: ticket not found, already reserved, successful reservation, concurrent modification).
- **Testing end-to-end**: Se usaron `curl` al Producer + verificacion directa en PostgreSQL + revision de logs de cada contenedor.
- **Estrategia MVP**: Solo se testearon las reglas de negocio del servicio propio (ReservationService), no infraestructura ni integraciones.

### 3.4 Code Review Asistido

Se uso la IA para revisar el codigo del companero (Payment Service) y encontrar bugs antes de la integracion. La IA identifico correctamente los bugs del dispatcher y el DTO, pero no detecto inmediatamente los problemas de interaccion entre raw SQL y EF Core change tracker; esos salieron durante testing.

## 4. Documentos Clave y Contextualizacion

Archivos que se comparten con la IA al iniciar sesion de trabajo:

| Documento | Proposito |
|-----------|-----------|
| `scripts/schema.sql` | Estructura de base de datos, tipos enum nativos |
| `compose.yml` | Servicios, puertos, dependencias, variables de entorno |
| `scripts/rabbitmq-definitions.json` | Exchanges, colas, bindings, routing keys |
| `.env` | Variables de entorno (sin secretos reales en el repo) |
| Codigo fuente del servicio en desarrollo | Para que la IA entienda patrones existentes |

### 4.1 Prompt de Contextualizacion Inicial

```
Estamos trabajando en TicketRush, un MVP de sistema de ticketing para eventos.

Stack: .NET 8 (LTS), PostgreSQL, RabbitMQ, Docker
Arquitectura: Microservicios con comunicacion asincrona (event-driven)

Microservicios:
- CRUD Service: API REST para gestion de eventos y tickets
- Producer API: Recibe peticiones HTTP, publica eventos a RabbitMQ
- Consumer Service 1 (Reservations): Procesa reservas de tickets
- Consumer Service 2 (Payments): Procesa pagos aprobados/rechazados y TTL
- Frontend: Next.js con interfaz para seleccion y compra de tickets

Eventos RabbitMQ (exchange: tickets, tipo: topic):
- ticket.reserved              -> q.ticket.reserved              (consumer: ReservationService)
- ticket.payment.requested     -> q.ticket.payment.requested     (consumer: PaymentService)
- ticket.payments.approved     -> q.ticket.payments.approved     (consumer: PaymentService)
- ticket.payments.rejected     -> q.ticket.payments.rejected     (consumer: PaymentService)
- ticket.status.changed        -> q.ticket.status.changed        (consumer: CrudService → SSE)

Base de datos: PostgreSQL con enums nativos (ticket_status, payment_status)
en lowercase (available, reserved, paid, released, cancelled).

[Adjuntar schema.sql y archivos relevantes del servicio a trabajar]
```

## 5. Dinamicas de Interaccion

### 5.1 Antes de cada sesion

1. `git status` y `git pull` para sincronizar con el remoto
2. Identificar la tarea especifica (feature, fix, test)
3. Compartir con la IA los archivos relevantes del servicio a trabajar

### 5.2 Durante la sesion

1. **Un objetivo por prompt** - Evitar prompts con multiples tareas no relacionadas
2. **Validar incrementalmente** - Probar cada cambio con Docker antes de seguir. No acumular cambios sin verificar.
3. **Documentar rechazos** - Si la IA propone algo incorrecto, anotar que fallo y por que (ver seccion 3.2 para ejemplo real)
4. **Pedir explicaciones** - Antes de aceptar una solucion, pedir que explique el "por que", no solo el "que". Esto fue especialmente util para entender optimistic locking y el ciclo de vida de transacciones en EF Core, ya que no se cuenta mucha experiencia en .NET.

### 5.3 Al finalizar

1. Revisar codigo generado contra criterios de aceptacion del servicio
2. Verificar que los `// HUMAN CHECK` esten en las partes criticas
3. Commit con mensaje descriptivo en ingles
4. PR para revision del companero

## 6. Convenciones de Comentarios

### 6.1 HUMAN CHECK

Para codigo critico donde el humano valido/modifico la sugerencia de IA:

```csharp
// HUMAN CHECK:
// La IA sugirio usar un poll simple para verificar estado de pago.
// Se cambio a un push model con prefetch de 1 para evitar saturar al worker,
// ya que la IA no consideraba la latencia de red.
```

### 6.2 AI-GENERATED

Para codigo generado por IA sin modificaciones significativas:

```csharp
// AI-GENERATED: Estructura base del consumer
```

## 7. Registro de Decisiones

| Fecha | Decision | Contexto | Responsable |
|-------|----------|----------|-------------|
| 2026-02-10 | Usar .NET 8 | LTS, consistencia en el equipo | Equipo |
| 2026-02-10 | Exchange tipo topic | Permite routing flexible por patron de routing keys | Jostin |
| 2026-02-10 | Enums nativos de PostgreSQL | Mejor rendimiento y type safety que varchar para estados finitos | Jostin |
| 2026-02-11 | Tests solo en logica de negocio (MVP) | Para un MVP, testear ReservationServiceImpl es suficiente; infraestructura se valida con pruebas E2E | Jorge |
| 2026-02-11 | xUnit + NSubstitute para tests | Estandar en .NET, NSubstitute mas legible que Moq para mocks simples | Jorge |
| 2026-02-11 | Rama separada para fix de Payment Service | `fix/jorge/payment-service-bugs` para no bloquear al companero; descartable si el soluciona primero | Jorge |
| 2026-02-11 | Detach entity despues de raw SQL | Evita conflicto entre ExecuteSqlRaw y el change tracker de EF Core en la misma transaccion | Jorge |
| 2026-02-17 | Migrar a arquitectura hexagonal | El ejercicio "Mock Imposible" revelo violaciones DIP/SRP que impedian tests unitarios puros. Se separo cada servicio en Domain, Application, Infrastructure y Worker como proyectos independientes | Jorge |
| 2026-02-19 | Corregir responsabilidad del Producer | El Producer simulaba aprobacion de pagos (80/20) y publicaba `PaymentApprovedEvent` / `PaymentRejectedEvent` directamente. Viola SRP: un gateway de publicacion no debe contener logica de negocio. Se movio la decision al PaymentService, que consume `ticket.payment.requested` y decide el resultado | Jorge |
| 2026-02-19 | Reemplazar polling por SSE | El frontend llamaba a la API cada 500ms hasta 20 veces para saber si el estado cambio. Se sustituyo por SSE (`EventSource`): el CrudService expone `GET /api/tickets/{id}/stream` y notifica una unica vez cuando el consumer recibe `ticket.status.changed`. Razones: menos trafico, sin race condition entre notificacion y escritura en DB, sin dependencia de WebSocket | Jorge |
| 2026-02-19 | Publicar `ticket.status.changed` despues del UPDATE en DB | Para evitar race condition (cliente recibe notificacion antes de que la BD este actualizada), se publico el evento de notificacion siempre despues del UPDATE exitoso en el consumer (ReservationService y PaymentService), no en el Producer ni en paralelo | Jorge |

---

*Documento vivo - Actualizar conforme el proyecto evolucione*
