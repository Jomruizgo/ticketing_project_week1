# Research: Waitlist Enrollment UI

**Feature**: 007-waitlist-enrollment-ui
**Date**: 2026-04-08

## Unknowns Resolved

### 1. Framework de testing en frontend

**Pregunta**: ¿Existe infraestructura de testing en el frontend? ¿Qué framework usar para los test cases TC-HU7-*?

**Investigación**: Se inspeccionó `frontend/package.json`. No hay dependencias de testing (jest, vitest, testing-library, playwright, cypress). No existen archivos `*.test.*` ni `*.spec.*` bajo `frontend/`.

**Decisión**: Los 4 test cases (TC-HU7-01 a TC-HU7-04) son de **tipo A (aceptación)**. Se validan manualmente contra el sistema desplegado (`docker compose up`) o mediante flujo E2E manual. No se agrega infraestructura de testing automatizado en esta feature — eso sería una adición de alcance fuera del requisito.

**Rationale**: El usuario indicó explícitamente "Sin cambios en backend: esta HU consume la API construida en HU1". El alcance es frontend-only y los test cases definidos en TestCases.md son de aceptación, no unitarios.

**Alternativas consideradas**:
- Agregar vitest + testing-library: rechazado por estar fuera del alcance de esta HU.
- Implementar tests E2E con Playwright: rechazado por la misma razón.

---

### 2. Integración del formulario: ¿página de compra o página de detalle?

**Pregunta**: ¿El formulario de waitlist se integra en `/buy/[id]` (buyer page) o en `/events/[id]` (admin page)?

**Investigación**: 
- `/events/[id]` es la vista de administración (muestra tabla de tickets, botones de edición, estadísticas). No es accesible para compradores.
- `/buy/[id]` es la página de compra del comprador. Muestra info del evento y el formulario de compra. El `buyer-event-card.tsx` enlaza a `/buy/[id]`.
- El prompt del usuario dice "Modificar la página de detalle del evento (`app/events/[id]/page.tsx` o el componente de detalle del evento relevante)".

**Decisión**: Integrar en **`/buy/[id]/page.tsx`** (la página del buyer). Es donde el comprador ve la disponibilidad y compra entradas. La mención de `/events/[id]` en el prompt es alternativa ("o el componente relevante"), y `/buy/[id]` es el componente relevante para el flujo de comprador.

**Rationale**: El flujo de usuario descrito en la spec — "un comprador navega a la página de un evento" — se mapea a `/buy/[id]`, que es la ruta de comprador. `/events/[id]` es admin-only.

**Alternativas consideradas**: 
- Modificar `/events/[id]`: rechazado porque es la vista admin y no tiene sentido mostrar inscripción waitlist a administradores.

---

### 3. Patrón de manejo de respuestas HTTP para waitlist

**Pregunta**: ¿Cómo manejar los códigos 409 y 422 si `handleResponse()` de api.ts los trata como errores genéricos?

**Investigación**: La función `handleResponse<T>()` en api.ts lanza `ApiError` para cualquier respuesta no-OK (excepto 202). Los campos son `status: number`, `message: string`, `serviceType: "crud" | "producer"`.

**Decisión**: Implementar `enrollInWaitlist()` como función independiente que NO usa `handleResponse()` genérico. En su lugar, maneja los códigos de respuesta directamente:
- 201 → parsear JSON como `WaitlistEntryDto` y devolver
- 409 → lanzar `ApiError(409, "Ya tienes una inscripción activa")`
- 422 → lanzar `ApiError(422, "La lista de espera ya cerró")`
- 404 → lanzar `ApiError(404, "Evento no encontrado")`
- Otros → lanzar `ApiError(status, mensaje genérico)`

El componente `WaitlistEnrollForm` captura el `ApiError` y distingue por `.status` para mostrar el mensaje apropiado.

**Rationale**: Sigue el patrón de `reserveTicket()` y `processPayment()` que también gestionan códigos de respuesta directamente sin pasar por `handleResponse()`.

**Alternativas consideradas**:
- Usar `handleResponse()` y capturar ApiError en el componente: viable pero pierde la oportunidad de mensajes de error específicos del dominio de waitlist.

---

### 4. Lógica de visibilidad condicional en la página de compra

**Pregunta**: ¿Cómo determinar si mostrar el flujo de compra, el formulario de waitlist, o el mensaje de lista cerrada?

**Investigación**: 
- `event.availableTickets` (number) indica entradas disponibles.
- `event.startsAt` (ISO string) indica la fecha/hora de inicio del evento.
- El polling via SWR (useEvent, useTickets) refresca automáticamente los datos.

**Decisión**: Lógica de renderizado condicional basada en tres estados:

```
if (availableTickets > 0) → flujo de compra existente
else if (new Date(event.startsAt) > new Date()) → WaitlistEnrollForm
else → mensaje "Lista de espera cerrada"
```

La comparación de fechas usa `new Date()` del navegador. El polling existente (cada 5s para event, cada 3s para tickets) garantiza que la transición entre estados sea reactiva.

**Rationale**: Alineado con la especificación (FR-001, FR-002, FR-009) y con el campo `availableTickets` ya disponible en el tipo `Event`.

**Alternativas consideradas**: Ninguna relevante — la lógica está claramente definida en la spec.

---

### 5. Componentes shadcn/ui disponibles

**Pregunta**: ¿Están disponibles todos los componentes UI necesarios?

**Investigación**: Se verificó el directorio `frontend/components/ui/`. Los componentes disponibles incluyen: button.tsx, card.tsx, input.tsx, label.tsx, skeleton.tsx, dialog.tsx, badge.tsx, table.tsx, etc.

**Decisión**: Usar `Card`, `Button`, `Input`, `Label` de shadcn/ui y `toast` de sonner para feedback. También lucide-react para iconos (`Clock`, `CheckCircle2`, `XCircle`, `Mail`). Todos ya están en las dependencias del proyecto.

**Rationale**: Seguir los patrones existentes del frontend mantiene consistencia visual y reduce código nuevo.

---

### 6. Análisis de inconsistencias con TestCases.md

**Pregunta**: ¿Hay inconsistencias entre los test cases definidos en TestCases.md y la spec/plan?

**Investigación**: Revisión de TC-HU7-01 a TC-HU7-04:

- **TC-HU7-01**: "El comprador navega a la página del evento" → alineado con el plan. La condición es `availableTickets === 0 && fecha futura`. ✅ Consistente.
- **TC-HU7-02**: "El comprador solicita unirse a la lista de espera" → alineado con FR-004 y FR-005. ✅ Consistente.
- **TC-HU7-03**: "La aplicación no muestra la opción de lista de espera cuando el evento ya cerró" → alineado con FR-009. La técnica VL (valor límite) sobre "fecha del evento alcanzada exactamente" es un edge case: se usa `>` estricto (`startsAt > now`), por lo que si son exactamente iguales, se considera cerrada. ✅ Consistente.
- **TC-HU7-04**: "El comprador ve que su inscripción fue rechazada por duplicado" → alineado con FR-006; se maneja 409. ✅ Consistente.

**Decisión**: No hay inconsistencias. Todos los test cases mapean directamente a funcionalidades planificadas.

**Escenarios adicionales identificados** (no definidos en TestCases.md):
- Error de red al inscribirse → cubierto por FR-010 / US5. No tiene TC explícito pero es de menor prioridad.
- Doble clic en botón de envío → cubierto por FR-008. Prevención técnica, no requiere TC de aceptación.
