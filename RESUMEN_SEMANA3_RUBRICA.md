# Resumen Semana 3 — Rúbrica

---

### Fase 1 — Dev A: TDD Atómico (Criterio: ciclo RED → GREEN → REFACTOR)

- Se identificó el caso de uso: expiración automática de reservas
- **RED** `f676b8a`: se escribieron los tests primero (fallando)
- **GREEN** `83eccf1`: se implementó el mínimo código para pasarlos
- **REFACTOR** `3f4638c`: se desacoplaron responsabilidades sin romper tests
- **E2E** `9ba2bcd`: se agregó script Docker reproducible (`scripts/verify-devA-expiration.sh`)

---

### Fase 2 — Dev B: Migración Hexagonal + TDD (Criterio: SOLID + mocks)

- Se migró `crud_service` de N-Layer → Hexagonal (Domain ← Application ← Infrastructure ← Api)
- Se aplicaron ISP, DIP, SRP sobre `TicketStatusHub`, `TicketStatusConsumer`, `TicketsController`
- Se crearon 58 tests con NSubstitute (mocks de `ITicketStatusNotifier`, `ITicketStatusSubscriber`)
- Build: 0 errores — Tests: 58/58 verde

> **⚠️ Human check #1 — Scope creep en PR #18 (Dev A frenó al agente)**
> El agente empujó un PR con ~184 archivos incluyendo PDFs y archivos no relacionados al alcance.
> Dev A emitió `CHANGES_REQUESTED` con 3 blockers: scope, conflictos de merge, y higiene del repo.
> El agente no lo había detectado solo — fue el humano quien revisó y señaló el problema.

> **⚠️ Human check #2 — Cherry-pick descartado, decisión de migración hexagonal**
> El agente intentó recuperar el trabajo via cherry-pick de commits anteriores.
> Cuando falló, el humano intervino y decidió cambiar la estrategia completa: hacer la migración hexagonal desde cero en lugar de seguir parcheando el historial.
> El agente habría seguido intentando el cherry-pick sin esa intervención.

---

### Fase 3 — Code Review (Criterio: revisión entre pares)

- PR #18 (Dev B) recibió `CHANGES_REQUESTED` de Dev A → se atendieron los 3 blockers (scope, conflictos, higiene)
- PR #19 (Dev A docs) fue revisado por Dev B → aprobado y mergeado con comentario fundamentado en la rúbrica

---

### Fase 4 — Reportes y Evidencia (Criterio: reportes automatizados + Verificar vs Validar)

- Se generaron reportes `.trx` para los 3 servicios (92 tests en verde)
- Se completó la tabla Verificar/Validar con 11 casos mapeados a tests concretos
- Se creó `TESTING_STRATEGY.md` con estrategia QA completa (pirámide, ciclo TDD, mocks, E2E)

---

### Fase 5 — Cierre documental

- `CHECKLIST_RUBRICA_SEMANA3.md` → todos los ítems `[x]`, veredicto: **Conforme (Nivel Experto)**
- `RESULTADOS_TESTS_SEMANA3.md` → estado `Parcial` corregido a **Conforme**
- Commit y push a `develop` ✅

> **⚠️ Human check #4 — Git notes descartados**
> El todo list incluía "Documentar TDD Dev B en git notes". El agente lo tenía planificado.
> El humano revisó el estado y determinó que la documentación en los archivos `.md` ya era suficiente evidencia — los git notes no aportaban valor adicional y se descartaron.

---

**Pendiente**: PR #18 abierto — requiere aprobación de Dev A para mergear.
