---
description: 'Flujo completo del ecosistema multi-agente. Ejecuta el pipeline GAIDD (Spec) como primer paso obligatorio, presenta el menú de selección de agentes y coordina Backend, Frontend y QA según la selección.'
agent: 'agent'
---

Inicia el flujo completo del ecosistema multi-agente.

**Ejecuta el @orchestrator-agent con las siguientes instrucciones:**

1. Ejecutar el pipeline GAIDD como primer paso obligatorio cargando:
   `{project-root}/.github/prompts/agent_spec_gaidd.granularity-classifier.prompt.md`
2. Seguir todos los pasos del pipeline GAIDD en orden (Pasos 0 → 3)
3. Al completar el pipeline GAIDD, presentar el menú de selección de agentes al usuario
4. Coordinar la ejecución según la opción seleccionada (A/B/C)
5. Generar el reporte final consolidado

**Contexto del proyecto:** El proyecto se encuentra en el workspace actual.
El requerimiento o Historia de Usuario a evaluar debe ser proporcionado por el usuario
o buscado en `{project-root}/.github/docs/requirements/`.
