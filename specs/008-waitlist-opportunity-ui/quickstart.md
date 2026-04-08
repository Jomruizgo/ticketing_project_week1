# Quickstart: Waitlist Opportunity Status & Claim UI

**Feature**: 008-waitlist-opportunity-ui
**Date**: 2026-04-08

## Prerrequisitos

1. `docker compose up -d --build` (infraestructura + servicios backend)
2. `cd frontend && npm install && npm run dev` (frontend en desarrollo)
3. Un evento sin entradas disponibles y con fecha futura
4. Un comprador inscrito en la lista de espera del evento (vía WaitlistEnrollForm de HU7)

## Validación de aceptación

### TC-HU8-01 — El comprador consulta su estado desde la aplicación

**Precondiciones**: Comprador inscrito con inscripción activa para un evento.

1. Navegar a `/buy/{eventId}`
2. Ingresar el email del comprador en el campo de consulta de estado
3. Verificar que la aplicación muestra el estado actual de la inscripción sin ambigüedad
4. Verificar que la información es coherente con el estado real del sistema

**Resultado esperado**: Estado "En espera" visible, sin botón de pago ni cuenta regresiva.

---

### TC-HU8-02 — El comprador ve una oportunidad activa con tiempo restante

**Precondiciones**: El comprador tiene una oportunidad activa con tiempo restante de vigencia (una entrada fue liberada y asignada por HU3).

1. El comprador consulta su estado desde la aplicación (o recibe notificación SSE)
2. Verificar que la aplicación muestra "Oportunidad activa"
3. Verificar que muestra el tiempo restante de vigencia (cuenta regresiva MM:SS)
4. Verificar que muestra el botón "Avanzar al pago"

**Resultado esperado**: Oportunidad activa visible con cuenta regresiva y botón de acción.

---

### TC-HU8-03 — El comprador actúa sobre una oportunidad activa desde la aplicación

**Precondiciones**: El comprador tiene una oportunidad activa con tiempo restante.

1. El comprador hace clic en "Avanzar al pago"
2. Verificar que la oportunidad pasa a estado "consumed"
3. Verificar que el comprador es redirigido al flujo de pago con la entrada reservada
4. Verificar que la aplicación deja de mostrar la oportunidad como activa

**Resultado esperado**: Transición Active→Consumed exitosa, flujo de pago visible con ticketId correcto.

---

### TC-HU8-04 — El comprador ve que su oportunidad expiró desde la aplicación

**Precondiciones**: La oportunidad del comprador venció sin ser utilizada (o se simula esperando 15 min).

1. El comprador consulta su estado desde la aplicación
2. Verificar que la aplicación muestra "Oportunidad expirada"
3. Verificar que no hay botón de pago
4. Verificar coherencia con el estado real del sistema

**Resultado esperado**: Estado "Oportunidad expirada" visible, sin acciones de pago.

## Comandos de desarrollo

```bash
# Backend (CRUD Service)
dotnet test crud_service/tests/CrudService.Application.Tests  # Tests unitarios handler
dotnet run --project crud_service/src/CrudService.Api          # Servidor CRUD

# Frontend
cd frontend && npm test                                        # Tests unitarios vitest
cd frontend && npm run dev                                     # Servidor de desarrollo

# Infraestructura completa
docker compose up -d --build
```
