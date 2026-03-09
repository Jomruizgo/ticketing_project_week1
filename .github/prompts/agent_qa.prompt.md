---
description: 'Ejecuta el QA Agent con todos sus 8 skills para generar el plan de calidad completo del proyecto.'
agent: 'agent'
---

Ejecuta el QA Agent completo con todos sus 8 skills en secuencia.

**Instrucciones para @qa-agent:**

1. Cargar `.github/docs/lineamientos/qa-guidelines.md` como primer paso
2. Leer el contexto completo del SPEC desde `.github/docs/output/{HU-ID}/{HU-ID}.step_3.requirement-analysis.md`
3. Ejecutar los 8 skills en orden estricto:
   - SKILL 1: `test-strategy-planner` → generar `.github/docs/output/qa/test-strategy.md`
   - SKILL 2: `gherkin-case-generator` → generar features en `.github/docs/output/qa/features/`
   - SKILL 3: `risk-identifier` → generar `.github/docs/output/qa/risk-matrix.md`
   - SKILL 4: `test-data-specifier` → generar `.github/docs/output/qa/data/test-data-catalog.md`
   - SKILL 5: `critical-flow-mapper` → generar `.github/docs/output/qa/critical-flows.md`
   - SKILL 6: `regression-strategy` → generar `.github/docs/output/qa/regression-plan.md`
   - SKILL 7: `automation-flow-proposer` → generar `.github/docs/output/qa/automation-roadmap.md`
   - SKILL 8: `performance-analyzer` → generar `.github/docs/output/qa/performance-plan.md`
4. Generar reporte consolidado de calidad al finalizar

**Prerequisito:** Debe existir `.github/docs/output/{HU-ID}/{HU-ID}.step_3.requirement-analysis.md`. Si no existe, ejecutar `agent_spec.prompt.md` primero.
