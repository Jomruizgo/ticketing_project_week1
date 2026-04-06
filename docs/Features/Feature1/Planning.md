Planificación de Feature de Alta Complejidad

Objetivo
Este documento tiene como objetivo presentar el plan de implementación de una feature de alta complejidad, o también conocida como épica.


Desarrollo

Feature: Como comprador interesado, quiero unirme a una lista de espera para un evento con tickets agotados o sin disponibilidad inmediata, para tener prioridad cuando un ticket se libere.

Problema que resuelve: Hoy, cuando un evento se queda sin disponibilidad inmediata, los compradores interesados no tienen una manera formal de mantener su intensión de compra ni de enterarse oportunamente si una entrada  (ticket) vuelve a estar disponible. Si luego una entrada se libera, esa oportunidad puede perderse porque el comprador no estaba mirando la aplicación en ese momento. Esta feature busca conservar la demanda interesada, asignar prioridad cuando una entrada vuelva a quedar libre y comunicar de forma clara dentro de la aplicación y por correo electrónico, para aumentar la posibilidad real de conversión.

Contexto y supuestos
El negocio ya permite que una entrada vuelva a quedar libre cuando una reserva vence o una compra no se confirma.
Existe una oportunidad clara de no perder demanda cuando el inventario se mueve de nuevo.
La lista de espera operará inicialmente a nivel de evento.
La asignación inicial seguirá una regla simple como orden de llegada.
La notificación no debe depender solo de que el comprador esté conectado a la aplicación; también debe existir una comunicación por correo electrónico.
El correo electrónico se entiende como un canal para avisar que existe una oportunidad activa, no como reemplazo de estado visible dentro de la experiencia del producto.

Alcance
Qué incluye esta épica:
Un comprador puede registrar su interés en un evento sin disponibilidad inmediata; la inscripción corresponde a una sola entrada.
Cuando una entrada queda libre, el sistema asigna una oportunidad al siguiente comprador elegible según orden de llegada.
El comprador puede enterarse de su oportunidad dentro de la aplicación, en tiempo real.
El comprador recibe una notificación por correo electrónico como canal de aviso complementario.
La oportunidad tiene un estado visible, trazable y coherente para el comprador, soporte y negocio.
Una vez que la oportunidad fue utilizada o expiró, el comprador puede volver a inscribirse mientras la lista siga vigente.
La lista de espera cierra al alcanzarse la fecha del evento; después no se aceptan inscripciones ni se asignan oportunidades.

No se incluye:
Cancelación voluntaria de la inscripción.
Personalización avanzada del contenido del correo más allá de la información esencial para actuar.
El correo como fuente oficial del estado: el estado real vive dentro del sistema.
Redefinición del proceso base de compra; esta épica lo extiende sin modificarlo.
Estrategias de automatización comercial multicanal.
Priorización por criterios diferentes al orden de llegada.
Lista de espera segmentada por tipo o categoría de ticket.
Pre-automatización de pago durante el tiempo de espera.

Palabras clave o lenguaje de negocio:
Sin disponibilidad: el evento no tiene entradas que se puedan adquirir en ese momento.
Oportunidad activa: la entrada quedó reservada temporalmente para ese comprador y la vigencia de esa reserva está corriendo.
Oportunidad utilizada: el comprador ya avanzó desde la oportunidad activa a la siguiente acción válida del flujo de compra.
Oportunidad expirada: la vigencia terminó sin que el comprador haya actuado.
Fuente oficial del estado: el estado real de la oportunidad vive dentro del sistema; el correo es solo un canal de aviso.
Política de liberación: si una oportunidad vence, la entrada se libera e inmediatamente el sistema intenta asignarla al siguiente comprador en espera; si no hay nadie más, la entrada vuelve al inventario sin prioridad especial.
Reinscripción: inscripción nueva que un comprador realiza después de que su oportunidad anterior fue utilizada o expiró. Es válida mientras la lista de espera del evento esté vigente.
Vigencia de la lista de espera: la lista acepta inscripciones y genera oportunidades solo hasta la fecha del evento. Al alcanzarse esa fecha, la lista cierra y las inscripciones activas sin oportunidad se cierran sin asignación.


Refinamiento en Historias de Usuario

HU1: Como comprador, quiero inscribirme en la lista de espera de un evento sin disponibilidad inmediata, para mantener mi intención de compra dentro del sistema.

Revisión INVEST

