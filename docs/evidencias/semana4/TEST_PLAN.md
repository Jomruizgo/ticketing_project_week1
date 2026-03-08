# TEST_PLAN.md — Plan Maestro de Pruebas para TicketRush (Backend)

## 1. Objetivo

Este documento presenta el **plan maestro de pruebas** para TicketRush enfocado en backend.

Su objetivo es definir de forma clara:

1. el alcance del testing del backend del proyecto,
2. la estrategia multinivel,
3. los tipos de pruebas aplicables,
4. la justificación teórica de las decisiones de calidad,
5. y los **test cases** del sistema en el mismo documento.

> Decisión explícita del equipo: **no incluir cronograma** en este documento. La razón es que este artefacto busca servir como fuente técnica viva del testing y no como plan temporal de sprint.

---

## 2. Descripción del sistema bajo prueba

TicketRush es un sistema distribuido de ticketing compuesto, del lado backend, por:

- `crud_service` para lectura/administración,
- `producer` para recepción de comandos asíncronos,
- `ReservationService` para reservas,
- `paymentService` para pagos,
- PostgreSQL para persistencia,
- RabbitMQ para mensajería,
- SSE como contrato backend de notificación de cambios de estado.

### Alcance operativo de este documento

Aunque TicketRush incluye frontend, **este plan se enfoca únicamente en trabajo de backend**. Por tanto:

- sí cubre APIs, workers, mensajería, persistencia, SSE como contrato backend y pruebas automatizadas del lado servidor,
- no cubre implementación UI, componentes visuales, hooks del cliente ni pruebas de interfaz de usuario.

### Comportamientos canónicos confirmados

- La **reserva** se acepta por HTTP con `202 Accepted` y se confirma de forma asíncrona.
- La **confirmación oficial del estado final** debe exponerse mediante **SSE** como contrato backend canónico.
- La **expiración oficial** del ticket se considera basada en **RabbitMQ**.
- Cualquier polling residual o job alterno debe tratarse como legado, residuo técnico o desalineación documental.

---

## 3. Alcance de las pruebas

### 3.1 Dentro del alcance

El alcance incluye:

1. Gestión backend de eventos.
2. Gestión backend de tickets.
3. Reserva asíncrona.
4. Pago asíncrono.
5. Contrato backend de notificación por SSE.
6. Idempotencia y control de concurrencia.
7. Persistencia e historial de estado.
8. Expiración/liberación de tickets.
9. Contratos entre APIs HTTP, SSE y RabbitMQ.
10. Verificación multinivel dentro del pipeline.

### 3.1.1 Historias funcionales reconstruidas para el plan de pruebas

Para efectos de organización del testing, este documento agrupa el alcance en **historias funcionales reconstruidas**. No se presentan como backlog histórico oficial, sino como base operativa de trazabilidad para el proyecto a partir de este punto.

| ID | Historia funcional reconstruida | Alcance backend |
|---|---|---|
| HU-R01 | Gestión de eventos | CRUD backend de eventos |
| HU-R02 | Gestión de tickets | creación, consulta y estado inicial de tickets |
| HU-R03 | Reserva asíncrona de tickets | aceptación de comando, concurrencia y transición a `reserved` |
| HU-R04 | Pago asíncrono de tickets | solicitud, aprobación/rechazo, TTL, idempotencia |
| HU-R05 | Notificación de estado por SSE | exposición backend del estado final observable |
| HU-R06 | Expiración y liberación de tickets | expiración canónica por RabbitMQ y liberación segura |

Estas historias pueden reutilizarse en adelante como referencia funcional para nuevas HUs y para la trazabilidad del plan.

### 3.2 Fuera del alcance

Este plan no cubre por ahora:

- implementación o refactor del frontend,
- componentes visuales, hooks y navegación del cliente,
- pruebas E2E de interfaz gráfica en navegador,
- pentesting formal,
- pruebas de carga masiva sostenida,
- compliance regulatorio,
- trazabilidad perfecta a todas las HUs históricas originales,
- validación de cronogramas de entrega.

---

## 4. Estrategia multinivel de pruebas

La estrategia se define según el riesgo y la arquitectura real de TicketRush.

