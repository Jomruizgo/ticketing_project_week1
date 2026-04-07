# Planificación de Feature de Alta Complejidad

## Objetivo

Este documento tiene como objetivo presentar el plan de implementación de una feature de alta complejidad, o también conocida como épica.

---

## Desarrollo

> **Feature:** Como comprador interesado, quiero unirme a una lista de espera para un evento con tickets agotados o sin disponibilidad inmediata, para tener prioridad cuando un ticket se libere.

### Problema que resuelve

Hoy, cuando un evento se queda sin disponibilidad inmediata, los compradores interesados no tienen una manera formal de mantener su intención de compra ni de enterarse oportunamente si una entrada (ticket) vuelve a estar disponible. Si luego una entrada se libera, esa oportunidad puede perderse porque el comprador no estaba mirando la aplicación en ese momento. Esta feature busca conservar la demanda interesada, asignar prioridad cuando una entrada vuelva a quedar libre y comunicar de forma clara dentro de la aplicación y por correo electrónico, para aumentar la posibilidad real de conversión.

### Contexto y supuestos

- El negocio ya permite que una entrada vuelva a quedar libre cuando una reserva vence o una compra no se confirma.
- Existe una oportunidad clara de no perder demanda cuando el inventario se mueve de nuevo.
- La lista de espera operará inicialmente a nivel de evento.
- La asignación inicial seguirá una regla simple: orden de llegada.
- La notificación no debe depender solo de que el comprador esté conectado a la aplicación; también debe existir una comunicación por correo electrónico.
- El correo electrónico se entiende como un canal para avisar que existe una oportunidad activa, no como reemplazo de estado visible dentro de la experiencia del producto.
- Una entrada se considera liberada cuando: (a) una reserva existente vence sin pago completado, o (b) un pago es rechazado. En ambos casos el sistema detecta la liberación automáticamente y activa el proceso de lista de espera.
- Cuando una entrada queda libre, la lista de espera tiene prioridad antes de que esa entrada vuelva al inventario general. Solo si no hay ningún comprador elegible en la lista de espera, la entrada queda disponible para compra directa.
- El comprador se identifica con su correo electrónico; no existe un sistema de usuarios con autenticación previa. El correo es suficiente para registrar el interés y recibir avisos.

---

## Alcance

### Qué incluye esta épica

- Un comprador puede registrar su interés en un evento sin disponibilidad inmediata; la inscripción corresponde a una sola entrada.
- Cuando una entrada queda libre, el sistema asigna una oportunidad al siguiente comprador elegible según orden de llegada.
- El comprador puede enterarse de su oportunidad dentro de la aplicación, en tiempo real.
- El comprador recibe una notificación por correo electrónico como canal de aviso complementario.
- La oportunidad tiene un estado visible, trazable y coherente para el comprador, soporte y negocio.
- Una vez que la oportunidad fue utilizada o expiró, el comprador puede volver a inscribirse mientras la lista siga vigente.
- La lista de espera cierra al alcanzarse la fecha del evento; después no se aceptan inscripciones ni se asignan oportunidades.

### No se incluye

- Cancelación voluntaria de la inscripción.
- Personalización avanzada del contenido del correo más allá de la información esencial para actuar.
- El correo como fuente oficial del estado: el estado real vive dentro del sistema.
- Redefinición del proceso base de compra; esta épica lo extiende sin modificarlo.
- Estrategias de automatización comercial multicanal.
- Priorización por criterios diferentes al orden de llegada.
- Lista de espera segmentada por tipo o categoría de ticket.
- Pre-automatización de pago durante el tiempo de espera.
- Modificación de la lógica interna de los servicios existentes de reservas y pagos; solo se les agrega la capacidad de notificar cuando una entrada queda libre.

### Palabras clave o lenguaje de negocio

| Término | Definición |
|---|---|
| **Sin disponibilidad** | El evento no tiene entradas disponibles para compra inmediata en ese momento. |
| **Entrada liberada** | Una entrada que estaba reservada o en proceso de pago y volvió a estar libre porque la reserva venció o el pago fue rechazado. La lista de espera intercepta esa entrada antes de que vuelva al inventario general. |
| **Oportunidad en proceso** | Estado transitorio: el sistema identificó al comprador elegible y está confirmando que puede reservarle la entrada. Si la confirmación falla, el comprador sigue activo en la lista de espera para el próximo intento. |
| **Oportunidad activa** | La entrada quedó reservada exclusivamente para ese comprador. La vigencia es de 15 minutos desde el momento en que la reserva fue confirmada. |
| **Oportunidad utilizada** | El comprador avanzó a la pantalla de pago y confirmó la compra desde la oportunidad activa. En ese momento la oportunidad pasa a estado utilizada y la entrada continúa el ciclo normal de pago. |
| **Oportunidad expirada** | Los 15 minutos de vigencia terminaron sin que el comprador haya iniciado el flujo de pago. |
| **Fuente oficial del estado** | El estado real de la oportunidad vive dentro del sistema; el correo es solo un canal de aviso. |
| **Política de liberación** | Si una oportunidad vence, la entrada queda libre e inmediatamente el sistema intenta asignarla al siguiente comprador en espera; si no hay nadie más, la entrada vuelve al inventario general sin prioridad especial. |
| **Reinscripción** | Inscripción nueva que un comprador realiza después de que su oportunidad anterior fue utilizada o expiró. Es válida mientras la lista de espera del evento esté vigente. |
| **Vigencia de la lista de espera** | La lista acepta inscripciones y genera oportunidades solo hasta la fecha del evento. Al alcanzarse esa fecha, la lista cierra y las inscripciones activas sin oportunidad se cierran sin asignación. |

---

## Diagramas de soporte

Los siguientes diagramas complementan este plan y detallan las decisiones de diseño desde distintas perspectivas. Se encuentran en la carpeta `drawio/` junto a este documento:

| Diagrama | Qué muestra |
|---|---|
| [Contenedores C4](drawio/c4_contenedores.drawio) | Visión general del sistema: qué contenedores participan, cómo se conectan y qué responsabilidades nuevas adquiere cada uno con esta feature. |
| [Componentes C4 — CRUD Service](drawio/c4_componentes_crud.drawio) | Descomposición interna del CRUD Service: controladores, handlers, repositorios, consumidores y servicios nuevos que esta feature agrega. |
| [Secuencia — Lista de espera](drawio/secuencia_lista_espera.drawio) | Flujo completo de interacción entre los actores y servicios para inscripción, asignación, notificación, expiración y reasignación. |
| [Esquema de base de datos](drawio/bd_esquema.drawio) | Tablas nuevas (`waitlist_entries`, `waitlist_opportunities`, `notification_deliveries`), relaciones con tablas existentes e índice parcial de unicidad. |