Independent
Puede construirse sin depender de otras cosas como notificación por ejemplo.
Negotiable
El objetivo real es conservar el interés del comprador, y hay otras formas de lograrlo.
Valuable
Aprovecha demanda que actualmente se puede perder
Estimable
alcance conocido sin dependencias externas
Small
solo entrar a la lista y evitar duplicados
Testable
el registro se crea o se rechaza si la lista no está cerrada


Criterios de aceptación en Gherkin:
Scenario: Inscripción exitosa cuando no hay disponibilidad inmediata
Given un evento sin disponibilidad inmediata
And un comprador no inscrito previamente a la lista de espera de ese evento
When el comprador solicita unirse a la lista de espera
Then el sistema registra su inscripción como ativa
And el sistema confirma que su interés fue registrado

Scenario: Prevención de inscripción duplicada
Given un evento sis disponibilidad inmediata
And un comprador ya inscrito activamente en la lista de espera de ese evento
When el comprador intenta unirse nuevamente a la lista de espera
Then el sistema rechaza la inscripción duplicada
And el sistema informa que ya existe una inscripción activa

Scenario: Lista de espera no disponible
Given un evento cuya fecha ya fue alcanzada
When el comprador intenta unirse a la lista de espera
Then el sistema rechaza la inscripción
And el sistema informa que la lista de espera de ese evento ya cerró


DoR
+ Está definido qué significa que un evento no tiene entradas disponibles en el momento de la solicitud.
+ Está definido el dato mínimo para identificar al comprador.
+ Está definido que cada inscripción corresponde a una sola entrada; no existe inscripción por cantidad.
+ Está definido hasta qué momento se acepta una inscripción nueva: la lista cierra al alcanzarse la fecha del evento.
+ Está claro qué mensaje de confirmación que verá el comprador.
+ Está claro qué mensaje de rechazo que verá el comprador en caso dado.

DoD
+ El comprador puede inscribirse exitosamente en lista de espera.
+ El sistema evita duplicado para el mismo evento-comprador.
+ La inscripción queda disponible para consulta posterior.
+ El sistema rechaza inscripciones cuando la lista de espera del evento ya cerró.
+ QA y negocio puede validar los escenarios anteriores: Inscripción exitosa y rechazada por duplicado.

Estimación. 3 puntos

HU2: Como comprador inscrito, quiero consultar el estado de mis inscripción en la lista de espera, para saber si sigo en espera o ya tengo una oportunidad activa.

Revisión INVEST

Independent
La dependencia con HU1 es de datos, no de comportamiento.
Negotiable
“Consultar” asume que el comprador va a buscar su estado. Si el negocio decide que el sistema debe ser proactivo esta HU podría desaparecer.
Valuable
Sin esta HU el comprador inscrito no sabe si sigue en espera o ya tiene una oportunidad.
Estimable
Solo requiere información de estados ya persistidos, sin escritura ni orquestación.
Small
solo lectura de estado actual del comprador
Testable
los tres escenarios son sobre ver inscripción activa, oportunidad vigente asociada u oportunidad expirada


Criterios de aceptación en Gherkin:
Scenario: Ver estado de inscripción activa
Given un comprador inscrito en la lista de espera de un evento
When el comprador consulta su estado
Then el sistema muestra su inscripción como ativa

Scenario: Ver oportunidad activa asociada a la inscripción
Given un comprador con una oportunidad activa derivada de la lista de espera
When el comprador consulta su estado
Then el sistema muestra que tiene una oportunidad activa
And el sistema muestra la vigencia de su oportunidad
And el sistema muestra que la entrada está reservada temporalmente para ese comprador

Scenario: Ver oportunidad expirada
Given un comprador cuya oportunidad venció sin ser utilizada
When el comprador consulta su estado
Then el sistema muestra que la oportunidad ha expirado
And el sistema no muestra ninguna reserva temporal activa para ese comprador

DoR
+ Están definidos los estados visibles para el comprador.
+ Está definido “Oportunidad Activa” significa que la entrada ya quedó reservada temporalmente para ese comprador.
+ Está definida la la información que se muestra sobre la vigencia.

DoD
+ El comprador puede consultar su estado actual.
+ El sistema distingue claramente entre espera activa, oportunidad activa y oportunidad expirada.
+ La vigencia de la oportunidad es visible, cuando aplica.
+ Negocio y QA validan que los estados sean comprensibles.

Estimación. 3 puntos

HU3: Como sistema, quiero asignar una oportunidad de compra al siguiente comprador elegible cuando una entrada se libere, para aprovechar la demanda pendiente antes de devolverla al inventario general.

Revisión INVEST

