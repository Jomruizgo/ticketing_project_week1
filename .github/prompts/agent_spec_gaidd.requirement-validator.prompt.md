---
name: "Paso 2: Validación de completitud y viabilidad del requerimiento"
description: "Carga el agente validador y ejecuta el Paso 2 del pipeline GAIDD."
agent: 'agent'
tools: ["read", "edit", "search", "execute/createAndRunTask", "todo"]
---

1. Cargar `{project-root}/.github/docs/config/config.yaml` y almacenar sus variables de sesión.
2. Cargar `{project-root}/.github/docs/context/reglas-de-oro.md`.
3. Cargar el archivo completo `{project-root}/.github/agents/agent_spec_gaidd.requirement-validator.agent.md`.
4. Seguir todas las instrucciones de activación del agente cargado.

