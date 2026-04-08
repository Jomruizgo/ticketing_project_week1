# TestPlan.md — Plan de Pruebas: Feature1 Lista de Espera

> **Documento fuente:** Este plan de pruebas se deriva de [Planning2.md](Planning2.md), que contiene la definición de la épica, las historias de usuario, los criterios de aceptación en Gherkin y el lenguaje de negocio que rige esta feature.

## 1. Objetivo

Este documento presenta el plan de pruebas para la épica de lista de espera de TicketRush.

Su objetivo es definir de forma clara:

1. qué comportamientos del negocio deben verificarse y cuáles quedan fuera del alcance de esta épica,
2. los riesgos concretos que el equipo enfrenta al implementar esta feature y cómo las pruebas los mitigan,
3. la estrategia multinivel que permite verificar cada comportamiento al nivel correcto de profundidad,
4. y las suites de prueba que traducen esa estrategia en trabajo ejecutable.

> Decisión explícita del equipo: este documento no contiene cronograma. Su propósito es funcionar como fuente técnica viva del testing de esta épica, no como plan temporal de sprint.

---

## 2. Descripción del sistema bajo prueba

La lista de espera es una extensión del flujo de compra existente de TicketRush. No reemplaza ni modifica el mecanismo de reserva y pago; lo extiende con un nuevo ciclo de vida que opera cuando el inventario ya no tiene disponibilidad inmediata.

En términos de negocio, el sistema bajo prueba debe garantizar que:

- un comprador interesado pueda declarar formalmente su intención de compra cuando no hay entradas disponibles,
- cuando una entrada vuelva a quedar libre, el sistema la ofrezca con prioridad al siguiente comprador en lista en lugar de devolverla silenciosamente al inventario general,
- esa oportunidad tenga una vigencia definida y visible para el comprador, tanto dentro de la aplicación como por correo electrónico,
- si la oportunidad vence sin ser utilizada, el sistema intente reasignarla o la devuelva al inventario según corresponda,
- y que en ningún momento se pierda trazabilidad del estado: ni del interés declarado, ni de la oportunidad asignada, ni del intento de notificación.

### Comportamientos canónicos confirmados

- Una inscripción en lista de espera no es una reserva. No bloquea ninguna entrada.
- Una oportunidad activa sí implica una entrada reservada temporalmente. Su vigencia es de 15 minutos.
- El estado oficial de una oportunidad vive en el sistema. El correo electrónico es un canal de aviso, no una fuente de verdad.
- Una oportunidad utilizada significa que el comprador avanzó al flujo de pago desde esa oportunidad. En ese momento la oportunidad deja de estar activa y la entrada continúa el ciclo normal de pago.
- Dos eventos de liberación simultáneos para el mismo evento no pueden producir la misma oportunidad para el mismo comprador.

---

## 3. Alcance de las pruebas

### 3.1 Dentro del alcance

| ID | Comportamiento funcional verificado |
|---|---|
| HU1 | Un comprador puede inscribirse en la lista de espera, el sistema previene duplicados, rechaza inscripciones cuando la lista está llena y permite reinscripción tras una oportunidad utilizada o expirada |
| HU2 | Un comprador puede consultar su estado actual: en espera, oportunidad activa, oportunidad utilizada u oportunidad expirada |
| HU3 | Cuando una entrada se libera, el sistema asigna una oportunidad al siguiente comprador elegible o no genera ninguna si la lista está vacía |
| HU4 | Cuando una oportunidad se activa, el comprador que tiene la aplicación abierta recibe la actualización en tiempo real sin recargar la página |
| HU5 | Cuando una oportunidad se activa, el sistema envía un correo de aviso al comprador; un fallo en el envío no afecta el estado de la oportunidad ni detiene el flujo |
| HU6 | Una oportunidad que no fue utilizada dentro de los 15 minutos expira automáticamente; el sistema intenta reasignar la entrada o la devuelve al inventario según corresponda |
| HU7 | El comprador puede ver la opción de lista de espera e inscribirse directamente desde la aplicación cuando un evento no tiene disponibilidad inmediata |
| HU8 | El comprador puede consultar su estado en la lista de espera, actuar sobre una oportunidad activa directamente desde la aplicación y recibir retroalimentación clara cuando la acción no puede completarse (oportunidad expirada, correo incorrecto, comprador no inscrito) |

