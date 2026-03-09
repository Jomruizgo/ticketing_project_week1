# Guía de Comprensión: Arquitectura del Proyecto Ticketing

> **Para quién es este documento:** Para alguien que está comenzando en el mundo del desarrollo de software y quiere entender a fondo cómo está construido este sistema, por qué se tomaron ciertas decisiones, y qué conceptos arquitectónicos están en juego.

---

## Tabla de Contenido

1. [¿Qué hace este sistema?](#1-qué-hace-este-sistema)
2. [Los 4 servicios del proyecto](#2-los-4-servicios-del-proyecto)
3. [El problema de la comunicación entre servicios](#3-el-problema-de-la-comunicación-entre-servicios)
4. [RabbitMQ: el corazón del sistema](#4-rabbitmq-el-corazón-del-sistema)
5. [Conceptos fundamentales de EDA](#5-conceptos-fundamentales-de-eda)
6. [El flujo completo de una reserva](#6-el-flujo-completo-de-una-reserva)
7. [El flujo completo de un pago](#7-el-flujo-completo-de-un-pago)
8. [La base de datos: cómo se guarda todo](#8-la-base-de-datos-cómo-se-guarda-todo)
9. [Arquitecturas internas de cada servicio](#9-arquitecturas-internas-de-cada-servicio)
10. [¿Es este sistema Event-Driven?](#10-es-este-sistema-event-driven)
11. [Fortalezas del diseño actual](#11-fortalezas-del-diseño-actual)
12. [Áreas de mejora identificadas](#12-áreas-de-mejora-identificadas)
13. [Glosario de términos](#13-glosario-de-términos)

---

## 1. ¿Qué hace este sistema?

Este es un sistema de **venta y reserva de tickets para eventos**. Piensa en él como una plataforma para comprar entradas a conciertos, obras de teatro, o cualquier evento.

El flujo básico que soporta es:

```
1. Un administrador crea un evento (ej: "Concierto de Rock - 500 tickets")
2. Un usuario ve los tickets disponibles
3. El usuario reserva un ticket (tiene 5 minutos para pagar)
4. El usuario paga
5. El ticket queda como "pagado" - ya es suyo
6. Si no paga en 5 minutos, el ticket se libera automáticamente
```

Para lograr esto, el sistema está dividido en **4 microservicios** que trabajan juntos.

---

## 2. Los 4 servicios del proyecto

### ¿Qué es un microservicio?

Antes de ver los servicios, entendamos el concepto. Un **microservicio** es una aplicación pequeña e independiente que hace UNA cosa bien. En lugar de tener un programa gigante que lo hace todo (lo que se llama un "monolito"), tienes varios programas pequeños que colaboran.

**Analogía:** Un restaurante de fast food. No hay un empleado que hace todo (toma el pedido, cocina, cobra, entrega). Hay personas especializadas: el cajero, el cocinero, el que entrega. Cada uno hace su parte.

### Los 4 microservicios:

```
┌─────────────────────────────────────────────────────────────────┐
│                    SISTEMA DE TICKETING                         │
│                                                                 │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │   Frontend   │  │   Producer   │  │ CRUD Service │          │
│  │  (Next.js)   │  │  Puerto 8001 │  │  Puerto 8002 │          │
│  │              │  │              │  │              │          │
│  │ Lo que ve    │  │ Recibe       │  │ Gestiona los │          │
│  │ el usuario   │  │ peticiones   │  │ datos (CRUD) │          │
│  │ en el nave-  │  │ y publica    │  │              │          │
│  │ gador        │  │ eventos      │  │              │          │
│  └──────────────┘  └──────────────┘  └──────────────┘          │
│                                                                 │
│  ┌──────────────┐  ┌──────────────────────────────────┐        │
│  │  Reservation │  │         PaymentService           │        │
│  │   Service    │  │                                  │        │
│  │              │  │ Procesa pagos aprobados y         │        │
│  │ Procesa      │  │ rechazados                       │        │
│  │ reservas de  │  │                                  │        │
│  │ tickets      │  │                                  │        │
│  └──────────────┘  └──────────────────────────────────┘        │
└─────────────────────────────────────────────────────────────────┘
```

| Servicio | Puerto | Tipo | Responsabilidad principal |
|----------|--------|------|--------------------------|
| **Frontend** | 3000 | Web (Next.js) | Interfaz visual para el usuario |
| **Producer** | 8001 | API REST | Recibe acciones del Frontend y las convierte en eventos |
| **CRUD Service** | 8002 | API REST | Crear, leer, actualizar, eliminar eventos y tickets |
| **ReservationService** | - | Worker | Procesa reservas de tickets en segundo plano |
| **PaymentService** | - | Worker | Procesa pagos aprobados y rechazados en segundo plano |

> **¿Qué es un Worker?** Es un servicio que no tiene puerto HTTP. No espera peticiones del navegador. En cambio, está constantemente "escuchando" una cola de mensajes, y cuando llega un mensaje, lo procesa. Es como un trabajador de bodega que siempre está mirando la cinta transportadora esperando cajas.

---

## 3. El problema de la comunicación entre servicios

Aquí está la pregunta clave: **¿Cómo se comunican estos 4 servicios entre sí?**

### Opción 1: HTTP directo (llamadas síncronas)

La forma más obvia sería que el Producer llame directamente al ReservationService por HTTP, igual que el Frontend llama al Producer.

```
Frontend → Producer → ReservationService
                    → PaymentService
```

**Esto tiene problemas serios:**

| Problema | Explicación |
|----------|-------------|
| **Acoplamiento** | Si el ReservationService se cae, el Producer también falla |
| **Bloqueo** | El Producer tiene que ESPERAR a que el ReservationService responda antes de hacer otra cosa |
| **Escala mal** | Si llegan 1000 reservas al mismo tiempo, el Producer tiene que hacer 1000 llamadas simultáneas |
| **Fragilidad** | Si necesitas agregar un nuevo servicio que también reaccione a reservas, hay que modificar el Producer |

**Analogía del problema:** Imagina que el cajero del restaurante llama por teléfono directamente al cocinero por cada pedido, espera que el cocinero confirme que recibió el pedido, y solo entonces atiende al siguiente cliente. El restaurante colapsa.

### Opción 2: Mensajería asíncrona (la solución que usa este proyecto)

En lugar de llamarse directamente, los servicios se comunican a través de un **intermediario de mensajes**: RabbitMQ.

```
Frontend → Producer → [RabbitMQ] → ReservationService
                                  → PaymentService
```

El Producer publica un mensaje y sigue su vida. Los demás servicios lo procesan cuando pueden.

**Analogía:** El cajero escribe el pedido en un papel y lo pone en la mesa de pedidos. El cocinero lo toma cuando puede. El cajero ya está atendiendo al siguiente cliente sin esperar.

---

## 4. RabbitMQ: el corazón del sistema

### ¿Qué es RabbitMQ?

**RabbitMQ** es un software de mensajería llamado **Message Broker** (intermediario de mensajes). Es como el sistema postal de tu aplicación: recibe "cartas" (mensajes) y se asegura de que lleguen al destinatario correcto.

No es parte de tu código. Es un servicio independiente que corre en su propio contenedor Docker (lo ves en el `compose.yml`).

### ¿Por qué se llama "Rabbit"?

Porque los mensajes se mueven rápido, como conejos. Es una broma interna de sus creadores.

### Los 5 conceptos fundamentales de RabbitMQ

#### 4.1 Mensaje (Message)

Es la unidad de información que viaja por el sistema. En este proyecto, un mensaje es un objeto JSON:

```json
{
  "ticketId": "abc-123",
  "userId": "user-456",
  "eventId": "event-789",
  "reservedAt": "2026-02-19T10:00:00Z"
}
```

**Importante:** El mensaje es solo datos. No contiene código ni instrucciones de cómo procesarlo. Eso lo decide el servicio que lo recibe.

#### 4.2 Cola (Queue)

Es una lista ordenada donde los mensajes esperan su turno para ser procesados. Funciona como una fila del banco: el primero en llegar es el primero en ser atendido (FIFO: First In, First Out).

```
Cola: q.ticket.reserved
┌──────────────────────────────────────────────┐
│  msg1  │  msg2  │  msg3  │  msg4  │  msg5   │
└──────────────────────────────────────────────┘
  ↑ nuevos mensajes entran por aquí
                                    salen por aquí ↑
```

Las colas en este proyecto:

| Cola | Quién la consume | Para qué |
|------|-----------------|----------|
| `q.ticket.reserved` | ReservationService | Procesar nuevas reservas |
| `q.ticket.payments.approved` | PaymentService | Marcar tickets como pagados |
| `q.ticket.payments.rejected` | PaymentService | Liberar tickets rechazados |
| `q.ticket.expired` | (futuro servicio) | Liberar tickets expirados |
| `q.ticket.reserved.delay` | Nadie (tiene TTL) | Cola de espera con temporizador |

#### 4.3 Producer (Publicador)

Es quien **crea y envía** mensajes. En este proyecto, el servicio llamado `producer` es el publicador principal. Tiene dos publicadores concretos:

- `RabbitMQTicketPublisher`: publica eventos de reserva
- `RabbitMQPaymentPublisher`: publica eventos de pago

**Importante:** El Producer no sabe quién va a procesar su mensaje. Solo lo pone en el sistema y se olvida. Esta característica se llama **fire and forget** (disparar y olvidar).

#### 4.4 Consumer (Consumidor)

Es quien **lee y procesa** mensajes de una cola. En este proyecto:

- `ReservationService` consume la cola `q.ticket.reserved`
- `PaymentService` consume las colas `q.ticket.payments.approved` y `q.ticket.payments.rejected`

Los consumers están siempre ejecutándose como procesos en segundo plano (`BackgroundService` en .NET), esperando mensajes continuamente.

#### 4.5 Exchange (Intercambiador)

El Exchange es el "repartidor inteligente" entre el Producer y las colas. El Producer no manda mensajes directamente a una cola, los manda al Exchange, y el Exchange decide a qué cola(s) redirigirlos.

**Analogía:** El Exchange es como una oficina postal central. Recibe todos los paquetes y los clasifica por destino.

En este proyecto hay un Exchange llamado `tickets` de tipo `topic`:

```
                    Exchange "tickets" (tipo: topic)
                    ┌─────────────────────────────┐
                    │                             │
                    │  Reglas de enrutamiento:    │
                    │  ticket.reserved      → Q1  │
                    │  ticket.payments.*    → Q2  │
                    │  ticket.expired       → Q3  │
                    └─────────────────────────────┘
                                  │
          ┌───────────────────────┼────────────────────────┐
          │                       │                        │
          ▼                       ▼                        ▼
┌──────────────────┐  ┌───────────────────────┐  ┌──────────────────┐
│q.ticket.reserved │  │q.ticket.payments.*    │  │q.ticket.expired  │
└──────────────────┘  └───────────────────────┘  └──────────────────┘
```

#### 4.6 Routing Key (Clave de enrutamiento)

Es la "dirección" que le pones al mensaje para que el Exchange sepa a qué cola mandarlo. Es una cadena de texto con puntos, como:

- `ticket.reserved` → va a `q.ticket.reserved`
- `ticket.payments.approved` → va a `q.ticket.payments.approved`
- `ticket.payments.rejected` → va a `q.ticket.payments.rejected`

El Exchange de tipo `topic` permite usar el comodín `*` para que una cola reciba múltiples routing keys. Por ejemplo, `ticket.payments.*` captura tanto `approved` como `rejected`.

#### 4.7 Binding (Enlace)

Es la regla que conecta un Exchange con una cola. Se define al configurar RabbitMQ (en `scripts/setup-rabbitmq.sh`):

```bash
# "Cuando llegue un mensaje con routing key 'ticket.reserved',
#  enviarlo a la cola 'q.ticket.reserved'"
rabbitmqadmin declare binding \
  source=tickets \
  destination=q.ticket.reserved \
  routing_key=ticket.reserved
```

#### 4.8 ACK / NACK (Confirmación)

Cuando un Consumer termina de procesar un mensaje, debe avisar a RabbitMQ si lo procesó bien o no:

- **ACK** (Acknowledge): "Procesé el mensaje correctamente, puedes eliminarlo de la cola"
- **NACK** (Negative Acknowledge): "Algo salió mal, vuelve a ponerlo en la cola para reintentarlo"

Esto garantiza que **ningún mensaje se pierda**. Si el servicio se cae mientras procesa un mensaje, RabbitMQ lo re-encola automáticamente.

> ⚠️ **Problema detectado en este proyecto:** El `ReservationService` hace ACK incluso cuando hay un error. Esto significa que si la base de datos falla momentáneamente, el mensaje se pierde. Es una deuda técnica a corregir.

---

## 5. Conceptos fundamentales de EDA

### ¿Qué es EDA (Event-Driven Architecture)?

**EDA** (Arquitectura Orientada a Eventos) es un estilo de diseño donde los componentes del sistema se comunican publicando y consumiendo **eventos**, en lugar de llamarse directamente.

Un **evento** es algo que ya ocurrió. Ejemplos:
- `TicketReservado` → el ticket fue reservado
- `PagoAprobado` → el pago fue aprobado
- `TicketExpirado` → el tiempo de reserva venció

La diferencia con una llamada directa es sutil pero poderosa:

| Llamada directa (HTTP) | Evento |
|----------------------|--------|
| "Haz esto ahora" (imperativo) | "Esto ocurrió" (informativo) |
| El llamador sabe a quién llama | El publicador no sabe quién escucha |
| Respuesta inmediata requerida | El procesamiento puede ser posterior |
| Acoplamiento fuerte | Desacoplamiento total |

### Los 4 beneficios clave de EDA

#### 1. Desacoplamiento
El Producer no sabe que existen ReservationService ni PaymentService. Solo sabe que ocurrió un evento. Si mañana agregas un servicio de notificaciones que envíe SMS cuando se reserve un ticket, no necesitas cambiar el Producer. Solo creas una nueva cola, la conectas al Exchange con el binding correcto, y listo.

```
ANTES (sin EDA):            DESPUÉS (con EDA):
Producer → Reservation      Producer → [Exchange] → Reservation
Producer → Payment                              ↘→ Payment
Producer → Notificacion                         ↘→ Notificacion (nuevo, sin tocar Producer)
```

#### 2. Resiliencia
Si el ReservationService se cae, los mensajes no se pierden. Se acumulan en la cola y cuando el servicio vuelve, los procesa todos en orden. El Producer nunca se entera de que hubo un problema.

#### 3. Escalabilidad
Si hay muchas reservas, puedes correr múltiples instancias del ReservationService, todas consumiendo de la misma cola. RabbitMQ distribuye los mensajes automáticamente entre ellas.

#### 4. Trazabilidad
Cada evento queda registrado. Puedes ver exactamente qué ocurrió y cuándo: "A las 10:03 se reservó el ticket X, a las 10:07 se aprobó el pago, a las 10:07:02 se marcó como pagado".

### EDA vs las otras arquitecturas

Es importante entender que EDA describe cómo se **comunican** los servicios, no cómo están **organizados internamente**. Es un patrón de integración, no de diseño interno.

```
┌─────────────────────────────────────────────────────────┐
│              NIVEL MACRO (entre servicios)              │
│           Patrón: EDA (eventos via RabbitMQ)            │
│                                                         │
│  Servicio A ──evento──► RabbitMQ ──evento──► Servicio B │
└─────────────────────────────────────────────────────────┘
              ↑                              ↑
              │                              │
┌─────────────────────┐       ┌─────────────────────────┐
│  NIVEL MICRO        │       │  NIVEL MICRO            │
│  (dentro del        │       │  (dentro del            │
│   Servicio A)       │       │   Servicio B)           │
│  Patrón: N-Layer,   │       │  Patrón: Hexagonal,     │
│  Hexagonal, etc.    │       │  DDD, etc.              │
└─────────────────────┘       └─────────────────────────┘
```

---

## 6. El flujo completo de una reserva

Veamos paso a paso qué ocurre cuando un usuario reserva un ticket, mencionando cada archivo de código que interviene:

```
PASO 1: El usuario hace clic en "Reservar"
══════════════════════════════════════════

[Navegador - Frontend Next.js]
    │
    │  POST /api/tickets/reserve
    │  (HTTP normal, como cualquier web)
    ▼
[Producer - TicketsController.cs]
    │
    │  1. Recibe la petición HTTP
    │  2. Crea un objeto TicketReservedEvent con los datos
    │  3. Llama a RabbitMQTicketPublisher.PublishTicketReservedAsync()
    │  4. Responde inmediatamente: HTTP 202 Accepted
    │     (NO espera a que se procese la reserva)
    │
    ▼
[Producer - RabbitMQTicketPublisher.cs]
    │
    │  Serializa el evento a JSON
    │  Lo publica al Exchange "tickets"
    │  Con routing key: "ticket.reserved"
    │  Con Persistent = true (sobrevive a reinicios)
    │
    ▼
[RabbitMQ - Exchange "tickets"]
    │
    │  Binding: ticket.reserved → q.ticket.reserved
    │
    ▼
[Cola: q.ticket.reserved]
    │  El mensaje espera aquí...
    │  (puede ser milisegundos o segundos)
    ▼

PASO 2: ReservationService procesa el mensaje
══════════════════════════════════════════════

[ReservationService - RabbitMQConsumer.cs]
    │
    │  Está en un bucle infinito escuchando la cola
    │  Llega el mensaje: lo deserializa a ProcessReservationCommand
    │
    ▼
[ReservationService - ProcessReservationCommandHandler.cs]
    │
    │  1. Busca el ticket en la BD por su ID
    │  2. Verifica que existe
    │  3. Verifica que su status es "available"
    │  4. Calcula: expires_at = ahora + 5 minutos
    │  5. Llama a TicketRepository.TryReserveAsync()
    │
    ▼
[ReservationService - TicketRepository.cs]
    │
    │  Ejecuta en PostgreSQL:
    │  UPDATE tickets
    │  SET status='reserved', reserved_at=NOW(), expires_at=NOW()+5min,
    │      reserved_by='userId', version=version+1
    │  WHERE id='abc'
    │    AND status='available'    ← Solo si sigue disponible
    │    AND version=X             ← Optimistic locking
    │
    ▼
[PostgreSQL]
    │
    │  tickets:
    │  ┌──────┬──────────┬───────────────────────┐
    │  │  id  │  status  │  expires_at           │
    │  ├──────┼──────────┼───────────────────────┤
    │  │ abc  │ reserved │ 2026-02-19T10:05:00Z  │
    │  └──────┴──────────┴───────────────────────┘

PASO 3: El usuario ve que su ticket está reservado
══════════════════════════════════════════════════

[Navegador - hace polling]
    │
    │  GET /api/tickets/abc
    ▼
[CRUD Service - TicketsController.cs]
    │  Consulta PostgreSQL
    ▼
[Navegador recibe]
    { "id": "abc", "status": "reserved", "expiresAt": "10:05:00" }
    "Tienes 5 minutos para pagar"
```

### ¿Qué pasa si no paga en 5 minutos? (Dead Letter Queue)

Esta es una de las partes más elegantes del sistema:

```
[RabbitMQTicketPublisher.cs]
    │
    │  Al publicar la reserva, TAMBIÉN publica en:
    │  Cola: q.ticket.reserved.delay
    │  Con TTL (Time To Live): 300,000 ms = 5 minutos
    │
    ▼
[Cola: q.ticket.reserved.delay]
    │
    │  El mensaje tiene un reloj regresivo de 5 minutos.
    │  Nadie consume esta cola.
    │  El mensaje solo espera...
    │
    │  ⏱ Pasan 5 minutos sin que el usuario pague...
    │
    │  RabbitMQ "mata" el mensaje (lo considera muerto)
    │  Lo envía automáticamente a la Dead Letter Exchange
    │  Con routing key: "ticket.expired"
    │
    ▼
[Cola: q.ticket.expired]
    │
    │  Aquí debería haber un consumer que diga:
    │  "Este ticket expiró, cambiar status a 'available'"
    │  (Esta parte está pendiente de implementar)
```

> **¿Qué es una Dead Letter Queue?**
> Es la "papelera" de RabbitMQ. Los mensajes que expiran (TTL) o que fallan repetidamente van a parar aquí. En lugar de perderlos, puedes procesarlos de forma especial. En este caso, se usa creativamente como mecanismo de temporizador.

---

## 7. El flujo completo de un pago

```
PASO 1: El usuario hace clic en "Pagar"
════════════════════════════════════════

[Navegador]
    │  POST /api/payments/process
    │  Body: { ticketId: "abc", userId: "xyz", amount: 50.00 }
    ▼
[Producer - PaymentsController.cs]
    │
    │  1. Recibe la petición
    │  2. Llama a SimulatePaymentProcessing()
    │     → Genera un número random 0-99
    │     → Si < 80: APROBADO (80% de probabilidad)
    │     → Si >= 80: RECHAZADO (20% de probabilidad)
    │  3. Según resultado, publica el evento correspondiente
    │  4. Responde: HTTP 202 Accepted
    │
    ▼
[Producer - RabbitMQPaymentPublisher.cs]
    │
    │  Si APROBADO:
    │    routing key: "ticket.payments.approved"
    │    → Cola: q.ticket.payments.approved
    │
    │  Si RECHAZADO:
    │    routing key: "ticket.payments.rejected"
    │    → Cola: q.ticket.payments.rejected

PASO 2: PaymentService procesa el resultado
════════════════════════════════════════════

[PaymentService - Worker.cs]
    │
    │  Inicia dos consumers en paralelo:
    │  - TicketPaymentConsumer en q.ticket.payments.approved
    │  - TicketPaymentConsumer en q.ticket.payments.rejected
    │
    ▼
[PaymentService - TicketPaymentConsumer.cs]
    │
    │  Recibe el mensaje, extrae la routing key
    │  Llama a PaymentEventDispatcherImpl.DispatchAsync()
    │
    ▼
[PaymentService - PaymentEventDispatcherImpl.cs]
    │
    │  Tiene una lista de handlers registrados:
    │  - PaymentApprovedEventHandler (para cola "approved")
    │  - PaymentRejectedEventHandler (para cola "rejected")
    │
    │  Busca cuál handler corresponde al mensaje recibido
    │  Lo ejecuta
    │
    ▼
[PaymentService - PaymentApprovedEventHandler.cs]  O
[PaymentService - PaymentRejectedEventHandler.cs]
    │
    │  Si APROBADO:
    │    PaymentValidationService.ValidateAndProcessApprovedPaymentAsync()
    │    → ticket.status = "paid"
    │    → Se registra en tabla payments
    │
    │  Si RECHAZADO:
    │    PaymentValidationService.ValidateAndProcessRejectedPaymentAsync()
    │    → ticket.status = "available" (se libera para otro usuario)
    │
    ▼
[PostgreSQL]
    │  El ticket queda en su estado final
```

---

## 8. La base de datos: cómo se guarda todo

Todos los servicios comparten una sola base de datos PostgreSQL. Estas son las tablas y cómo se relacionan:

```
┌─────────────────────────────────────────────────────────────┐
│                         events                              │
├──────────────┬───────────────────────────────────────────── │
│ id (PK)      │ Identificador único del evento               │
│ name         │ Nombre (ej: "Concierto de Rock")             │
│ starts_at    │ Cuándo comienza el evento                    │
└──────┬───────┘                                              │
       │ Un evento tiene muchos tickets (1:N)                 │
       ▼                                                      │
┌─────────────────────────────────────────────────────────────┐
│                         tickets                             │
├──────────────┬───────────────────────────────────────────── │
│ id (PK)      │ Identificador único del ticket               │
│ event_id(FK) │ A qué evento pertenece                       │
│ status       │ available / reserved / paid / expired        │
│ reserved_at  │ Cuándo se reservó                            │
│ expires_at   │ Cuándo expira la reserva (reserved_at + 5min)│
│ paid_at      │ Cuándo se pagó                               │
│ order_id     │ ID de la orden de compra                     │
│ reserved_by  │ ID del usuario que reservó                   │
│ version      │ Número para Optimistic Locking ← CLAVE       │
└──────┬───────┘                                              │
       │                                                      │
       ├──────────────────────┐                               │
       │ Un ticket tiene      │ Un ticket tiene               │
       │ muchos pagos (1:N)   │ mucho historial (1:N)         │
       ▼                      ▼                               │
┌──────────────┐  ┌──────────────────────────────────────┐   │
│   payments   │  │          ticket_history              │   │
├──────────────┤  ├──────────────────────────────────────┤   │
│ id           │  │ ticket_id (FK)                       │   │
│ ticket_id(FK)│  │ old_status  (ej: "available")        │   │
│ status       │  │ new_status  (ej: "reserved")         │   │
│ provider_ref │  │ changed_at  (cuándo ocurrió)         │   │
│ amount_cents │  │ reason      (por qué cambió)         │   │
│ currency     │  └──────────────────────────────────────┘   │
└──────────────┘                                             │
```

### El campo `version`: Optimistic Locking explicado

Imagina que dos usuarios intentan reservar el mismo ticket al mismo tiempo:

```
Usuario A                     Usuario B
─────────────────────         ─────────────────────
Lee ticket: version=5         Lee ticket: version=5
(status: available)           (status: available)

         ← ambos quieren reservarlo →

Ejecuta UPDATE:               Ejecuta UPDATE:
WHERE id=X                    WHERE id=X
AND version=5                 AND version=5
AND status='available'        AND status='available'
SET version=6                 SET version=6
```

Solo UNO puede ganar. El primero que llega actualiza la versión a 6. El segundo intenta actualizar con `version=5` pero ya no existe esa versión → no afecta ninguna fila → sabe que alguien más ganó.

**Esto previene que dos personas compren el mismo ticket**, sin necesidad de bloquear toda la base de datos.

---

## 9. Arquitecturas internas de cada servicio

Hasta ahora hemos visto cómo se comunican los servicios (EDA). Ahora veamos cómo está organizado el código internamente en cada uno.

### 9.1 CRUD Service: N-Layer clásica

La organización más simple y directa. Cada capa solo conoce la inmediatamente inferior:

```
Petición HTTP
     ↓
Controllers/          ← Recibe la petición, valida, responde
     ↓
Services/             ← Contiene la lógica de negocio
     ↓
Repositories/         ← Define las interfaces (contratos)
     ↓
Data/                 ← Implementa el acceso real a la BD
     ↓
PostgreSQL
```

**Problema principal:** Las entidades (`Ticket`, `Event`) son **anémicas**: solo tienen propiedades (datos), sin comportamiento (métodos con lógica). Toda la lógica está en los Services, lo que los hace muy grandes y difíciles de mantener.

### 9.2 ReservationService: Arquitectura Hexagonal

La más madura del proyecto. Se organiza en capas donde las dependencias siempre apuntan hacia el centro (el dominio):

```
┌─────────────────────────────────────────────────────┐
│              INFRAESTRUCTURA (exterior)              │
│    RabbitMQConsumer        TicketRepository          │
│         │                       ↑                   │
│         │ implementa            │ implementa         │
│         ▼                       │                   │
│    IMessageConsumer        ITicketRepository         │
│         │                       │                   │
│         └───────────────────────┘                   │
│                     ↓ usa                           │
│    ┌────────────────────────────────────────────┐   │
│    │           APLICACIÓN (medio)               │   │
│    │  ProcessReservationCommandHandler          │   │
│    │  ProcessReservationCommand                 │   │
│    │  ProcessReservationResponse                │   │
│    └────────────────────────────────────────────┘   │
│                     ↓ usa                           │
│    ┌────────────────────────────────────────────┐   │
│    │            DOMINIO (centro)                │   │
│    │  Ticket (entidad)                          │   │
│    │  TicketStatus (enum)                       │   │
│    │  ITicketRepository (interfaz)              │   │
│    │  TicketNotAvailableException               │   │
│    └────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────┘
```

**La regla de oro:** El Dominio no depende de nada externo. La Infraestructura depende del Dominio (no al revés). Esto hace que el negocio sea independiente de la tecnología.

### 9.3 PaymentService: Patrón Dispatcher/Handler

Usa un patrón similar al Mediator. Hay un despachador central que recibe todos los mensajes y los enruta al handler correcto:

```
Mensaje llega
     ↓
TicketPaymentConsumer.cs
     ↓
PaymentEventDispatcherImpl.cs  ← "¿Cuál handler procesa este mensaje?"
     ├→ PaymentApprovedEventHandler.cs  (si es pago aprobado)
     └→ PaymentRejectedEventHandler.cs  (si es pago rechazado)
```

**Ventaja:** Para agregar un nuevo tipo de pago (ej: reembolso), solo se crea un nuevo handler. No se modifica el Dispatcher. Esto cumple el principio **Open/Closed**: abierto para extensión, cerrado para modificación.

---

## 10. ¿Es este sistema Event-Driven?

**Sí, pero con matices importantes.**

El sistema ES event-driven en la forma en que los servicios se **comunican entre sí**. Eso es 100% correcto y bien implementado.

Sin embargo, el término "arquitectura basada en eventos" puede llevar a confusión porque hay dos conceptos distintos que suenan parecido:

### EDA (Event-Driven Architecture) ✅ Sí aplica

Los servicios publican y consumen eventos. El Producer no llama directamente al ReservationService. RabbitMQ actúa como intermediario. Esto es EDA pura.

### Event Sourcing ❌ No aplica

Event Sourcing es un patrón donde el **estado de una entidad se reconstruye reproduciendo todos sus eventos**. En este sistema, el estado vive en tablas PostgreSQL y se actualiza directamente con `UPDATE`. La tabla `ticket_history` es un audit log (registro de auditoría), no un event store.

### La descripción correcta del sistema completo:

> **"Microservicios que se comunican mediante Event-Driven Architecture (EDA) usando RabbitMQ como broker, donde cada servicio tiene su propia arquitectura interna (N-Layer, Hexagonal, Dispatcher/Handler)."**

---

## 11. Fortalezas del diseño actual

### ✅ Desacoplamiento genuino
El Producer no conoce a ReservationService ni PaymentService. Si mañana se agrega un servicio de notificaciones por email, solo se crea una nueva cola y se conecta al Exchange. Cero cambios en el Producer.

### ✅ Resiliencia ante fallos
Si un servicio se cae, los mensajes esperan en la cola. Cuando el servicio vuelve, los procesa en orden. El usuario no nota la diferencia.

### ✅ Mensajes persistentes
Los mensajes se crean con `Persistent = true`. Si RabbitMQ se reinicia, los mensajes sobreviven porque están guardados en disco.

### ✅ Optimistic Locking
El campo `version` en la tabla `tickets` previene que dos usuarios compren el mismo ticket simultáneamente, sin bloquear la base de datos.

### ✅ Dead Letter Queue para expiración
El mecanismo de TTL + dead-letter es elegante: no necesita un cron job ni un proceso que esté chequeando tickets expirados constantemente. RabbitMQ lo maneja automáticamente.

### ✅ Arquitectura hexagonal en ReservationService
Bien estructurado, testeable, con separación clara de capas y Dependency Inversion aplicado correctamente.

### ✅ Patrón Open/Closed en PaymentService
El Dispatcher/Handler permite agregar nuevos tipos de evento sin modificar código existente.

---

## 12. Áreas de mejora identificadas

### ⚠️ Base de datos compartida (Shared Database anti-pattern)
Todos los servicios acceden a la misma base PostgreSQL. En microservicios maduros, cada servicio debería tener su propia base de datos.

**¿Por qué es un problema?** Si cambias el schema de la tabla `tickets` para el ReservationService, podrías romper el CRUD Service o el PaymentService.

### ⚠️ ACK en caso de error (ReservationService)
Si la base de datos falla mientras se procesa un mensaje, el sistema igual le dice a RabbitMQ "recibí y procesé el mensaje" (ACK), cuando en realidad no lo procesó. El mensaje se pierde.

**Corrección:** Hacer NACK (Negative Acknowledge) en caso de error, para que RabbitMQ vuelva a encolar el mensaje y se reintente.

### ⚠️ Lógica de negocio en el Producer
El `PaymentsController` decide si un pago es aprobado o rechazado con un número aleatorio. Esta decisión es **lógica de negocio** y debería estar en el PaymentService, no en el Producer.

**El Producer debería:** Recibir la petición y publicar un evento `PaymentRequested`.
**El PaymentService debería:** Recibir ese evento, procesarlo, y publicar `PaymentApproved` o `PaymentRejected`.

### ⚠️ Inconsistencia arquitectónica entre servicios
Los tres servicios están organizados de forma diferente internamente. Esto dificulta que un nuevo desarrollador entienda el sistema completo.

### ⚠️ Modelos duplicados
La entidad `Ticket` y el enum `TicketStatus` existen en 3 servicios diferentes. Si se agrega un nuevo estado, hay que actualizarlo en 3 lugares.

---

## 13. Glosario de términos

| Término | Definición simple |
|---------|------------------|
| **Microservicio** | Aplicación pequeña e independiente que hace una sola cosa |
| **API REST** | Interfaz que permite comunicación HTTP entre sistemas |
| **Worker / BackgroundService** | Proceso que corre en segundo plano sin interfaz HTTP |
| **RabbitMQ** | Software intermediario de mensajes (Message Broker) |
| **Message Broker** | Intermediario que recibe y distribuye mensajes entre servicios |
| **Mensaje** | Objeto de datos (JSON) que viaja entre servicios |
| **Cola (Queue)** | Lista ordenada donde los mensajes esperan ser procesados |
| **Exchange** | Repartidor que decide a qué cola enviar cada mensaje |
| **Routing Key** | Etiqueta del mensaje que define su destino |
| **Binding** | Regla que conecta un Exchange con una Cola |
| **Producer** | Quien publica mensajes |
| **Consumer** | Quien consume y procesa mensajes |
| **ACK** | Confirmación de que un mensaje fue procesado correctamente |
| **NACK** | Señal de que el mensaje falló y debe reintentarse |
| **Dead Letter Queue** | Cola que recibe mensajes expirados o fallidos |
| **TTL** | Time To Live - tiempo de vida máximo de un mensaje |
| **EDA** | Event-Driven Architecture - comunicación basada en eventos |
| **Event Sourcing** | Patrón donde el estado se reconstruye a partir de eventos (NO es lo que usa este sistema) |
| **Optimistic Locking** | Técnica para prevenir escrituras simultáneas usando un campo `version` |
| **FIFO** | First In, First Out - el primero en entrar es el primero en salir |
| **Fire and Forget** | Publicar un evento sin esperar respuesta |
| **Arquitectura Hexagonal** | Organización interna donde el dominio no depende de nada externo |
| **N-Layer** | Organización en capas donde cada una conoce solo a la siguiente |
| **DTO** | Data Transfer Object - objeto que transfiere datos entre capas |
| **Dependency Injection** | Patrón donde las dependencias se reciben desde afuera, no se crean internamente |
| **Monolito** | Aplicación única y grande que hace todo (lo opuesto a microservicios) |
| **CRUD** | Create, Read, Update, Delete - las 4 operaciones básicas sobre datos |
| **Docker / Compose** | Herramientas para ejecutar aplicaciones en contenedores aislados |
| **PostgreSQL** | Base de datos relacional usada en este proyecto |

---

*Documento generado como apoyo al aprendizaje del proyecto Ticketing.*
*Fecha: 19/02/2026*
