# TEST_PLAN.md — Plan Maestro de Pruebas para TicketRush (Backend)

## 1. Objetivo

Este documento presenta el **plan maestro de pruebas** para TicketRush enfocado en backend.

Su objetivo es definir de forma clara:

1. el alcance del testing del backend del proyecto,
2. la estrategia multinivel,
3. los tipos de pruebas aplicables,
4. los riesgos principales del negocio,
5. y la referencia a los casos de prueba detallados del sistema.

> Decisión explícita del equipo: **no incluir cronograma** en este documento. La razón es que este artefacto busca servir como fuente técnica viva del testing y no como plan temporal de sprint.

---

## 2. Descripción del sistema bajo prueba

TicketRush es una solución de ticketing en la que el backend debe asegurar que el proceso de compra sea consistente desde el punto de vista del negocio.

En términos funcionales, el sistema debe permitir:

- administrar eventos,
- crear y consultar tickets,
- reservar un ticket sin duplicarlo,
- confirmar la compra cuando el pago es válido,
- liberar el ticket cuando la compra no se concreta,
- y permitir conocer con claridad el estado final del ticket.

### Alcance operativo de este documento

Aunque TicketRush incluye frontend, **este plan se enfoca únicamente en trabajo de backend**. Por tanto:

- sí cubre reglas de negocio, contratos funcionales y pruebas automatizadas del lado servidor,
- no cubre implementación UI, componentes visuales, hooks del cliente ni pruebas de interfaz de usuario.

### Comportamientos canónicos confirmados

- Una solicitud aceptada no equivale por sí sola a una compra completada.
- El sistema debe confirmar el estado final real del ticket antes de dar el flujo por exitoso.
- Un ticket que no completa la compra debe volver a estar disponible.
- El mecanismo oficial que materializa estos comportamientos debe respetarse de forma consistente en todo el backend.

---

## 3. Alcance de las pruebas

### 3.1 Dentro del alcance

El alcance incluye:

1. Gestión backend de eventos.
2. Gestión backend de tickets.
3. Reserva asíncrona.
4. Pago asíncrono.
5. Confirmación clara del estado final del ticket.
6. Prevención de duplicidades y dobles asignaciones.
7. Persistencia e historial de estado.
8. Expiración/liberación de tickets.
9. Coherencia entre solicitud, procesamiento y resultado final.
10. Verificación multinivel dentro del pipeline.

### 3.1.1 Historias funcionales para el plan de pruebas

Para efectos de organización del testing, este documento agrupa el alcance en **historias funcionales reconstruidas**. No se presentan como backlog histórico oficial, sino como base operativa de trazabilidad para el proyecto a partir de este punto.

| ID | Historia funcional reconstruida | Alcance backend |
|---|---|---|
| HU-R01 | Administración de eventos | crear, consultar, actualizar y eliminar eventos |
| HU-R02 | Disponibilidad de tickets por evento | crear tickets, consultarlos y mantener su estado inicial correcto |
| HU-R03 | Reserva correcta de un ticket | aceptar la solicitud y evitar doble asignación |
| HU-R04 | Confirmación o liberación por pago | confirmar la compra cuando el pago es válido o liberar el ticket cuando no lo es |
| HU-R05 | Consulta clara del estado final | permitir conocer sin ambigüedad en qué estado terminó el ticket |
| HU-R06 | Liberación automática de tickets no concretados | devolver el ticket a disponibilidad cuando la compra no se completa |

Estas historias pueden reutilizarse en adelante como referencia funcional para nuevas HUs y para la trazabilidad del plan.

### 3.1.2 Reglas de negocio del sistema

Conviene distinguir entre **reglas funcionales de negocio** y **restricciones técnicas relevantes para pruebas**. Un Product Owner normalmente expresaría las primeras; las segundas aparecen porque este plan es de backend y debe justificar cómo se verifica el comportamiento real.

#### Reglas funcionales de negocio

1. Un ticket disponible solo puede quedar reservado una vez.
2. Una reserva aceptada no debe considerarse completada hasta que el sistema confirme el estado final correspondiente.
3. Un ticket reservado puede pasar a pagado solo si la validación del pago resulta exitosa dentro de la ventana permitida.
4. Un pago rechazado o fuera de tiempo debe liberar el ticket para que vuelva a estar disponible.
5. Un mismo evento de pago no debe generar efectos duplicados sobre el ticket ni sobre su historial.
6. Un ticket expirado debe quedar nuevamente disponible y conservar trazabilidad del cambio de estado.
7. El cliente o consumidor debe poder conocer el estado final real del ticket sin ambigüedad.

#### Restricciones técnicas y arquitectónicas relevantes para pruebas

1. Una respuesta `202 Accepted` representa aceptación del comando, no confirmación del resultado final del negocio.
2. La confirmación oficial del estado final debe ser observable por el mecanismo backend definido para el sistema.
3. La validación temporal del pago se implementa mediante una ventana de vigencia (`TTL`), por lo que debe probarse explícitamente.
4. La expiración oficial sigue la ruta canónica basada en mensajería del sistema.
5. El contrato de notificación de cambios de estado debe mantenerse estable para los consumidores backend.

Estas reglas y restricciones se validan luego en los casos `TC-001` a `TC-009` y `TC-012`, según el riesgo y el nivel de prueba definido en este plan.

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

## 4. Riesgos del sistema y priorización

