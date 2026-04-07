# Contratos de API — Lista de Espera

Todos los endpoints nuevos se sirven desde el **CRUD Service**. No se crean endpoints nuevos en Producer ni en los workers.

El Producer existente (`POST /api/payments`) se reutiliza sin cambios cuando el comprador avanza al pago después de consumir su oportunidad.

---

## Endpoints

### POST /api/waitlist/entries

Inscribe al comprador en la lista de espera de un evento.

**Request body:**

```json
{
  "eventId": 42,
  "buyerEmail": "comprador@ejemplo.com"
}
```

**Respuestas:**

| Código | Significado | Body |
|---|---|---|
| 201 Created | Inscripción registrada | `{ "id": 1, "eventId": 42, "buyerEmail": "...", "status": "active", "enrolledAt": "..." }` |
| 409 Conflict | Ya existe una inscripción activa para ese comprador y evento | `{ "error": "El comprador ya tiene una inscripción activa para este evento" }` |
| 422 Unprocessable Entity | La lista de espera del evento ya cerró (fecha alcanzada) | `{ "error": "La lista de espera de este evento ya cerró" }` |

**Reglas de negocio que valida:**
- El comprador no tiene otra inscripción activa para el mismo evento (unicidad).
- La fecha del evento no ha sido alcanzada (vigencia de la lista).

**HU asociadas:** HU1 (Inscripción), HU7 (Inscripción desde la aplicación)

---

### GET /api/waitlist/entries

Consulta el estado actual del comprador en la lista de espera de un evento.

**Query parameters:**

| Parámetro | Tipo | Requerido | Descripción |
|---|---|---|---|
| `eventId` | int | Sí | ID del evento |
| `email` | string | Sí | Correo del comprador |

**Ejemplo:** `GET /api/waitlist/entries?eventId=42&email=comprador@ejemplo.com`

**Respuestas:**

| Código | Significado | Body |
|---|---|---|
| 200 OK | Estado encontrado | Ver esquema abajo |
| 404 Not Found | No existe inscripción para ese comprador y evento | `{ "error": "No se encontró inscripción para este comprador y evento" }` |

**Esquema de respuesta 200:**

```json
{
  "entry": {
    "id": 1,
    "eventId": 42,
    "buyerEmail": "comprador@ejemplo.com",
    "status": "active",
    "enrolledAt": "2026-04-07T10:00:00Z"
  },
  "opportunity": {
    "id": 5,
    "ticketId": 100,
    "status": "active",
    "activatedAt": "2026-04-07T11:00:00Z",
    "expiresAt": "2026-04-07T11:15:00Z",
    "remainingMinutes": 12
  }
}
```

Si no hay oportunidad asociada, `opportunity` es `null`. El campo `remainingMinutes` solo está presente cuando el estado de la oportunidad es `active`.

**HU asociadas:** HU2 (Consulta de estado), HU8 (Consulta desde la aplicación)

---

### POST /api/waitlist/opportunities/{id}/claim

El comprador consume su oportunidad activa y avanza al flujo de pago.

**Path parameters:**

| Parámetro | Tipo | Descripción |
|---|---|---|
| `id` | int | ID de la oportunidad |

**Request body:**

```json
{
  "buyerEmail": "comprador@ejemplo.com"
}
```

**Respuestas:**

| Código | Significado | Body |
|---|---|---|
| 200 OK | Oportunidad consumida; el comprador puede avanzar a pago | `{ "opportunityId": 5, "ticketId": 100, "eventId": 42, "status": "consumed" }` |
| 404 Not Found | La oportunidad no existe | `{ "error": "Oportunidad no encontrada" }` |
| 409 Conflict | La oportunidad ya fue consumida o expiró | `{ "error": "La oportunidad ya no está activa" }` |
| 403 Forbidden | El email no corresponde al dueño de la oportunidad | `{ "error": "Esta oportunidad no pertenece al comprador indicado" }` |

**Reglas de negocio que valida:**
- La oportunidad existe y está en estado `active`.
- El `buyerEmail` corresponde al comprador que tiene la oportunidad.
- La oportunidad no ha expirado (el sistema verifica contra `expiresAt`).

**Efecto:** la oportunidad transiciona a `consumed`. El frontend redirige al comprador al flujo de pago existente (`POST /api/payments` en Producer) con el `ticketId` ya reservado.

**HU asociadas:** HU8 (Acción sobre oportunidad desde la aplicación)

---

### GET /api/waitlist/stream

Canal SSE (Server-Sent Events) para notificaciones en tiempo real de lista de espera.

**Query parameters:**

| Parámetro | Tipo | Requerido | Descripción |
|---|---|---|---|
| `email` | string | Sí | Correo del comprador |

**Ejemplo:** `GET /api/waitlist/stream?email=comprador@ejemplo.com`

**Tipo de contenido:** `text/event-stream`

**Eventos emitidos:**

| Evento | Cuándo se emite | Data |
|---|---|---|
| `opportunity_activated` | Se creó una oportunidad activa para el comprador | `{ "opportunityId": 5, "ticketId": 100, "eventId": 42, "expiresAt": "...", "remainingMinutes": 15 }` |
| `opportunity_expired` | La oportunidad del comprador expiró | `{ "opportunityId": 5, "eventId": 42, "reason": "timeout" }` |

**HU asociadas:** HU4 (Notificación in-app)

---

## Resumen de verbos HTTP

| Método | Endpoint | Operación | HU |
|---|---|---|---|
| POST | `/api/waitlist/entries` | Inscribirse | HU1, HU7 |
| GET | `/api/waitlist/entries` | Consultar estado | HU2, HU8 |
| POST | `/api/waitlist/opportunities/{id}/claim` | Consumir oportunidad | HU8 |
| GET | `/api/waitlist/stream` | Canal SSE | HU4 |

> No hay `PUT` ni `DELETE`. La inscripción no se modifica ni se cancela (fuera del alcance de esta épica). Los cambios de estado son producto de eventos del sistema (asignación, expiración), no de acciones HTTP directas del comprador.
