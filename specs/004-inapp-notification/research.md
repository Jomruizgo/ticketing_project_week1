# Research: Notificación In-App SSE de Lista de Espera

**Feature**: 004-inapp-notification  
**Date**: 2026-04-07

---

## R1 — Patrón SSE en ASP.NET Core 8

**Tarea**: Investigar cómo implementar un endpoint SSE (Server-Sent Events) en ASP.NET Core 8 usando streaming asíncrono con `HttpContext.Response`.

**Decisión**: Usar acción de controller que configura los headers SSE manualmente y mantiene la conexión abierta con un `while` loop que espera en un `TaskCompletionSource` o `Channel<T>` hasta que `HttpContext.RequestAborted` se cancele.

**Rationale**: ASP.NET Core no tiene un middleware SSE nativo, pero el modelo de streaming asíncrono es directo:
1. Configurar headers: `Content-Type: text/event-stream`, `Cache-Control: no-cache`, `Connection: keep-alive`.
2. Registrar la conexión en el hub con el email del comprador.
3. Esperar eventos o cancelación usando `CancellationToken` de `HttpContext.RequestAborted`.
4. Al desconectarse el cliente, limpiar la conexión del hub.

El formato SSE es: `event: {type}\ndata: {json}\n\n`. Los keep-alive son comentarios: `: keepalive\n\n`.

**Alternativas consideradas**:
- SignalR: Descartado. Agrega dependencia innecesaria, negociación de protocolos y complejidad para un flujo unidireccional simple.
- Minimal API con `IAsyncEnumerable`: Posible, pero el controller existente (`WaitlistController`) ya tiene las acciones de waitlist; agregar una acción al mismo controller es más cohesivo.

---

## R2 — Gestión de conexiones concurrentes con ConcurrentDictionary

**Tarea**: Determinar la estructura de datos thread-safe para gestionar conexiones SSE concurrentes por correo electrónico.

**Decisión**: `ConcurrentDictionary<string, ConcurrentBag<SseClient>>` donde `SseClient` encapsula `HttpResponse`, `CancellationToken` y un `Channel<SseEvent>` para desacoplar escritura del broadcast.

**Rationale**: 
- `ConcurrentDictionary` es thread-safe para add/remove/lookup por email.
- `ConcurrentBag` permite múltiples conexiones por email (multi-pestaña, hasta el límite de `SSE_MAX_CONNECTIONS_PER_EMAIL`).
- `Channel<SseEvent>` por cliente desacopla el productor (consumer RabbitMQ) del escritor (cada stream SSE), evitando que un cliente lento bloquee a los demás.
- La limpieza de clientes desconectados ocurre al detectar `RequestAborted` o `IOException` al escribir.

**Alternativas consideradas**:
- `List<T>` con lock manual: Más error-prone, sin beneficio de rendimiento para el volumen esperado.
- Broadcasting síncrono sin Channel: Riesgo de bloqueo si un cliente tiene red lenta.

---

## R3 — Keep-alive SSE y configuración de intervalo

**Tarea**: Investigar el mecanismo de keep-alive para conexiones SSE y cómo hacerlo configurable.

**Decisión**: Un `BackgroundService` o timer periódico dentro del hub que escribe comentarios SSE (`: keepalive\n\n`) a todos los clientes conectados cada N segundos (configurable vía `SSE_KEEPALIVE_INTERVAL_SECONDS`, default 30).

**Rationale**: 
- La spec SSE define que líneas que empiezan con `:` son comentarios ignorados por el cliente pero mantienen la conexión TCP viva.
- Proxies y load balancers cierran conexiones inactivas (típicamente 60-120s), así que 30s es seguro.
- El intervalo debe ser configurable vía variable de entorno para ajustar según infraestructura de despliegue.

**Alternativas consideradas**:
- Timer por cliente: Innecesario; un timer global que itera todos los clientes es suficiente y más eficiente.
- Sin keep-alive: Arriesga desconexiones por proxies intermedios.

---

## R4 — Consumer RabbitMQ para SSE con escalamiento horizontal

**Tarea**: Determinar cómo todas las instancias del CRUD Service reciben el evento `waitlist.opportunity.activated` para despachar al hub SSE correcto.

**Decisión**: Crear una cola **exclusiva por instancia** (auto-delete, generada con nombre único) con binding a `waitlist.opportunity.activated` en el exchange `tickets`. Esto es un patrón fanout vía topic exchange: cada instancia tiene su propia cola y recibe todos los mensajes.

**Rationale**:
- Con una cola compartida, solo una instancia recibiría el mensaje (competing consumers). Pero la instancia que procesa el consumer no necesariamente tiene la conexión SSE del comprador.
- Con colas exclusivas por instancia, todas las instancias reciben el evento. Cada instancia verifica si tiene conexiones SSE activas para el email del comprador; si no, descarta silenciosamente.
- El overhead es mínimo: el volumen de `waitlist.opportunity.activated` es bajo (solo ocurre cuando se asigna una oportunidad).
- Al usar `autoDelete: true` y nombre generado, las colas se limpian automáticamente cuando la instancia se detiene.

**Alternativas consideradas**:
- Cola compartida (competing consumers): No funciona si la instancia que procesa no tiene la conexión SSE.
- Fanout exchange dedicado: Posible, pero el topic exchange existente (`tickets`) con routing key ya soporta múltiples bindings. Agregar un exchange sería complejidad innecesaria.
- Redis pub/sub como intermediario: Agrega dependencia de infraestructura inexistente en el proyecto.

---

## R5 — Testing SSE con WebApplicationFactory

**Tarea**: Investigar cómo probar endpoints SSE en tests de integración con `WebApplicationFactory<Program>`.

**Decisión**: Para tests unitarios, mockear el hub y verificar que el consumer despacha correctamente. Para el endpoint SSE, usar tests unitarios del controller con `HttpContext` mockeado, verificando headers y formato de escritura. La integración E2E completa (con RabbitMQ real) queda fuera del alcance de esta feature (se cubriría en E2E del sistema).

**Rationale**:
- Tests unitarios del hub: verificar register/unregister/broadcast con `ConcurrentDictionary` en memoria.
- Tests unitarios del consumer: mockear el hub, verificar que al recibir un mensaje invoca `SendEventAsync` con el email y payload correctos.
- Tests unitarios del controller action: verificar que configura headers SSE correctamente y registra/desregistra del hub.
- TC-HU4-03 (solo lectura): verificar que el consumer no invoca ningún repositorio de escritura.

**Alternativas consideradas**:
- `HttpClient.GetStreamAsync` contra `WebApplicationFactory`: Complejo porque SSE es long-lived y requiere leer asíncronamente mientras el server emite. Reservar para E2E.
