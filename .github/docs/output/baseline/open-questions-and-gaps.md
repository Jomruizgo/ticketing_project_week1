# Preguntas Abiertas y Huecos del Baseline — TicketRush

## 1. Preguntas abiertas prioritarias

### Q-001 — ¿Cuál es el mecanismo canónico de confirmación de reserva en frontend?

**Hallazgo:**
- La documentación general describe un flujo con SSE.
- Existe un hook de polling para reserva (`use-reservation-status.ts`).
- Existen componentes/herramientas SSE para otros flujos.

**Por qué importa:**
Afecta el plan de pruebas, la regresión de UX y la definición de checks de integración frontend-backend.

**Decisión pendiente:**
Definir si la reserva debe seguir por polling, migrarse totalmente a SSE o soportar ambos explícitamente.

---

### Q-002 — ¿Cuál es la fuente de verdad final para expiración automática?

**Hallazgo:**
- Existe topología RabbitMQ para `q.ticket.expired` y `q.ticket.reserved.delay`.
- Hay documentación histórica que muestra evolución/iteración de este mecanismo y algunos residuos documentales todavía pueden inducir a error.
- Hay configuración de `ExpiredQueueName` en ReservationService.

**Por qué importa:**
Afecta cobertura E2E, definición de smoke tests y criterios de aceptación de expiración.

**Decisión pendiente:**
Mantener explícitamente RabbitMQ como mecanismo productivo canónico y depurar cualquier referencia residual a alternativas ya retiradas.

---

### Q-003 — ¿Dónde debe vivir la fuente de verdad de requisitos del producto?

**Hallazgo:**
No existe un backlog centralizado vigente dentro del repo que reúna todas las HUs implementadas con trazabilidad completa.

**Por qué importa:**
Sin esa fuente de verdad, el plan maestro de pruebas siempre tendrá un componente de reconstrucción forense.

**Decisión pendiente:**
Definir si el baseline generado en `.github/docs/output/baseline/` será:

- fuente operativa temporal,
- fuente oficial viva,
- o insumo para crear documentación funcional más formal en `docs/`.

---

## 2. Huecos de trazabilidad

| ID | Hueco | Efecto |
|---|---|---|
| G-001 | No hay mapeo unívoco requisito histórico ↔ implementación ↔ pruebas para todo el proyecto | dificulta auditoría funcional |
| G-002 | Parte de la historia del proyecto está en documentos de actividad/semana y no en una especificación viva única | aumenta riesgo de contradicciones |
| G-003 | Las HUs originales no parecen estar consolidadas en una carpeta activa de backlog | complica uso directo del `full-flow` para alcance heredado |

---

## 3. Huecos de calidad a tratar en el siguiente paso

| ID | Gap | Prioridad | Motivo |
|---|---|---|---|
| QG-001 | Definir suite mínima obligatoria en PR por servicio | Alta | hoy no hay workflow consolidado |
| QG-002 | Separar smoke, integración y E2E por costo/tiempo | Alta | necesario para GitHub Actions realista |
| QG-003 | Congelar contrato canónico de SSE y respuesta asíncrona | Alta | evita regresiones entre frontend y backend |
| QG-004 | Formalizar cobertura de expiración sobre RabbitMQ | Alta | flujo crítico ya decidido, pero aún requiere trazabilidad y automatización limpias |
| QG-005 | Consolidar matriz flujo ↔ riesgo ↔ pruebas existentes | Media | mejora trazabilidad de QA global |

---

## 4. Recomendaciones inmediatas

1. Tomar este baseline como fuente operativa inicial del proyecto completo.
2. Ejecutar a continuación un flujo de **plan maestro de pruebas** basado en estos documentos, no en una HU única.
3. Diseñar GitHub Actions en dos niveles:
   - checks rápidos obligatorios en PR,
   - validaciones más costosas (integración/E2E) por merge, nightly o manual.
4. Resolver primero la ambigüedad estructural pendiente y el cleanup documental asociado:
   - reserva por polling vs SSE,
   - cobertura automatizada y documentación residual de la expiración canónica por RabbitMQ.

---

## 5. Juicio final

El baseline es **suficiente para empezar QA de sistema**, pero todavía **no es suficiente para afirmar trazabilidad histórica exacta de todas las HUs originales**. Eso requiere una decisión humana de gobernanza documental, no solo arqueología técnica.
