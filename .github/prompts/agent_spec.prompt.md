---
description: 'Ejecuta el pipeline GAIDD directamente para validar y analizar un requerimiento o Historia de Usuario (Pasos 0 → 3).'
agent: 'agent'
---

Ejecuta el pipeline GAIDD completo de especificación.

**Instrucciones:**

1. Cargar y ejecutar el clasificador de granularidad:
   `{project-root}/.github/prompts/agent_spec_gaidd.granularity-classifier.prompt.md`
2. Seguir TODAS las instrucciones del archivo cargado sin omitir ningún paso
3. El pipeline ejecutará internamente en orden:
   - Paso 0: Clasificación del artefacto (HU o Requerimiento Tradicional)
   - Paso 1: Evaluación de granularidad (INVEST o IEEE 830/ISO 29148)
   - Paso 2: Validación de completitud y viabilidad técnica
   - Paso 2.1 (opcional): Resolución de conflictos
   - Paso 3: Análisis técnico del requerimiento
4. Al finalizar preguntar al usuario con qué agente desea continuar

**Nota:** Este prompt ejecuta solo el pipeline GAIDD. Para el flujo completo usa `agent_full-flow.prompt.md`.
