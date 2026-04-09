# Metodología de Proyecto — TicketRush MVP

**Versión:** 1.0.0  
**Fecha:** 2026-04-09  
**Complementa:** `.specify/memory/constitution.md` (principios técnicos)

Este documento define **cómo se gestiona el trabajo** en TicketRush: cómo se planifica, se diseña, se implementa, se rastrea y se documenta.

---

## 1. Tipos de trabajo

Todo el trabajo se clasifica en uno de tres tipos:

| Tipo | Origen | Ejemplo | Etiqueta GitHub |
|------|--------|---------|-----------------|
| **Feature** | Requisito de negocio nuevo | "Inscripción en lista de espera" | `feature` |
| **Deuda / Fix** | Reality Check, DEBT_REPORT, bug encontrado | "TicketReturnedConsumer no implementado" | `tech-debt`, `bug`, `security`, `incomplete` |
| **Decisión arquitectónica** | Necesidad transversal sin implementación inmediata | "Definir estrategia de autenticación" | `decision` |

Cada tipo sigue un flujo distinto, y todos convergen en el mismo **GitHub Project Board**.

---

## 2. Flujo de Features

Una feature (épica) pasa por dos grandes fases: **planificación humana** y **ejecución asistida por IA**. La planificación produce los artefactos de diseño; Speckit los consume para ejecutar la implementación.

### 2.1 Fase 1 — Planificación humana

Esta fase es deliberadamente manual e intelectual. Su objetivo es pensar y diseñar antes de escribir código.

```
  ① Problema y contexto
     │  Definir el problema que resuelve la feature, los supuestos,
     │  el alcance (qué incluye y qué no) y el lenguaje de negocio.
     │
  ② Refinamiento en Historias de Usuario
     │  Descomponer la épica en HUs con criterios de aceptación en Gherkin.
     │  Aplicar INVEST a cada HU.
     │
  ③ Diseño arquitectónico y diagramas
     │  Producir los diagramas que la feature necesite
     │  (ver sección 6: Diagramas).
     │
  ④ Patrones de diseño
     │  Identificar qué patrones GoF ayudan a sostener las decisiones
     │  de negocio. Documentar cada patrón con problema, diagrama UML
     │  y código declarativo (ver sección 7: Patrones de diseño).
     │
  ⑤ Plan de pruebas y casos de test
     │  Definir TestPlan (estrategia, riesgos, niveles)
     │  y TestCases (casos concretos por HU).
     │
  ⑥ Decisiones de diseño
     │  Documentar compromisos arquitectónicos, decisiones sobre BD,
     │  herramientas de soporte, y patrones de orquestación/resiliencia
     │  que podrían necesitarse en el futuro.
     │
     ▼
  Artefacto resultante: Planning.md
  (documento único que contiene todo lo anterior)
```

#### Artefactos de la fase 1

| Artefacto | Contenido | Formato |
|-----------|-----------|---------|
| `Planning.md` | Problema, alcance, HUs con Gherkin, decisiones de diseño, patrones, herramientas, compromisos arquitectónicos | Markdown |
| `drawio/*.drawio` + `*.png` | Diagramas C4, secuencia, esquema BD | draw.io + PNG exportado |
| `patterns/*.md` + `*.drawio` + `*.png` | Un archivo por patrón de diseño: problema, diagrama UML, código declarativo | Markdown + draw.io + PNG |
| `TestPlan.md` | Estrategia de pruebas, riesgos, niveles, alcance | Markdown |
| `TestCases.md` | Casos de prueba concretos derivados de los criterios de aceptación | Markdown |

### 2.2 Puente — Generación de prompts para Speckit

Una vez que la planificación humana está completa, se preparan los prompts que alimentarán a Speckit. Esto se hace **una vez por HU**:

1. Se genera un prompt de `specify` por cada HU, extrayendo del Planning los criterios de aceptación, reglas de negocio y contexto técnico relevante.
2. Se genera un prompt de `plan` por cada HU, con el contexto arquitectónico, patrones, contratos y decisiones ya tomadas.