---

## Orden de implementación recomendado

El orden respeta dependencias de datos y reduce el riesgo de integración:

1. Integración de los eventos de liberación de entradas en los servicios existentes + configuración del mecanismo de expiración automática en la mensajería
2. **HU1** — Inscripción en lista de espera
3. **HU2** — Consulta de estado
4. **HU3** — Asignación de oportunidad *(depende del trabajo previo de integración y HU1)*
5. **HU4** — Notificación in-app *(depende de HU3)*
6. **HU5** — Notificación por correo *(depende de HU3, puede ir en paralelo con HU4)*
7. **HU6** — Expiración y reasignación *(depende de HU3; la expiración se activa automáticamente sin intervención manual)*
8. **HU7** — Inscripción y disponibilidad en la aplicación *(depende de HU1; puede avanzar en paralelo con HU4-HU6)*
9. **HU8** — Consulta de estado y acción sobre oportunidad en la aplicación *(depende de HU2, HU3 y HU4; se construye al final porque integra los flujos de estado y acción)*

**Total estimado:** 42 puntos + trabajo previo de integración de eventos.

---

## Refinamiento en Historias de Usuario

---

### HU1 — Inscripción en lista de espera

> Como comprador, quiero inscribirme en la lista de espera de un evento sin disponibilidad inmediata, para mantener mi intención de compra dentro del sistema.

#### Revisión INVEST

| Criterio | Análisis |
|---|---|
| **Independent** | Puede construirse sin depender de otras HU; no requiere que el mecanismo de liberación de entradas exista ni que la asignación funcione. |
| **Negotiable** | El objetivo real es conservar el interés del comprador, y hay otras formas de lograrlo. |
| **Valuable** | Aprovecha demanda que actualmente se puede perder. |
| **Estimable** | Alcance conocido sin dependencias externas. |
| **Small** | Solo entrar a la lista, evitar duplicados y permitir reinscripción. |
| **Testable** | El registro se crea, se rechaza por duplicado, se rechaza por lista cerrada, o se acepta como reinscripción según el escenario. |

#### Criterios de aceptación

```gherkin
Scenario: Inscripción exitosa cuando no hay disponibilidad inmediata
  Given un evento sin disponibilidad inmediata
  And un comprador no inscrito activamente en la lista de espera de ese evento
  When el comprador solicita unirse a la lista de espera
  Then el sistema registra su inscripción como activa
  And el sistema confirma que su interés fue registrado

Scenario: Prevención de inscripción duplicada
  Given un evento sin disponibilidad inmediata
  And un comprador ya inscrito activamente en la lista de espera de ese evento
  When el comprador intenta unirse nuevamente a la lista de espera
  Then el sistema rechaza la inscripción duplicada
  And el sistema informa que ya existe una inscripción activa

Scenario: Lista de espera no disponible
  Given un evento cuya fecha ya fue alcanzada
  When el comprador intenta unirse a la lista de espera
  Then el sistema rechaza la inscripción
  And el sistema informa que la lista de espera de ese evento ya cerró

Scenario: Reinscripción válida después de oportunidad utilizada o expirada
  Given un evento sin disponibilidad inmediata
  And un comprador cuya oportunidad anterior fue utilizada o expiró
  And la lista de espera del evento sigue vigente
  When el comprador solicita unirse nuevamente a la lista de espera
  Then el sistema registra una nueva inscripción activa
  And el sistema confirma que su interés fue registrado nuevamente
```

#### DoR

- Está definido qué significa que un evento no tiene entradas disponibles en el momento de la solicitud.
- Está definido el dato mínimo para identificar al comprador.
- Está definido que cada inscripción corresponde a una sola entrada; no existe inscripción por cantidad.
- Está definido hasta qué momento se acepta una inscripción nueva: la lista cierra al alcanzarse la fecha del evento.
- Está claro el mensaje de confirmación que verá el comprador.
- Está claro el mensaje de rechazo que verá el comprador en cada caso.
- Está definido que la reinscripción es válida solo cuando la inscripción/oportunidad anterior ya no está activa.

#### DoD

- El comprador puede inscribirse exitosamente en lista de espera.
- El sistema evita duplicado para el mismo evento-comprador con inscripción activa.
- La inscripción queda disponible para consulta posterior.
- El sistema rechaza inscripciones cuando la lista de espera del evento ya cerró.
- El sistema permite reinscripción cuando la inscripción u oportunidad anterior ya no está activa.
- QA y negocio pueden validar los cuatro escenarios anteriores.

**Estimación: 3 puntos**

---

### HU2 — Consulta de estado

> Como comprador inscrito, quiero consultar el estado de mi inscripción en la lista de espera, para saber si sigo en espera o ya tengo una oportunidad activa.

#### Revisión INVEST

| Criterio | Análisis |
|---|---|
| **Independent** | La dependencia con HU1 es de datos, no de comportamiento. |
| **Negotiable** | "Consultar" asume que el comprador va a buscar su estado. Si el negocio decide que el sistema debe ser proactivo esta HU podría desaparecer. |
| **Valuable** | Sin esta HU el comprador inscrito no sabe si sigue en espera o ya tiene una oportunidad. |
| **Estimable** | Solo requiere información de estados ya persistidos, sin escritura ni orquestación. |
| **Small** | Solo lectura de estado actual del comprador. |
| **Testable** | Los tres escenarios son sobre ver inscripción activa, oportunidad vigente asociada u oportunidad expirada. |

#### Criterios de aceptación

