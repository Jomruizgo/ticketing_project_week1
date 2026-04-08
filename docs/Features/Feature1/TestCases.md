Casos de Prueba — Feature1: Lista de Espera

Los casos de prueba están organizados por historia de usuario. Cada caso es trazable a un escenario Gherkin de [Planning2.md](Planning2.md).

La estrategia de pruebas, los riesgos priorizados y las suites planificadas que dan contexto a estos casos están documentados en [TestPlan.md](TestPlan.md).

Niveles de prueba: U = unitaria, I = integración, A = aceptación.

Técnicas de diseño de prueba:
- PE — Partición de equivalencias: agrupa entradas con el mismo comportamiento esperado; solo se prueba un representante de cada clase.
- VL — Análisis de valores límite: complementa PE probando los extremos de cada partición (justo dentro, justo fuera).
- TE — Prueba de transición de estados: verifica que las transiciones entre estados son correctas y que las transiciones inválidas son rechazadas.
- TD — Tabla de decisiones: combina múltiples condiciones que se evalúan juntas; útil cuando el resultado depende de combinaciones de entradas.
- SE — Suposición de errores: diseñada a partir de la experiencia y el conocimiento de fallos comunes; apunta a caminos de error específicos que las otras técnicas no cubren.
- CP — Prueba de camino (cobertura de flujo): asegura que cada camino lógico relevante del caso de uso se ejecuta al menos una vez.

---

## HU1 — Inscripción en lista de espera

### TC-HU1-01 — Inscripción exitosa cuando no hay disponibilidad inmediata
Tipo: A
Técnica: PE — clase válida: comprador sin inscripción activa + evento sin disponibilidad + lista vigente.
Gherkin: "Inscripción exitosa cuando no hay disponibilidad inmediata"

Precondiciones:
- Existe un evento con todas sus entradas en estado no disponible.
- El comprador no tiene ninguna inscripción activa para ese evento.
- La fecha del evento no ha sido alcanzada.

Pasos:
1. El comprador envía una solicitud de inscripción para el evento indicado.

Resultado esperado:
- El sistema crea una inscripción con estado activo.
- La respuesta confirma que el interés del comprador fue registrado.
- La inscripción es consultable en el sistema.

---

### TC-HU1-02 — Rechazo de inscripción duplicada
Tipo: A
Técnica: PE — clase inválida: comprador con inscripción activa existente para el mismo evento.
Gherkin: "Prevención de inscripción duplicada"

Precondiciones:
- Existe una inscripción activa del comprador para el evento indicado.

Pasos:
1. El mismo comprador envía una segunda solicitud de inscripción para el mismo evento.

Resultado esperado:
- El sistema rechaza la solicitud.
- La respuesta informa que ya existe una inscripción activa.
- No se crea ningún registro adicional en la base de datos.

---

### TC-HU1-03 — Rechazo cuando la lista de espera ya cerró
Tipo: A
Técnica: VL — valor límite: fecha del evento alcanzada exactamente (límite superior de la vigencia de la lista).
Gherkin: "Lista de espera no disponible"

Precondiciones:
- La fecha del evento ya fue alcanzada.

Pasos:
1. Un comprador envía una solicitud de inscripción para ese evento.

Resultado esperado:
- El sistema rechaza la solicitud.
- La respuesta informa que la lista de espera de ese evento ya cerró.

---

### TC-HU1-04 — Reinscripción válida después de oportunidad utilizada o expirada
Tipo: A
Técnica: TE — transición: oportunidad utilizada → inscripción nueva activa; oportunidad expirada → inscripción nueva activa.
Gherkin: "Reinscripción válida después de oportunidad utilizada o expirada"

Precondiciones:
- El comprador tiene una oportunidad previa en estado utilizada o expirada para el evento.
- La lista de espera del evento sigue vigente.

Pasos:
1. El comprador envía una solicitud de inscripción para el mismo evento.

Resultado esperado:
- El sistema crea una nueva inscripción activa.
- La respuesta confirma que el interés fue registrado nuevamente.

---

### TC-HU1-05 — Unicidad garantizada a nivel de base de datos
Tipo: I
Técnica: SE — suposición de errores: bypass del código de aplicación para verificar que la base de datos rechaza el duplicado de forma independiente.
Sin Gherkin directo — cubre la restricción de índice parcial.

Precondiciones:
- Base de datos real disponible con el índice parcial aplicado.
- Un comprador tiene una inscripción activa para un evento.

Pasos:
1. Intentar insertar directamente una segunda inscripción activa para el mismo comprador y evento, sin pasar por la capa de aplicación.

Resultado esperado:
- La base de datos rechaza la inserción con violación de restricción de unicidad.

---

### TC-HU1-06 — Rechazo cuando la lista de espera está llena
Tipo: A
Técnica: VL — valor límite: la cantidad de inscripciones activas alcanza exactamente el límite global configurado.
Gherkin: "Lista de espera llena"

Precondiciones:
- Existe un evento sin disponibilidad inmediata.
- La lista de espera del evento tiene exactamente el número máximo de inscripciones activas configurado en el sistema.

Pasos:
1. Un comprador nuevo envía una solicitud de inscripción para ese evento.

Resultado esperado:
- El sistema rechaza la solicitud.
- La respuesta informa que la lista de espera de ese evento está llena.
- No se crea ningún registro adicional.

---

## HU2 — Consulta de estado

### TC-HU2-01 — Ver inscripción activa
Tipo: A
Técnica: PE — clase válida: comprador en espera sin oportunidad asignada.
Gherkin: "Ver estado de inscripción activa"

Precondiciones:
- El comprador tiene una inscripción activa sin oportunidad asociada.

Pasos:
1. El comprador consulta su estado para ese evento.

Resultado esperado:
- La respuesta muestra la inscripción con estado activo.
- No se muestra ninguna oportunidad activa ni expirada.

---

### TC-HU2-02 — Ver oportunidad activa con tiempo restante
Tipo: A
Técnica: PE + VL — clase válida: oportunidad activa con tiempo restante > 0. El límite inferior es 1 minuto restante (justo dentro de la vigencia).
Gherkin: "Ver oportunidad activa asociada a la inscripción"

Precondiciones:
- El comprador tiene una oportunidad activa con tiempo restante mayor a cero.

Pasos:
1. El comprador consulta su estado para ese evento.

