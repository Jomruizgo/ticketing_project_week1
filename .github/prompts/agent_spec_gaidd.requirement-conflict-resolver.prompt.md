---
name: "Paso 2.1: Resolución de conflictos del requerimiento"
description: "Analiza el reporte del Paso 2, resuelve hallazgos accionables y genera el documento puente hacia el análisis técnico."
agent: 'agent'
tools: ["read", "edit", "search", "execute/createAndRunTask", "todo"]
---

1. Cargar `{project-root}/.github/docs/config/config.yaml`.
2. Cargar `{project-root}/.github/docs/context/reglas-de-oro.md`.
3. Cargar el artefacto original y el archivo `{artifact_id}.step_2.requirement-validator.md` desde `.github/docs/output/{artifact_id}/`.
4. Si la decisión del validador es `DEVOLVER`, detener el flujo e informar que el artefacto requiere refinamiento humano antes de continuar.
5. Si la decisión es `CONTINUAR`, inventariar todos los hallazgos y clasificarlos por categoría, severidad y acción requerida.
6. Para cada hallazgo:
   - proponer una resolución concreta,
   - incluir texto corregido cuando aplique,
   - justificar la decisión,
   - marcar si la resolución es definitiva o provisional.
7. Generar `{artifact_id}.step_2.resolution-of-conflicts.md` en `.github/docs/output/{artifact_id}/`.
8. Mostrar solo un resumen ejecutivo con:
   - cantidad de hallazgos,
   - resoluciones definitivas,
   - resoluciones provisionales,
   - decisión de transición a Paso 3.

