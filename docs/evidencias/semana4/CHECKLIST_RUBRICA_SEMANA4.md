# Checklist de Rúbrica — Semana 4

> Estado actual: **En preparación**

## 1) Plan de pruebas como informe
- [x] Existe archivo `TEST_PLAN.md`
- [x] El documento incluye test plan y test cases
- [x] Se justifican los 7 principios de testing
- [x] Se describe estrategia multinivel
- [x] Se distinguen pruebas de caja blanca y caja negra

**Evidencia objetivo:**
- [x] [TEST_PLAN.md](TEST_PLAN.md)

## 2) Infraestructura como código
- [x] Dockerfile alineado al entregable esperado por la guía
- [x] Endurecimiento mínimo de imágenes backend (multi-stage build, imagen base `mcr.microsoft.com/dotnet/aspnet:8.0`)
- [x] Ejecución sin privilegios donde aplique en imágenes backend
- [x] Evidencia de build reproducible en entorno CI/CD

**Evidencia objetivo:**
- [x] [Dockerfile crud-service](https://github.com/Jomruizgo/ticketing_project_week1/blob/main/crud_service/Dockerfile)
- [x] [Dockerfile payment-service](https://github.com/Jomruizgo/ticketing_project_week1/blob/main/paymentService/Dockerfile)
- [x] [Dockerfile producer](https://github.com/Jomruizgo/ticketing_project_week1/blob/main/producer/Dockerfile)
- [x] [Dockerfile reservation-service](https://github.com/Jomruizgo/ticketing_project_week1/blob/main/ReservationService/Dockerfile)
- [x] matriz local en `MATRIZ_LOCAL_PREWORKFLOW_SEMANA4.md`
- [x] resultados de build en `RESULTADOS_PIPELINE_SEMANA4.md` (sección 2.1)

## 3) Pipeline CI/CD multinivel
- [x] Existe carpeta `.github/workflows/`
- [x] Existe workflow YAML versionado (`ci.yml`)
- [x] Los jobs separan visualmente componente e integración
- [x] El pipeline incluye build
- [x] El pipeline bloquea integración defectuosa (runs #8, #9, #10 fallaron; merge sólo tras run #11 verde)

**Evidencia objetivo:**
- [x] [ci.yml en GitHub](https://github.com/Jomruizgo/ticketing_project_week1/blob/main/.github/workflows/ci.yml)
- [x] [Pipeline verde run #11](https://github.com/Jomruizgo/ticketing_project_week1/actions/runs/22839897387) en `capturas/pipeline_verde_PR24_resumen.md`
- [x] resumen técnico en `RESULTADOS_PIPELINE_SEMANA4.md`

## 4) Caja Blanca y Caja Negra
- [x] Existe evidencia clara de Caja Blanca en pipeline (Unit Tests — job 🧪)
- [x] Existe al menos una Caja Negra ejecutada sobre entorno real (WebApplicationFactory)
- [x] Se puede defender por qué la prueba elegida es realmente Caja Negra

**Evidencia objetivo:**
- [x] suites unit/component/integration en pipeline con artifacts `.trx`
- [x] `TicketsApiBlackBoxTests.cs` — 9 tests HTTP reales
- [x] resultados en `artifacts/unit-tests/`, `artifacts/blackbox/`, `RESULTADOS_PIPELINE_SEMANA4.md` (sección 2.2–2.5)

## 5) Seguridad y calidad continua
- [x] Existe análisis de vulnerabilidades de imagen (Trivy integrado en ci.yml)
- [x] Existe salida o reporte del escaneo (4 reportes, uno por servicio)
- [x] El resultado queda archivado en evidencias

**Evidencia objetivo:**
- [x] `artifacts/trivy/trivy-producer.txt` — 0 vulnerabilidades
- [x] `artifacts/trivy/trivy-crud-service.txt` — 0 vulnerabilidades
- [x] `artifacts/trivy/trivy-reservation-service.txt` — 0 vulnerabilidades
- [x] `artifacts/trivy/trivy-payment-service.txt` — 1 HIGH (CVE-2024-43483, fix disponible)
- [x] resumen del hallazgo en `RESULTADOS_PIPELINE_SEMANA4.md` (sección 2.6)

## 6) Flujo GitFlow y release
- [x] Existe PR de feature hacia `develop` (PR #23)
- [x] Existe evidencia del release `develop -> main` (PR #24 via `release/v3.0.1`)
- [x] La ejecución del pipeline queda asociada al PR o release

**Evidencia objetivo:**
- [x] `capturas/PR23_feature_a_develop.md` — PR #23 con 9/9 verde
- [x] `capturas/PR24_release_a_main.md` — PR #24 con 9/9 verde
- [x] Tag [v3.0.1](https://github.com/Jomruizgo/ticketing_project_week1/releases/tag/v3.0.1)
- [x] resumen de release en `RESULTADOS_PIPELINE_SEMANA4.md` (sección 3)

## 7) Human Check
- [x] El equipo puede explicar por qué cada job existe
- [x] El equipo puede diferenciar componente vs integración
- [x] El equipo puede explicar qué generó la IA y qué se auditó manualmente
- [x] El equipo puede defender la elección de la prueba de Caja Negra

**Evidencia objetivo:**
- [x] notas de defensa en `RESULTADOS_PIPELINE_SEMANA4.md` (sección 4)

---

## Veredicto interno final

✅ **COMPLETO** — Todos los criterios de la rúbrica están cubiertos con evidencia verificable:
- Pipeline CI/CD multinivel con 6 tipos de jobs diferenciados
- Tests de todos los niveles con artifacts `.trx` reales del CI
- Trivy scan activo con reportes por servicio archivados
- GitFlow correcto: feature → PR → develop → release → PR → main → tag
- Bloqueo real de integración defectuosa (3 runs fallidos antes del merge)