Resultado esperado:
- La respuesta muestra la oportunidad en estado activo.
- Se muestra el tiempo restante de vigencia en minutos.
- Se indica que la entrada está reservada temporalmente para el comprador.

---

### TC-HU2-03 — Ver oportunidad expirada
Tipo: A
Técnica: VL — valor límite: oportunidad con tiempo restante = 0 (exactamente vencida).
Gherkin: "Ver oportunidad expirada"

Precondiciones:
- La oportunidad del comprador venció sin ser utilizada y su estado es expirada.

Pasos:
1. El comprador consulta su estado para ese evento.

Resultado esperado:
- La respuesta muestra la oportunidad en estado expirada.
- No se muestra ninguna reserva temporal activa.
- El comprador puede saber que puede reinscribirse.

---

### TC-HU2-04 — Distinción correcta entre los cuatro estados
Tipo: U
Técnica: TD — tabla de decisiones: cuatro combinaciones de estado de inscripción y estado de oportunidad producen cuatro respuestas distintas sin solapamiento.
Sin Gherkin directo — cubre la lógica de proyección de estado.

Precondiciones:
- Se tienen cuatro escenarios de datos: inscripción activa, oportunidad activa, oportunidad utilizada, oportunidad expirada.
- Todas las dependencias de repositorio sustituidas por dobles.

Pasos:
1. Invocar el caso de uso de consulta de estado para cada uno de los cuatro escenarios.

Resultado esperado:
- Cada escenario devuelve el estado correcto sin ambigüedad.
- No se devuelve nunca un estado inexistente o nulo.

---

### TC-HU2-05 — Ver oportunidad utilizada
Tipo: A
Técnica: PE — clase válida: comprador cuya oportunidad fue utilizada para avanzar al pago.
Gherkin: "Ver oportunidad utilizada"

Precondiciones:
- El comprador tiene una oportunidad cuyo estado es utilizada (el comprador avanzó al pago desde esa oportunidad).

Pasos:
1. El comprador consulta su estado para ese evento.

Resultado esperado:
- La respuesta muestra la oportunidad en estado utilizada.
- No se muestra ninguna acción de pago pendiente para ese comprador.
- No se muestra cuenta regresiva ni reserva temporal activa.

---

## HU3 — Asignación de oportunidad

### TC-HU3-01 — Asignación al siguiente comprador elegible con reserva exitosa
Tipo: A
Técnica: CP — camino principal: evento ticket.released recibido → elegible encontrado → reserva confirmada → oportunidad creada → inscripción inactivada.
Gherkin: "Asignación al siguiente comprador elegible"

Precondiciones:
- El evento ticket.released llega al sistema con un ticketId y eventId válidos.
- Existe al menos un comprador con inscripción activa para ese evento, ordenados por fecha de inscripción.
- La reserva temporal del ticket para el comprador se confirma exitosamente.

Pasos:
1. El sistema recibe el evento ticket.released para el evento indicado.

Resultado esperado:
- Se crea una oportunidad activa para el comprador con la inscripción más antigua.
- La inscripción de ese comprador pasa a estado inactivo.
- La vigencia de la oportunidad es de 15 minutos desde el momento de activación.

---

### TC-HU3-02 — Sin compradores en lista de espera
Tipo: U
Técnica: PE — clase inválida: lista de espera vacía para el evento liberado.
Gherkin: "Sin compradores en espera"

Precondiciones:
- No existe ninguna inscripción activa para el evento del evento liberado.
- Todas las dependencias sustituidas por dobles.

Pasos:
1. El caso de uso de asignación se invoca con el evento ticket.released.

Resultado esperado:
- No se crea ninguna oportunidad.
- No se llama al servicio de reserva temporal.
- No se produce ningún error.

---

### TC-HU3-03 — Fallo en la reserva temporal del ticket
Tipo: U
Técnica: SE — suposición de errores: la dependencia externa (reserva temporal) falla; se verifica que el sistema no queda en estado inconsistente.
Gherkin: "Fallo al reservar temporalmente la entrada"

Precondiciones:
- Existe un comprador elegible con inscripción activa.
- El doble del servicio de reserva temporal lanza una excepción o devuelve fallo.

Pasos:
1. El caso de uso de asignación se invoca. La llamada al servicio de reserva falla.

Resultado esperado:
- La oportunidad creada como "pending" transiciona a "failed" para trazabilidad.
- La inscripción del comprador elegible permanece activa.
- El evento de liberación queda registrado con el error para diagnóstico.

---

### TC-HU3-04 — Orden de llegada respetado
Tipo: U
Técnica: PE — clase válida con múltiples representantes: tres compradores en lista; se verifica que la selección no es aleatoria sino determinista por fecha de inscripción ascendente.
Sin Gherkin directo — cubre la regla de prioridad.

Precondiciones:
- Tres compradores inscritos para el mismo evento en momentos distintos.
- Todas las dependencias sustituidas por dobles; reserva temporal siempre exitosa.

Pasos:
1. Invocar el caso de uso de asignación.

Resultado esperado:
- La oportunidad se asigna al comprador con la inscripción más antigua, no al más reciente.

---

## HU4 — Notificación in-app

### TC-HU4-01 — Recepción de notificación sin recargar la página
Tipo: A
Técnica: CP — camino de integración: oportunidad activada en backend → evento publicado al canal SSE → frontend recibe la actualización sin recarga.
Gherkin: "Visualización de oportunidad activa dentro de la aplicación"

Precondiciones:
- El comprador tiene la aplicación abierta y está suscrito al canal en tiempo real.
- El comprador tiene una inscripción activa.

Pasos:
1. El sistema activa una oportunidad para el comprador.

Resultado esperado:
- El frontend recibe la actualización de estado sin que el comprador recargue la página.
- Se muestra la oportunidad como activa, con el tiempo restante y la referencia a la reserva temporal.

---

### TC-HU4-02 — Consulta posterior consistente con el estado real
Tipo: A
Técnica: PE — clase válida: oportunidad activa consultada antes de que venza; el resultado debe ser coherente con la fuente oficial.
Gherkin: "Consulta posterior del estado si el comprador sigue navegando"

Precondiciones:
- El comprador tiene una oportunidad activa en el sistema.

Pasos:
1. El comprador consulta su estado mientras la oportunidad sigue vigente.

Resultado esperado:
- El sistema muestra la oportunidad como activa.
- El tiempo restante es coherente con el momento de activación.
- No se muestra la oportunidad como expirada si aún no venció.