### 4.1 Pruebas de Caja Blanca

Se usan cuando se necesita validar decisiones internas de diseño o reglas de negocio, por ejemplo:

- optimistic locking en reserva,
- guards de estado,
- validación de TTL,
- idempotencia en workers,
- parsing/formatting de mensajes,
- rutas internas del pipeline SSE.

### 4.2 Pruebas de Caja Negra

Se usan cuando interesa validar el sistema desde el comportamiento observable, sin depender de detalles internos, por ejemplo:

- reservar ticket desde el endpoint expuesto,
- procesar pago y observar el resultado final,
- consumir el stream SSE,
- ejecutar el flujo distribuido con Docker Compose.

### 4.3 Niveles de prueba

#### Unit
- lógica de negocio aislada,
- validaciones de dominio,
- decisiones internas de handlers y services.

#### Component
- interacción real entre piezas del mismo servicio sin infraestructura externa completa,
- por ejemplo pipeline consumer → hub.

#### Integration
- validación de contratos HTTP/SSE,
- integración con persistencia o componentes de infraestructura,
- pruebas entre módulos backend con comunicación real o semirreal.

#### E2E
- validación del flujo completo del sistema distribuido,
- incluyendo Docker Compose, RabbitMQ y PostgreSQL.

---

## 5. Justificación teórica — Los 7 Principios de las Pruebas aplicados a TicketRush

### Principio 1 — Las pruebas muestran la presencia de defectos, no su ausencia

En TicketRush no basta con que un flujo “funcione en local”. Las pruebas solo reducen incertidumbre. Por eso se cubren estados críticos como:

- doble reserva,
- pagos duplicados,
- TTL excedido,
- ruptura de SSE,
- y divergencia entre evento aceptado y estado final.

### Principio 2 — Las pruebas exhaustivas son imposibles

No se prueban todas las combinaciones posibles. Se priorizan flujos críticos y riesgos altos:

- reserva,
- pago,
- expiración,
- notificación SSE,
- y contratos inter-servicio.

### Principio 3 — Las pruebas tempranas ahorran tiempo y dinero

La lógica crítica debe probarse desde capas bajas:

- unit para guards y reglas,
- component/integration para contratos,
- E2E solo para validación final del flujo distribuido.

### Principio 4 — Los defectos tienden a agruparse

Los riesgos se concentran en:

- cambios de estado del ticket,
- workers y mensajería,
- contratos SSE,
- y sincronización entre aceptación de comando y resultado final.

### Principio 5 — Paradoja del pesticida

Si siempre se ejecutan las mismas pruebas, dejan de encontrar defectos nuevos. Por eso el plan exige:

- revisar regresión por flujo impactado,
- ampliar casos cuando entra una HU nueva,
- y revisar residuos técnicos o documentación no alineada.

### Principio 6 — Las pruebas dependen del contexto

Este principio es central aquí. TicketRush no es un CRUD simple. Es un sistema distribuido, eventual-consistent y orientado a eventos. Por eso el plan prioriza:

- idempotencia,
- concurrencia,
- contratos SSE,
- y verificación de integración entre servicios.

### Principio 7 — La falacia de la ausencia de errores

No sirve tener muchas pruebas si no cubren los riesgos reales. Un backend puede “pasar tests” y seguir fallando si:

- una API devuelve `202` pero no materializa correctamente el estado final,
- el estado no se expone correctamente por SSE,
- o se rompe la topología de RabbitMQ.

---

## 6. Riesgos del sistema y priorización

| ID | Riesgo | Probabilidad | Impacto | Nivel |
|---|---|---:|---:|---|
| R-001 | Doble reserva por pérdida del control de concurrencia | 3 | 3 | Crítico |
| R-002 | Un consumidor interpreta `202 Accepted` como éxito final sin confirmar estado persistido/SSE | 3 | 3 | Crítico |
| R-003 | Pago duplicado o reprocesado sin idempotencia | 3 | 3 | Crítico |
| R-004 | Ruptura del contrato SSE | 2 | 3 | Alto |
| R-005 | Topología RabbitMQ y consumidores divergen | 2 | 3 | Alto |
| R-006 | Expiración RabbitMQ no se refleja correctamente en el flujo | 2 | 3 | Alto |
| R-007 | Cambio de estados rompe servicios consumidores | 2 | 3 | Alto |
| R-008 | CRUD de eventos/tickets presenta regresiones funcionales | 2 | 2 | Medio |