```gherkin
Scenario: Ver estado de inscripción activa
  Given un comprador inscrito en la lista de espera de un evento
  When el comprador consulta su estado
  Then el sistema muestra su inscripción como activa

Scenario: Ver oportunidad activa asociada a la inscripción
  Given un comprador con una oportunidad activa derivada de la lista de espera
  When el comprador consulta su estado
  Then el sistema muestra que tiene una oportunidad activa
  And el sistema muestra el tiempo restante de vigencia de su oportunidad
  And el sistema muestra que la entrada está reservada temporalmente para ese comprador

Scenario: Ver oportunidad expirada
  Given un comprador cuya oportunidad venció sin ser utilizada
  When el comprador consulta su estado
  Then el sistema muestra que la oportunidad ha expirado
  And el sistema no muestra ninguna reserva temporal activa para ese comprador
```

#### DoR

- Están definidos los estados visibles para el comprador: inscripción activa, oportunidad activa, oportunidad utilizada, oportunidad expirada.
- Está definido que "Oportunidad Activa" significa que la entrada ya quedó reservada temporalmente para ese comprador.
- Está definido que la vigencia se muestra como tiempo restante en minutos.

#### DoD

- El comprador puede consultar su estado actual.
- El sistema distingue claramente entre espera activa, oportunidad activa, oportunidad utilizada y oportunidad expirada.
- La vigencia de la oportunidad es visible como tiempo restante, cuando aplica.
- Negocio y QA validan que los estados sean comprensibles.

**Estimación: 3 puntos**

---

### HU3 — Asignación de oportunidad

> Como sistema, quiero asignar una oportunidad de compra al siguiente comprador elegible cuando una entrada se libere, para aprovechar la demanda pendiente antes de devolverla al inventario general.

#### Revisión INVEST

| Criterio | Análisis |
|---|---|
| **Independent** | La dependencia con HU1 es de datos, no de comportamiento. Requiere que el mecanismo de detección de entradas liberadas esté en funcionamiento antes de implementarse. |
| **Negotiable** | "Asignar a uno" asume exclusividad automática. El negocio podría preferir notificar a varios y dejar que el primero en reaccionar compre, aunque esto cambia por completo el modelo. |
| **Valuable** | Sin esta HU la lista de espera es una base de datos inerte. |
| **Estimable** | La regla de orden de llegada es conocida, la integración con el servicio de reserva existente también. El manejo de fallo de reserva temporal está acotado. |
| **Small** | Excluye notificación y expiración; su única función es dejar una oportunidad activa para el siguiente elegible o no crear nada si falla la reserva. |
| **Testable** | Con elegible y reserva exitosa: oportunidad activa. Con elegible y reserva fallida: no se genera oportunidad, inscripción sigue activa. Sin elegible: la entrada vuelve al inventario general. |

#### Criterios de aceptación

```gherkin
Scenario: Asignación al siguiente comprador elegible
  Given una entrada que acaba de quedar libre en un evento
  And existe al menos un comprador con inscripción activa en la lista de espera de ese evento
  When el sistema procesa la liberación de la entrada
  Then el sistema asigna una oportunidad al siguiente comprador elegible
  And el sistema reserva temporalmente la entrada para ese comprador
  And la oportunidad queda activa con la vigencia configurada
  And la inscripción relacionada queda inactiva

Scenario: Sin compradores en espera
  Given una entrada que acaba de quedar libre en un evento
  And no existen compradores con inscripción activa en la lista de espera de ese evento
  When el sistema procesa la liberación de la entrada
  Then el sistema no genera una oportunidad en lista de espera
  And la entrada vuelve al inventario general disponible para compra directa

Scenario: Fallo al confirmar la reserva temporal
  Given una entrada que acaba de quedar libre en un evento
  And existe al menos un comprador con inscripción activa en la lista de espera de ese evento
  When el sistema procesa la liberación de la entrada
  And la confirmación de la reserva temporal falla
  Then no se crea una oportunidad para ese comprador
  And la inscripción del comprador elegible permanece activa para el siguiente intento
  And el incidente queda registrado para diagnóstico
```

#### DoR

- Está definida la regla inicial de prioridad: orden de llegada.
- Está definido qué significa que una entrada fue liberada: el sistema detecta automáticamente cuando una reserva venció sin pago o un pago fue rechazado.
- Está definido que la oportunidad solo queda activa cuando la reserva temporal fue confirmada exitosamente. Si la confirmación falla, no se genera una oportunidad y la inscripción del comprador permanece activa.
- Está definido el comportamiento cuando la confirmación falla: el comprador sigue en la lista de espera para el próximo intento.
- Está acordado que dos liberaciones simultáneas del mismo evento no pueden asignarse al mismo comprador; el bloqueo se resuelve con la restricción única de una oportunidad activa por inscripción.

#### DoD

- El sistema puede detectar automáticamente cuando una entrada se libera y es relevante para la lista de espera.
- El sistema selecciona correctamente al siguiente comprador elegible por orden de llegada.
- La oportunidad queda activa con la vigencia configurada solo cuando la reserva temporal fue confirmada exitosamente. Si falla, no se crea ninguna oportunidad para ese comprador.
- La oportunidad activa implica que la entrada quedó reservada temporalmente para ese comprador.
- La inscripción relacionada queda inactiva una vez que la oportunidad se activa, permitiendo una futura reinscripción del comprador.
- Cuando no hay elegibles, la entrada vuelve al inventario general disponible para compra directa.
- El sistema no genera oportunidades sin respaldo en el inventario.
- QA valida casos con elegible exitoso, con elegible y fallo de reserva, y sin comprador en lista de espera.

**Estimación: 8 puntos**

---

### HU4 — Notificación in-app

> Como comprador con una oportunidad activa, quiero ser notificado dentro de la aplicación, para enterarme a tiempo de que ya puedo intentar completar la compra.

#### Revisión INVEST

| Criterio | Análisis |
|---|---|
| **Independent** | Consume estado producido por HU3 sin modificarlo; no hay acoplamiento de comportamiento con otra HU. |
| **Negotiable** | "Dentro de la aplicación" define el canal, no la forma. Un banner, un badge o una sección de notificación son opciones negociables. |
| **Valuable** | Sin esta HU un comprador presente en la aplicación no se entera de su oportunidad sin refrescar manualmente. |
| **Estimable** | El mecanismo de notificación en tiempo real, el hub de notificación y la suscripción del frontend son componentes conocidos con referencia en el sistema existente. |
| **Small** | Excluye canal de correo y expiración. |
| **Testable** | Se verifica que el front recibe la actualización sin recarga al activarse la oportunidad y que el estado visible es consistente con el sistema. |

#### Criterios de aceptación