---

### TC-HU4-03 — La capa de notificación in-app no modifica el estado de la oportunidad
Tipo: U
Técnica: SE — suposición de errores: verifica que el handler de notificación no tiene efectos secundarios no deseados sobre el estado del dominio.
Sin Gherkin directo.

Precondiciones:
- Doble del repositorio de oportunidades configurado.

Pasos:
1. Invocar el handler de notificación in-app.

Resultado esperado:
- El repositorio de oportunidades no recibe ninguna llamada de escritura.
- Solo se invoca la publicación al canal in-app.

---

## HU5 — Notificación por correo

### TC-HU5-01 — Correo enviado al activarse la oportunidad
Tipo: U
Técnica: CP — camino principal: oportunidad activada → proveedor de correo invocado exactamente una vez → intento registrado como exitoso.
Gherkin: "Envío de correo cuando se activa una oportunidad"

Precondiciones:
- El comprador tiene una dirección de correo válida registrada.
- El doble del proveedor de correo responde con éxito.

Pasos:
1. El caso de uso de notificación se invoca al activarse la oportunidad.

Resultado esperado:
- El proveedor de correo recibe exactamente una llamada de envío.
- El intento queda registrado en NotificationDelivery con resultado exitoso y momento de envío.

---

### TC-HU5-02 — Contenido mínimo presente en el correo
Tipo: U
Técnica: TD — tabla de decisiones: cada campo obligatorio del correo es una condición independiente; todos deben estar presentes; la ausencia de cualquiera es un fallo.
Gherkin: "Contenido mínimo del correo"

Precondiciones:
- Doble del proveedor de correo que captura el contenido enviado.

Pasos:
1. Invocar la generación del correo para una oportunidad activa.

Resultado esperado:
- El contenido del correo incluye la referencia del evento.
- El contenido indica una vigencia de 15 minutos.
- El contenido orienta al comprador a consultar el estado dentro del sistema.
- El contenido no presenta el correo como fuente oficial del estado.

---

### TC-HU5-03 — Fallo del proveedor de correo no afecta la oportunidad
Tipo: U
Técnica: SE — suposición de errores: dependencia externa falla; se verifican tres garantías independientes: sin excepción propagada, intento auditado como fallido, oportunidad sin cambio de estado.
Gherkin: "Fallo en el envío del correo"

Precondiciones:
- El doble del proveedor de correo lanza una excepción.

Pasos:
1. Invocar el caso de uso de notificación.

Resultado esperado:
- No se lanza ninguna excepción al llamador.
- El intento queda registrado en NotificationDelivery con resultado fallido y momento.
- El estado de la oportunidad no cambia.
- El repositorio de oportunidades no recibe ninguna llamada de escritura.

---

### TC-HU5-04 — El correo no se envía antes de que la oportunidad esté persistida
Tipo: I
Técnica: SE — suposición de errores: fallo de persistencia a mitad del flujo; se verifica que el correo no se envía si la oportunidad no llegó a guardarse.
Sin Gherkin directo — cubre el orden de operaciones.

Precondiciones:
- Base de datos real disponible.

Pasos:
1. Ejecutar el flujo completo de activación de oportunidad con base de datos real y doble del proveedor de correo.
2. Verificar el orden de las llamadas: primero persistencia, luego envío de correo.

Resultado esperado:
- La oportunidad existe en base de datos antes de que se invoque el envío del correo.
- Si la persistencia falla, el correo nunca se intenta enviar.

---

## HU6 — Expiración de oportunidad

### TC-HU6-01 — Expiración automática al alcanzar los 15 minutos
Tipo: U
Técnica: VL — análisis de valores límite: oportunidad con exactamente 15 minutos transcurridos (límite exacto de vigencia). Complemento: oportunidad con 14 minutos 59 segundos no debe expirar.
Gherkin: "Expiración automática por vencimiento"

Precondiciones:
- Una oportunidad activa con momento de activación igual a 15 minutos antes del momento actual.
- Doble del repositorio configurado.

Pasos:
1. El proceso de expiración se ejecuta.

Resultado esperado:
- La oportunidad pasa al estado expirada.
- El vencimiento queda registrado con motivo "vencimiento por tiempo" y momento exacto.

---

### TC-HU6-02 — Reasignación al siguiente comprador elegible tras expiración
Tipo: U
Técnica: TE — transición de estados: oportunidad activa → expirada → nueva oportunidad activa para el siguiente comprador.
Gherkin: "Reasignación inmediata al siguiente comprador elegible"

Precondiciones:
- Una oportunidad activa que acaba de expirar.
- Un segundo comprador con inscripción activa para el mismo evento.
- Doble del caso de uso de asignación (HU3) configurado para verificar que se invoca.

Pasos:
1. El proceso de expiración se ejecuta para esa oportunidad.

Resultado esperado:
- La oportunidad expirada queda registrada.
- El caso de uso de asignación de HU3 se invoca con el ticketId liberado.
- La inscripción del comprador cuya oportunidad expiró queda inactiva.

---

### TC-HU6-03 — Devolución al inventario cuando no hay más compradores
Tipo: U
Técnica: TE + PE — transición: oportunidad activa → expirada → ticket disponible. Clase inválida para reasignación: lista de espera vacía.
Gherkin: "Devolución al inventario cuando no hay más compradores"

Precondiciones:
- Una oportunidad activa que acaba de expirar.
- No existe ningún comprador con inscripción activa para ese evento.
- Doble del servicio de inventario configurado.

Pasos:
1. El proceso de expiración se ejecuta para esa oportunidad.

Resultado esperado:
- La oportunidad expirada queda registrada.
- La entrada vuelve al estado disponible en el inventario general.
- No se crea ninguna nueva oportunidad.

---

### TC-HU6-04 — La inscripción del comprador cuya oportunidad expiró queda inactiva
Tipo: U
Técnica: TE — transición: oportunidad activa → expirada implica inscripción activa → inactiva. Se verifica en ambas ramas (con y sin elegible siguiente).
Sin Gherkin directo — cubre la condición de reinscripción futura.

Precondiciones:
- El comprador tiene una oportunidad activa.
- Doble del repositorio de inscripciones configurado.

Pasos:
1. El proceso de expiración procesa la oportunidad.

Resultado esperado:
- El repositorio de inscripciones recibe una llamada que cambia la inscripción relacionada a estado inactivo.
- La llamada ocurre independientemente de si hay o no otro comprador en lista.

