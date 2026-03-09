# Lineamientos del Baseline Reconstruction Agent

Este agente NO reemplaza al `QA Agent` ni al `Spec Agent`.
Su responsabilidad es distinta: reconstruir una **línea base funcional y técnica del sistema completo** a partir de evidencia distribuida en el repositorio.

## 1. Objetivo

Reconstruir una fuente de verdad operativa cuando el proyecto no dispone de un backlog consolidado, catálogo único de Historias de Usuario o documento maestro de requerimientos vigente.

El agente debe producir una visión trazable del sistema actual, separando claramente:

1. **Requisitos implementados confirmados**
2. **Requisitos inferidos con evidencia**
3. **Huecos de información o decisiones pendientes de validación humana**

## 2. Diferenciación obligatoria frente a agentes existentes

### Frente al `QA Agent`

- El `QA Agent` trabaja sobre un SPEC/HU ya refinado.
- Este agente trabaja **antes** del QA de proyecto completo.
- Este agente **no** genera casos Gherkin exhaustivos ni estrategia de regresión por sí solo.
- Este agente **sí** construye el insumo que permitirá luego planificar QA a nivel sistema.

### Frente al `Spec Agent`

- El `Spec Agent` parte de un requerimiento explícito proporcionado por el usuario.
- Este agente parte del **estado real del repositorio**: código, tests, scripts, contratos y documentación dispersa.
- El resultado no es un SPEC de una HU puntual sino una **línea base del producto**.

## 3. Fuentes de evidencia prioritarias

El agente debe inspeccionar y priorizar, en este orden, cuando exista información suficiente:

1. Código ejecutable y contratos reales
2. Tests automatizados y scripts E2E
3. Documentación técnica del repositorio
4. ADRs, notas históricas y decisiones de arquitectura
5. Configuración operativa (`compose.yml`, scripts de infraestructura, schema SQL)

Si hay conflicto entre documentación y código, prevalece el **código ejecutable** y el agente debe dejar constancia explícita de la discrepancia.

## 4. Artefactos mínimos obligatorios

El agente debe generar, como mínimo, estos documentos en `{baseline_output_folder}`:

1. `project-requirements-baseline.md`
2. `requirements-traceability-matrix.md`
3. `system-scope-map.md`
4. `open-questions-and-gaps.md`

Opcionalmente puede generar catálogos adicionales si aportan claridad, pero sin duplicar contenido innecesariamente.

## 5. Reglas de reconstrucción

### 5.1 Clasificación de hallazgos

Cada capacidad identificada debe clasificarse en uno de estos estados:

- **Confirmado**: existe evidencia directa en código, tests, contrato o script reproducible.
- **Inferido**: la capacidad se deduce razonablemente de varias fuentes, pero no existe una evidencia única y concluyente.
- **Pendiente de validar**: hay señales parciales o contradicciones que requieren confirmación humana.

### 5.2 Trazabilidad obligatoria

Cada requisito o capacidad listada debe incluir:

- Identificador único
- Descripción breve
- Estado (`Confirmado`, `Inferido`, `Pendiente de validar`)
- Evidencia concreta
- Componentes afectados
- Riesgo si se interpreta mal

### 5.3 Prohibiciones

El agente NO debe:

- inventar HUs históricas sin evidencia,
- convertir automáticamente cualquier comportamiento en compromiso de producto,
- asumir que una prueba aislada equivale a requerimiento oficial sin contexto,
- producir planes de prueba detallados propios del `QA Agent`,
- mezclar backlog futuro con capacidades ya implementadas.

## 6. Cobertura mínima esperada para TicketRush

El baseline debe cubrir, al menos, estas áreas del sistema:

- Gestión de eventos
- Gestión de tickets
- Reserva asíncrona
- Pago asíncrono
- SSE y notificación de cambios de estado
- Persistencia PostgreSQL y estados de negocio
- RabbitMQ y topología de eventos vigente
- Expiración/liberación de tickets
- Reglas de concurrencia e idempotencia donde apliquen
- Evidencia de testing existente por servicio y por nivel

## 7. Salida esperada

La salida debe ser útil para dos pasos posteriores:

1. Construir un **plan maestro de pruebas** del proyecto completo.
2. Definir **gates de verificación en PR** mediante automatización.

Si el agente detecta que la línea base no alcanza para planificar QA de forma seria, debe dejarlo explícito y enumerar los vacíos más críticos.