Adicionalmente se verifican:

- la restricción de unicidad de inscripción activa por comprador y evento, garantizada tanto por la capa de aplicación como por la base de datos,
- el límite global de tamaño de la lista de espera, que rechaza nuevas inscripciones cuando se alcanza el máximo configurado,
- el contrato del evento de dominio ticket.released como disparador único de la asignación,
- la auditoría de todos los intentos de envío de correo con resultado y momento, independientemente del éxito o fallo,
- la consistencia entre el estado real del sistema y la información que el comprador puede consultar.

### 3.2 Reglas de negocio que deben verificarse

#### Reglas funcionales de negocio

1. Un comprador no puede tener más de una inscripción activa para el mismo evento al mismo tiempo.
2. Una inscripción activa no equivale a una reserva; no bloquea ninguna entrada del inventario.
3. Una oportunidad solo pasa a estado activo cuando la reserva temporal de la entrada fue confirmada exitosamente; si la reserva falla, el sistema no crea la oportunidad.
4. La vigencia de una oportunidad es de 15 minutos desde su activación. Pasado ese tiempo, la oportunidad expira automáticamente sin importar si el comprador está o no en la aplicación.
5. Cuando una oportunidad expira, el sistema debe intentar reasignar la entrada al siguiente comprador en lista antes de devolverla al inventario general.
6. La inscripción del comprador cuya oportunidad fue utilizada o expiró queda inactiva, habilitando una futura reinscripción mientras la lista del evento siga vigente.
7. La lista de espera cierra al alcanzarse la fecha del evento. Después de ese momento no se aceptan inscripciones y no se asignan oportunidades.
8. La lista de espera tiene un límite global de tamaño configurable. Cuando se alcanza ese límite, las nuevas inscripciones son rechazadas hasta que alguna inscripción activa deje de estarlo.
9. El comprador con una oportunidad activa puede reclamarla para avanzar al flujo de pago; al hacerlo, la oportunidad pasa a estado utilizada y la entrada sigue el ciclo normal de compra.

#### Restricciones técnicas relevantes para las pruebas

1. El evento de dominio ticket.released es el único mecanismo que dispara la asignación. No existe otro camino válido.
2. La publicación de ticket.released es una responsabilidad de ReservationService (expiración de reserva) y de paymentService (rechazo de pago). Ambos deben verificarse como prerequisito antes de que HU3 pueda considerarse funcional en entorno integrado.
3. El fallo del proveedor de correo externo no puede propagarse como excepción al flujo que activó la oportunidad. El sistema debe absorberlo y registrarlo.
4. La vigencia de 15 minutos es un parámetro de negocio configurable por variable de entorno, no una constante en código. Las pruebas que dependan de él deben leerlo del entorno o inyectarlo explícitamente.

### 3.3 Fuera del alcance

- Pruebas de carga, volumen o rendimiento sostenido.
- Pruebas del proveedor de correo externo en entorno productivo.
- Pruebas del canal SSE bajo condiciones de red degradada o pérdida de conexión.
- Pruebas de los flujos internos de ReservationService y paymentService más allá de confirmar que publican ticket.released en los momentos correctos.
- Cancelación voluntaria de inscripción (excluida explícitamente del alcance de la épica).

---

## 4. Riesgos del sistema y priorización

| ID | Riesgo | Probabilidad | Impacto | Nivel |
|---|---|---:|---:|---|
| R-001 | Una entrada liberada podría asignarse a dos compradores distintos de forma simultánea si la concurrencia no está controlada | 3 | 3 | Crítico |
| R-002 | El sistema podría crear una oportunidad sin reserva real detrás, dejando al comprador con una oportunidad sin respaldo en el inventario | 3 | 3 | Crítico |
| R-003 | Una oportunidad expirada podría no liberar la entrada, bloqueando el inventario indefinidamente | 2 | 3 | Alto |
| R-004 | El estado de la oportunidad podría no reflejarse correctamente para el comprador, generando confusión sobre si la entrada está o no disponible para él | 2 | 3 | Alto |
| R-005 | Un fallo en el envío de correo podría detener el flujo de asignación completo si no está aislado correctamente | 2 | 3 | Alto |
| R-006 | La restricción de unicidad podría no sostenerse a nivel de base de datos si solo existe en el código de aplicación | 2 | 3 | Alto |
| R-007 | La lista podría seguir aceptando inscripciones después de que el evento haya cerrado | 2 | 2 | Medio |
| R-008 | El reinscrito tras una oportunidad expirada podría quedar bloqueado si la inscripción anterior no quedó inactiva correctamente | 2 | 2 | Medio |
| R-009 | La lista podría seguir aceptando inscripciones por encima del límite global configurado si la validación solo existe en código de aplicación | 2 | 2 | Medio |
| R-010 | El comprador podría reclamar una oportunidad que ya expiró si la validación de vigencia tiene condiciones de carrera entre frontend y backend | 2 | 2 | Medio |

