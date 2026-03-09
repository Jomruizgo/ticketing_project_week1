# Evidencias Semana 4

Este directorio centraliza la evidencia auditable de la **Semana 4**.

## Objetivo

Organizar en un solo lugar los entregables y evidencias solicitadas por la guía de evaluación de Semana 4:

- plan de pruebas formal,
- definición y evidencia del pipeline,
- evidencias de pruebas multinivel,
- evidencias de build y análisis de imagen,
- y evidencia del flujo de release con GitFlow.

## Contenido

- `TEST_PLAN.md`: copia del plan de pruebas entregable para la semana.
- `TEST_CASES.md`: casos de prueba detallados separados del plan maestro para ejecución y trazabilidad operativa.
- `JUSTIFICACION_TEORICA_7_PRINCIPIOS.md`: soporte académico complementario con la fundamentación de los 7 principios de testing aplicados al proyecto.
- `CHECKLIST_RUBRICA_SEMANA4.md`: checklist interno de cumplimiento contra la guía.
- `PLAN_SEGUIMIENTO_SEMANA4.md`: seguimiento operativo del trabajo de Semana 4.
- `MATRIZ_LOCAL_PREWORKFLOW_SEMANA4.md`: comandos y validaciones locales que deben funcionar antes de construir el workflow.
- `RESULTADOS_PIPELINE_SEMANA4.md`: registro de ejecuciones, jobs, enlaces, capturas y artifacts del pipeline.
- `capturas/`: screenshots del pipeline, jobs, PRs, scans o releases.
- `artifacts/`: reportes exportados (`.trx`, logs, reportes de scan, salidas resumidas) cuando aplique.
- `artifacts/README.md`: criterio mínimo para qué tipo de artifacts conviene conservar como evidencia.

## Criterio de organización

Cada evidencia debería incluir, cuando exista:

- fecha,
- comando o workflow ejecutado,
- resultado resumido,
- ruta del artifact o captura,
- y observaciones relevantes.

## Relación con la guía

La guía de evaluación de Semana 4 no pide exclusivamente historias de usuario. Pide principalmente **entregables auditables** de testing, Docker, CI/CD y release. Por eso en esta carpeta el seguimiento se organiza por:

- entregables,
- workstreams,
- y evidencia verificable.

## Estado actual

- ✅ Plan de pruebas: `TEST_PLAN.md` disponible.
- ✅ Casos de prueba detallados: `TEST_CASES.md` separado del plan maestro.
- ✅ Justificación teórica: `JUSTIFICACION_TEORICA_7_PRINCIPIOS.md` separada del plan maestro.
- ✅ Pipeline CI/CD: implementado y ejecutado — 9/9 jobs verdes en PR #24.
- ✅ Escaneo de imagen: Trivy activo — 4 reportes archivados en `artifacts/trivy/`.
- ✅ Evidencia de release GitFlow: PRs #23 y #24, tag v3.0.1, capturas en `capturas/`.
- ✅ Artifacts de tests: `.trx` reales del CI en `artifacts/` (unit, component, integration, blackbox).
