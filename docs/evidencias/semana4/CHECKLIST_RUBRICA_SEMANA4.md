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
- [ ] Dockerfile alineado al entregable esperado por la guía
- [x] Endurecimiento mínimo de imágenes backend en progreso validado localmente
- [x] Ejecución sin privilegios donde aplique en imágenes backend
- [x] Evidencia de build reproducible en entorno local documentada

**Evidencia objetivo:**
- [ ] Dockerfile(s) finales
- [x] matriz local en `MATRIZ_LOCAL_PREWORKFLOW_SEMANA4.md`
- [ ] log o salida resumida de build en `RESULTADOS_PIPELINE_SEMANA4.md`

## 3) Pipeline CI/CD multinivel
- [ ] Existe carpeta `.github/workflows/`
- [ ] Existe workflow YAML versionado
- [ ] Los jobs separan visualmente componente e integración
- [ ] El pipeline incluye build
- [ ] El pipeline bloquea integración defectuosa

**Evidencia objetivo:**
- [ ] YAML del workflow
- [ ] captura o enlace de ejecución verde en `capturas/`
- [ ] resumen técnico en `RESULTADOS_PIPELINE_SEMANA4.md`

## 4) Caja Blanca y Caja Negra
- [ ] Existe evidencia clara de Caja Blanca en pipeline
- [x] Existe al menos una Caja Negra ejecutada sobre entorno orquestado
- [x] Se puede defender por qué la prueba elegida es realmente Caja Negra

**Evidencia objetivo:**
- [x] suites unit/component/integration referenciadas
- [x] script E2E o equivalente registrado
- [x] resultados de ejecución documentados en `MATRIZ_LOCAL_PREWORKFLOW_SEMANA4.md`

## 5) Seguridad y calidad continua
- [ ] Existe análisis de vulnerabilidades de imagen
- [ ] Existe salida o reporte del escaneo
- [ ] El resultado queda archivado en evidencias

**Evidencia objetivo:**
- [ ] reporte de `trivy`, `docker scout` o equivalente en `artifacts/`
- [ ] resumen del hallazgo en `RESULTADOS_PIPELINE_SEMANA4.md`

## 6) Flujo GitFlow y release
- [ ] Existe PR de feature hacia `develop`
- [ ] Existe evidencia del release `develop -> main` cuando corresponda
- [ ] La ejecución del pipeline queda asociada al PR o release

**Evidencia objetivo:**
- [ ] captura del PR en `capturas/`
- [ ] resumen de release en `RESULTADOS_PIPELINE_SEMANA4.md`

## 7) Human Check
- [ ] El equipo puede explicar por qué cada job existe
- [ ] El equipo puede diferenciar componente vs integración
- [ ] El equipo puede explicar qué generó la IA y qué se auditó manualmente
- [ ] El equipo puede defender la elección de la prueba de Caja Negra

**Evidencia objetivo:**
- [ ] notas de defensa incluidas en `PLAN_SEGUIMIENTO_SEMANA4.md` o `RESULTADOS_PIPELINE_SEMANA4.md`

---

## Veredicto interno provisional

- Fuerte en testing documental.
- Mejorado en precondiciones locales de contenedores; pendiente en CI/CD, seguridad de imagen y evidencia de release.
- La brecha principal de Semana 4 no es de teoría de pruebas; es de automatización y evidencia ejecutable.
