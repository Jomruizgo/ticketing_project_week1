# Resultados y Evidencias de Pipeline — Semana 4

## Estado actual

> Pendiente de ejecución. Este archivo queda preparado para registrar la evidencia exigida por la guía una vez se implemente el pipeline.

## 1. Registro de ejecuciones

| Fecha | Workflow / job | Comando o trigger | Resultado | Evidencia |
|---|---|---|---|---|
| Pendiente | build | PR / push | Pendiente | Pendiente |
| Pendiente | component-tests | PR / push | Pendiente | Pendiente |
| Pendiente | integration-tests | PR / push | Pendiente | Pendiente |
| Pendiente | black-box-e2e | post-merge / manual | Pendiente | Pendiente |
| Pendiente | image-scan | PR / push | Pendiente | Pendiente |

## 2. Evidencia mínima esperada

### 2.1 Build
- resultado de compilación,
- imagen o servicios construidos,
- evidencia de job en verde.

### 2.2 Component
- referencia a pruebas de componente ejecutadas,
- duración,
- resultado,
- artifact si aplica.

### 2.3 Integration
- referencia a contratos o pruebas de integración ejecutadas,
- evidencia de separación visual respecto al job de componente.

### 2.4 Caja Negra / E2E
- script ejecutado,
- entorno levantado,
- resultado final observable,
- justificación de por qué es Caja Negra.

### 2.5 Escaneo de imagen
- herramienta usada,
- resumen de hallazgos,
- severidades detectadas,
- decisión tomada.

## 3. Evidencia de PR y release

| Evidencia | Estado | Ubicación esperada |
|---|---|---|
| PR de feature hacia `develop` | Pendiente | `capturas/` + enlace/documentación |
| Pipeline asociado al PR | Pendiente | `capturas/` + resumen en este archivo |
| PR de release `develop -> main` | Pendiente | `capturas/` + enlace/documentación |
| Tag o versión de release | Pendiente | resumen en este archivo |

## 4. Notas para defensa

- El pipeline debe mostrar separación real entre componente e integración.
- La prueba de Caja Negra debe correr sobre entorno orquestado o equivalente defendible.
- El análisis de imagen no debe quedar implícito; debe tener evidencia visible.
- Toda captura debe acompañarse de una explicación breve, no solo la imagen.
