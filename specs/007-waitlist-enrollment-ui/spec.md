# Feature Specification: Waitlist Enrollment UI

**Feature Branch**: `007-waitlist-enrollment-ui`  
**Created**: 2026-04-08  
**Status**: Draft  
**Input**: User description: "Construye la experiencia frontend de inscripción para la feature de lista de espera. En la aplicación Next.js/React existente, cuando un comprador navega a la página de un evento y el evento no tiene entradas disponibles de inmediato, la aplicación muestra una opción de inscripción en lista de espera en lugar del botón de compra directa. El comprador ingresa su correo electrónico y envía el formulario. En caso de éxito, la aplicación confirma su inscripción y muestra la inscripción como activa. Si el comprador ya está inscrito (respuesta 409), la aplicación le informa sin crear un duplicado. Si la fecha del evento pasó y la lista de espera está cerrada (respuesta 422), la aplicación muestra que la lista de espera ya no acepta inscripciones. El formulario de inscripción solo es visible cuando: (1) el evento no tiene entradas disponibles y (2) la lista de espera sigue abierta (fecha del evento no alcanzada). Cuando hay entradas disponibles, se muestra el flujo de compra normal. La interacción con la API usa POST /api/waitlist/entries con { eventId, buyerEmail } del CRUD Service."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Inscripción exitosa en lista de espera (Priority: P1)

Un comprador navega a la página de un evento que no tiene entradas disponibles pero cuya fecha aún no ha pasado. En lugar del botón de compra habitual, ve un formulario de inscripción en lista de espera. El comprador ingresa su correo electrónico y envía el formulario. La aplicación confirma la inscripción y muestra el estado como activo.

**Why this priority**: Es el flujo principal de la feature. Sin inscripción exitosa, no hay lista de espera funcional. Entrega el valor central de capturar la demanda cuando no hay disponibilidad.

**Independent Test**: Se puede probar creando un evento sin entradas disponibles, navegando a su página como comprador, completando el formulario de inscripción y verificando que la confirmación se muestra correctamente.

**Acceptance Scenarios**:

1. **Given** un evento sin entradas disponibles y cuya fecha no ha pasado, **When** el comprador navega a la página del evento, **Then** se muestra el formulario de inscripción en lista de espera en lugar del botón de compra.
2. **Given** el formulario de inscripción visible, **When** el comprador ingresa un correo electrónico válido y envía el formulario, **Then** la aplicación muestra una confirmación de inscripción con estado activo.
3. **Given** el formulario de inscripción visible, **When** el comprador envía el formulario con un correo electrónico inválido o vacío, **Then** la aplicación muestra un mensaje de error de validación sin enviar la solicitud al servidor.

---

### User Story 2 - Inscripción duplicada rechazada (Priority: P2)

Un comprador que ya está inscrito en la lista de espera de un evento intenta inscribirse de nuevo con el mismo correo electrónico. La aplicación le informa que ya tiene una inscripción activa, sin crear un duplicado.

**Why this priority**: Prevenir confusión del usuario y mantener integridad de la lista de espera. Es el segundo escenario más probable tras la inscripción exitosa.

**Independent Test**: Se puede probar inscribiendo a un comprador, y luego intentando inscribirlo de nuevo con el mismo email en el mismo evento. Verificar que se muestra un mensaje informativo sin duplicar la inscripción.

**Acceptance Scenarios**:

1. **Given** un comprador que ya está inscrito en la lista de espera de un evento, **When** intenta inscribirse de nuevo con el mismo correo electrónico, **Then** la aplicación le informa que ya tiene una inscripción activa.
2. **Given** el escenario de duplicado, **When** la respuesta del servidor es 409, **Then** el mensaje mostrado es informativo (no de error agresivo) y no se crea una nueva inscripción.

---

### User Story 3 - Lista de espera cerrada por evento pasado (Priority: P3)

Un comprador navega a la página de un evento cuya fecha ya pasó y no tiene entradas disponibles. La aplicación no muestra el formulario de inscripción sino un mensaje indicando que la lista de espera ya no acepta inscripciones.

**Why this priority**: Importante para la experiencia del usuario, pero es un caso menos frecuente que los escenarios de inscripción activa. Previene intentos inútiles.

**Independent Test**: Se puede probar navegando a un evento cuya fecha ya pasó y sin entradas disponibles. Verificar que no se muestra el formulario y que el mensaje de lista cerrada es visible.

**Acceptance Scenarios**:

1. **Given** un evento sin entradas disponibles cuya fecha ya pasó, **When** el comprador navega a la página del evento, **Then** no se muestra el formulario de inscripción.
2. **Given** un evento sin entradas disponibles cuya fecha ya pasó, **When** el comprador navega a la página del evento, **Then** se muestra un mensaje indicando que la lista de espera ya no acepta inscripciones.

---

### User Story 4 - Flujo de compra normal cuando hay entradas disponibles (Priority: P1)

Un comprador navega a la página de un evento que tiene entradas disponibles. En este caso, el flujo de compra normal (existente) se muestra sin cambios. El formulario de inscripción en lista de espera no es visible.

**Why this priority**: Critico para no romper el flujo de compra existente. Es un requisito de compatibilidad que afecta directamente a la experiencia de los compradores con eventos disponibles.

**Independent Test**: Se puede probar navegando a un evento con entradas disponibles y verificando que el flujo de compra normal se muestra y funciona como antes.

**Acceptance Scenarios**:

1. **Given** un evento con entradas disponibles, **When** el comprador navega a la página del evento, **Then** se muestra el flujo de compra normal (botón de compra habitual).
2. **Given** un evento con entradas disponibles, **When** el comprador navega a la página del evento, **Then** el formulario de inscripción en lista de espera NO es visible.