Independent
La dependencia con HU1 es de datos, no de comportamiento.
Negotiable
“Asignar a uno” asume exclusividad automática. El negocio podría preferir notificar a varios y dejar que el primero en reaccionar compre, aunque esto cambia por completo el modelo.
Valuable
Sin esta HU la lista de espera es una base de datos inerte.
Estimable
FIFO conocido, integración con servicio de reserva existente.
Small
Excluye notificación y expiración su única función es dejar una oportunidad activa para el siguiente elegible.
Testable
Con elegible: oportunidad activa y entrada reservada. Sin elegible: no se crea nada. Ambos casos son verificables


Criterios de aceptación en Gherkin:
Scenario: Asignación al siguiente comprador elegible
Given una entrada liberada de un evento
And existe al menos un comprador con inscripción activa en la lista de espare de ese evento
When el sistema procesa la liberación de la entrada
Then el sistema asigna una oportunidad al siguiente comprador elegible
And el sistema reserva temporalmente la entrada para ese comprador
And el sistema deja la oportunidad en estado activo

Scenario: sin compradores en espera
Given una entrada liberada de un evento
And no existen compradores con inscripción activa en la lista de espera de ese evento.
When el sistema procesa la liberación de la entrada
Then el sistema no genera una oportunidad en lista de espera

DoR
+ Está definida la regla inicial de prioridad (orden de llegada)
+ Está definido qué significa que una entrada fué liberada.
+ Está definido que una oportunidad pasa a estado activo solo cuando la reserva temporal fue confirmada para ese comprador.

DoD
+ El sistema puede detectar una liberación relevante para la lista de espera
+ El sistema selecciona correctamente al siguiente comprador elegible.
+ La oportunidad queda creada con estado activo y vigente.
+ La oportunidad activa implica que la entrada quedó reservada temporalmente para ese comprador.
+ La inscripción relacionada queda inactiva una vez que la oportunidad se activa, permitiendo una futura reinscripción del comprador. 
+ QA validar casos con y sin comprador en lista de espera.

Estimación. 8 puntos

HU4: Como comprador con una oportunidad activa, quiero ser notificado dentro de la aplicación, para enterarme a tiempo de que ya puedo intentar completar la compra.

Revisión INVEST

Independent
Consume estado producido por HU3 sin modificarlo, no hay acoplamiento de comportamiento con otra HU.
Negotiable
“dentro de la aplicación” define el canal, no la forma. Un banner, un bagge o una sección de notificación, son opciones negociables.
Valuable
Sin esta HU un comprador presente en la aplicación no se entera de su oportunidad sin refrescar manualmente.
Estimable
Endpoint SSE, hub de notificación y suscripción frontend son tres componentes conocidos.
Small
Excluye canal de correo y expiración.
Testable
Se verifica que el front recibe la actualización sin recarga al activarse la oportunidad y que el estado visible es consistente al expirar.


Criterios de aceptación en Gherkin:
Scenario: Visualización de oportunidad activa dentro de la aplicación
Given un comprador con una oportunidad activa
And el comprador tiene abierta la aplicación
When el estado de su oportunidad es actualizado
Then el sistema muestra dentro de la aplicación que tiene una oportunidad activa
And el sistema muestra la vigencia de esa oportunidad
And el sistema muestra que existe una reserva temporal asociada a esa oportunidad

Scenario: Consulta posterior del estado si el comprador sigue navegando
Given un comprador con una oportunidad activa derivada de la lista de espera
When el comprador consulta nuevamente su estado dentro de la aplicación
Then el sistema muestra que la oportunidad sigue activa
And el sistema muestra que la oportunidad sigue activa mientras no haya vencido

DoR
+ Está definido qué mensaje verá el comprador en la aplicación.
+ Está definido qué estados deben reflejarse visualmente
+ Está definido si la vigencia se muestra como fecha, tiempo restante o ambos.
+ Está acordado que la notificación in-app debe expresar que la oportunidad activa ya tiene una reserva temporal asociada.

DoD
+ El comprador puede enterarse dentro de la aplicación que tiene una oportunidad activa.
+ La información mostrada es consistente con el estado real del sistema.
+ La notificación in-app deja claro que la entrada está retenida temporalmente para ese comprador.
+ Negocio y QA validan que la comunicación in-app sea suficientemente clara.

Estimación. 5 puntos

HU5: Como comprador con una oportunidad activa, quiero recibir un correo electrónico cuando se me asigne esa oportunidad, para enterarme aunque no esté dentro de la aplicación.

Revisión INVEST

