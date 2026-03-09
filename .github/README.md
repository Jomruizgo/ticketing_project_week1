# ASD – Agentic Spec-Driven Development para TicketRush

Marco operativo de agentes y prompts para analizar requerimientos, coordinar implementación y producir artefactos trazables dentro del repo TicketRush.

## Estructura

```
.github/
├── agents/            # agentes del flujo y subagentes especializados
├── prompts/           # puntos de entrada del ecosistema
├── skills/            # habilidades reutilizables por dominio
├── docs/
│   ├── config/        # configuración del usuario y carpetas de salida
│   ├── lineamientos/  # reglas generales, dev y QA
│   ├── context/       # dominio TicketRush, arquitectura, ADRs, DoR y DoD
│   └── output/        # artefactos generados por los agentes
└── INDEX.md           # inventario detallado
```

## Fuente de verdad actual

El framework quedó alineado a TicketRush y asume como contexto real del proyecto:

- `frontend` en Next.js,
- `producer` como API de comando asíncrono,
- `crud_service` como API síncrona de lectura/administración,
- `ReservationService` y `paymentService` como workers,
- RabbitMQ y PostgreSQL como infraestructura principal.

Los outputs del flujo se guardan en `.github/docs/output/`.

Además del flujo GAIDD y de los agentes de implementación/QA por artefacto, existe ahora un flujo separado para **reconstrucción del baseline del proyecto completo**. Ese flujo no depende de una HU ni de un SPEC puntual: inspecciona el repositorio tal como existe y prepara el insumo para QA global y automatización de verificación.

## Inicio rápido

### 1. Configura tu perfil

Edita `.github/docs/config/config.yaml`.

### 2. Ejecuta el flujo completo

Usa el prompt principal:

- `agent_full-flow.prompt.md`

Si tu herramienta expone prompts como comandos, normalmente el nombre base del archivo es el identificador a invocar.

## Prompts disponibles

| Archivo | Descripción |
| --- | --- |
| `agent_full-flow.prompt.md` | Flujo completo: clasificación → validación → análisis → selección de agentes |
| `agent_spec.prompt.md` | Ejecuta solo el pipeline GAIDD de especificación |
| `agent_backend.prompt.md` | Activa directamente el agente de backend |
| `agent_frontend.prompt.md` | Activa directamente el agente de frontend |
| `agent_qa.prompt.md` | Activa directamente el agente de QA |
| `agent_project-baseline.prompt.md` | Reconstruye el baseline del proyecto completo antes del QA global |
| `agent_spec_gaidd.granularity-classifier.prompt.md` | Paso 0: clasifica HU vs requerimiento tradicional |
| `agent_spec_gaidd.requirement-validator.prompt.md` | Paso 2: valida completitud y viabilidad |
| `agent_spec_gaidd.requirement-conflict-resolver.prompt.md` | Paso 2.1: resuelve hallazgos del validador |
| `agent_spec_gaidd.requirement-analysis.prompt.md` | Paso 3: análisis técnico del requerimiento |

## Pipeline GAIDD actual

```
Artefacto de entrada
       ↓
[Paso 0] Clasificación del tipo de artefacto
       ↓
[Paso 1] Evaluación de granularidad
       ↓
[Paso 2] Validación de completitud y viabilidad
       ↓
[Paso 2.1] Resolución de conflictos del validador
       ↓
[Paso 3] Análisis técnico del requerimiento
       ↓
Selección de agente especializado
       ↓
Outputs en .github/docs/output/
```

## Flujo separado de baseline del proyecto

Usa este flujo cuando no exista un catálogo centralizado de requisitos/HUs y necesites reconstruir la fuente de verdad del sistema completo antes de planificar QA global:

```
agent_project-baseline.prompt.md
       ↓
agent_project-baseline.agent.md
       ↓
.github/docs/output/baseline/
```

## Notas importantes para TicketRush

1. Reserva y pago se asumen asíncronos salvo evidencia explícita en contrario.
2. `scripts/setup-rabbitmq.sh` es la fuente de verdad para la topología del broker.
3. `frontend/lib/api.ts` es el cliente HTTP canónico del frontend.
4. Si cambias contrato, evento o esquema, debes revisar todos los consumidores afectados.
5. Si quieres un plan maestro de pruebas del proyecto completo, ejecuta primero `agent_project-baseline.prompt.md` y después el flujo de QA.

## Documentación adicional

Consulta [INDEX.md](INDEX.md) para el inventario completo de agentes, prompts, skills y contexto.