Estos prompts se guardan en `speckit-prompts/` como referencia y reproducibilidad:

```
speckit-prompts/
  ├── README.md              ← Orden de uso
  ├── constitution.md        ← Prompt para speckit.constitution (una vez)
  ├── hu1-specify.md         ← Prompt para speckit.specify de HU1
  ├── hu1-plan.md            ← Prompt para speckit.plan de HU1
  ├── hu2-specify.md
  ├── hu2-plan.md
  └── ...
```

### 2.3 Fase 2 — Ejecución con Speckit

Con los prompts listos, se ejecuta el pipeline de Speckit **por cada HU en orden**:

```
  Por cada HU (en orden de dependencia):

  ① speckit.specify   Recibe el prompt de specify → genera spec.md
         │
  ② speckit.clarify   Refina ambigüedades del spec (opcional)
         │
  ③ speckit.plan      Recibe el prompt de plan → genera plan.md + research.md
         │
  ④ speckit.tasks     Genera tasks.md ordenado por dependencia
         │
  ⑤ speckit.analyze   Valida coherencia spec ↔ plan ↔ tasks
         │
  ⑥ speckit.taskstoissues   Convierte tareas en GitHub Issues
         │
  ⑦ speckit.implement      Ejecuta las tareas con TDD (RED → GREEN → REFACTOR)
         │
         └──► PR con "Closes #N" → merge → issues cerrados
```

### 2.4 Pipeline completo (visual)

```
┌─────────────────────────────────────────────────────────────────────┐
│                    FLUJO COMPLETO DE UNA FEATURE                   │
└─────────────────────────────────────────────────────────────────────┘

  FASE 1: PLANIFICACIÓN HUMANA
  ─────────────────────────────
  Problema + contexto + alcance
       │
  Historias de Usuario (Gherkin, INVEST)
       │
  Diagramas (C4, secuencia, BD)
       │
  Patrones de diseño (GoF: problema + UML + código)
       │
  TestPlan + TestCases
       │
  Decisiones de diseño
       │
       ▼
  Planning.md + drawio/ + patterns/ + TestPlan.md + TestCases.md


  PUENTE: PROMPTS
  ───────────────
  Generación de prompts por HU
       │
       ▼
  speckit-prompts/hu1-specify.md, hu1-plan.md, ...


  FASE 2: EJECUCIÓN CON SPECKIT (por cada HU)
  ────────────────────────────────────────────
  speckit.specify → speckit.clarify → speckit.plan
       │
  speckit.tasks → speckit.analyze
       │
  speckit.taskstoissues → GitHub Issues
       │
  speckit.implement → TDD → PR → merge → issues cerrados
```

### 2.5 Ubicación de artefactos de una feature

```
docs/Features/FeatureN/       ← Planificación humana
  ├── Planning.md             ← Documento central (problema, HUs, decisiones)
  ├── TestPlan.md             ← Estrategia de pruebas
  ├── TestCases.md            ← Casos de prueba por HU
  ├── API_CONTRACTS.md        ← Contratos de API (si aplica)
  ├── drawio/                 ← Diagramas arquitectónicos
  │   ├── c4_contenedores.drawio / .png
  │   ├── c4_componentes_xxx.drawio / .png
  │   ├── secuencia_xxx.drawio / .png
  │   └── bd_esquema.drawio / .png
  ├── patterns/               ← Patrones de diseño
  │   ├── observer.md / .drawio / .png
  │   ├── strategy.md / .drawio / .png
  │   ├── state.md / .drawio / .png
  │   └── command.md / .drawio / .png
  └── speckit-prompts/        ← Prompts generados para Speckit
      ├── README.md
      ├── constitution.md
      ├── hu1-specify.md
      ├── hu1-plan.md
      └── ...

specs/NNN-feature-name/       ← Artefactos generados por Speckit
  ├── spec.md
  ├── plan.md
  ├── research.md
  ├── tasks.md
  └── checklists/
```