### Orden de ejecución recomendado por riesgo

1. Asignación con concurrencia y garantía de reserva real detrás de la oportunidad.
2. Expiración y liberación de entrada.
3. Unicidad de inscripción en base de datos.
4. Consistencia del estado observable para el comprador.
5. Aislamiento del fallo de correo.
6. Cierre de lista por fecha del evento y habilitación de reinscripción.

---

## 5. Estrategia de ejecución

### Smoke

Primera barrera. Confirma que la solución compila, que los endpoints clave responden y que las suites rápidas no tienen ruptura obvia antes de ejecutar validaciones más profundas.

### Componente (unitaria)

Se ejecutan sin base de datos ni broker reales. Validan la lógica de dominio en aislamiento: reglas de transición de estado de la oportunidad, regla de prioridad por orden de llegada, comportamiento del sistema cuando la reserva temporal falla, lógica de expiración con control de tiempo inyectado.

### Integración

Se ejecutan con base de datos real (PostgreSQL en contenedor mediante Testcontainers) y sin broker real. Validan la restricción de unicidad a nivel de esquema, el comportamiento de los repositorios con datos reales y el orden garantizado de operaciones (persistencia antes que envío de correo).

Cada test de integración debe limpiar sus datos al finalizar (teardown). La limpieza se implementa con `IAsyncLifetime.DisposeAsync()` en xUnit, que trunca o elimina las filas insertadas durante el test. Esto garantiza que los tests no se contaminan entre sí y elimina la causa principal de tests inestables (flaky tests) en suites de integración.

### Aceptación (E2E sobre compose)

Se ejecutan sobre Docker Compose levantado completo. Validan los criterios Gherkin de cada HU de extremo a extremo: desde la solicitud HTTP hasta el estado final observable en la API de consulta, incluyendo la recepción del evento ticket.released en entorno real.

### Regresión

Cualquier cambio en los flujos de reserva, pago o expiraci\u00f3n existentes debe activar regresión sobre las suites de HU3 y HU6, ya que ambas dependen del comportamiento de servicios que no son propiedad de esta épica. No se asume aislamiento funcional.
### Integración continua (GitHub Actions)

El repositorio ya cuenta con un pipeline de CI (`.github/workflows/ci.yml`) que se ejecuta en cada push y pull request hacia `develop` o `main`. El pipeline actual tiene seis jobs secuenciales:

1. **Build** — compila los cuatro servicios (.NET 8).
2. **Unit Tests** — ejecuta pruebas unitarias (dominio y aplicación) de todos los servicios.
3. **Component Tests** — ejecuta pruebas de componente con filtro `Category=Component`.
4. **Integration Tests** — ejecuta pruebas de integración (contratos, repositorios con BD real) con filtro `Category=Integration`.
5. **Black-Box Tests** — ejecuta pruebas de caja negra contra la API HTTP con filtro `Category=BlackBox`.
6. **Docker Build + Trivy** — construye las imágenes Docker de los cuatro servicios y ejecuta escaneo de vulnerabilidades.

Las suites nuevas de esta épica se incorporan al mismo pipeline según su nivel:

| Suite de esta épica | Job del pipeline donde se ejecuta | Categoría / filtro |
|---|---|---|
| Reglas de inscripción, proyección de estado, asignación, expiración, notificación por correo | Unit Tests | (sin filtro — se ejecuta con el resto de unitarias) |
| Unicidad en base de datos, contratos de estado | Integration Tests | `Category=Integration` |
| Flujo completo de lista de espera, experiencia del comprador | Aceptación E2E | Requiere `docker compose` — pendiente de decisión sobre si se agrega un job E2E al pipeline o se ejecuta en un entorno dedicado |