---

### TC-HU6-05 — Fallo en la liberación del ticket no deja la oportunidad en estado inconsistente
Tipo: U
Técnica: SE — suposición de errores: fallo en la dependencia de liberación de ticket durante la expiración; se verifica que el sistema no avanza a reasignación ni deja registros huérfanos.
Sin Gherkin directo — cubre el camino de fallo de HU6.

Precondiciones:
- Una oportunidad activa expirada.
- El doble del servicio de liberación de ticket lanza una excepción.

Pasos:
1. El proceso de expiración se ejecuta.

Resultado esperado:
- La oportunidad no cambia su estado (permanece activa o entra en un estado de error trazable).
- El fallo queda registrado con motivo y momento.
- No se intenta reasignar ni liberar al inventario.

---

## Resumen de trazabilidad

| ID | HU | Tipo | Técnica | Gherkin |
|---|---|---|---|---|
| TC-HU1-01 | HU1 | A | PE | Inscripción exitosa |
| TC-HU1-02 | HU1 | A | PE | Prevención de duplicado |
| TC-HU1-03 | HU1 | A | VL | Lista cerrada |
| TC-HU1-04 | HU1 | A | TE | Reinscripción válida |
| TC-HU1-05 | HU1 | I | SE | Unicidad en base de datos |
| TC-HU1-06 | HU1 | A | VL | Lista de espera llena |
| TC-HU2-01 | HU2 | A | PE | Inscripción activa |
| TC-HU2-02 | HU2 | A | PE + VL | Oportunidad activa con tiempo restante |
| TC-HU2-03 | HU2 | A | VL | Oportunidad expirada |
| TC-HU2-04 | HU2 | U | TD | Distinción entre cuatro estados |
| TC-HU2-05 | HU2 | A | PE | Oportunidad utilizada |
| TC-HU3-01 | HU3 | A | CP | Asignación exitosa |
| TC-HU3-02 | HU3 | U | PE | Sin compradores |
| TC-HU3-03 | HU3 | U | SE | Fallo en reserva temporal |
| TC-HU3-04 | HU3 | U | PE | Orden de llegada |
| TC-HU4-01 | HU4 | A | CP | Notificación sin recarga |
| TC-HU4-02 | HU4 | A | PE | Consulta posterior consistente |
| TC-HU4-03 | HU4 | U | SE | Notificación no modifica estado |
| TC-HU5-01 | HU5 | U | CP | Correo enviado al activar |
| TC-HU5-02 | HU5 | U | TD | Contenido mínimo |
| TC-HU5-03 | HU5 | U | SE | Fallo no afecta oportunidad |
| TC-HU5-04 | HU5 | I | SE | Orden persistencia antes que correo |
| TC-HU6-01 | HU6 | U | VL | Expiración automática |
| TC-HU6-02 | HU6 | U | TE | Reasignación con elegible |
| TC-HU6-03 | HU6 | U | TE + PE | Devolución al inventario |
| TC-HU6-04 | HU6 | U | TE | Inscripción pasa a inactiva |
| TC-HU6-05 | HU6 | U | SE | Fallo en liberación de ticket |

---

## HU7 — Inscripción y disponibilidad en la aplicación

### TC-HU7-01 — El comprador ve la opción de lista de espera cuando no hay disponibilidad
Tipo: A
Técnica: PE — clase válida: evento sin disponibilidad + lista vigente → la aplicación muestra la opción de lista de espera.
Gherkin: "El comprador ve la opción de lista de espera cuando no hay disponibilidad"

Precondiciones:
- Un evento sin entradas disponibles.
- La fecha del evento no ha sido alcanzada.

Pasos:
1. El comprador navega a la página del evento en la aplicación.

Resultado esperado:
- La aplicación muestra la opción de unirse a la lista de espera.
- La aplicación no muestra la opción de compra directa.

---

### TC-HU7-02 — El comprador se inscribe desde la aplicación
Tipo: A
Técnica: CP — camino principal: comprador navega al evento → solicita inscripción → recibe confirmación → ve inscripción activa.
Gherkin: "El comprador se inscribe en la lista de espera desde la aplicación"

Precondiciones:
- Un evento sin disponibilidad inmediata.
- El comprador no tiene inscripción activa para ese evento.

Pasos:
1. El comprador navega a la página del evento.
2. El comprador solicita unirse a la lista de espera desde la interfaz.

Resultado esperado:
- La aplicación confirma que el interés del comprador fue registrado.
- La aplicación muestra la inscripción como activa.

---

### TC-HU7-03 — La aplicación no muestra lista de espera cuando el evento cerró
Tipo: A
Técnica: VL — valor límite: fecha del evento alcanzada exactamente.
Gherkin: "La aplicación no muestra la opción de lista de espera cuando el evento ya cerró"

Precondiciones:
- Un evento cuya fecha ya fue alcanzada.

Pasos:
1. El comprador navega a la página del evento en la aplicación.

Resultado esperado:
- La aplicación no muestra la opción de unirse a la lista de espera.
- La aplicación informa que la lista de espera de ese evento ya cerró.

---

### TC-HU7-04 — Rechazo de duplicado visible desde la aplicación
Tipo: A
Técnica: PE — clase inválida: comprador con inscripción activa intenta inscribirse de nuevo desde la interfaz.
Gherkin: "El comprador ve que su inscripción fue rechazada por duplicado desde la aplicación"

Precondiciones:
- El comprador ya tiene una inscripción activa para el evento.

Pasos:
1. El comprador intenta unirse nuevamente a la lista de espera desde la aplicación.

Resultado esperado:
- La aplicación informa que ya existe una inscripción activa.
- No se crea una inscripción adicional.

---

## HU8 — Consulta de estado y acción sobre oportunidad en la aplicación

### TC-HU8-01 — El comprador consulta su estado desde la aplicación
Tipo: A
Técnica: PE — clase válida: comprador inscrito consulta su estado y la aplicación muestra la información correcta.
Gherkin: "El comprador consulta su estado en la lista de espera desde la aplicación"

Precondiciones:
- El comprador tiene una inscripción activa para un evento.

Pasos:
1. El comprador consulta su estado desde la aplicación.

Resultado esperado:
- La aplicación muestra el estado actual de la inscripción sin ambigüedad.
- La información es coherente con el estado real del sistema.

---