Independent
El canal de correo se aisla usando un puerto, puede construirse o reemplazar sin afectar HU3 ni HU4.
Negotiable
el canal es conversable, por ejemplo SMS podría sustituir el correo. Lo que no cambia es que el aviso externo no es fuente oficial del estado.
Valuable
Sin esta HU un comprador fuera de línea pierde la oportunidad porque no recibe aviso por ningún canal externo.
Estimable
Integración con proveedor externo, manejo de fallos y trazabilidad de intentos son conocidos.
Small
Todos los componentes nuevos sirven a una sola cosa. avisar por correo cuando la oportunidad se activa
Testable
Se verifica que el correo se genera con contenido acordado y que el intento queda registrado como enviado o fallido.


Criterios de aceptación en Gherkin:
Scenario: Envío de correo cuando se activa una oportunidad
Given un comprador con una dirección de correo válida
And una oportunidad activa recién asignada
When el sistema activa la oportunidad
Then el sistema envía un correo electrónico al comprador
And el correo informa que existe una oportunidad activa de compra
And el correo informa que la entrada está reservada temporalmente durante una vigencia limitada

Scenario: Contenido mínimo del correo
Given un comprador con una oportunidad activa derivada de la lista de espera
When el sistema genera el correo electrónico
Then el correo incluye la referencia del evento
And el correo informa que la oportunidad tiene una vigencia limitada
And el correo orienta al comprador a revisar el estado dentro del sistema

DoR
+ Está definido el contenido del correo.
+ Está acordado la acción exacta que dispara el envío.
+ Está clara la regla sobre qué dirección de correo usar.
+ Está acordado que el correo es un canal de aviso y no la fuente oficial del estado.

DoD
+ El sistema genera el correo cuando una oportunidad se activa
+ El correo contiene la información mínima acordada.
+ El correo remite al comprador a consultar el estado dentro del sistema.
+ El correo deja claro que es un aviso y no reemplaza el estado oficial.
+ El intento de envío queda auditado.
+ QA y negocio validan el contenido y el momento del envío.

Estimación. 8 puntos

HU6: Como sistema, quiero expirar una oportunidad dentro del tiempo definido, para mantener la disponibilidad dinámica y continuar con la reasignación o liberación según la política del negocio.

Revisión INVEST

Independent
Consume oportunidades de HU3,pero la lógica de vencimiento y reasignación es autónoma y no modifica el flujo de HU3.
Negotiable
La expiración automática es la solución propuesta. Podría ser manual o asistida.
Valuable
Sin esta HU una oportunidad no utilizada bloquea una entrada indefinidamente, paralizando la reasignación de demanda
Estimable
La reasignación reutiliza el flujo de HU3, de manera que una vez estimada HU3 es fácil estimar esta.
Small
Limitando al ciclo de vida oportunidades no usadas; no incluye notificar la expiración (HU4) ni modificar la lógica de asignación original.
Testable
Los cuatro comportamientos se pueden cubrir en tres escenarios independientes: expiración de estado, liberación y reasignación con elegible y liberación y devolución sin elegible.


Criterios de aceptación en Gherkin:
Scenario: Expiración automática por vencimiento
Given una oportunidad activa con vigencia limitada
And la oportunidad no fue utilizada dentro del tiempo permitido
When se alcanza el vencimiento de la oportunidad
Then el sistema cambia el estado de la oportunidad a expirada

Scenario: Reasignación inmediata al siguiente comprador elegible
Given una oportunidad activa que acaba de expirar
And existe otro comprador con inscripción activa en la lista de espera del mismo evento
When el sistema procesa la expiración
Then el sistema libera la entrada previamente retenida
And el sistema asigna una nueva oportunidad al siguiente comprador elegible

Scenario: Devolución al inventario cuando no hay más compradores
Given una oportunidad activa que acaba de expirar
And no existe comprador con inscripción activa en la lista de espera del mismo evento
When el sistema procesa la expiración
Then el sistema libera la entrada previamente retenida
And el sistema vuelve al inventario general sin prioridad especial

DoR
+ Está definida la vigencia de la oportunidad.
+ Está definido qué significa “utilizada” para esta primera versión.
+ Está definida la política posterior al vencimiento: liberar, intentar reasignación inmediata; si no hay siguiente comprador, volver al inventario general.

DoD
+ La oportunidad cambia correctamente a expirada o el estado equivalente cuando vence.
+ El vencimiento queda trazable con motivo y momento.
+ La inscripción relacionada queda inactiva cuando la oportunidad expira, permitiendo una futura reinscripción del comprador.
+ QA puede validar expiración y transición posterior.

Estimación. 5 puntos
Análisis de Impacto Arquitectónico