```gherkin
Scenario: Visualización de oportunidad activa dentro de la aplicación
  Given un comprador con una oportunidad activa
  And el comprador tiene abierta la aplicación
  When el estado de su oportunidad es actualizado
  Then el sistema muestra dentro de la aplicación que tiene una oportunidad activa
  And el sistema muestra el tiempo restante de vigencia de esa oportunidad
  And el sistema muestra que existe una reserva temporal asociada a esa oportunidad

Scenario: Consulta posterior del estado si el comprador sigue navegando
  Given un comprador con una oportunidad activa derivada de la lista de espera
  When el comprador consulta nuevamente su estado dentro de la aplicación
  Then el sistema muestra que la oportunidad sigue activa mientras no haya vencido
```

#### DoR

- Está definido qué mensaje verá el comprador en la aplicación.
- Están definidos los estados que deben reflejarse visualmente: oportunidad activa con tiempo restante.
- Está acordado que la vigencia se muestra como tiempo restante en minutos.
- Está acordado que la notificación in-app debe expresar que la oportunidad activa ya tiene una reserva temporal asociada.

#### DoD

- El comprador puede enterarse dentro de la aplicación que tiene una oportunidad activa sin recargar la página.
- La información mostrada es consistente con el estado real del sistema.
- La notificación in-app deja claro que la entrada está retenida temporalmente para ese comprador.
- Negocio y QA validan que la comunicación in-app sea suficientemente clara.

**Estimación: 5 puntos**

---

### HU5 — Notificación por correo

> Como comprador con una oportunidad activa, quiero recibir un correo electrónico cuando se me asigne esa oportunidad, para enterarme aunque no esté dentro de la aplicación.

#### Revisión INVEST

| Criterio | Análisis |
|---|---|
| **Independent** | El canal de correo se aisla como mecanismo independiente; puede construirse o reemplazarse sin afectar HU3 ni HU4. |
| **Negotiable** | El canal es conversable; por ejemplo, SMS podría sustituir el correo. Lo que no cambia es que el aviso externo no es fuente oficial del estado. |
| **Valuable** | Sin esta HU un comprador fuera de línea pierde la oportunidad porque no recibe aviso por ningún canal externo. |
| **Estimable** | Integración con proveedor externo, manejo de fallos y trazabilidad de intentos son conocidos. |
| **Small** | Todos los componentes nuevos sirven a una sola cosa: avisar por correo cuando la oportunidad se activa. |
| **Testable** | Se verifica que el correo se genera con contenido acordado y que el intento queda registrado como enviado o fallido. |

#### Criterios de aceptación

```gherkin
Scenario: Envío de correo cuando se activa una oportunidad
  Given un comprador con una dirección de correo válida
  And una oportunidad activa recién asignada
  When el sistema activa la oportunidad
  Then el sistema envía un correo electrónico al comprador
  And el correo informa que existe una oportunidad activa de compra
  And el correo informa que la entrada está reservada temporalmente durante 15 minutos

Scenario: Contenido mínimo del correo
  Given un comprador con una oportunidad activa derivada de la lista de espera
  When el sistema genera el correo electrónico
  Then el correo incluye la referencia del evento
  And el correo informa que la oportunidad tiene una vigencia de 15 minutos
  And el correo orienta al comprador a revisar el estado dentro del sistema
  And el correo deja claro que es un aviso y no la fuente oficial del estado

Scenario: Fallo en el envío del correo
  Given una oportunidad activa recién asignada
  When el proveedor de correo devuelve un error
  Then el sistema registra el intento con resultado fallido
  And la oportunidad permanece activa independientemente del fallo del correo
```

#### DoR

- Está definido el contenido mínimo del correo: referencia del evento, vigencia de 15 minutos, instrucción para consultar el estado dentro del sistema.
- Está acordada la acción exacta que dispara el envío: activación de la oportunidad.
- Está clara la regla sobre qué dirección de correo usar: la asociada al comprador en el sistema.
- Está acordado que el correo es un canal de aviso y no la fuente oficial del estado.
- Está acordado que un fallo en el envío no debe afectar el estado de la oportunidad.

#### DoD

- El sistema genera el correo cuando una oportunidad se activa.
- El correo contiene la información mínima acordada.
- El correo remite al comprador a consultar el estado dentro del sistema.
- El correo deja claro que es un aviso y no reemplaza el estado oficial.
- El intento de envío queda auditado con resultado y momento, tanto en caso de éxito como de fallo.
- QA y negocio validan el contenido y el momento del envío.

**Estimación: 8 puntos**

---

### HU6 — Expiración de oportunidad

> Como sistema, quiero expirar una oportunidad dentro del tiempo definido, para mantener la disponibilidad dinámica y continuar con la reasignación o liberación según la política del negocio.

#### Revisión INVEST

| Criterio | Análisis |
|---|---|
| **Independent** | Consume oportunidades de HU3, pero la lógica de vencimiento y reasignación es autónoma y no modifica el flujo de HU3. |
| **Negotiable** | La expiración automática es la solución propuesta; reutiliza el mecanismo ya probado en la expiración de reservas. Podría ser manual o asistida si el negocio lo prefiriera. |
| **Valuable** | Sin esta HU una oportunidad no utilizada bloquea una entrada indefinidamente, paralizando la reasignación de demanda. |
| **Estimable** | La reasignación reutiliza el flujo de HU3; los pasos son conocidos una vez estimada HU3. |
| **Small** | Limitada al ciclo de vida de oportunidades no usadas; no incluye notificar la expiración ni modificar la lógica de asignación original. |
| **Testable** | Tres escenarios independientes: expiración de estado, reasignación con elegible y devolución sin elegible. |

#### Criterios de aceptación

```gherkin
Scenario: Expiración automática por vencimiento
  Given una oportunidad activa que no fue utilizada dentro del tiempo de vigencia
  When el tiempo de vigencia se cumple
  Then el sistema marca la oportunidad como expirada
  And el vencimiento queda registrado con motivo y momento exacto

Scenario: Reasignación inmediata al siguiente comprador elegible
  Given una oportunidad activa que acaba de expirar
  And existe otro comprador con inscripción activa en la lista de espera del mismo evento
  When el sistema procesa la expiración
  Then el sistema libera la entrada previamente retenida
  And el sistema asigna una nueva oportunidad al siguiente comprador elegible
  And la inscripción del comprador cuya oportunidad expiró queda inactiva, habilitando reinscripción futura

Scenario: Devolución al inventario cuando no hay más compradores
  Given una oportunidad activa que acaba de expirar
  And no existe comprador con inscripción activa en la lista de espera del mismo evento
  When el sistema procesa la expiración
  Then la entrada vuelve al inventario general disponible para compra directa
  And no queda ninguna reserva asociada a esa entrada
```

