---
name: "Paso 0: Clasificación de granularidad del artefacto"
description: "Clasifica el artefacto recibido como Historia de Usuario o Requerimiento Tradicional y activa el agente evaluador correspondiente."
agent: 'agent'
tools: ["read", "edit", "search", "execute/createAndRunTask", "todo"]
---

1. Cargar `{project-root}/.github/docs/config/config.yaml` y almacenar todos sus campos como variables de sesión.

2. Cargar `{project-root}/.github/docs/context/reglas-de-oro.md` y respetarlo durante toda la clasificación.

3. Determinar si el artefacto a evaluar ya fue proporcionado en la conversación:
   - Si ya está en contexto, continuar.
   - Si no está, solicitarlo al usuario indicando que puede ser una Historia de Usuario o un requerimiento tradicional. Detenerse y esperar respuesta.

4. Clasificar el artefacto aplicando estos criterios:

   **Historia de Usuario** si cumple al menos uno:
   - Tiene estructura narrativa del tipo `Como / Quiero / Para` o equivalente.
   - Está formulado desde la perspectiva de un actor o rol de negocio.
   - Incluye criterios BDD o campos típicos de backlog ágil.

   **Requerimiento Tradicional** si cumple al menos uno:
   - Está redactado como especificación funcional o no funcional del sistema.
   - Usa identificadores formales (`RF-*`, `RNF-*`, `REQ-*`).
   - Describe comportamiento o atributos de calidad sin narrativa centrada en usuario.

5. Si la clasificación es ambigua:
   - explicar por qué,
   - listar indicadores encontrados en ambas categorías,
   - pedir confirmación del tipo al usuario,
   - detenerse hasta recibirla.

6. Comunicar el tipo detectado en el idioma configurado con una justificación breve y pedir confirmación explícita.

7. Según el tipo confirmado, cargar y ejecutar el agente correspondiente:
   - **Historia de Usuario** → `{project-root}/.github/agents/agent_spec_gaidd.epic-vs-user-story-evaluator.agent.md`
   - **Requerimiento Tradicional** → `{project-root}/.github/agents/agent_spec_gaidd.high-level-requirement-evaluator.agent.md`

8. Al transferir el control al agente evaluador, indicar que:
   - el artefacto ya fue proporcionado,
   - el tipo ya fue clasificado y confirmado,
   - no debe volver a solicitar el mismo insumo salvo que detecte una inconsistencia real.