### TC-HU8-02 — El comprador ve una oportunidad activa con tiempo restante
Tipo: A
Técnica: PE + VL — clase válida: oportunidad activa con tiempo restante > 0 desde la interfaz de la aplicación.
Gherkin: "El comprador ve una oportunidad activa con tiempo restante"

Precondiciones:
- El comprador tiene una oportunidad activa con tiempo restante de vigencia.

Pasos:
1. El comprador consulta su estado desde la aplicación.

Resultado esperado:
- La aplicación muestra que tiene una oportunidad activa.
- La aplicación muestra el tiempo restante de vigencia.
- La aplicación muestra la acción para avanzar al pago.
- La información es coherente con el estado real del sistema.

---

### TC-HU8-03 — El comprador actúa sobre una oportunidad activa desde la aplicación
Tipo: A
Técnica: TE — transición de estados: oportunidad activa → oportunidad utilizada, disparada por la acción del comprador de avanzar al pago.
Gherkin: "El comprador actúa sobre una oportunidad activa desde la aplicación"

Precondiciones:
- El comprador tiene una oportunidad activa con tiempo restante.

Pasos:
1. El comprador decide avanzar al pago desde la aplicación.

Resultado esperado:
- La oportunidad pasa a estado utilizada.
- El comprador es dirigido al flujo de pago con la entrada reservada temporalmente para él.
- La aplicación deja de mostrar la oportunidad como activa.

---

### TC-HU8-04 — El comprador ve que su oportunidad expiró desde la aplicación
Tipo: A
Técnica: VL — valor límite: oportunidad con tiempo restante = 0 (exactamente vencida) vista desde la interfaz de la aplicación.
Gherkin: "El comprador ve que su oportunidad expiró"

Precondiciones:
- La oportunidad del comprador venció sin ser utilizada.

Pasos:
1. El comprador consulta su estado desde la aplicación.

Resultado esperado:
- La aplicación muestra que la oportunidad ha expirado.
- La aplicación no muestra acción de pago.
- La información es coherente con el estado real del sistema.

---

### TC-HU8-05 — El comprador ve que su oportunidad fue utilizada desde la aplicación
Tipo: A
Técnica: PE — clase válida: comprador cuya oportunidad fue reclamada, consulta su estado.
Gherkin: "El comprador ve que su oportunidad fue utilizada"

Precondiciones:
- La oportunidad del comprador fue reclamada para avanzar al pago.

Pasos:
1. El comprador consulta su estado desde la aplicación.

Resultado esperado:
- La aplicación muestra que la oportunidad fue utilizada.
- La aplicación no muestra acción de pago ni cuenta regresiva.

---

### TC-HU8-06 — El comprador intenta avanzar al pago pero la oportunidad ya expiró
Tipo: A
Técnica: SE — suposición de errores: condición de carrera entre la cuenta regresiva del cliente y la expiración en el servidor.
Gherkin: "El comprador intenta avanzar al pago pero la oportunidad ya expiró"

Precondiciones:
- El comprador tiene una oportunidad que acaba de vencer en el servidor.
- La aplicación aún muestra la oportunidad como activa (por latencia).

Pasos:
1. El comprador intenta avanzar al pago desde la aplicación.

Resultado esperado:
- La aplicación informa que la oportunidad ya no está activa.
- La aplicación actualiza la vista al estado de oportunidad expirada.
- No se redirige al flujo de pago.

---

### TC-HU8-07 — El comprador intenta avanzar al pago con un correo que no corresponde
Tipo: A
Técnica: PE — clase inválida: correo electrónico proporcionado no coincide con el asignado a la oportunidad.
Gherkin: "El comprador intenta avanzar al pago con un correo que no corresponde"

Precondiciones:
- Existe una oportunidad activa asignada a un comprador específico.

Pasos:
1. Un comprador proporciona un correo electrónico diferente al del comprador asignado e intenta avanzar al pago.

Resultado esperado:
- La aplicación informa que la oportunidad no pertenece al comprador indicado.
- La oportunidad no cambia de estado.
- No se redirige al flujo de pago.

---

### TC-HU8-08 — Comprador sin inscripción consulta su estado desde la aplicación
Tipo: A
Técnica: PE — clase inválida: comprador sin inscripción intenta consultar estado.
Gherkin: "Comprador sin inscripción consulta su estado"

Precondiciones:
- El comprador no tiene inscripción en la lista de espera del evento.

Pasos:
1. El comprador consulta su estado desde la aplicación proporcionando su correo electrónico.

Resultado esperado:
- La aplicación muestra que no existe inscripción para ese comprador.
- No se muestra ninguna oportunidad ni acción de pago.

---

## Escenarios de automatización funcional

Estos escenarios se planifican con Serenity BDD. Su propósito es generar evidencia ejecutable de que la feature cumple los criterios de aceptación acordados, expresados en lenguaje que negocio y QA puedan leer directamente en el reporte.

La planificación cubre todos los escenarios que se consideran necesarios para cobertura funcional completa. De estos, se seleccionan tres para implementación efectiva; el resto queda planificado para implementación futura si la capacidad del equipo lo permite.

### Escenarios planificados — Aplicación (Front)

---

### TC-AUTO-F01 — El comprador ve la opción de lista de espera cuando no hay disponibilidad
Enfoque planificado: Front
Gherkin: "El comprador ve la opción de lista de espera cuando no hay disponibilidad" (HU7)

Precondiciones:
- Existe un evento visible en la aplicación sin entradas disponibles.
- La lista de espera del evento sigue vigente.

Pasos:
1. El comprador navega a la página del evento en la aplicación.

Resultado esperado:
- La aplicación muestra la opción de unirse a la lista de espera.
- La aplicación no muestra la opción de compra directa.

---

### TC-AUTO-F02 — El comprador se inscribe en la lista de espera desde la aplicación ⬅ SELECCIONADO
Enfoque de implementación: Front — POM con PageFactory
Gherkin: "El comprador se inscribe en la lista de espera desde la aplicación" (HU7)

Precondiciones:
- Existe un evento visible en la aplicación sin entradas disponibles.
- El comprador no tiene ninguna inscripción activa para ese evento.
- La fecha del evento no ha sido alcanzada.

Pasos:
1. El comprador navega a la página del evento en la aplicación.
2. El comprador observa que no hay entradas disponibles.
3. El comprador solicita unirse a la lista de espera desde la interfaz.