#### DoR

- Está definida la vigencia de la oportunidad: 15 minutos desde la activación, configurable sin necesidad de un despliegue.
- Está definido que la expiración es automática: el sistema detecta el vencimiento y lo procesa sin intervención manual ni revisión periódica.
- Está definido qué significa "utilizada": el comprador avanzó a la pantalla de pago y confirmó la compra desde la oportunidad activa.
- Está definida la política posterior al vencimiento: la entrada queda libre, se intenta reasignación inmediata; si no hay siguiente comprador, vuelve al inventario general.
- Está definido que la inscripción del comprador cuya oportunidad expiró queda inactiva, habilitando reinscripción.

#### DoD

- La oportunidad cambia correctamente a expirada cuando vence.
- El vencimiento queda trazable con motivo y momento.
- La inscripción relacionada queda inactiva cuando la oportunidad expira, permitiendo una futura reinscripción del comprador.
- La reasignación reutiliza el mismo flujo de HU3 sin duplicar lógica.
- QA puede validar expiración y la transición posterior en los dos escenarios posibles.

**Estimación: 5 puntos**

---

### HU7 — Inscripción y disponibilidad en la aplicación

> Como comprador, quiero ver la opción de lista de espera e inscribirme directamente desde la aplicación cuando un evento no tiene disponibilidad, para mantener mi interés sin salir de la experiencia del producto.

#### Revisión INVEST

| Criterio | Análisis |
|---|---|
| **Independent** | Los flujos de inscripción y validación ya están resueltos por HU1. Esta HU es independiente en su entregable: la interfaz de inscripción. |
| **Negotiable** | El diseño visual y la disposición de los elementos son negociables. Lo que no cambia es que el comprador debe poder inscribirse desde la aplicación. |
| **Valuable** | Sin esta HU el comprador no tiene manera de inscribirse en la lista de espera ni de saber si la opción existe. |
| **Estimable** | La pantalla es conocida: página del evento con opción condicional de lista de espera. |
| **Small** | Solo cubre inscripción, rechazo por duplicado y lista cerrada. No incluye consulta de estado ni acción sobre oportunidad. |
| **Testable** | Se puede verificar que el comprador ve la opción correcta según el estado del evento y que la inscripción funciona desde la aplicación. |

#### Criterios de aceptación

```gherkin
Scenario: El comprador ve la opción de lista de espera cuando no hay disponibilidad
  Given un evento sin disponibilidad inmediata
  And la lista de espera del evento sigue vigente
  When el comprador navega a la página del evento en la aplicación
  Then la aplicación muestra la opción de unirse a la lista de espera
  And la aplicación no muestra la opción de compra directa

Scenario: El comprador se inscribe en la lista de espera desde la aplicación
  Given un evento sin disponibilidad inmediata
  And el comprador no tiene inscripción activa para ese evento
  When el comprador solicita unirse a la lista de espera desde la aplicación
  Then la aplicación confirma que el interés del comprador fue registrado
  And la aplicación muestra la inscripción como activa

Scenario: La aplicación no muestra la opción de lista de espera cuando el evento ya cerró
  Given un evento cuya fecha ya fue alcanzada
  When el comprador navega a la página del evento en la aplicación
  Then la aplicación no muestra la opción de unirse a la lista de espera
  And la aplicación informa que la lista de espera de ese evento ya cerró

Scenario: El comprador ve que su inscripción fue rechazada por duplicado desde la aplicación
  Given un comprador ya inscrito activamente en la lista de espera de un evento
  When el comprador intenta unirse nuevamente desde la aplicación
  Then la aplicación informa que ya existe una inscripción activa
  And la aplicación no crea una inscripción adicional
```

#### DoR

- Están definidos los flujos de inscripción que el comprador debe poder completar desde la aplicación.
- Está definido qué ve el comprador según el estado del evento: sin disponibilidad con lista vigente, lista cerrada, inscripción activa existente.
- Está definido que la información visible en la aplicación debe ser coherente con el estado real del sistema en todo momento.
- Están disponibles los endpoints de backend que soportan los flujos de HU1.

#### DoD

- El comprador puede inscribirse en la lista de espera desde la aplicación cuando hay un evento sin disponibilidad inmediata.
- La aplicación no ofrece la opción de lista de espera cuando el evento ya cerró.
- La aplicación rechaza visualmente el duplicado de inscripción sin crear registros adicionales.
- La información visible es coherente con el estado real del sistema.
- QA y negocio validan cada flujo desde la perspectiva del comprador en la aplicación.

**Estimación: 5 puntos**

---

### HU8 — Consulta de estado y acción sobre oportunidad en la aplicación

> Como comprador inscrito, quiero consultar mi estado en la lista de espera y actuar sobre una oportunidad activa directamente desde la aplicación, para completar el flujo de compra sin salir de la experiencia del producto.

#### Revisión INVEST

| Criterio | Análisis |
|---|---|
| **Independent** | Los flujos de consulta y asignación ya están resueltos por HU2 y HU3. Esta HU es independiente en su entregable: la interfaz de estado y acción. |
| **Negotiable** | La forma de presentar el tiempo restante y la acción de pago son negociables. Lo que no cambia es que el comprador debe poder ver su estado y actuar sobre una oportunidad. |
| **Valuable** | Sin esta HU el comprador inscrito no puede ver su estado ni actuar sobre una oportunidad desde la aplicación. |
| **Estimable** | La pantalla es conocida: vista de estado del comprador con acción para avanzar al pago. |
| **Small** | Solo cubre consulta de estado y acción sobre oportunidad activa. No incluye inscripción ni disponibilidad del evento. |
| **Testable** | Se puede verificar que el comprador ve su estado actualizado y que puede avanzar al pago desde una oportunidad activa. |

#### Criterios de aceptación

