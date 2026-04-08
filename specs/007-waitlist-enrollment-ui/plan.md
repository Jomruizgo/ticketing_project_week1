# Implementation Plan: Waitlist Enrollment UI

**Branch**: `007-waitlist-enrollment-ui` | **Date**: 2026-04-08 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/007-waitlist-enrollment-ui/spec.md`

## Summary

Integrar un formulario de inscripción en lista de espera en la página de compra de eventos del frontend Next.js existente. Cuando un evento no tiene entradas disponibles y su fecha no ha pasado, se muestra un formulario de email con «Unirse a la lista de espera» en lugar del flujo de compra normal. El formulario envía POST /api/waitlist/entries al CRUD Service y maneja respuestas 201 (éxito), 409 (duplicado) y 422 (lista cerrada). No se modifica el backend — esta feature consume la API existente de HU1.

## Technical Context

**Language/Version**: TypeScript 5.x, React 18/19, Next.js 15 (App Router)
**Primary Dependencies**: React, Next.js, Tailwind CSS, shadcn/ui (Card, Button, Input, Label), SWR (data fetching/polling), sonner (toast), lucide-react (iconos)
**Storage**: N/A (feature puramente frontend; los datos persisten en el CRUD Service backend)
**Testing**: No hay framework de test configurado actualmente en el frontend (sin jest, vitest ni testing-library en package.json). Los test cases TC-HU7-01 a TC-HU7-04 son **tipo A (aceptación)** — se validan manualmente o mediante E2E sobre el sistema desplegado. Si se decide automatizar, se debe agregar la dependencia de testing correspondiente.
**Target Platform**: Navegadores web modernos (Chrome, Firefox, Safari, Edge)
**Project Type**: Aplicación web (frontend SPA dentro de monorepo de sistema distribuido)
**Performance Goals**: Inscripción completable en menos de 30 segundos desde la vista del formulario (SC-001)
**Constraints**: El formulario debe integrarse dentro de la página de compra existente (`/buy/[id]`) sin romper el flujo actual
**Scale/Scope**: 1 componente nuevo, 1 función de API nueva, 1 tipo nuevo, 1 modificación de página existente

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Aplica | Estado | Notas |
|-----------|--------|--------|-------|
| I. Hexagonal Architecture with DDD | No directamente | ✅ PASS | Feature puramente frontend. No se modifica Domain ni Infrastructure de ningún microservicio. La separación de capas existente del frontend (lib/api.ts como adaptador, componentes como UI, hooks como application) se respeta |
| II. Asynchronous-First Pipeline | Parcialmente | ✅ PASS | El POST /api/waitlist/entries es síncrono (201 directo, no 202 Accepted). Esto es correcto: la inscripción en waitlist es una operación CRUD síncrona del crud_service, no pasa por el pipeline asíncrono de RabbitMQ |
| III. TDD Strict | Parcialmente | ⚠️ NOTA | No hay framework de testing configurado en el frontend. Los test cases TC-HU7-* son tipo A (aceptación manual). Se documenta esta limitación. El ciclo RED→GREEN→REFACTOR se aplicará a nivel de componente siguiendo la secuencia lógica, pero la ejecución automatizada de tests requiere agregar dependencias de testing |
| IV. Testing Pyramid | Parcialmente | ⚠️ NOTA | Sin infraestructura de test en frontend. Los 4 test cases definidos son de aceptación (capa superior de la pirámide). Se documenta que la base (unit) y medio (integration) de la pirámide no se cubren en esta iteración |
| V. SOLID | Sí | ✅ PASS | SRP: WaitlistEnrollForm tiene una sola responsabilidad (formulario de inscripción). OCP: se extiende la página de compra sin modificar sus componentes internos. DIP: el componente depende de la abstracción api, no de fetch directo |
| VI. GoF-Only Design Patterns | N/A | ✅ PASS | No se introducen patrones de diseño en esta feature |
| VII. Code Quality: Zero AI Smells | Sí | ✅ PASS | Nombres descriptivos, sin code muerto, sin imports no usados |
| VIII. Language Convention | Sí | ✅ PASS | Código en inglés, documentación en español, commits en inglés con Conventional Commits |

**Gate result**: ✅ PASS con notas documentadas sobre testing.

## Project Structure

### Documentation (this feature)

```text
specs/007-waitlist-enrollment-ui/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
frontend/
├── app/
│   └── buy/
│       └── [id]/
│           └── page.tsx           # MODIFICAR: renderizado condicional waitlist vs compra
├── components/
│   ├── waitlist-enroll-form.tsx    # NUEVO: componente de formulario de inscripción
│   └── ui/                        # Existente: Button, Input, Label, Card, etc.
├── lib/
│   ├── api.ts                     # MODIFICAR: agregar enrollInWaitlist()
│   └── types.ts                   # MODIFICAR: agregar WaitlistEntryDto
└── hooks/                         # Sin cambios
```

**Structure Decision**: Se integra dentro de la estructura frontend existente. Un solo componente nuevo (`waitlist-enroll-form.tsx`) siguiendo la convención de nombres existente (kebab-case). Las modificaciones se limitan a 3 archivos existentes + 1 archivo nuevo.

## Complexity Tracking

> Sin violaciones de constitución que justificar. La feature es de baja complejidad: 1 componente, 1 función API, renderizado condicional en 1 página.