---

### User Story 5 - Error de red o fallo del servidor al inscribirse (Priority: P2)

Un comprador intenta inscribirse en la lista de espera pero la solicitud falla por un error de red o un error inesperado del servidor. La aplicación muestra un mensaje de error genérico y permite al comprador reintentar.

**Why this priority**: Los fallos de red son inevitables. Sin manejo de errores, el usuario queda sin feedback y no sabe si su acción tuvo efecto.

**Independent Test**: Se puede probar simulando un fallo de red o error 500 del servidor y verificando que se muestra un mensaje de error y se permite el reintento.

**Acceptance Scenarios**:

1. **Given** el formulario de inscripción visible, **When** el comprador envía el formulario y ocurre un error de red, **Then** la aplicación muestra un mensaje de error amigable.
2. **Given** un error al inscribirse, **When** el comprador ve el mensaje de error, **Then** puede reintentar el envío del formulario sin recargar la página.

---

### Edge Cases

- ¿Qué sucede si la disponibilidad del evento cambia mientras el comprador está en la página? (e.g., se liberan entradas mientras ve el formulario de waitlist, o se agotan mientras ve el botón de compra). La interfaz debe reflejar el estado actual en su próximo ciclo de polling.
- ¿Qué pasa si el comprador ingresa un correo electrónico con formato válido pero inexistente? Se acepta la inscripción (la validación existente del backend es solo de formato).
- ¿Qué ocurre si el servidor responde con un código de error no documentado (e.g., 500, 503)? Se muestra un error genérico con opción de reintento.
- ¿Qué sucede si el comprador envía el formulario dos veces rápidamente (doble clic)? Se debe prevenir el envío duplicado deshabilitando el botón durante el procesamiento.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: La aplicación DEBE mostrar el formulario de inscripción en lista de espera cuando un evento no tiene entradas disponibles y la fecha del evento no ha pasado.
- **FR-002**: La aplicación DEBE mostrar el flujo de compra normal cuando un evento tiene entradas disponibles, ocultando el formulario de lista de espera.
- **FR-003**: El formulario de inscripción DEBE requerir un campo de correo electrónico con validación de formato antes del envío.
- **FR-004**: La aplicación DEBE enviar la inscripción al endpoint POST /api/waitlist/entries con los campos eventId y buyerEmail.
- **FR-005**: Tras una inscripción exitosa (201), la aplicación DEBE mostrar una confirmación con el estado de la inscripción como activa.
- **FR-006**: Cuando el servidor responde con 409 (duplicado), la aplicación DEBE informar al comprador que ya tiene una inscripción activa, con un mensaje informativo y no destructivo.
- **FR-007**: Cuando el servidor responde con 422 (lista cerrada), la aplicación DEBE mostrar que la lista de espera ya no acepta inscripciones para ese evento.
- **FR-008**: La aplicación DEBE prevenir envíos duplicados del formulario deshabilitando el botón de envío mientras la solicitud está en curso.
- **FR-009**: La aplicación DEBE mostrar un mensaje indicando que la lista de espera está cerrada cuando el evento no tiene entradas disponibles y su fecha ya ha pasado, sin mostrar el formulario.
- **FR-010**: Ante errores de red o respuestas inesperadas del servidor, la aplicación DEBE mostrar un mensaje de error genérico y permitir al comprador reintentar.
- **FR-011**: La validación del correo electrónico DEBE ocurrir en el cliente antes de enviar la solicitud al servidor.

### Key Entities

- **Inscripción en lista de espera (Waitlist Entry)**: Registro de un comprador inscrito en la lista de espera de un evento. Atributos principales: identificador, evento asociado, correo del comprador, estado de la inscripción (activa), fecha de inscripción.
- **Evento (Event)**: Evento con venta de entradas. Atributos relevantes para esta feature: identificador, nombre, fecha/hora de inicio, cantidad de entradas disponibles.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El comprador puede completar la inscripción en lista de espera en menos de 30 segundos desde que ve el formulario.
- **SC-002**: El 100% de los intentos de inscripción duplicada muestra un mensaje informativo sin crear registros duplicados.
- **SC-003**: El formulario de lista de espera solo es visible en páginas de eventos sin entradas disponibles y con fecha futura; nunca se muestra junto al flujo de compra normal.
- **SC-004**: El flujo de compra existente funciona sin alteraciones cuando hay entradas disponibles.
- **SC-005**: El 100% de los errores de red o respuestas inesperadas muestra un mensaje de error amigable con opción de reintento.
- **SC-006**: El comprador no puede enviar el formulario más de una vez simultáneamente (prevención de doble envío).

## Assumptions

- El CRUD Service ya expone el endpoint POST /api/waitlist/entries con las respuestas 201, 409, 404 y 422 documentadas; esta feature no modifica el backend.
- El campo `availableTickets` del modelo de evento existente es la fuente de verdad para determinar si hay entradas disponibles (0 = sin entradas).
- El campo `startsAt` del evento se usa para determinar si la fecha del evento ya pasó (comparación con la fecha/hora actual del navegador del comprador).
- La aplicación ya tiene un mecanismo de polling que refresca automáticamente los datos del evento, lo que permite al UI reaccionar a cambios de disponibilidad sin intervención manual.
- No se requiere autenticación del comprador; el correo electrónico es el único identificador.
- El formulario de inscripción se integra en el flujo de vista de comprador existente (página de compra de evento), no como una página separada.
- La responsividad (mobile/desktop) sigue los patrones ya existentes en la aplicación; no se definen requisitos de diseño adicionales.
