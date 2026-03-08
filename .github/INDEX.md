# Índice del Proyecto ASD para TicketRush

Inventario de agentes, prompts, skills y documentación ya alineados al dominio distribuido de TicketRush.

## 🤖 Agentes (`agents/`)

| Archivo | Rol | Descripción |
| --- | --- | --- |
| `agent_orchestrator.agent.md` | Orchestrator | Coordina el pipeline GAIDD y la selección de agentes especializados |
| `agent_spec.agent.md` | Spec Agent | Genera una especificación técnica consolidada |
| `agent_backend.agent.md` | Backend Agent | Implementación backend orientada a contratos, eventos y persistencia |
| `agent_frontend.agent.md` | Frontend Agent | Implementación frontend orientada a UI y flujos asíncronos |
| `agent_qa.agent.md` | QA Agent | Estrategia de pruebas, riesgo, automatización y performance |
| `agent_project-baseline.agent.md` | Baseline Reconstruction Agent | Reconstruye la línea base funcional y técnica del proyecto completo desde evidencia del repositorio |
| `agent_spec_gaidd.epic-vs-user-story-evaluator.agent.md` | Evaluador INVEST | Paso 1 para historias de usuario |
| `agent_spec_gaidd.high-level-requirement-evaluator.agent.md` | Evaluador IEEE | Paso 1 para requerimientos tradicionales |
| `agent_spec_gaidd.requirement-validator.agent.md` | Validador | Paso 2: completitud y viabilidad técnica |
| `agent_spec_gaidd.requirement-conflict-resolver.agent.md` | Resolutor | Paso 2.1: resuelve hallazgos del validador |
| `agent_spec_gaidd.requirement-analysis.agent.md` | Analizador Técnico | Paso 3: análisis técnico del requerimiento |

## 📝 Prompts (`prompts/`)

### Flujos principales

| Archivo | Descripción |
| --- | --- |
| `agent_full-flow.prompt.md` | Punto de entrada recomendado: GAIDD + selección de agentes |
| `agent_spec.prompt.md` | Ejecuta solo el pipeline GAIDD |
| `agent_backend.prompt.md` | Activa directamente el backend agent |
| `agent_frontend.prompt.md` | Activa directamente el frontend agent |
| `agent_qa.prompt.md` | Activa directamente el QA agent |
| `agent_project-baseline.prompt.md` | Activa el flujo de reconstrucción del baseline del proyecto completo |

### Prompts del pipeline GAIDD

| Archivo | Paso | Descripción |
| --- | --- | --- |
| `agent_spec_gaidd.granularity-classifier.prompt.md` | Paso 0 | Clasifica HU vs requerimiento tradicional |
| `agent_spec_gaidd.requirement-validator.prompt.md` | Paso 2 | Valida completitud y viabilidad |
| `agent_spec_gaidd.requirement-conflict-resolver.prompt.md` | Paso 2.1 | Resuelve hallazgos del Paso 2 |
| `agent_spec_gaidd.requirement-analysis.prompt.md` | Paso 3 | Genera análisis técnico |

## 🛠️ Skills (`skills/`)

### QA

- `skill_qa_test-strategy-planner.md`
- `skill_qa_gherkin-case-generator.md`
- `skill_qa_risk-identifier.md`
- `skill_qa_test-data-specifier.md`
- `skill_qa_critical-flow-mapper.md`
- `skill_qa_regression-strategy.md`
- `skill_qa_automation-flow-proposer.md`
- `skill_qa_performance-analyzer.md`

### Backend

- `skill_backend_clean-code-reviewer.md`
- `skill_backend_integration-test-generator.md`
- `skill_backend_contract-test-generator.md`

### Frontend

- `skill_frontend_component-reviewer.md`
- `skill_frontend_accessibility-checker.md`
- `skill_frontend_ui-test-generator.md`

## 📚 Documentación (`docs/`)

### Configuración

- `config/config.yaml`

### Lineamientos

- `lineamientos/guidelines.md`
- `lineamientos/dev-guidelines.md`
- `lineamientos/qa-guidelines.md`
- `lineamientos/baseline-guidelines.md`

### Contexto TicketRush

- `context/project_architecture.context.md`
- `context/project_architecture_standards.context.md`
- `context/project_structure_principles.context.md`
- `context/tech_stack_constraints.context.md`
- `context/business_domain_dictionary.context.md`
- `context/architecture_decision_records.context.md`
- `context/database_schema.context.md`
- `context/api_integration_contracts.context.md`
- `context/security_and_compliance_requirements.context.md`
- `context/definition_of_ready.context.md`
- `context/definition_of_done.context.md`
- `context/reglas-de-oro.md`

## 📦 Outputs (`docs/output/`)

```
.github/docs/output/
├── {artifact_id}/
│   ├── {artifact_id}.step_1.epic_vs_user-story_evaluation.md
│   ├── {artifact_id}.step_2.requirement-validator.md
│   ├── {artifact_id}.step_2.resolution-of-conflicts.md
│   └── {artifact_id}.step_3.requirement-analysis.md
├── baseline/
├── qa/
├── backend/
└── frontend/
```

## 🔗 Relación principal del flujo

```
agent_full-flow.prompt.md
       ↓
agent_orchestrator.agent.md
       ↓
agent_spec_gaidd.granularity-classifier.prompt.md
       ↓
evaluador INVEST o evaluador IEEE
       ↓
agent_spec_gaidd.requirement-validator.agent.md
       ↓
agent_spec_gaidd.requirement-conflict-resolver.agent.md
       ↓
agent_spec_gaidd.requirement-analysis.agent.md
       ↓
Backend / Frontend / QA
       ↓
.github/docs/output/
```

## 🔎 Flujo separado para baseline del proyecto completo

```
agent_project-baseline.prompt.md
       ↓
agent_project-baseline.agent.md
       ↓
.github/docs/output/baseline/
       ├── project-requirements-baseline.md
       ├── requirements-traceability-matrix.md
       ├── system-scope-map.md
       └── open-questions-and-gaps.md
```

## ⚙️ Configuración requerida

El framework depende de `docs/config/config.yaml` y asume `.github/docs/output/` como carpeta única de salida.