---

## 3. Flujo de Deuda Técnica y Fixes

Los hallazgos que no son features nuevas siguen un flujo más corto porque no requieren planificación humana ni Speckit.

### 3.1 Pipeline

```
  Origen del hallazgo
  (Reality Check, DEBT_REPORT, bug report, revisión de código)
         │
         ▼
  GitHub Issue directo
  (título, descripción, label, milestone, severidad)
         │
         ▼
  GitHub Project Board (Backlog)
         │
         ▼
  Branch fix/XXX o chore/XXX desde develop
         │
         ▼
  TDD si aplica → commits → PR con "Closes #N"
         │
         ▼
  Merge → issue cerrado automáticamente
```

### 3.2 ¿Cuándo y cómo se hace un Reality Check?

Un Reality Check es una radiografía del estado real del proyecto: cruza specs, código, infra, tests y documentación para detectar desalineaciones.

| Pregunta | Respuesta |
|----------|-----------|
| **¿Qué es?** | Un documento (`REALITY_CHECK.md`) que confronta lo planeado vs. lo implementado |
| **¿Quién lo hace?** | Un humano, un agente de IA, o ambos juntos |
| **¿Es un solo documento?** | Sí. Se reescribe con fecha nueva cada vez. El historial de Git preserva versiones anteriores |
| **¿Cada cuándo?** | No tiene cadencia fija. Se hace en momentos clave (ver tabla) |

**Cuándo hacer un Reality Check:**

| Momento | Por qué |
|---------|---------|
| Al cerrar una épica o milestone grande | Verificar que todo lo planeado quedó hecho |
| Antes de preparar un release o deploy | Encontrar bloqueantes antes de que sea tarde |
| Al incorporarse al proyecto o retomar después de una pausa | Entender qué hay realmente |
| Cuando el equipo percibe que "algo no cuadra" | Síntoma de desalineación entre documentación y realidad |

**Qué cubre:**

1. Features declaradas vs. implementadas (tabla por feature)
2. Contratos de API: spec vs. código real
3. Infraestructura: compose, schema, RabbitMQ
4. Tests: cobertura real y gaps
5. Seguridad: hallazgos concretos
6. Deuda técnica confirmada
7. Riesgos para producción

---

## 4. Decisiones Arquitectónicas (ADR)

### 4.1 ¿Qué es un ADR?

Un **Architecture Decision Record** documenta una decisión que afecta al sistema de forma **transversal** — cruza features o define una política del proyecto.

### 4.2 ADR vs. research.md vs. Planning.md

| Aspecto | Planning.md (humano) | research.md (Speckit) | ADR |
|---------|---------------------|----------------------|-----|
| **Scope** | Una épica/feature completa | Una HU específica | Todo el proyecto |
| **Pregunta** | "¿Cómo diseño esta feature?" | "¿Cómo implemento esta HU?" | "¿Por qué el sistema funciona así?" |
| **Vive en** | `docs/Features/FeatureN/` | `specs/NNN/research.md` | `docs/adr/ADR-NNN.md` |
| **Quién lo escribe** | Humano | IA (Speckit) | Humano o humano+IA |
| **Ejemplos** | Patrones GoF, diseño de la épica, compromisos sobre BD única, topología RabbitMQ | Partial unique index en EF Core, Testcontainers para integración | Auth para producción, CORS policy, estrategia de email |

### 4.3 ¿Y las cosas que sabemos que hay que hacer pero no se han priorizado?

Items como autenticación, rate limiting o email real se registran como **ADR con estado `proposed`**. Esto deja constancia de que el equipo es consciente de la necesidad, documentó las opciones, pero decidió postergar. Cuando se prioricen, se actualizan a `accepted` y se crea una feature con el flujo completo para implementarlos.

### 4.4 Formato

Los ADRs se guardan en `docs/adr/` con esta estructura:

```markdown
# ADR-NNN: Título de la decisión

**Fecha:** YYYY-MM-DD  
**Estado:** proposed | accepted | superseded | deprecated

## Contexto
¿Qué problema o necesidad motivó esta decisión?

## Opciones evaluadas
1. Opción A — pros/contras
2. Opción B — pros/contras

## Decisión
¿Qué se eligió y por qué?
(Si el estado es `proposed`, describe la recomendación pero deja claro que no se ha ejecutado)

## Consecuencias
¿Qué implica esta decisión? ¿Qué trade-offs se aceptan?
```

---

## 5. GitHub como centro de trazabilidad

### 5.1 Estructura

```
┌──────────────────────────────────────────────────────────────┐
│              GitHub Project Board "TicketRush"                │
├─────────────┬──────────────┬────────────┬───────────────────┤
│   Backlog   │ In Progress  │   Review   │       Done        │
├─────────────┼──────────────┼────────────┼───────────────────┤
│ #12 feature │ #3 fix       │ #7 feature │ #1 fix ✓          │
│ #13 feature │              │            │ #2 fix ✓          │
│ #14 debt    │              │            │ #5 feature ✓      │
│ #15 decision│              │            │                   │
└─────────────┴──────────────┴────────────┴───────────────────┘
```

### 5.2 Labels

| Label | Color | Cuándo se usa |
|-------|-------|---------------|
| `feature` | `#a2eeef` | Issues de features (generados por `speckit.taskstoissues` o manualmente) |
| `tech-debt` | `#fbca04` | Deuda técnica del DEBT_REPORT o Reality Check |
| `bug` | `#d73a4a` | Algo que debería funcionar y no funciona |
| `security` | `#b60205` | Hallazgo de seguridad (CORS, auth, SQLi, etc.) |
| `incomplete` | `#e99695` | Feature planeada en spec/plan que no se implementó completa |
| `documentation` | `#0075ca` | Cambios exclusivamente en docs o scripts |
| `decision` | `#d4c5f9` | ADR o decisión pendiente de resolver |

### 5.3 Milestones

| Milestone | Qué agrupa |
|-----------|------------|
| `mvp-hardening` | Hallazgos del Reality Check (deuda, fixes, seguridad) |
| `feature-NNN-nombre` | Issues generados de una feature (ej: `feature-009-auth`) |

### 5.4 Ciclo de vida de un issue

```
  Creado
  (automático vía taskstoissues o manual)
      │
      ▼
  Backlog en el Board
      │
      ▼
  Asignado + branch creado ──► In Progress
      │
      ▼
  PR abierto con "Closes #N" ──► Review
      │
      ▼
  PR mergeado ──► issue cerrado automáticamente ──► Done
```

---

## 6. Diagramas

Los diagramas son parte de la **planificación humana** (Fase 1). Se crean para pensar, discutir y validar decisiones antes de escribir código. No son artefactos de Speckit.

### 6.1 Herramienta

- **draw.io (diagrams.net)** — editor visual con archivos `.drawio` versionables en Git
- Cada diagrama se exporta también como `.png` para visualización rápida sin abrir draw.io

### 6.2 Tipos de diagrama y cuándo crearlos

| # | Tipo | Notación | Qué muestra | Cuándo se crea | Orden de lectura |
|---|------|----------|-------------|----------------|------------------|
| 1 | **C4 — Contenedores** | C4 Model (nivel 2) | Vista general del sistema: servicios, bases de datos, message broker, frontend y sus interacciones | Cuando la feature **agrega o modifica servicios/contenedores** | **Primero** — "¿Qué piezas tiene el sistema y cómo se conectan?" |
| 2 | **C4 — Componentes** | C4 Model (nivel 3) | Zoom a un servicio: controllers, handlers, repositorios, consumers, observers y sus dependencias | Cuando se necesita detallar **la arquitectura interna de un servicio** | **Segundo** — "¿Qué partes tiene este servicio por dentro?" |
| 3 | **Diagrama de secuencia** | UML Secuencia | Flujo temporal de mensajes entre actores y servicios para un escenario completo | Cuando la feature involucra **flujos asíncronos o multi-servicio** | **Tercero** — "¿Qué pasa paso a paso cuando el usuario hace X?" |
| 4 | **Esquema de base de datos** | ER / relacional | Tablas, columnas, tipos, foreign keys, constraints e índices | Cuando la feature **agrega o modifica tablas** | **Cuarto** — "¿Qué se persiste y cómo se relaciona?" |