> Las suites E2E sobre compose no están en el pipeline actual porque requieren levantar infraestructura completa (PostgreSQL, RabbitMQ, todos los servicios). Si el equipo decide incluirlas, se agregaría un job adicional con `docker compose up` como paso previo. Mientras tanto, se ejecutan de forma manual o en un entorno dedicado de staging.

**Acción pendiente — Quality Gate bloqueante:** el pipeline actual no bloquea el merge si los tests fallan. Para que funcione como Quality Gate estricto se requiere: (1) activar branch protection rules en GitHub para `develop` y `main` exigiendo que los checks de CI pasen antes del merge, y (2) cambiar `exit-code: "0"` a `exit-code: "1"` en el escaneo Trivy para que vulnerabilidades críticas bloqueen el pipeline.
---

## 6. Suites de prueba planificadas

| Suite | Nivel | Tipo | Objetivo |
|---|---|---|---|
| Suite de reglas de inscripci\u00f3n | Componente | Caja blanca | Verificar unicidad activa por comprador-evento, cierre por fecha y habilitaci\u00f3n de reinscripci\u00f3n |
| Suite de proyecci\u00f3n de estado | Componente | Caja blanca | Verificar que los cuatro estados posibles del comprador se proyectan sin ambig\u00fcedad |
| Suite de asignaci\u00f3n de oportunidad | Componente | Caja blanca | Verificar orden de llegada, ausencia de oportunidad cuando la lista est\u00e1 vac\u00eda y ausencia de oportunidad cuando la reserva temporal falla |
| Suite de expiraci\u00f3n | Componente | Caja blanca | Verificar el l\u00edmite de los 15 minutos, la reasignaci\u00f3n inmediata y la devoluci\u00f3n al inventario |
| Suite de notificaci\u00f3n por correo | Componente | Caja blanca | Verificar que el fallo del proveedor externo no afecta el estado de la oportunidad y que el intento queda auditado |
| Suite de unicidad en base de datos | Integraci\u00f3n | Caja negra structural | Verificar que la restricci\u00f3n de unicidad del \u00edndice parcial opera de forma independiente al c\u00f3digo de aplicaci\u00f3n |
| Suite de contratos de estado | Integraci\u00f3n | Contrato | Verificar que la API de consulta de estado expone los cuatro estados correctamente con datos reales en base de datos |
| Suite de flujo completo de lista de espera | Aceptación E2E | Caja negra | Validar las escenas Gherkin de HU1 a HU8 en entorno compose completo, incluyendo la recepción real del evento ticket.released |
| Suite de experiencia del comprador en la aplicación | Aceptación E2E | Caja negra | Verificar que el comprador puede inscribirse, consultar su estado, actuar sobre una oportunidad activa y recibir retroalimentación adecuada cuando la acción no puede completarse (oportunidad expirada, correo incorrecto, comprador no inscrito) directamente desde la interfaz de la aplicación |

---

## 7. Trazabilidad de suites por historia de usuario

| Historia | Suites asociadas |
|---|---|
| HU1 Inscripción en lista de espera | Suite de reglas de inscripción, Suite de unicidad en base de datos, Suite de flujo completo |
| HU2 Consulta de estado | Suite de proyección de estado, Suite de contratos de estado, Suite de flujo completo |
| HU3 Asignación de oportunidad | Suite de asignación de oportunidad, Suite de flujo completo |
| HU4 Notificación in-app | Suite de flujo completo |
| HU5 Notificación por correo | Suite de notificación por correo, Suite de flujo completo |
| HU6 Expiración de oportunidad | Suite de expiración, Suite de flujo completo |
| HU7 Inscripción y disponibilidad en la aplicación | Suite de experiencia del comprador en la aplicación, Suite de flujo completo |
| HU8 Consulta de estado y acción sobre oportunidad en la aplicación | Suite de experiencia del comprador en la aplicación, Suite de flujo completo |

---

## 8. Disciplina de desarrollo

Las pruebas de esta épica siguen la disciplina TDD sin excepciones: primero la prueba en rojo, luego el mínimo código que la hace pasar, luego el refactor. Ningún código de producción puede existir sin una prueba que lo justifique. La trazabilidad de esa disciplina queda en los commits: `test(red):`, `feat(green):`, `refactor:`.