### Orden de ejecución recomendado por riesgo

1. Reserva y concurrencia.
2. Pago, TTL e idempotencia.
3. SSE y estado final observable.
4. Expiración automática.
5. CRUD de eventos y tickets.

---

## 7. Estrategia de ejecución

### 7.1 Smoke

Primera barrera para confirmar que:

- la aplicación compila,
- los servicios clave responden,
- las suites rápidas están sanas,
- y no hay ruptura obvia en contratos críticos.

### 7.2 Componente

Se ejecutan para validar integración local entre piezas de un mismo servicio sin necesidad de levantar todo el ecosistema.

### 7.3 Integración

Se ejecutan para validar:

- contratos HTTP,
- contratos SSE,
- integración consumer → formatter/parser → hub,
- y comunicación real o semirreal entre componentes backend.

### 7.4 E2E

Se ejecutan sobre Docker Compose para validar:

- reserva completa,
- pago completo,
- expiración/liberación,
- y consistencia observable del sistema.

### 7.5 Regresión

Toda HU nueva debe activar regresión por flujo impactado. No se asume aislamiento funcional.

---

## 8. Estrategia de datos de prueba

- Datos de eventos y tickets deben ser controlados y reproducibles.
- Los IDs usados en pruebas deben poder correlacionarse con el estado final en BD o SSE.
- Los datos de pago deben permitir distinguir:
  - aprobación,
  - rechazo,
  - TTL excedido,
  - y reentrega duplicada.
- Se prohíbe usar datos de producción.
- Para flujos E2E se prefieren scripts reproducibles y datos efímeros generados en cada corrida.

---

## 9. Ambientes y requerimientos

### Ambientes

| Ambiente | Uso |
|---|---|
| Local unit | TDD y validación rápida |
| Local integration | contratos y componentes del servicio |
| Docker Compose | E2E del sistema distribuido |
| CI PR | smoke + unit + integración rápida |
| CI post-merge/nightly | integración reforzada + E2E |

### Requerimientos

- Acceso al repositorio.
- Docker Compose funcional.
- RabbitMQ y PostgreSQL disponibles en entorno de prueba.
- Evidencia de suites automatizadas actuales.
- Acceso a logs y resultados de ejecución del pipeline.

---

## 10. Roles y responsabilidades

| Rol | Responsabilidades |
|---|---|
| Dev A | Dueño principal de pruebas de backend distribuido: reserva, expiración, concurrencia, contratos RabbitMQ, scripts E2E de flujos de ticket. |
| Dev B | Dueño principal de pruebas backend de APIs y contratos: CRUD de eventos/tickets, SSE, pagos, documentación de test cases y quality gates de PR. |

> Ambos comparten responsabilidad sobre regresión backend transversal, validación del pipeline y revisión cruzada del plan.

---

## 11. Gestión de defectos y criterios de salida

### Gestión de defectos

Todo defecto identificado debe registrarse con:

- flujo afectado,
- severidad,
- pasos de reproducción,
- evidencia,
- resultado esperado vs resultado actual,
- suite donde fue detectado.

### Criterios de entrada

- baseline aceptado,
- flujo impactado identificado,
- datos y ambiente disponibles,
- suites seleccionadas por riesgo.

### Criterios de salida

- smoke verde,
- sin ruptura de contratos críticos,
- regresión ejecutada para flujos impactados,
- evidencias disponibles para integración o E2E cuando aplique.

---

## 12. Suites de prueba planificadas

| Suite | Nivel | Tipo principal | Objetivo |
|---|---|---|---|
| Suite de command handlers | Unit | Caja Blanca | Validar reglas de negocio y guards |
| Suite de workers y validación de pagos | Unit | Caja Blanca | Validar TTL, idempotencia, transiciones |
| Suite pipeline SSE | Component | Caja Blanca | Validar consumer → hub → formatter/parser |
| Suite de contratos SSE | Integration | Caja Negra / contrato | Garantizar formato observable del backend |
| Suite HTTP APIs | Integration | Caja Negra | Validar endpoints y respuestas |
| Suite E2E Compose | E2E | Caja Negra | Validar flujos completos distribuidos |