### 6.3 ¿Son obligatorios?

No todos aplican a todas las features:

| Si la feature... | Diagramas recomendados |
|-----------------|----------------------|
| Agrega/modifica servicios o interacciones entre ellos | C4 Contenedores |
| Modifica la arquitectura interna de un servicio | C4 Componentes del servicio afectado |
| Tiene flujos asíncronos multi-servicio | Secuencia |
| Agrega/modifica tablas | Esquema BD |
| Es solo frontend (componentes UI) | Ninguno obligatorio (wireframes opcionales) |
| Es un fix o deuda técnica | Ninguno (salvo que cambie la arquitectura) |

### 6.4 ¿Cómo se crean?

1. **Manualmente** en draw.io (desktop o web) durante la planificación humana
2. Se exportan a `.png` para referencia rápida
3. Se versionan ambos archivos (`.drawio` + `.png`) en Git
4. Se referencian desde `Planning.md`

---

## 7. Patrones de diseño

La identificación de patrones de diseño es parte de la **planificación humana**. El objetivo es detectar qué patrones GoF ayudan a sostener las decisiones de negocio de la feature, **antes** de implementar.

### 7.1 Alcance

Solo se consideran patrones de la clasificación GoF (ver constitución del proyecto). No se clasifican DI/IoC/Composition Root como patrones de diseño.

### 7.2 Qué se documenta por patrón

Cada patrón identificado tiene su propio archivo en `patterns/` con tres secciones:

| Sección | Contenido |
|---------|-----------|
| **Problema que resuelve** | Qué problema **concreto de la feature** resuelve este patrón. No es una definición genérica del patrón sino su justificación en este contexto de negocio |
| **Diagrama UML** | Diagrama de clases o estados en draw.io que muestra la estructura del patrón aplicada al dominio de la feature (`.drawio` + `.png`) |
| **Código declarativo** | Fragmentos de código que ilustran puertos (Domain), implementaciones (Infrastructure) y uso (Application). Es guía de diseño, no código final |

### 7.3 Ejemplo de estructura

```
patterns/
  ├── observer.md         ← Notificación multicanal
  ├── observer.drawio
  ├── observer.png
  ├── strategy.md         ← Política de priorización
  ├── strategy.drawio
  ├── strategy.png
  ├── state.md            ← Ciclo de vida de la oportunidad
  ├── state.drawio
  ├── state.png
  ├── command.md           ← Casos de uso como objetos
  ├── command.drawio
  └── command.png
```

### 7.4 ¿Cuándo se identifican?

Durante la planificación humana (Fase 1, paso ④), después de definir las HUs y el diseño arquitectónico. La pregunta guía es: **"¿Qué patrones hacen que este diseño sea más escalable, testeable o mantenible?"**

No todas las features necesitan patrones nuevos. Si la feature reutiliza patrones ya documentados en features anteriores, basta con referenciarlos desde el `Planning.md`.

### 7.5 Relación con el Planning.md

El `Planning.md` contiene una **tabla resumen** de los patrones identificados con la justificación de negocio de cada uno. Los archivos en `patterns/` son el detalle técnico expandido (diagrama + código).

Ejemplo de tabla en Planning.md:

| Patrón | Justificación |
|--------|---------------|
| **Observer** | Cuando el estado de una oportunidad cambia, el sistema reacciona en múltiples canales sin acoplamiento |
| **Strategy** | La política de "a quién le toca" se puede cambiar sin deshacer el proceso de asignación |
| **State** | La oportunidad tiene estados con reglas claras de transición; si el negocio agrega un estado, el modelo lo soporta |
| **Command** | Las acciones de negocio existen como objetos formales con su propio handler, facilitando trazabilidad |