---

## 9. Estrategia de automatización de pruebas funcionales

La automatización de pruebas funcionales se planifica con Serenity BDD como marco de referencia. Serenity BDD permite expresar las pruebas en lenguaje de negocio y generar reportes que negocio y QA pueden leer sin conocimiento técnico previo.

**Requisitos técnicos de los repositorios de automatización:**

- **Gradle** como sistema de build para los tres repositorios Java (AUTO_FRONT_POM_FACTORY, AUTO_FRONT_SCREENPLAY, AUTO_API_SCREENPLAY). Cada repositorio debe compilar y ejecutar las pruebas con `gradle clean test`.
- **`serenity.conf`** correctamente configurado con: URL base del sistema bajo prueba, timeouts de espera, driver del navegador (para E2E front) y nivel de reporte. No debe contener datos hardcodeados; las URLs base deben ser configurables por variable de entorno para que funcionen tanto en local como en CI.
- **Estructura de proyecto limpia:** sin código comentado, sin dependencias sin usar en `build.gradle`, sin archivos de configuración huérfanos.

Esta sección documenta **todos los escenarios que se consideran necesarios** para una cobertura funcional completa de la épica. La decisión de cuáles se implementan efectivamente se toma después de la planificación, en función de la capacidad del equipo y el valor de cada escenario como evidencia.

### 9.1 Escenarios planificados — Automatización desde la aplicación (Front)

Estos escenarios verifican que el comprador puede completar los flujos de negocio de la lista de espera directamente desde la interfaz de la aplicación. Lo que se valida no es un componente visual aislado, sino la experiencia completa del comprador frente a cada situación de negocio.

| ID | HU | Escenario | Qué verifica desde el negocio |
|---|---|---|---|
| AUTO-F01 | HU7 | El comprador ve la opción de lista de espera cuando un evento no tiene disponibilidad y la lista sigue vigente | La aplicación presenta la opción correcta cuando no hay entradas disponibles, sin ofrecer compra directa |
| AUTO-F02 | HU7 | El comprador se inscribe en la lista de espera desde la aplicación | El comprador puede declarar su interés usando la aplicación y recibe confirmación de que su inscripción quedó registrada |
| AUTO-F03 | HU7 | La aplicación rechaza la inscripción duplicada del comprador | El comprador entiende que ya tiene una inscripción activa y no se crea un registro adicional |
| AUTO-F04 | HU7 | La aplicación no muestra la opción de lista de espera cuando el evento ya cerró | El comprador no puede intentar inscribirse a un evento cuya lista de espera ya cerró |
| AUTO-F05 | HU8 | El comprador consulta su inscripción activa desde la aplicación | El comprador puede conocer que su inscripción sigue activa y que aún no tiene una oportunidad asignada |
| AUTO-F06 | HU8, HU2 | El comprador consulta su oportunidad activa con tiempo restante desde la aplicación | El comprador ve que tiene una oportunidad activa, cuánto tiempo le queda y que la entrada está reservada temporalmente para él |
| AUTO-F07 | HU8 | El comprador actúa sobre una oportunidad activa y avanza al pago desde la aplicación | El comprador puede utilizar su oportunidad desde la aplicación; la oportunidad pasa a utilizada y el comprador es dirigido al flujo de pago |
| AUTO-F08 | HU8, HU2 | El comprador ve que su oportunidad expiró desde la aplicación | El comprador entiende que la oportunidad ya no está vigente y que no tiene ninguna reserva temporal activa |
| AUTO-F09 | HU4 | El comprador recibe la actualización de su oportunidad sin recargar la página | El comprador que está navegando se entera de su oportunidad activa en tiempo real sin tener que refrescar manualmente |

### 9.2 Escenarios planificados — Automatización de servicios (Back)

Estos escenarios verifican que los servicios del sistema responden correctamente a las operaciones de negocio de la lista de espera. Se validan las reglas de negocio, las transiciones de estado y la coherencia de las respuestas.

