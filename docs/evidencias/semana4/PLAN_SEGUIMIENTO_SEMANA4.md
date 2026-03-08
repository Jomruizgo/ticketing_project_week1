# Plan de Seguimiento — Semana 4

## 1. Criterio de seguimiento

Para **Semana 4** no conviene trabajar solo con HUs clásicas.

La guía de evaluación pide principalmente:

- entregables técnicos,
- evidencia de ejecución,
- pipeline CI/CD,
- endurecimiento Docker,
- pruebas multinivel,
- release con GitFlow.

Eso significa que el seguimiento más correcto en esta semana es por **workstreams y entregables auditables**, no únicamente por historias funcionales.

## 2. ¿Entonces son HUs o no?

La respuesta técnica es: **no necesariamente**.

Lo pedido por la guía mezcla dos naturalezas:

1. **Trabajo funcional/técnico del producto**
   - aquí sí pueden existir HUs o historias reconstruidas.
2. **Trabajo transversal de plataforma y calidad**
   - pipeline,
   - Docker,
   - escaneo de imagen,
   - evidencias,
   - release.

Ese segundo grupo normalmente no se gestiona bien como HU de usuario final, porque no describe valor de negocio visible para un comprador de tickets, sino capacidad operativa y calidad del equipo.

## 3. Propuesta de seguimiento para esta semana

Usar una **Épica Operativa Semana 4** con paquetes de trabajo trazables.

### EP-W4 — DevOps, Testing Multinivel y Evidencia de Release

| ID | Línea de trabajo | Tipo | Objetivo | Estado |
|---|---|---|---|---|
| W4-01 | Informe de pruebas | Entregable | consolidar plan y casos en `TEST_PLAN.md` | Completado |
| W4-02 | Estructura de evidencias | Entregable | centralizar evidencia en `docs/evidencias/semana4/` | Completado |
| W4-03 | Hardening Docker | Plataforma | revisar y endurecer Dockerfiles según rúbrica | En progreso |
| W4-04 | Workflow CI/CD | Plataforma | crear pipeline con jobs separados por nivel | Pendiente |
| W4-05 | Caja Negra en contenedores | Calidad | ejecutar al menos una prueba black-box sobre entorno orquestado | En progreso |
| W4-06 | Escaneo de imagen | Seguridad | incorporar análisis de vulnerabilidades en pipeline | Pendiente |
| W4-07 | Evidencia de pipeline | Evidencia | guardar capturas, artifacts y resultados del workflow | Pendiente |
| W4-08 | Release GitFlow | Release | preparar evidencia de PR y release `develop -> main` | Pendiente |
| W4-09 | Guion de defensa | Auditoría | preparar respuestas para human check | Pendiente |

## 4. Relación con las HUs reconstruidas existentes

Las HUs reconstruidas del plan de pruebas **sí siguen sirviendo**, pero para otro propósito:

- HU-R01 a HU-R06 organizan **el alcance funcional que se prueba**.
- W4-01 a W4-09 organizan **el trabajo operativo para cumplir la guía de evaluación**.

En otras palabras:

- las HUs reconstruidas responden **qué backend estamos protegiendo**,
- el plan de Semana 4 responde **cómo demostramos calidad, automatización y release**.

## 5. Regla práctica de seguimiento

Mientras dure Semana 4, cada cambio debería mapearse a dos ejes si aplica:

1. **Impacto funcional**
   - HU-R01 ... HU-R06
2. **Entregable operativo**
   - W4-01 ... W4-09

Ejemplo:

- Si se agrega un job E2E que valida expiración por RabbitMQ:
  - impacto funcional: `HU-R06`
  - seguimiento operativo: `W4-04`, `W4-05`, `W4-07`

### Principio adicional: paridad local antes de CI

Para esta semana se adopta una regla operativa explícita:

- **nada debería entrar al workflow si antes no funciona localmente con el mismo comando o con un equivalente directo**;
- el workflow futuro debe ser una formalización de ejecuciones locales confiables, no un lugar para descubrir por primera vez si algo compila, construye o prueba;
- por eso, antes de construir `W4-04`, debe existir una matriz local mínima de build y suites ejecutables.

## 6. Orden recomendado de ejecución

1. W4-03 Hardening Docker
2. W4-04 Workflow CI/CD
3. W4-05 Caja Negra en contenedores
4. W4-06 Escaneo de imagen
5. W4-07 Evidencia de pipeline
6. W4-08 Release GitFlow
7. W4-09 Guion de defensa

## 7. Cierre

Para esta semana, el seguimiento correcto no es forzar todo como HU. Lo más sólido es usar:

- **HUs reconstruidas** para trazabilidad funcional,
- y **workstreams W4** para ejecución, evidencia y defensa del entregable.

En consecuencia, el paso previo inmediato al workflow es dejar estabilizado el comportamiento local esperado de build, suites y scripts candidatos a caja negra.

Estado actual de esa precondición:

- build local backend validado,
- suites mínimas por nivel validadas localmente,
- caja negra general y caja negra de expiración ejecutadas con éxito en entorno compose local.