```gherkin
Scenario: El comprador consulta su estado en la lista de espera desde la aplicación
  Given un comprador inscrito en la lista de espera de un evento
  When el comprador consulta su estado desde la aplicación
  Then la aplicación muestra el estado actual de su inscripción sin ambigüedad
  And la información es coherente con el estado real del sistema

Scenario: El comprador ve una oportunidad activa con tiempo restante
  Given un comprador con una oportunidad activa
  And la oportunidad tiene tiempo restante de vigencia
  When el comprador consulta su estado desde la aplicación
  Then la aplicación muestra que tiene una oportunidad activa
  And la aplicación muestra el tiempo restante de vigencia
  And la aplicación muestra la acción para avanzar al pago

Scenario: El comprador actúa sobre una oportunidad activa desde la aplicación
  Given un comprador con una oportunidad activa
  And la oportunidad tiene tiempo restante de vigencia
  When el comprador decide avanzar al pago desde la aplicación
  Then la oportunidad pasa a estado utilizada
  And el comprador es dirigido al flujo de pago con la entrada reservada temporalmente para él
  And la aplicación deja de mostrar la oportunidad como activa

Scenario: El comprador ve que su oportunidad expiró
  Given un comprador cuya oportunidad venció sin ser utilizada
  When el comprador consulta su estado desde la aplicación
  Then la aplicación muestra que la oportunidad ha expirado
  And la aplicación no muestra acción de pago
```

#### DoR

- Están definidos los estados visibles en la aplicación: inscripción activa, oportunidad activa con tiempo restante, oportunidad utilizada, oportunidad expirada.
- Está definido qué significa "actuar sobre una oportunidad activa": el comprador avanza al flujo de pago desde esa oportunidad, y en ese momento la oportunidad pasa a estado utilizada.
- Está definido que la información visible en la aplicación debe ser coherente con el estado real del sistema en todo momento.
- Están disponibles los endpoints de backend que soportan los flujos de HU2 y HU3.

#### DoD

- El comprador puede consultar su estado actual desde la aplicación y distinguir entre inscripción activa, oportunidad activa, oportunidad utilizada y oportunidad expirada.
- El comprador puede actuar sobre una oportunidad activa desde la aplicación, avanzando al flujo de pago; en ese momento la oportunidad pasa a utilizada.
- La aplicación muestra el tiempo restante de vigencia cuando el comprador tiene una oportunidad activa.
- La información visible es coherente con el estado real del sistema.
- QA y negocio validan cada flujo desde la perspectiva del comprador en la aplicación.

**Estimación: 5 puntos**

---

## Análisis de Impacto Arquitectónico

En esta sección se busca responder inquietudes sobre capacidades del sistema que cambian o se incorporan, nuevos conceptos de dominio y qué compromisos de diseño garantizan que la arquitectura pueda sostener esos cambios sin crear deuda técnica adicional a la que exista.

### Capacidades que se amplían o incorporan en el sistema

- El sistema pasa de ignorar la demanda no atendida a retenerla y reactivarla.
- La liberación de una entrada deja de ser un evento silencioso: se convierte en un disparador que activa la lista de espera.
- El comprador adquiere visibilidad en su lugar en la fila de espera, sin depender de soporte o atención al cliente.
- La comunicación con el comprador se extiende a dos canales complementarios.
- El mecanismo de reserva existente no cambia; la lista de espera lo usa a través de su interfaz ya existente.

### Cambios requeridos en servicios existentes

- **ReservationService:** publicar `ticket.released` al expirar una reserva y el ticket pasa a `released` (publicación adicional a la existente de `ticket.status.changed`).
- **paymentService:** publicar `ticket.released` cuando un pago es rechazado y el ticket pasa a `released` (publicación adicional a la existente de `ticket.status.changed`).
- **`scripts/setup-rabbitmq.sh`:** declarar las colas `q.ticket.released`, `q.waitlist.opportunity.delay` (con TTL y DLX) y `q.waitlist.opportunity.expired` con sus bindings sobre el exchange `tickets` existente.
- **Reserva temporal:** la lógica de reserva por lista de espera opera sobre tickets en estado `released` (`WHERE status = 'released'`), mientras que la reserva directa sigue operando exclusivamente sobre `available` (`WHERE status = 'available'`). El flujo de compra existente no se modifica.

> Estos tres cambios son prerequisito antes de implementar HU3.

### Contrato del evento de liberación

Esta épica requiere que ReservationService y paymentService publiquen el evento `ticket.released` cuando una entrada queda disponible nuevamente. Esta publicación es **adicional** a la ya existente de `ticket.status.changed` (que alimenta el SSE del frontend); son eventos independientes con consumidores distintos. Este es el contrato mínimo que activa la lista de espera:

- **Routing key:** `ticket.released`
- **Exchange:** `tickets` (ya existente)
- **Quién publica:**
  - ReservationService: cuando una reserva expira y el ticket pasa a `released` (en `TicketExpiredConsumer`, al lado de la publicación existente de `ticket.status.changed`).
  - paymentService: cuando un pago es rechazado y el ticket pasa a `released` (en `PaymentRejectedEventHandler`, al lado de la publicación existente de `ticket.status.changed`).
- **Payload mínimo:**

```json
{ "ticketId": int, "eventId": int, "releasedAt": datetime }
```

> La cola `q.ticket.released` y su binding deben agregarse en `scripts/setup-rabbitmq.sh` antes de que esta feature entre a `develop`.

### Topología de expiración de oportunidades (DLX)

La expiración de oportunidades de lista de espera reutiliza el patrón TTL + Dead Letter Exchange (DLX) que ya funciona para la expiración de reservas (`q.ticket.reserved.delay` → `q.ticket.expired`). La topología nueva es:

- **Cola delay:** `q.waitlist.opportunity.delay`
  - `x-message-ttl`: configurable por variable de entorno `WAITLIST_OPPORTUNITY_TTL_MS` (default: `900000` = 15 min)
  - `x-dead-letter-exchange`: `tickets`
  - `x-dead-letter-routing-key`: `waitlist.opportunity.expired`
- **Cola consumer:** `q.waitlist.opportunity.expired` (binding: `waitlist.opportunity.expired`)
- **Consumer:** `WaitlistOpportunityExpiredConsumer` en el CRUD Service

El mensaje al delay queue se publica **cuando la oportunidad transiciona a `active`** (no cuando se crea como `pending`), para que el TTL cuente desde la activación real.

