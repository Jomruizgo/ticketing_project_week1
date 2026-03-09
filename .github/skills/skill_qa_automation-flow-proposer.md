---
description: 'Skill que propone cuáles flujos automatizar, con qué framework, en qué orden y bajo qué criterios. Evalúa el retorno de inversión de la automatización basado en repetitividad, estabilidad, impacto y costo.'
---

# Skill: automation-flow-proposer [QA]

## Responsabilidad
Identificar qué flujos tienen mejor retorno de inversión para automatizar
y definir la hoja de ruta de automatización del proyecto.

---

## Los 4 Criterios de Automatización (TODOS deben cumplirse)

Un flujo es candidato a automatización SOLO si cumple los 4:

```
✅ CRITERIO 1 — REPETITIVO
   El flujo se ejecuta frecuentemente (por release, por sprint, o diariamente)
   Señales: está en la suite de regresión, es un smoke test, se re-ejecuta post-deploy

✅ CRITERIO 2 — ESTABLE
   El flujo no cambia con mucha frecuencia (UI estable, lógica de negocio consolidada)
   Señales: no está marcado como @wip, lleva más de 1 sprint sin cambios significativos

✅ CRITERIO 3 — ALTO IMPACTO
   Su falla en producción tiene consecuencias importantes
   Señales: clasificado como criticidad Alta en risk-matrix, en smoke suite, flujo crítico

✅ CRITERIO 4 — COSTO ALTO EN MANUAL
   Ejecutarlo manualmente es costoso en tiempo o propenso a error humano
   Señales: tiene > 5 pasos, requiere datos específicos, se ejecuta en múltiples browsers
```

---

## Matriz de Priorización de Automatización

Genera esta tabla ordenada por ROI (mayor a menor):

```markdown
| Flujo        | Repetitivo | Estable | Alto Impacto | Costo Manual | ROI Score | Prioridad |
|--------------|-----------|---------|--------------|--------------|-----------|-----------|
| FLUJO-001    | ✅ Alta    | ✅ Sí  | ✅ Alta      | ✅ Alto      | 4/4       | P1        |
| FLUJO-004    | ✅ Media   | ✅ Sí  | ✅ Alta      | ✅ Alto      | 4/4       | P1        |
| FLUJO-002    | ✅ Alta    | ✅ Sí  | ⚠️ Media    | ✅ Alto      | 3/4       | P2        |
| FLUJO-006    | ❌ Baja   | ✅ Sí  | ✅ Alta      | ❌ Bajo      | 2/4       | P3        |
| FLUJO-007    | ✅ Alta    | ❌ No  | ⚠️ Media    | ✅ Alto      | 2/4       | Posponer  |
```

---

## Selección del Framework de Automatización

Evalúa según el stack del proyecto desde el SPEC:

```
PARA APLICACIONES WEB (UI/E2E):
  Playwright → Primera opción si el stack es JavaScript/TypeScript
                Ventaja: multi-browser, API testing integrado, CI-first
  Cypress    → Si ya existe en el proyecto y el equipo lo conoce
                Ventaja: DX excelente, re-run en UI
  Selenium   → Solo si hay requerimiento de browsers legacy (IE11)

PARA APIs REST/GRPC (sin UI):
  Supertest  → Para Node.js/Express, integrado con Jest
  REST Assured → Para proyectos Java/Spring
  k6         → Si los tests de API tienen requerimentos de carga

PARA APLICACIONES MÓVILES:
  Appium     → Cross-platform iOS + Android
  Detox      → Para React Native
  XCUITest   → Para iOS nativo
  Espresso   → Para Android nativo

CRITERIO DE SELECCIÓN:
  1. Concurrencia con el stack tecnológico del proyecto
  2. Curva de aprendizaje del equipo de QA
  3. Integración nativa con el CI/CD actual
  4. Costo de mantenimiento a largo plazo
```

---

## Entregable: Hoja de Ruta de Automatización

Genera `{qa_output_folder}/automation-roadmap.md`:

```markdown
# Hoja de Ruta de Automatización — [Nombre del Proyecto]
**Versión:** 1.0 | **Fecha:** [fecha] | **Generado por:** QA Agent

## Framework Seleccionado
**[Nombre] — Justificación:** [razón basada en el SPEC del proyecto]

## Flujos a Automatizar (Prioridad P1)
[Tabla con los flujos P1 y su descripción de implementación]

## Flujos a Automatizar (Prioridad P2)
[Tabla con los flujos P2]

## Flujos Excluidos de Automatización
| Flujo     | Razón para NO automatizar              | Alternativa        |
|-----------|----------------------------------------|--------------------|
| FLUJO-XXX | Inestable, cambia cada sprint          | Testing exploratorio|
| FLUJO-YYY | Solo 1 ejecución por release           | Checklist manual   |

## Estimación de Esfuerzo
| Sprint | Flujos a implementar | Esfuerzo estimado |
|--------|---------------------|-------------------|
| Sprint 1 | FLUJO-001, FLUJO-004 | 4 días persona    |
| Sprint 2 | FLUJO-002, FLUJO-005 | 3 días persona    |

## Estructura de Carpetas del Framework
[Estructura propuesta para organizar los tests automáticos]
```

---

## Proceso de Análisis

```
PASO 1 → Revisar {qa_output_folder}/critical-flows.md y {qa_output_folder}/regression-plan.md
PASO 2 → Evaluar cada flujo contra los 4 criterios
PASO 3 → Calcular ROI score (0-4) por flujo
PASO 4 → Ordenar por prioridad P1 > P2 > P3 > Posponer
PASO 5 → Seleccionar framework según stack del SPEC
PASO 6 → Estimar esfuerzo por sprint
PASO 7 → Generar {qa_output_folder}/automation-roadmap.md
```

## Reporte

```
🤖 AUTOMATION-FLOW-PROPOSER [QA] — REPORTE
════════════════════════════════════════════════
Flujos evaluados:                X
  P1 (ROI = 4/4): automatizar ya: X
  P2 (ROI = 3/4): siguiente sprint: X
  P3 (ROI = 2/4): backlog:          X
  Posponer (ROI <= 1/4):            X

Framework recomendado:           [nombre]
Esfuerzo total estimado:         X días persona

Documento generado: {qa_output_folder}/automation-roadmap.md ✅
════════════════════════════════════════════════
```