| ID | Riesgo | Probabilidad | Impacto | Nivel |
|---|---|---:|---:|---|
| R-001 | Un mismo ticket podría terminar asignado más de una vez | 3 | 3 | Crítico |
| R-002 | Una solicitud aceptada podría confundirse con una compra realmente completada | 3 | 3 | Crítico |
| R-003 | Un pago podría generar efectos duplicados sobre la compra | 3 | 3 | Crítico |
| R-004 | El estado final del ticket podría no quedar claro para el consumidor | 2 | 3 | Alto |
| R-005 | El flujo oficial de actualización de estado podría quedar inconsistente | 2 | 3 | Alto |
| R-006 | Un ticket no concretado podría no volver a estar disponible | 2 | 3 | Alto |
| R-007 | Un cambio de estado podría afectar negativamente a otros consumidores del sistema | 2 | 3 | Alto |
| R-008 | CRUD de eventos/tickets presenta regresiones funcionales | 2 | 2 | Medio |

### Orden de ejecución recomendado por riesgo

1. Reserva y concurrencia.
2. Pago, ventana válida e idempotencia.
3. Estado final observable.
4. Expiración automática.
5. CRUD de eventos y tickets.

---

## 5. Estrategia de ejecución

### 5.1 Smoke

Primera barrera para confirmar que:

- la aplicación compila,
- los servicios clave responden,
- las suites rápidas están sanas,
- y no hay ruptura obvia en contratos críticos.

### 5.2 Componente

Se ejecutan para validar integración local entre piezas de un mismo servicio sin necesidad de levantar todo el ecosistema.

### 5.3 Integración

Se ejecutan para validar:

- comportamiento consistente entre solicitud y resultado,
- contratos funcionales expuestos,
- persistencia e historial,
- y comunicación real o semirreal entre componentes backend.

### 5.4 E2E

Se ejecutan sobre Docker Compose para validar:

- reserva completa,
- pago completo,
- expiración/liberación,
- y consistencia observable del sistema.

### 5.5 Regresión

Toda HU nueva debe activar regresión por flujo impactado. No se asume aislamiento funcional.

---

## 6. Estrategia de datos de prueba

- Datos de eventos y tickets deben ser controlados y reproducibles.
- Los IDs usados en pruebas deben poder correlacionarse con el estado final del ticket.
- Los datos de pago deben permitir distinguir:
  - aprobación,
  - rechazo,
  - pago fuera de tiempo,
  - y reentrega duplicada.
- Se prohíbe usar datos de producción.
- Para flujos E2E se prefieren scripts reproducibles y datos efímeros generados en cada corrida.

---

## 7. Ambientes y requerimientos

### Ambientes

| Ambiente | Uso |
|---|---|
| Local unit | TDD y validación rápida |
| Local integration | validación de reglas y contratos funcionales |
| Docker Compose | E2E del proceso completo |
| CI PR | smoke + unit + integración rápida |
| CI post-merge/nightly | integración reforzada + E2E |

### Requerimientos

- Acceso al repositorio.
- Docker Compose funcional.
- Servicios de soporte disponibles en entorno de prueba.
- Evidencia de suites automatizadas actuales.
- Acceso a logs y resultados de ejecución del pipeline.

---

## 8. Roles y responsabilidades

| Rol | Responsabilidades |
|---|---|
| Dev A | Dueño principal de pruebas de reserva, liberación automática, concurrencia y flujos E2E del ticket. |
| Dev B | Dueño principal de pruebas de eventos, tickets, confirmación de estado final, pagos y documentación de casos. |

> Ambos comparten responsabilidad sobre regresión backend transversal, validación del pipeline y revisión cruzada del plan.

---

## 9. Suites de prueba planificadas

| Suite | Nivel | Tipo principal | Objetivo |
|---|---|---|---|
| Suite de reglas de reserva | Unit | Caja Blanca | Validar que no exista doble asignación ni cambios inválidos |
| Suite de reglas de pago | Unit | Caja Blanca | Validar pago válido, pago fuera de tiempo y prevención de duplicados |
| Suite de consistencia interna | Component | Caja Blanca | Validar que los cambios de estado se propaguen de forma coherente |
| Suite de confirmación de estado | Integration | Caja Negra / contrato | Garantizar que el resultado final pueda observarse con claridad |
| Suite de APIs de negocio | Integration | Caja Negra | Validar operaciones de eventos, tickets, reserva y pago |
| Suite E2E del proceso de compra | E2E | Caja Negra | Validar reserva, pago o liberación en entorno integrado |

---

## 10. Trazabilidad de casos por historia funcional

| Historia | Casos asociados |
|---|---|
| HU-R01 Administración de eventos | TC-010 |
| HU-R02 Disponibilidad de tickets por evento | TC-011 |
| HU-R03 Reserva correcta de un ticket | TC-001, TC-002, TC-008 |
| HU-R04 Confirmación o liberación por pago | TC-003, TC-004, TC-005, TC-006 |
| HU-R05 Consulta clara del estado final | TC-001, TC-007, TC-008 |
| HU-R06 Liberación automática de tickets no concretados | TC-004, TC-005, TC-009, TC-012 |

---

## 11. Recomendaciones finales

1. Este documento debe usarse como fuente principal para defensa funcional y control del enfoque de pruebas.
2. Si entra una HU nueva, debe actualizarse este plan en el mismo cambio si altera flujos backend existentes, usando estas historias funcionales como base de trazabilidad mientras no exista un backlog histórico formal consolidado.
3. El siguiente paso natural es materializar esta estrategia en el pipeline con etapas separadas de validación rápida, integración, build y flujo completo cuando aplique.

---