---

## 8. Resumen visual del flujo completo

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         METODOLOGÍA TICKETRUSH                             │
└─────────────────────────────────────────────────────────────────────────────┘

  FEATURE NUEVA                  DEUDA / FIX / BUG              DECISIÓN
  ─────────────                  ─────────────────              ─────────
  Planificación humana:          Reality Check                  Identificar
  · Planning.md                  o DEBT_REPORT                  necesidad
  · Diagramas (drawio)           o reporte de bug                    │
  · Patrones de diseño (GoF)          │                              │
  · TestPlan + TestCases               │                              ▼
       │                              │                         ADR (proposed)
       ▼                              │                         en docs/adr/
  Prompts para Speckit                │                              │
  (speckit-prompts/)                  │                              │
       │                              │                              │
       ▼ (por cada HU)               │                              │
  Speckit pipeline:                   │                              │
  specify → clarify → plan            │                              │
  → tasks → analyze                   │                              │
       │                              │                              │
       └──────────────┬───────────────┘                              │
                      │                                              │
                      ▼                                              │
              GitHub Issues ◄────────────────────────────────────────┘
              (taskstoissues o manual)
                      │
                      ▼
              GitHub Project Board
              (Backlog → In Progress → Review → Done)
                      │
                      ▼
              GitFlow branch
              (feature/ o fix/ o chore/)
                      │
                      ▼
              TDD: RED → GREEN → REFACTOR
              (speckit.implement o manual)
                      │
                      ▼
              PR con "Closes #N"
                      │
                      ▼
              Merge → issue cerrado
              Reality Check actualizado (si aplica)
              ADR actualizado a accepted (si aplica)
```

---

## 9. Orden de lectura de una feature existente

Para entender una feature ya implementada, leer en este orden:

```
  Planning.md                     ¿Qué problema resuelve? ¿Cuáles son las HUs?
    │
    ├──► C4 Contenedores (.png)   Vista general del sistema con la feature
    ├──► C4 Componentes (.png)    Zoom al servicio principal
    ├──► Secuencia (.png)         Flujo paso a paso por HU
    ├──► Esquema BD (.png)        Modelo de datos
    │
    ├──► patterns/*.md + *.png    Patrones GoF aplicados (problema + UML + código)
    │
    ├──► TestPlan.md              Estrategia de pruebas
    ├──► TestCases.md             Casos de prueba por HU
    │
    ▼
  specs/NNN/spec.md               Especificación formal (generada por Speckit)
    │
    ├──► plan.md                  Plan técnico de implementación
    ├──► research.md              Decisiones técnicas de implementación
    │
    ▼
  specs/NNN/tasks.md              Tareas ejecutadas en orden
```

---

## 10. Referencia rápida

| Necesito... | Uso... |
|------------|--------|
| Planificar una feature nueva | Fase 1 humana (Planning + diagramas + patrones + tests) → prompts → Speckit |
| Identificar patrones GoF para una feature | `patterns/` con problema, diagrama UML y código declarativo |
| Registrar un bug o deuda | GitHub Issue directo con label |
| Documentar una decisión transversal del proyecto | ADR en `docs/adr/` |
| Documentar una decisión técnica de una HU | `research.md` en la spec (Speckit) |
| Documentar el diseño completo de una épica | `Planning.md` + diagramas + patrones (humano) |
| Ver el estado real del proyecto | `REALITY_CHECK.md` (reescribir si está desactualizado) |
| Ver el progreso de trabajo | GitHub Project Board |
| Rastrear qué PR resolvió qué tarea | Issue cerrado automáticamente por PR |
| Entender una feature existente | Planning → diagramas → patrones → tests → spec → tasks |
| Registrar algo que se debe hacer pero no se ha priorizado | ADR con estado `proposed` |
