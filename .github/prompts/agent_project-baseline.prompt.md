---
description: 'Ejecuta el Project Baseline Reconstruction Agent para reconstruir el baseline funcional y técnico del sistema completo a partir del repositorio actual.'
agent: 'agent'
---

Ejecuta el `Project Baseline Reconstruction Agent`.

**Objetivo:** reconstruir una línea base consolidada del proyecto completo, claramente separada de los flujos por HU/SPEC y del `QA Agent`.

**Instrucciones para el agente:**

1. Leer `.github/docs/config/config.yaml`
2. Leer `.github/docs/lineamientos/baseline-guidelines.md`
3. Inspeccionar código, tests, scripts y documentación del repositorio para reconstruir la fuente de verdad del sistema actual
4. Generar obligatoriamente estos artefactos:
   - `.github/docs/output/baseline/project-requirements-baseline.md`
   - `.github/docs/output/baseline/requirements-traceability-matrix.md`
   - `.github/docs/output/baseline/system-scope-map.md`
   - `.github/docs/output/baseline/open-questions-and-gaps.md`
5. Distinguir cada hallazgo como `Confirmado`, `Inferido` o `Pendiente de validar`
6. Señalar contradicciones entre documentación, tests y código, priorizando el comportamiento ejecutable

**Importante:**

- Este prompt NO depende de `.github/docs/output/{HU-ID}/...`
- Este flujo es previo al plan maestro de pruebas del proyecto completo
- Este flujo NO reemplaza al `QA Agent`; prepara su insumo