En esta sección se busca responder inquietudes sobre capacidades del sistema que cambian o se incorporan, nuevos conceptos de dominio y qué compromisos de diseño garantizan que la arquitectura pueda sostener esos cambios sin crear deuda técnica adicional a la que exista.

Capacidades que se amplían o incorporan en el sistema
El sistema pasa de ignorar la demanda no atendida a reterna y reactivarla
La liberación de una entrada deja de ser un evento silencioso
El comprador adquiere visibilidad en su lugar en la fila de espera, sin depender de soporte o atención al cliente.
La comunicación con el comprador se extiende a dos canales complementarios.
El mecanismo de reserva existente no cambia, la lista de espera lo usa.

Nuevos conceptos de dominio
Inscripción en lista de espera
Oportunidad de compra
Registro de notificación

Puntos de integración que se agregan
Registro de demanda. El comprador puede declarar su interés desde la misma experiencia donde intentó comprar, sin salir del flujo.
Consulta de estado: el comprador puede saber en cualquier momento en que punto del proceso se encuentra, sin esperar a que el sistema lo contacte.
Canal en tiempo real: si el comprador está navegando cuando su oportunidad se activa, el aviso llega sin que tenga que recargar la página.
Canal de correo: si el comprador no está en la aplicación, recibe un aviso externo que lo invita a actuar, dejando claro que el estado oficial vive en la plataforma.

Patrones de Diseño que ayudan a sostener las decisiones de negocio
Observer. Cuando el estado de una oportunidad cambia, el sistema reacciona en múltiples canales sin que esa reacción quede acoplada a quien tomó la decisión. Si el negocio agrega un canal en el futuro, este simplemente se añade como nuevo suscriptor.
Strategy. La política de “a quién le toca” se puede cambiar sin deshacer el proceso de asignación. Hoy es orden de llegada, mañana puede ser otro criterio sin afectar el resto del sistema.
State. La oportunidad tiene estados con reglas caras de transición. Si el negocio quiere agregar un estado intermedio, el modelo lo soporta sin condicionales dispersos que dificulten diagnósticos.
Facade. El proceso completo de asignación y notificación se puede invocar con una unidad coherente. El resto del sistema no necesita conocer ni depender de sus pasos internos.
Command. Las acciones relevantes para el negocio como: registrar interés, enviar aviso, expirar oportunidad, existen como objetos formales. Esto facilita su trazabilidad, incluso auditoría si llega el caso.

Herramienta de Soporte que pueden ayudar a cubrir reglas de negocio
Aquí se mencionan solo alguna herramientas, puede que existan mas con el mismo propósito o incluso mejores, pero lo aqui se busca mostrar es que existe al menos una herramienta que nos ayuda a cubrir la necesidad.

FluentValidation. La regla de inscripción se valida de forma consistente y visible, aunque también se puede evaluar si este tipo de validación se realiza en el dominio como regla de negocio o solo una parte de ella.
OpenTelemetry. Cuando Soporte quiera saber qué pasó con una oportunidad que cruzó varios servicios y canales, hay una traza que le responde sin depender de logs dispersos.
Amazon SES. Cada correo enviado queda registrado con su resultado. Si un aviso falla, el sistema lo sabe y puede reportarlo; no se pierde en silencio.
RabbitMQ TTL/DLX. El vencimiento de una oportunidad es una capacidad del sistema, no un proceso manual que puede fallar sin consecuencias visibles. Y esto es una herramienta con la que ya contamos.
PostgreSQL índices parciales. El sistema garantiza en la base de datos que nadie puede inscribirse dos veces en el mismo evento activo, sin dejar esa responsabilidad únicamente al código de la aplicación. Así, tenemos diferentes blindajes.

Compromisos arquitectónico con impacto directo en el negocio

El estado de la oportunidad no vive en el correo. Si el negocio o soporte llegaran a usar el correo como referencia oficial del estado, aparecen inconsistencias inevitables cuando el mensaje llega tarde, falla o el proveedor tiene una caída. El sistema es la única fuente de verdad, el correo es un aviso.
Las notificaciones se registran siempre, con cualquier resultado. Un intento de notificación que falla en silencio crea una zona ciega para Soporte. Registrar cada intento convierte un problema de canal en un dato consultable: Soporte puede decirle al comprador exactamente si recibió el correo y cuándo.
La integración con el mecanismo de reserva existente es de uso, no de modificación. La lista de espera aprovecha la reserva temporal existente sin reescribirla. Esto protege el comportamiento ya validado del flujo base de compra y limita el riesgo de esta feature al código nuevo.
