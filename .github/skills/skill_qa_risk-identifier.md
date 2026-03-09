---
description: 'Skill que identifica y clasifica riesgos de calidad usando la Regla ASD (Alto=obligatorio, Medio=recomendado, Bajo=opcional). Analiza complejidad, integraciones, datos sensibles y flujos críticos del SPEC.'
---

# Skill: risk-identifier [QA]

## Responsabilidad
Identificar, clasificar y priorizar riesgos de calidad del proyecto
usando la Regla ASD definida en el lineamiento de QA.

---

## Regla ASD — Clasificación de Riesgos

```
NIVEL ALTO (A)   → Testing OBLIGATORIO, bloquea el release
NIVEL MEDIO (S)  → Testing RECOMENDADO, documentar si se omite
NIVEL BAJO (D)   → Testing OPCIONAL, priorizar en el backlog
```

---

## Catálogo de Factores de Riesgo

### Factores que elevan riesgo a ALTO
| Factor                                    | Razón                                     |
|-------------------------------------------|-------------------------------------------|
| Manejo de dinero o pagos                  | Impacto financiero directo                |
| Datos personales y privacidad (GDPR/LOPD) | Obligación legal                          |
| Autenticación y autorización              | Compromete la seguridad completa          |
| Operaciones destructivas (DELETE masivo)  | Sin recuperación posible                  |
| Integraciones con sistemas externos       | Dependencia no controlada                 |
| Flujos de negocio críticos e irreversibles| Daño operacional grave                    |
| SLA definidos en contrato con el cliente  | Obligación contractual                    |

### Factores que elevan riesgo a MEDIO
| Factor                                    | Razón                                     |
|-------------------------------------------|-------------------------------------------|
| Lógica de negocio compleja                | Alta probabilidad de defectos             |
| Componentes con muchas dependencias       | Cambio impacta múltiples áreas            |
| Código nuevo sin historial                | Sin métricas de confiabilidad             |
| Funcionalidades de alta frecuencia de uso | Impacto en muchos usuarios                |

### Factores que corresponden a BAJO
| Factor                                    | Razón                                     |
|-------------------------------------------|-------------------------------------------|
| Features internas o administrativas       | Impacto limitado                          |
| Ajustes estéticos de UI                   | Sin impacto funcional                     |
| Refactorizaciones sin cambio de lógica    | Sin cambio de comportamiento              |
| Funciones auxiliares de reporting         | Datos secundarios                         |

---

## Entregable: Matriz de Riesgos

Genera `{qa_output_folder}/risk-matrix.md`:

```markdown
# Matriz de Riesgos — [Nombre del Proyecto]
**Versión:** 1.0 | **Fecha:** [fecha] | **Generado por:** QA Agent

## Resumen Ejecutivo
Total de riesgos: X
  - Alto (A): X → testing obligatorio
  - Medio (S): X → testing recomendado
  - Bajo (D): X → testing opcional

## Detalle de Riesgos

| ID    | HU Relacionada | Descripción del Riesgo              | Factores          | Nivel | Testing Obligatorio | Estado   |
|-------|---------------|--------------------------------------|-------------------|-------|---------------------|----------|
| R-001 | HU-002        | Proceso de pago puede fallar silenciosamente | Pagos, externo | A | Sí — bloqueante | Pendiente |
| R-002 | HU-003        | Validaciones del formulario incompletas | Lógica compleja   | S | Recomendado     | Pendiente |
| R-003 | HU-005        | Ajuste de estilos de la tabla        | UI cosmético       | D | Opcional        | Backlog  |

## Plan de Mitigación por Riesgo Alto

### R-001: [descripción]
- **Mitigación**: [controles técnicos a implementar]
- **Tests obligatorios**: [tipos de tests a generar]
- **Criterio de cierre**: [cuándo se considera mitigado]
```

---

## Proceso de Identificación

```
PASO 1 → Leer arquitectura y componentes del SPEC
PASO 2 → Leer todas las HU con criterios de aceptación
PASO 3 → Aplicar catálogo de factores a cada HU y componente
PASO 4 → Clasificar cada riesgo con nivel ASD
PASO 5 → Generar plan de mitigación para riesgos Alto
PASO 6 → Priorizar backlog de riesgos Medio y Bajo
PASO 7 → Generar {qa_output_folder}/risk-matrix.md
PASO 8 → Comunicar riesgos Alto al equipo antes de continuar
```

## Reporte

```
⚠️ RISK-IDENTIFIER [QA] — REPORTE
════════════════════════════════════════════════
HU analizadas:                   X
Componentes analizados:          X

RIESGOS IDENTIFICADOS:
  Alto  (A) — Obligatorio:       X  ← ATENCIÓN requerida
  Medio (S) — Recomendado:       X
  Bajo  (D) — Opcional:          X
  TOTAL:                         X

Riesgos Alto sin mitigación:     X  ← BLOQUEAN release si > 0

Documento generado: {qa_output_folder}/risk-matrix.md ✅
════════════════════════════════════════════════
```