Resultado esperado:
- La aplicación confirma que el interés del comprador fue registrado.
- El comprador puede ver su inscripción como activa dentro de la aplicación.

---

### TC-AUTO-F03 — La aplicación rechaza la inscripción duplicada del comprador
Enfoque planificado: Front
Gherkin: "El comprador ve que su inscripción fue rechazada por duplicado desde la aplicación" (HU7)

Precondiciones:
- El comprador ya tiene una inscripción activa para el evento.

Pasos:
1. El comprador intenta unirse nuevamente a la lista de espera desde la aplicación.

Resultado esperado:
- La aplicación informa que ya existe una inscripción activa.
- No se crea una inscripción adicional.

---

### TC-AUTO-F04 — La aplicación no muestra la opción de lista de espera cuando el evento cerró
Enfoque planificado: Front
Gherkin: "La aplicación no muestra la opción de lista de espera cuando el evento ya cerró" (HU7)

Precondiciones:
- Un evento cuya fecha ya fue alcanzada.

Pasos:
1. El comprador navega a la página del evento en la aplicación.

Resultado esperado:
- La aplicación no muestra la opción de unirse a la lista de espera.
- La aplicación informa que la lista de espera ya cerró.

---

### TC-AUTO-F05 — El comprador consulta su inscripción activa desde la aplicación
Enfoque planificado: Front
Gherkin: "El comprador consulta su estado en la lista de espera desde la aplicación" (HU8)

Precondiciones:
- El comprador tiene una inscripción activa sin oportunidad asignada.

Pasos:
1. El comprador consulta su estado desde la aplicación.

Resultado esperado:
- La aplicación muestra la inscripción como activa.
- No se muestra ninguna oportunidad activa.

---

### TC-AUTO-F06 — El comprador consulta su oportunidad activa con tiempo restante ⬅ SELECCIONADO
Enfoque de implementación: Front — Screenplay
Gherkin: "Ver oportunidad activa asociada a la inscripción" (HU2) + "El comprador consulta su estado en la lista de espera desde la aplicación" (HU8)

Precondiciones:
- El comprador tiene una oportunidad activa derivada de la lista de espera.
- La oportunidad tiene tiempo restante de vigencia.

Pasos:
1. El comprador abre la aplicación.
2. El comprador consulta el estado de su inscripción en la lista de espera.

Resultado esperado:
- La aplicación muestra que el comprador tiene una oportunidad activa.
- Se muestra el tiempo restante de vigencia de la oportunidad.
- Se indica que la entrada está reservada temporalmente para ese comprador.
- La información es coherente con el estado real del sistema.

---

### TC-AUTO-F07 — El comprador actúa sobre una oportunidad activa y avanza al pago
Enfoque planificado: Front
Gherkin: "El comprador actúa sobre una oportunidad activa desde la aplicación" (HU8)

Precondiciones:
- El comprador tiene una oportunidad activa con tiempo restante.

Pasos:
1. El comprador decide avanzar al pago desde la aplicación.

Resultado esperado:
- La oportunidad pasa a estado utilizada.
- El comprador es dirigido al flujo de pago.
- La aplicación deja de mostrar la oportunidad como activa.

---

### TC-AUTO-F08 — El comprador ve que su oportunidad expiró
Enfoque planificado: Front
Gherkin: "Ver oportunidad expirada" (HU8, HU2)

Precondiciones:
- La oportunidad del comprador venció sin ser utilizada.

Pasos:
1. El comprador consulta su estado desde la aplicación.

Resultado esperado:
- La aplicación muestra la oportunidad en estado expirada.
- No se muestra ninguna reserva temporal activa.

---

### TC-AUTO-F09 — El comprador recibe la actualización de su oportunidad sin recargar la página
Enfoque planificado: Front
Gherkin: "Visualización de oportunidad activa dentro de la aplicación" (HU4)

Precondiciones:
- El comprador tiene la aplicación abierta y está navegando.
- Se activa una oportunidad para el comprador.

Pasos:
1. El sistema activa una oportunidad para el comprador mientras está navegando.

Resultado esperado:
- La aplicación muestra la oportunidad activa sin que el comprador recargue la página.
- Se muestra el tiempo restante y la referencia a la reserva temporal.

---

### TC-AUTO-F10 — El comprador ve que su oportunidad fue utilizada
Enfoque planificado: Front
Gherkin: "El comprador ve que su oportunidad fue utilizada" (HU8)

Precondiciones:
- La oportunidad del comprador fue reclamada para avanzar al pago.

Pasos:
1. El comprador consulta su estado desde la aplicación.

Resultado esperado:
- La aplicación muestra que la oportunidad fue utilizada.
- No se muestra acción de pago ni cuenta regresiva.

---

### TC-AUTO-F11 — El comprador intenta avanzar al pago pero la oportunidad ya expiró
Enfoque planificado: Front
Gherkin: "El comprador intenta avanzar al pago pero la oportunidad ya expiró" (HU8)

Precondiciones:
- La oportunidad del comprador acaba de vencer en el servidor.

Pasos:
1. El comprador intenta avanzar al pago desde la aplicación.

Resultado esperado:
- La aplicación informa que la oportunidad ya no está activa.
- La vista se actualiza al estado de oportunidad expirada.

---

### TC-AUTO-F12 — Comprador sin inscripción consulta su estado
Enfoque planificado: Front
Gherkin: "Comprador sin inscripción consulta su estado" (HU8)

Precondiciones:
- El comprador no tiene inscripción en la lista de espera del evento.

Pasos:
1. El comprador consulta su estado desde la aplicación.

Resultado esperado:
- La aplicación muestra que no existe inscripción para ese comprador.

---

### Escenarios planificados — Servicios (Back)

---

### TC-AUTO-B01 — Inscripción exitosa en lista de espera
Enfoque planificado: Back — Serenity REST
Gherkin: "Inscripción exitosa cuando no hay disponibilidad inmediata" (HU1)

Precondiciones:
- Existe un evento sin entradas disponibles.
- El comprador no tiene inscripción activa para ese evento.

Pasos:
1. El comprador solicita inscribirse en la lista de espera del evento.

Resultado esperado:
- El sistema registra la inscripción con estado activo.
- La respuesta confirma que el interés fue registrado.

---

### TC-AUTO-B02 — Rechazo de inscripción duplicada
Enfoque planificado: Back — Serenity REST
Gherkin: "Prevención de inscripción duplicada" (HU1)

