# Evidencia: PR #24 — release/v3.0.1 → main

**Fecha**: 2026-03-09  
**Autor**: emolinaf99  
**URL**: https://github.com/Jomruizgo/ticketing_project_week1/pull/24  
**Estado**: ✅ Merged

---

## Datos del PR

| Campo | Valor |
|---|---|
| Número | #24 |
| Título | release(v3.0.1): Semana 3 — CI/CD Pipeline Multinivel + GitFlow Correcto |
| Rama origen | `release/v3.0.1` |
| Rama destino | `main` |
| Archivos cambiados | 23 |
| Líneas añadidas | +1815 |
| Líneas eliminadas | -421 |
| Commits | 8 |
| Creado | 2026-03-09T05:14:50Z |
| Mergeado | 2026-03-09T05:37:55Z |

---

## Contexto GitFlow — Release

Este PR representa el flujo de release de GitFlow:

```
main  ←── release/v3.0.1 ←── (creada desde develop)
  ↑           ↑
[v3.0.1]   [este PR con CI verde]
  ↓
develop (back-merge posterior)
```

---

## Criterios de rúbrica cubiertos en este release

| Criterio | Estado |
|---|---|
| Dockerfile multi-stage, no-root, HEALTHCHECK | ✅ |
| Pipeline CI separado por niveles (unit/component/integration/blackbox) | ✅ |
| TEST_PLAN.md con 7 principios | ✅ |
| Pruebas de Caja Negra (BlackBox) | ✅ |
| GitFlow con tag de release | ✅ |
| Trivy vulnerability scan en pipeline | ✅ |

---

## Resultado del Pipeline CI

**Run ID**: 22839897387  
**URL**: https://github.com/Jomruizgo/ticketing_project_week1/actions/runs/22839897387  
**Trigger**: `pull_request`  
**Jobs totales**: 9/9 ✅

| Job | Estado | Inicio | Fin | Duración |
|---|---|---|---|---|
| 🔨 Build — Todos los servicios | ✅ success | 05:35:16Z | 05:35:47Z | ~31s |
| 🌐 Integration Tests | ✅ success | 05:35:50Z | 05:36:24Z | ~34s |
| 🔗 Component Tests | ✅ success | 05:35:50Z | 05:36:19Z | ~29s |
| 🧪 Unit Tests — Caja Blanca | ✅ success | 05:35:51Z | 05:36:36Z | ~45s |
| 📦 Black-Box Tests | ✅ success | 05:35:51Z | 05:36:22Z | ~31s |
| 🐳 Trivy (producer) | ✅ success | 05:36:39Z | 05:37:21Z | ~42s |
| 🐳 Trivy (payment-service) | ✅ success | 05:36:39Z | 05:37:20Z | ~41s |
| 🐳 Trivy (crud-service) | ✅ success | 05:36:39Z | 05:37:20Z | ~41s |
| 🐳 Trivy (reservation-service) | ✅ success | 05:36:39Z | 05:37:20Z | ~41s |

---

## Tag de release

```
git tag -a v3.0.1 -m "release: v3.0.1 — Semana 3..."
git push origin v3.0.1
```

**URL del tag**: https://github.com/Jomruizgo/ticketing_project_week1/releases/tag/v3.0.1
