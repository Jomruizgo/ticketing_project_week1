---
description: 'Skill que analiza y define estrategias de performance testing. Clasifica pruebas en Load, Stress, Spike y Soak. Define SLAs, umbrales de alerta y el plan de ejecución con herramientas como k6 o JMeter.'
---

# Skill: performance-analyzer [QA]

## Responsabilidad
Definir y planificar las pruebas de performance del proyecto basándose
en los SLAs y requisitos de carga definidos en el SPEC.

---

## Clasificación de Pruebas de Performance (según qa-guidelines)

### 🟢 Load Testing (Carga Normal)
```
Propósito:  Verificar el comportamiento bajo la carga esperada en producción
Cuándo:     Antes de cada release con cambios en APIs de alto tráfico
Duración:   15-30 minutos
Patrón:     Ramp-up gradual → carga sostenida → ramp-down

Ejemplo k6:
  Ramp-up:  0 → 100 VUs en 2 minutos
  Sostenida: 100 VUs durante 15 minutos
  Ramp-down: 100 → 0 VUs en 2 minutos
```

### 🟡 Stress Testing (Carga Máxima)
```
Propósito:  Encontrar el punto de quiebre del sistema y validar la degradación graceful
Cuándo:     Antes de lanzamientos importantes, campañas o eventos de alto tráfico
Duración:   30-60 minutos
Patrón:     Incremento progresivo hasta el doble de la carga esperada

Ejemplo k6:
  Etapa 1: 0 → 100 VUs (normal, 5 min)
  Etapa 2: 100 → 200 VUs (1.5x, 5 min)
  Etapa 3: 200 → 300 VUs (2x, 10 min)
  Etapa 4: 300 → 400 VUs (3x, 10 min)  ← punto de quiebre esperado
```

### 🔴 Spike Testing (Picos de Carga)
```
Propósito:  Verificar que el sistema se recupera ante picos repentinos de tráfico
Cuándo:     Cuando hay eventos programados (Black Friday, lanzamiento de producto)
Duración:   20-30 minutos
Patrón:     Carga base → pico repentino → regreso a base

Ejemplo k6:
  Base:  50 VUs durante 5 minutos
  Pico:  50 → 800 VUs en 30 segundos (pico abrupto)
  Pico:  800 VUs durante 5 minutos
  Bajada: 800 → 50 VUs en 30 segundos (recuperación)
  Base:  50 VUs durante 5 minutos (verificar recuperación)
```

### 🔵 Soak Testing (Resistencia en el Tiempo)
```
Propósito:  Detectar memory leaks, connection pool exhaustion y degradación progresiva
Cuándo:     Antes de releases mayores, cambios en gestión de recursos
Duración:   2-4 horas (o durante la noche)
Patrón:     Carga normal sostenida por largo tiempo

Ejemplo k6:
  100 VUs constantes durante 2-4 horas
  Monitorear: memoria, CPU, tiempo de respuesta en el tiempo
```

---

## Umbrales SLA (Adaptar a los del SPEC)

```javascript
// Umbrales base — ajustar según SLAs del SPEC
export const thresholds = {
  // Tiempo de respuesta (percentiles)
  'http_req_duration': [
    'p(50) < 200ms',   // mediana bajo 200ms
    'p(95) < 1000ms',  // 95% de requests bajo 1 segundo
    'p(99) < 2000ms',  // 99% bajo 2 segundos
  ],
  // Tasa de errores
  'http_req_failed': ['rate < 0.01'],  // menos del 1% de errores
  // Solicitudes por segundo
  'http_reqs': ['rate > 50'],          // mínimo 50 RPS
};
```

---

## Entregable: Plan de Performance

Genera `{qa_output_folder}/performance-plan.md`:

```markdown
# Plan de Performance Testing — [Nombre del Proyecto]
**Versión:** 1.0 | **Fecha:** [fecha] | **Generado por:** QA Agent

## SLAs del Proyecto (del SPEC)
| Métrica            | SLA Objetivo  | Umbral de Alerta |
|--------------------|---------------|------------------|
| Tiempo respuesta p50 | < X ms      | > Y ms           |
| Tiempo respuesta p95 | < X ms      | > Y ms           |
| Tasa de errores    | < X%          | > Y%             |
| Throughput mínimo  | > X RPS       | < Y RPS          |

## Endpoints Críticos a Testear
| Endpoint                 | Tipo de prueba | Carga esperada | SLA      |
|--------------------------|---------------|----------------|----------|
| POST /api/v1/[recurso]   | Load + Stress | XXXX req/día   | < 500ms  |
| GET  /api/v1/[recurso]   | Load          | YYYY req/día   | < 200ms  |

## Plan de Ejecución por Tipo
[Parámetros específicos de cada tipo de prueba para este proyecto]

## Herramienta Seleccionada
**[k6 / JMeter / Gatling]** — Justificación: [razón]

## Infraestructura de Prueba
- Ambiente: [cuál ambiente usar para performance tests]
- Datos: [qué datos usar, cómo cargarlos]
- Monitoreo: [qué dashboards revisar durante la prueba]

## Criterios de Pase/Falla
[Condiciones exactas que determinan si la prueba pasa o falla]
```

---

## Proceso de Análisis

```
PASO 1 → Leer SLAs y requisitos de carga en el SPEC
PASO 2 → Identificar endpoints y flujos de alto tráfico
PASO 3 → Seleccionar tipos de prueba (Load/Stress/Spike/Soak)
PASO 4 → Definir parámetros de carga para cada tipo
PASO 5 → Definir umbrales de SLA como criterios de éxito
PASO 6 → Seleccionar herramienta según stack del proyecto
PASO 7 → Generar scripts base de prueba (si aplica)
PASO 8 → Generar {qa_output_folder}/performance-plan.md
```

## Reporte

```
📊 PERFORMANCE-ANALYZER [QA] — REPORTE
════════════════════════════════════════════════
SLAs identificados en el SPEC:   X
Endpoints críticos identificados: X

Pruebas planificadas:
  Load Testing:     ✅/❌ (¿aplica?)
  Stress Testing:   ✅/❌ (¿aplica?)
  Spike Testing:    ✅/❌ (¿aplica?)
  Soak Testing:     ✅/❌ (¿aplica?)

Herramienta seleccionada:        [nombre]
Scripts base generados:          X

Documento generado: {qa_output_folder}/performance-plan.md ✅
════════════════════════════════════════════════
```