Precondiciones:
- El comprador tiene una inscripción activa para el evento.

Pasos:
1. El comprador solicita inscribirse nuevamente.

Resultado esperado:
- El sistema rechaza la solicitud.
- La respuesta informa que ya existe una inscripción activa.

---

### TC-AUTO-B03 — Rechazo de inscripción cuando la lista cerró
Enfoque planificado: Back — Serenity REST
Gherkin: "Lista de espera no disponible" (HU1)

Precondiciones:
- La fecha del evento ya fue alcanzada.

Pasos:
1. Un comprador solicita inscribirse en la lista de espera.

Resultado esperado:
- El sistema rechaza la solicitud.
- La respuesta informa que la lista de espera ya cerró.

---

### TC-AUTO-B04 — Reinscripción tras oportunidad utilizada o expirada
Enfoque planificado: Back — Serenity REST
Gherkin: "Reinscripción válida después de oportunidad utilizada o expirada" (HU1)

Precondiciones:
- El comprador tiene una oportunidad previa en estado utilizada o expirada.
- La lista de espera sigue vigente.

Pasos:
1. El comprador solicita inscribirse nuevamente.

Resultado esperado:
- El sistema registra una nueva inscripción activa.
- La respuesta confirma que el interés fue registrado nuevamente.

---

### TC-AUTO-B05 — Consulta de estado con inscripción activa
Enfoque planificado: Back — Serenity REST
Gherkin: "Ver estado de inscripción activa" (HU2)

Precondiciones:
- El comprador tiene una inscripción activa sin oportunidad asociada.

Pasos:
1. El comprador consulta su estado.

Resultado esperado:
- La respuesta muestra la inscripción con estado activo.
- No se muestra ninguna oportunidad.

---

### TC-AUTO-B06 — Consulta de estado con oportunidad activa
Enfoque planificado: Back — Serenity REST
Gherkin: "Ver oportunidad activa asociada a la inscripción" (HU2)

Precondiciones:
- El comprador tiene una oportunidad activa con tiempo restante.

Pasos:
1. El comprador consulta su estado.

Resultado esperado:
- La respuesta muestra la oportunidad en estado activo.
- Se incluye el tiempo restante de vigencia.
- Se indica que la entrada está reservada temporalmente.

---

### TC-AUTO-B07 — Consulta de estado con oportunidad expirada
Enfoque planificado: Back — Serenity REST
Gherkin: "Ver oportunidad expirada" (HU2)

Precondiciones:
- La oportunidad del comprador venció sin ser utilizada.

Pasos:
1. El comprador consulta su estado.

Resultado esperado:
- La respuesta muestra la oportunidad en estado expirada.
- No se muestra reserva temporal activa.

---

### TC-AUTO-B08 — Flujo completo: inscripción → consulta → intento de duplicado ⬅ SELECCIONADO
Enfoque de implementación: Back — Screenplay con Serenity REST
Gherkin: "Inscripción exitosa cuando no hay disponibilidad inmediata" (HU1) + "Ver estado de inscripción activa" (HU2) + "Prevención de inscripción duplicada" (HU1)

Precondiciones:
- Existe un evento sin entradas disponibles en el sistema.
- El comprador no tiene inscripción activa para ese evento.
- La lista de espera del evento está vigente.

Pasos:
1. El comprador solicita inscribirse en la lista de espera del evento.
2. El sistema confirma que la inscripción fue registrada.
3. El comprador consulta su estado en la lista de espera.
4. El comprador intenta inscribirse nuevamente en el mismo evento.

Resultado esperado:
- La primera inscripción se registra exitosamente con estado activo.
- La consulta de estado muestra la inscripción activa sin ambigüedad.
- El segundo intento de inscripción es rechazado porque ya existe una inscripción activa.
- Cada respuesta del sistema es coherente con las reglas de negocio de unicidad y vigencia.

---

### TC-AUTO-B09 — Flujo de asignación: liberación → oportunidad → consulta
Enfoque planificado: Back — Serenity REST
Gherkin: "Asignación al siguiente comprador elegible" (HU3) + "Ver oportunidad activa asociada a la inscripción" (HU2)

Precondiciones:
- Un comprador inscrito en la lista de espera.
- Una entrada del evento se libera.

Pasos:
1. El sistema procesa la liberación de la entrada.
2. El comprador consulta su estado.

Resultado esperado:
- Se crea una oportunidad activa para el comprador elegible.
- La consulta de estado muestra la oportunidad activa con tiempo restante.

---

### TC-AUTO-B10 — Flujo de expiración: oportunidad vence → reasignación
Enfoque planificado: Back — Serenity REST
Gherkin: "Reasignación inmediata al siguiente comprador elegible" (HU6)

Precondiciones:
- Un comprador tiene una oportunidad activa que está por vencer.
- Otro comprador tiene inscripción activa para el mismo evento.

Pasos:
1. La oportunidad expira.
2. El sistema procesa la expiración.

Resultado esperado:
- La oportunidad del primer comprador pasa a expirada.
- Se crea una nueva oportunidad para el siguiente comprador elegible.

---

### TC-AUTO-B11 — El sistema registra el intento de correo con resultado
Enfoque planificado: Back — Serenity REST
Gherkin: "Fallo en el envío del correo" (HU5)

Precondiciones:
- Una oportunidad activa recién asignada.

Pasos:
1. El sistema intenta enviar el correo de aviso.

Resultado esperado:
- El intento queda registrado con resultado (exitoso o fallido) y momento.
- La oportunidad permanece activa independientemente del resultado del envío.

---

### TC-AUTO-B12 — Rechazo de inscripción por lista de espera llena
Enfoque planificado: Back — Serenity REST
Gherkin: "Lista de espera llena" (HU1)

Precondiciones:
- La lista de espera del evento tiene exactamente el número máximo de inscripciones activas configurado.

Pasos:
1. Un comprador nuevo solicita inscribirse en la lista de espera.

Resultado esperado:
- El sistema rechaza la solicitud.
- La respuesta informa que la lista de espera está llena.

---

### TC-AUTO-B13 — Consulta de estado con oportunidad utilizada
Enfoque planificado: Back — Serenity REST
Gherkin: "Ver oportunidad utilizada" (HU2)

Precondiciones:
- El comprador tiene una oportunidad cuyo estado es utilizada.

Pasos:
1. El comprador consulta su estado.