---

## 13. Trazabilidad de casos por historia funcional

| Historia | Casos asociados |
|---|---|
| HU-R01 Gestión de eventos | TC-010 |
| HU-R02 Gestión de tickets | TC-011 |
| HU-R03 Reserva asíncrona | TC-001, TC-002, TC-008 |
| HU-R04 Pago asíncrono | TC-003, TC-004, TC-005, TC-006 |
| HU-R05 Notificación SSE | TC-001, TC-007, TC-008 |
| HU-R06 Expiración/liberación | TC-004, TC-005, TC-009, TC-012 |

---

## 14. Test Cases

### HU-R01 — Gestión de eventos

#### TC-010 — CRUD de eventos
- **Flujo:** Administración
- **Nivel:** Integration
- **Tipo:** Caja Negra
- **Prioridad:** Alta
- **Precondiciones:** API de CRUD disponible.
- **Pasos:**
  1. Crear evento.
  2. Consultarlo.
  3. Actualizarlo.
  4. Eliminarlo.
- **Resultado esperado:** respuestas correctas y estado consistente.
- **Automatización:** Sí.

### HU-R02 — Gestión de tickets

#### TC-011 — Creación y consulta de tickets por evento
- **Flujo:** Inventario
- **Nivel:** Integration
- **Tipo:** Caja Negra
- **Prioridad:** Alta
- **Precondiciones:** evento existente.
- **Pasos:**
  1. Crear tickets en lote.
  2. Consultar tickets del evento.
- **Resultado esperado:** cantidad y estados iniciales correctos.
- **Automatización:** Sí.

### HU-R03 — Reserva asíncrona de tickets

#### TC-001 — Reserva aceptada y confirmada por SSE
- **Flujo:** Reserva de ticket
- **Nivel:** E2E
- **Tipo:** Caja Negra
- **Prioridad:** Crítica
- **Precondiciones:** ticket disponible; servicios levantados; SSE operativo.
- **Pasos:**
  1. Consultar ticket disponible.
  2. Enviar `POST /api/tickets/reserve`.
  3. Verificar respuesta `202 Accepted`.
  4. Escuchar SSE del ticket desde el endpoint backend.
- **Resultado esperado:** el cliente de prueba recibe cambio a `reserved` por SSE; no se toma el `202` como confirmación final.
- **Automatización:** Sí, objetivo de pipeline E2E.

#### TC-002 — Reserva rechazada por concurrencia
- **Flujo:** Reserva concurrente
- **Nivel:** Unit / Integration
- **Tipo:** Caja Blanca
- **Prioridad:** Crítica
- **Precondiciones:** ticket disponible; dos intentos sobre mismo ticket.
- **Pasos:**
  1. Ejecutar reservas concurrentes sobre el mismo ticket.
  2. Observar resultado del repositorio/handler.
- **Resultado esperado:** solo una reserva se confirma; la otra falla sin doble asignación.
- **Automatización:** Sí.

#### TC-008 — API asíncrona no se considera exitosa hasta confirmar estado final
- **Flujo:** Contrato asíncrono backend
- **Nivel:** Integration
- **Tipo:** Caja Negra
- **Prioridad:** Alta
- **Precondiciones:** endpoint asíncrono disponible; mecanismo de consulta o SSE operativo.
- **Pasos:**
  1. Ejecutar reserva o pago.
  2. Verificar respuesta `202 Accepted`.
  3. Confirmar después el estado final por SSE o persistencia.
- **Resultado esperado:** el contrato backend distingue aceptación de comando vs resultado final de negocio.
- **Automatización:** Sí.

### HU-R04 — Pago asíncrono de tickets

#### TC-003 — Pago aprobado dentro de TTL
- **Flujo:** Pago exitoso
- **Nivel:** Integration / E2E
- **Tipo:** Caja Negra
- **Prioridad:** Crítica
- **Precondiciones:** ticket reservado dentro del tiempo válido.
- **Pasos:**
  1. Solicitar pago.
  2. Procesar evento aprobado.
  3. Esperar confirmación por SSE o consulta de estado.
