---
description: 'Ejecuta el Backend Agent directamente con el contexto del SPEC existente.'
agent: 'agent'
---

Ejecuta el Backend Agent usando el SPEC disponible en `.github/docs/output/{HU-ID}/{HU-ID}.step_3.requirement-analysis.md`.

**Instrucciones para @backend-agent:**

1. Cargar `.github/docs/lineamientos/dev-guidelines.md` como primer paso
2. Leer el contexto del SPEC desde `.github/docs/output/{HU-ID}/{HU-ID}.step_3.requirement-analysis.md`
3. Ejecutar el flujo completo de implementación backend:
   - Lógica de negocio por HU asignadas al backend
   - Endpoints según contratos del SPEC
   - Capa de persistencia
   - Manejo de errores del dominio
   - Observabilidad
   - Validación y seguridad
4. Activar skills según se detecte la necesidad:
   - `clean-code-reviewer` para código generado
   - `integration-test-generator` para endpoints implementados
   - `contract-test-generator` si la arquitectura lo requiere
5. Preparar PR con checklist completo

**Prerequisito:** Debe existir `.github/docs/output/{HU-ID}/{HU-ID}.step_3.requirement-analysis.md`. Si no existe, ejecutar `agent_spec.prompt.md` primero.