> Estas colas también deben declararse en `scripts/setup-rabbitmq.sh`.

### Nuevos conceptos de dominio

- Inscripción en lista de espera
- Oportunidad de compra
- Registro de notificación

### Puntos de integración que se agregan

- **Registro de demanda:** el comprador puede declarar su interés desde la misma experiencia donde intentó comprar, sin salir del flujo.
- **Consulta de estado:** el comprador puede saber en cualquier momento en qué punto del proceso se encuentra, sin esperar a que el sistema lo contacte.
- **Canal en tiempo real:** si el comprador está navegando cuando su oportunidad se activa, el aviso llega sin que tenga que recargar la página.
- **Canal de correo:** si el comprador no está en la aplicación, recibe un aviso externo que lo invita a actuar, dejando claro que el estado oficial vive en la plataforma.
- **Evento de dominio `ticket.released`:** nuevo contrato entre ReservationService/paymentService y el CRUD Service para activar la lista de espera.

### Patrones de diseño que ayudan a sostener las decisiones de negocio

| Patrón | Justificación |
|---|---|
| **Observer** | Cuando el estado de una oportunidad cambia, el sistema reacciona en múltiples canales sin que esa reacción quede acoplada a quien tomó la decisión. Si el negocio agrega un canal en el futuro, simplemente se añade como nuevo suscriptor, sin modificar la lógica de asignación. |
| **Strategy** | La política de "a quién le toca" se puede cambiar sin deshacer el proceso de asignación. Hoy es orden de llegada; mañana puede ser otro criterio sin afectar el resto del sistema. |
| **State** | La oportunidad tiene estados con reglas claras de transición (`pending` → `active` → `consumed`/`expired`, y `pending` → `failed`). Si el negocio quiere agregar un estado intermedio, el modelo lo soporta sin condicionales dispersos que dificulten diagnósticos. |
| **Command** | Las acciones relevantes para el negocio (registrar interés, expirar oportunidad, enviar aviso) existen como objetos formales con su propio handler. Esto facilita trazabilidad y auditoría. |

### Herramientas de soporte que pueden ayudar a cubrir reglas de negocio

> Se mencionan algunas herramientas; pueden existir otras con el mismo propósito. Lo que se busca mostrar es que existe al menos una herramienta que cubre cada necesidad.

| Herramienta | Necesidad que cubre |
|---|---|
| **FluentValidation** | La regla de inscripción se valida de forma consistente y visible, aunque también puede evaluarse si parte de esa validación corresponde al dominio como regla de negocio. |
| **OpenTelemetry** | Cuando Soporte quiera saber qué pasó con una oportunidad que cruzó varios servicios y canales, hay una traza que responde sin depender de logs dispersos. |
| **Amazon SES** | Cada correo enviado queda registrado con su resultado. Si un aviso falla, el sistema lo sabe y puede reportarlo; no se pierde en silencio. |
| **Expiración automática por mensajería** | El vencimiento de una oportunidad lo detecta y procesa el sistema automáticamente, sin revisión periódica ni intervención manual. Cuando el tiempo configurado vence, el sistema recibe la señal y actúa de inmediato. El tiempo de vigencia es ajustable por configuración sin necesidad de modificar código ni hacer un despliegue. |
| **PostgreSQL índices parciales** | El sistema garantiza en la base de datos que nadie puede tener dos inscripciones activas en el mismo evento, sin dejar esa responsabilidad únicamente al código de la aplicación. |
| **GitHub Actions (CI)** | Cada push y cada pull request hacia `develop` o `main` ejecuta automáticamente las suites de prueba relevantes. El pipeline actual ya corre build, pruebas unitarias, de componente, de integración, de caja negra y escaneo de seguridad de imágenes Docker. Las suites nuevas de esta épica se integran al mismo pipeline sin crear uno paralelo. |

### Compromisos arquitectónicos con impacto directo en el negocio

**El estado de la oportunidad no vive en el correo.**
Si el negocio o soporte llegaran a usar el correo como referencia oficial del estado, aparecen inconsistencias inevitables cuando el mensaje llega tarde, falla o el proveedor tiene una caída. El sistema es la única fuente de verdad; el correo es un aviso.

**Las notificaciones se registran siempre, con cualquier resultado.**
Un intento de notificación que falla en silencio crea una zona ciega para Soporte. Registrar cada intento convierte un problema de canal en un dato consultable.

**La integración con el mecanismo de reserva existente es de uso, no de modificación.**
La lista de espera aprovecha la reserva temporal existente sin reescribirla. Esto protege el comportamiento ya validado del flujo base de compra y limita el riesgo de esta feature al código nuevo.

**La vigencia de 15 minutos es un parámetro de negocio, no una constante en código.**
Debe configurarse mediante variable de entorno o tabla de configuración para que el negocio pueda ajustarlo sin un despliegue.

**El fallo en la confirmación de la reserva no genera oportunidades sin respaldo.**
Si el sistema no puede confirmar la reserva temporal para el comprador elegible, no crea la oportunidad y la inscripción permanece activa. Esto evita que el comprador quede con una oportunidad que no puede usar.

### Decisión sobre la base de datos compartida

Actualmente los cuatro servicios (Producer, ReservationService, paymentService, CRUD Service) operan contra una sola instancia de PostgreSQL con un único esquema. En una arquitectura de microservicios, lo natural sería que cada servicio tuviera su propia base de datos para aislar responsabilidades y evitar acoplamiento por datos compartidos. Esta épica agrega tres tablas nuevas (`waitlist_entries`, `waitlist_opportunities`, `notification_deliveries`) que pertenecen conceptualmente al bounded context de lista de espera, y eso refuerza la pregunta de si es momento de separar.

**Decisión: se mantiene la base de datos única para esta épica.**

Las razones son pragmáticas, no de principio:

1. **El equipo ya tiene deuda técnica identificada** y agregar una separación de bases de datos en este momento multiplica la superficie de cambio sin que esta épica lo necesite para funcionar correctamente.
2. **Las tablas nuevas no entran en conflicto con las existentes.** No comparten columnas, no se escriben desde múltiples servicios de forma concurrente (solo el CRUD Service las lee y escribe) y las foreign keys hacia `events` y `tickets` son de lectura, no de escritura cruzada.
3. **El costo operativo de múltiples bases de datos** (migraciones independientes, conexiones separadas en compose, backups, monitoreo) no se justifica cuando el sistema todavía cabe en una sola instancia sin problemas de rendimiento ni de propiedad de datos ambigua.