| ID | HU | Escenario | Qué verifica desde el negocio |
|---|---|---|---|
| AUTO-B01 | HU1 | Inscripción exitosa en lista de espera | El sistema registra correctamente el interés del comprador cuando el evento no tiene disponibilidad y la lista sigue vigente |
| AUTO-B02 | HU1 | Rechazo de inscripción duplicada | El sistema impide que un comprador tenga dos inscripciones activas para el mismo evento |
| AUTO-B03 | HU1 | Rechazo de inscripción cuando la lista cerró | El sistema no acepta inscripciones para un evento cuya fecha ya fue alcanzada |
| AUTO-B04 | HU1 | Reinscripción tras oportunidad utilizada o expirada | El sistema permite que un comprador vuelva a inscribirse cuando su inscripción u oportunidad anterior ya no está activa |
| AUTO-B05 | HU2 | Consulta de estado con inscripción activa | El sistema responde con el estado correcto cuando el comprador tiene inscripción activa sin oportunidad |
| AUTO-B06 | HU2 | Consulta de estado con oportunidad activa | El sistema responde con oportunidad activa, tiempo restante y la referencia a la reserva temporal |
| AUTO-B07 | HU2 | Consulta de estado con oportunidad expirada | El sistema responde que la oportunidad expiró y no muestra reserva temporal activa |
| AUTO-B08 | HU1 + HU2 | Flujo completo: inscripción → consulta de estado → intento de duplicado | El sistema mantiene coherencia a lo largo de varias operaciones consecutivas del mismo comprador |
| AUTO-B09 | HU3 + HU2 | Flujo de asignación: liberación de entrada → oportunidad asignada → consulta de oportunidad activa | El sistema asigna la oportunidad al comprador elegible cuando una entrada se libera y el estado es consultable inmediatamente |
| AUTO-B10 | HU6 + HU3 | Flujo de expiración: oportunidad vence → reasignación al siguiente comprador | El sistema expira la oportunidad no utilizada y reasigna la entrada al siguiente comprador en lista |
| AUTO-B11 | HU5 | El sistema registra el intento de correo con resultado exitoso o fallido | El intento de notificación queda auditado independientemente de si el envío fue exitoso o falló |

### 9.3 Selección para implementación

De los escenarios planificados, se seleccionan tres para implementación efectiva. La selección prioriza cobertura representativa de cada enfoque y valor como evidencia funcional para negocio.

| # | ID | Enfoque de implementación | Justificación de la selección |
|---|---|---|---|
| 1 | AUTO-F02 | Front — POM con PageFactory | Cubre el flujo de inscripción desde la aplicación; es el punto de entrada del comprador a la lista de espera y el escenario más representativo de la interacción con la interfaz |
| 2 | AUTO-F06 | Front — Screenplay | Cubre la consulta de estado con oportunidad activa, que es el momento más crítico para el comprador (tiene una oportunidad con tiempo limitado); además demuestra Screenplay como patrón en front |
| 3 | AUTO-B08 | Back — Screenplay con Serenity REST | Cubre un flujo completo de negocio que encadena inscripción, consulta y duplicado en una sola secuencia, verificando coherencia de extremo a extremo a nivel de servicios |

> Los escenarios seleccionados están documentados como casos de prueba en [TestCases.md](TestCases.md) con el prefijo `TC-AUTO-`. Los escenarios no seleccionados quedan planificados para implementación futura si la capacidad del equipo lo permite.

### 9.4 Criterios para considerar la automatización completa

- Los tres escenarios seleccionados pasan en el entorno de ejecución acordado.
- El reporte generado por Serenity BDD es legible para negocio y QA sin explicación adicional.
- Cada escenario automatizado es trazable a un escenario Gherkin de Planning2.md.

---

## 10. Recomendaciones finales

1. Antes de implementar HU3, verificar en entorno compose que ReservationService y paymentService publican ticket.released en los momentos correctos. Sin esa evidencia, HU3 no debe considerarse completable.
2. Si el tiempo de vigencia de la oportunidad cambia (por acuerdo de negocio), basta con actualizar la variable de entorno. Las pruebas que dependan de él deben inyectarlo; no deben asumir 15 minutos como constante.
3. La suite de flujo completo es la única que puede dar señal definitiva de que la épica funciona de extremo a extremo. Las suites de componente e integración son necesarias pero no suficientes.
4. Si en el futuro se incorpora una HU nueva a esta épica, este documento debe actualizarse en el mismo cambio si el nuevo comportamiento afecta alguno de los flujos existentes.