- **Resultado esperado:** ticket en `paid`, payment en `approved`, historial registrado.
- **Automatización:** Sí.

#### TC-004 — Pago rechazado libera ticket
- **Flujo:** Pago rechazado
- **Nivel:** Integration / E2E
- **Tipo:** Caja Negra
- **Prioridad:** Crítica
- **Precondiciones:** ticket reservado.
- **Pasos:**
  1. Solicitar pago.
  2. Procesar evento rechazado.
  3. Verificar estado final.
- **Resultado esperado:** ticket en `released`; el contrato backend expone notificación coherente.
- **Automatización:** Sí.

#### TC-005 — Pago tardío excede TTL
- **Flujo:** TTL
- **Nivel:** Unit / Integration
- **Tipo:** Caja Blanca
- **Prioridad:** Crítica
- **Precondiciones:** ticket reservado fuera del tiempo permitido.
- **Pasos:**
  1. Simular aprobación tardía.
  2. Ejecutar validación del worker.
- **Resultado esperado:** resultado de negocio fallido; transición a `released`; no queda ticket en `paid`.
- **Automatización:** Sí.

#### TC-006 — Reentrega duplicada de evento aprobado
- **Flujo:** Idempotencia
- **Nivel:** Unit
- **Tipo:** Caja Blanca
- **Prioridad:** Crítica
- **Precondiciones:** evento aprobado ya procesado una vez.
- **Pasos:**
  1. Reprocesar el mismo evento.
- **Resultado esperado:** el sistema no duplica efectos ni altera indebidamente estado/historial.
- **Automatización:** Sí.

### HU-R05 — Notificación de estado por SSE

#### TC-007 — Contrato SSE del ticket
- **Flujo:** Notificación SSE
- **Nivel:** Integration
- **Tipo:** Caja Negra / contrato
- **Prioridad:** Crítica
- **Precondiciones:** stream activo por ticket.
- **Pasos:**
  1. Provocar cambio de estado.
  2. Capturar payload SSE.
- **Resultado esperado:** payload con campos y tipos esperados para el contrato backend.
- **Automatización:** Sí.

### HU-R06 — Expiración y liberación de tickets

#### TC-009 — Expiración automática por RabbitMQ
- **Flujo:** Expiración
- **Nivel:** E2E / Integration
- **Tipo:** Caja Negra
- **Prioridad:** Crítica
- **Precondiciones:** ticket reservado con expiración configurada.
- **Pasos:**
  1. Publicar o esperar evento de expiración canónico.
  2. Observar cambio de estado.
- **Resultado esperado:** ticket liberado mediante la ruta oficial basada en RabbitMQ.
- **Automatización:** Sí.

#### TC-012 — Historial de cambios de estado
- **Flujo:** Auditabilidad técnica
- **Nivel:** Integration
- **Tipo:** Caja Blanca
- **Prioridad:** Media-Alta
- **Precondiciones:** ejecutar transición de estado.
- **Pasos:**
  1. Forzar transición `reserved → paid` o `reserved → released`.
  2. Consultar persistencia de historial.
- **Resultado esperado:** registro correcto en `ticket_history` con razón y timestamps coherentes.
- **Automatización:** Sí.

---

## 15. Recomendaciones finales

1. Este documento debe usarse como fuente principal para defensa técnica y auditoría del ecosistema de testing.
2. Si entra una HU nueva, debe actualizarse este plan en el mismo cambio si altera flujos backend existentes, usando estas historias funcionales como base de trazabilidad mientras no exista un backlog histórico formal consolidado.
3. El siguiente paso natural es materializar esta estrategia en GitHub Actions con jobs separados de:
   - componente,
   - integración,
   - build,
   - análisis de imagen,
   - y E2E cuando aplique.

---

## 16. Juicio final

El valor de este plan no está en listar pruebas de forma decorativa, sino en demostrar que TicketRush requiere una estrategia **dependiente del contexto**, multinivel, automatizable y alineada con riesgos reales de arquitectura distribuida.