#### Consideraciones para futuras features

Esta decisión no es permanente. Si una próxima feature introduce un bounded context con requisitos de aislamiento real (por ejemplo, usuarios con autenticación, datos sensibles con regulación distinta, o un servicio que necesite escalar su almacenamiento de forma independiente), la separación se vuelve necesaria. Para que ese momento no sea traumático, esta épica respeta las siguientes restricciones:

- **Las tablas nuevas solo las lee y escribe el CRUD Service.** Ningún otro servicio accede directamente a `waitlist_entries`, `waitlist_opportunities` ni `notification_deliveries`. Si otro servicio necesita esa información, la obtiene por API o por evento, nunca por consulta directa a la base de datos.
- **Los servicios existentes no adquieren dependencias nuevas sobre las tablas de lista de espera.** ReservationService y paymentService solo publican `ticket.released`; no consultan ni escriben tablas de lista de espera.
- **Las migraciones de esquema de esta épica están aisladas en su propio script.** No se mezclan con alteraciones a las tablas existentes (`events`, `tickets`, `payments`, `ticket_history`), para que una futura separación pueda extraerlas sin desenredar DDL compartido.
- **No se crean vistas ni funciones que crucen datos de lista de espera con datos del flujo base de compra.** Los joins, si son necesarios para reportes o consultas de negocio, se resuelven en la capa de aplicación, no en la base de datos.

Si en el futuro se decide separar, el camino probable sería:

1. Crear una instancia de PostgreSQL dedicada para el nuevo bounded context.
2. Migrar las tablas correspondientes con su esquema y datos.
3. Reemplazar las foreign keys por referencias lógicas (IDs sin constraint de FK) y validar la integridad en la capa de aplicación o mediante eventos.
4. Actualizar el connection string del servicio afectado en `compose.yml`.

El hecho de que las tablas nuevas ya estén aisladas por acceso (solo un servicio las toca) y por esquema (script separado, sin vistas cruzadas) hace que esa migración futura sea mecánica, no arquitectónica.

### Patrones de orquestación y resiliencia: cuándo se vuelven necesarios

Con una base de datos única y comunicación asíncrona por RabbitMQ, el sistema actual no necesita patrones de orquestación distribuida ni de resiliencia avanzada. Pero la separación de bases de datos o la incorporación de nuevos bounded contexts (usuarios, autenticación, pagos con proveedores externos con SLA propio) cambian las condiciones. Esta sección documenta qué patrones serían necesarios y bajo qué condiciones, para que la decisión no se tome reactivamente cuando ya hay un problema en producción.

#### Orquestación: Saga

Hoy la lista de espera coordina varios pasos (asignar oportunidad → reservar ticket → notificar) dentro de un solo servicio (CRUD Service) con una sola base de datos. Si esos pasos fueran a ocurrir en servicios distintos con bases de datos separadas, ya no existe una transacción ACID que los agrupe. En ese escenario, el patrón Saga permite coordinar la secuencia con compensaciones explícitas cuando un paso falla.

**Condiciones que activarían la necesidad:**

- La reserva temporal pasa a ejecutarse en un servicio con su propia base de datos, y la oportunidad de lista de espera vive en otra. Si la reserva falla después de que la oportunidad se marcó como activa, no hay rollback automático: hace falta una compensación que revierta la oportunidad.
- Se incorpora un servicio de usuarios o autenticación que valide la identidad del comprador antes de permitir la reserva. Si esa validación es un paso del flujo que puede fallar de forma independiente, entra en el alcance de la Saga.

**Lo que esta épica deja preparado:** los pasos del flujo de asignación ya están separados lógicamente (buscar elegible → intentar reserva → crear oportunidad → notificar). Si en el futuro cada paso lo ejecuta un servicio distinto, la secuencia lógica ya existe; lo que falta es agregar los eventos de compensación (por ejemplo, `opportunity.rollback` si la reserva falla después del commit de la oportunidad).

**Variante relevante:** la Saga coreografiada (cada servicio reacciona a eventos del anterior sin un coordinador central) es más coherente con la arquitectura actual basada en eventos que una Saga orquestada con un coordinador centralizado. La decisión final depende de cuántos pasos y servicios participen cuando se concrete la separación.

#### Resiliencia: Circuit Breaker, Retry, Timeout

Hoy los servicios se comunican por mensajería asíncrona (RabbitMQ), lo que absorbe gran parte de los problemas de disponibilidad transitoria: si un consumer está caído, los mensajes esperan en la cola. Pero hay puntos donde las llamadas son síncronas o donde una dependencia externa puede degradar el sistema:

- **Proveedor de correo (Amazon SES):** la HU5 ya maneja el fallo del proveedor aislándolo del flujo principal (el correo falla sin afectar la oportunidad). Eso es suficiente hoy, pero si en el futuro se agregan más canales externos (SMS, push notifications) o el volumen crece, un Circuit Breaker sobre el cliente de correo evitaría que reintentos acumulados saturen el servicio.
- **Llamadas HTTP entre servicios:** actualmente no hay llamadas HTTP síncronas entre servicios en el flujo de lista de espera (todo pasa por RabbitMQ). Si la separación de bases de datos introduce la necesidad de consultar APIs de otro servicio de forma síncrona (por ejemplo, consultar disponibilidad de tickets en tiempo real a ReservationService), un Circuit Breaker + Retry con backoff exponencial protege al llamador de cascadas de fallo.
- **Base de datos:** si se separan las bases de datos y un servicio necesita datos de otro por API, un timeout mal calibrado puede bloquear hilos. Definir timeouts explícitos y políticas de degradación (devolver un estado parcial o un error controlado en lugar de esperar indefinidamente) se vuelve obligatorio.

**Lo que esta épica deja preparado:** el flujo de lista de espera no introduce llamadas síncronas entre servicios. La comunicación es por eventos y la única dependencia externa (correo) ya tiene aislamiento de fallo. Esto significa que no hay deuda de resiliencia que pagar antes de la separación, pero establece la expectativa de que cualquier feature futura que introduzca llamadas síncronas entre servicios debe incluir Circuit Breaker y políticas de retry como parte de su propio DoR.