Resultado esperado:
- La respuesta muestra la oportunidad en estado utilizada.
- No se muestra acción de pago pendiente ni reserva temporal activa.

---

## Resumen de trazabilidad

| ID | HU | Tipo | Técnica / Enfoque | Gherkin |
|---|---|---|---|---|
| TC-HU1-01 | HU1 | A | PE | Inscripción exitosa |
| TC-HU1-02 | HU1 | A | PE | Prevención de duplicado |
| TC-HU1-03 | HU1 | A | VL | Lista cerrada |
| TC-HU1-04 | HU1 | A | TE | Reinscripción válida |
| TC-HU1-05 | HU1 | I | SE | Unicidad en base de datos |
| TC-HU1-06 | HU1 | A | VL | Lista de espera llena |
| TC-HU2-01 | HU2 | A | PE | Inscripción activa |
| TC-HU2-02 | HU2 | A | PE + VL | Oportunidad activa con tiempo restante |
| TC-HU2-03 | HU2 | A | VL | Oportunidad expirada |
| TC-HU2-04 | HU2 | U | TD | Distinción entre cuatro estados |
| TC-HU2-05 | HU2 | A | PE | Oportunidad utilizada |
| TC-HU3-01 | HU3 | A | CP | Asignación exitosa |
| TC-HU3-02 | HU3 | U | PE | Sin compradores |
| TC-HU3-03 | HU3 | U | SE | Fallo en reserva temporal |
| TC-HU3-04 | HU3 | U | PE | Orden de llegada |
| TC-HU4-01 | HU4 | A | CP | Notificación sin recarga |
| TC-HU4-02 | HU4 | A | PE | Consulta posterior consistente |
| TC-HU4-03 | HU4 | U | SE | Notificación no modifica estado |
| TC-HU5-01 | HU5 | U | CP | Correo enviado al activar |
| TC-HU5-02 | HU5 | U | TD | Contenido mínimo |
| TC-HU5-03 | HU5 | U | SE | Fallo no afecta oportunidad |
| TC-HU5-04 | HU5 | I | SE | Orden persistencia antes que correo |
| TC-HU6-01 | HU6 | U | VL | Expiración automática |
| TC-HU6-02 | HU6 | U | TE | Reasignación con elegible |
| TC-HU6-03 | HU6 | U | TE + PE | Devolución al inventario |
| TC-HU6-04 | HU6 | U | TE | Inscripción pasa a inactiva |
| TC-HU6-05 | HU6 | U | SE | Fallo en liberación de ticket |
| TC-HU7-01 | HU7 | A | PE | Opción de lista de espera visible |
| TC-HU7-02 | HU7 | A | CP | Inscripción desde la aplicación |
| TC-HU7-03 | HU7 | A | VL | Lista cerrada en la aplicación |
| TC-HU7-04 | HU7 | A | PE | Duplicado visible en la aplicación |
| TC-HU8-01 | HU8 | A | PE | Consulta de estado desde la aplicación |
| TC-HU8-02 | HU8 | A | PE + VL | Oportunidad activa con tiempo restante |
| TC-HU8-03 | HU8 | A | TE | Actuar sobre oportunidad activa |
| TC-HU8-04 | HU8 | A | VL | Oportunidad expirada desde la aplicación |
| TC-HU8-05 | HU8 | A | PE | Oportunidad utilizada desde la aplicación |
| TC-HU8-06 | HU8 | A | SE | Claim fallido por oportunidad expirada |
| TC-HU8-07 | HU8 | A | PE | Claim fallido por correo incorrecto |
| TC-HU8-08 | HU8 | A | PE | Comprador sin inscripción consulta estado |
| TC-AUTO-F01 | HU7 | AUTO | Front (planificado) | Opción visible sin disponibilidad |
| TC-AUTO-F02 | HU7 | AUTO | Front — POM + PageFactory ⬅ | Inscripción desde la aplicación |
| TC-AUTO-F03 | HU7 | AUTO | Front (planificado) | Duplicado rechazado |
| TC-AUTO-F04 | HU7 | AUTO | Front (planificado) | Lista cerrada |
| TC-AUTO-F05 | HU8 | AUTO | Front (planificado) | Inscripción activa consultada |
| TC-AUTO-F06 | HU8, HU2 | AUTO | Front — Screenplay ⬅ | Oportunidad activa con tiempo |
| TC-AUTO-F07 | HU8 | AUTO | Front (planificado) | Actuar sobre oportunidad |
| TC-AUTO-F08 | HU8, HU2 | AUTO | Front (planificado) | Oportunidad expirada visible |
| TC-AUTO-F09 | HU4 | AUTO | Front (planificado) | Actualización sin recarga |
| TC-AUTO-F10 | HU8 | AUTO | Front (planificado) | Oportunidad utilizada visible |
| TC-AUTO-F11 | HU8 | AUTO | Front (planificado) | Claim fallido por expiración |
| TC-AUTO-F12 | HU8 | AUTO | Front (planificado) | Comprador sin inscripción |
| TC-AUTO-B01 | HU1 | AUTO | Back (planificado) | Inscripción exitosa |
| TC-AUTO-B02 | HU1 | AUTO | Back (planificado) | Duplicado rechazado |
| TC-AUTO-B03 | HU1 | AUTO | Back (planificado) | Lista cerrada |
| TC-AUTO-B04 | HU1 | AUTO | Back (planificado) | Reinscripción válida |
| TC-AUTO-B05 | HU2 | AUTO | Back (planificado) | Estado inscripción activa |
| TC-AUTO-B06 | HU2 | AUTO | Back (planificado) | Estado oportunidad activa |
| TC-AUTO-B07 | HU2 | AUTO | Back (planificado) | Estado oportunidad expirada |
| TC-AUTO-B08 | HU1 + HU2 | AUTO | Back — Screenplay + REST ⬅ | Flujo inscripción + consulta + duplicado |
| TC-AUTO-B09 | HU3 + HU2 | AUTO | Back (planificado) | Flujo liberación → asignación → consulta |
| TC-AUTO-B10 | HU6 + HU3 | AUTO | Back (planificado) | Flujo expiración → reasignación |
| TC-AUTO-B11 | HU5 | AUTO | Back (planificado) | Intento de correo auditado |
| TC-AUTO-B12 | HU1 | AUTO | Back (planificado) | Lista de espera llena |
| TC-AUTO-B13 | HU2 | AUTO | Back (planificado) | Estado oportunidad utilizada |
