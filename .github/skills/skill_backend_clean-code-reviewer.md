---
description: 'Skill especializada en revisión de código limpio para backends. Detecta violaciones SOLID, funciones largas, nombres confusos, código duplicado y aplica refactoring siguiendo las reglas del lineamiento.'
---

# Skill: clean-code-reviewer [BACKEND]

## Responsabilidad
Revisar y refactorizar el código del backend para que cumpla
con los estándares de código limpio del lineamiento `dev-guidelines.md`.

---

## Checklist de Revisión

### 1. Longitud de Funciones/Métodos
- [ ] Ninguna función supera las 20 líneas
- [ ] Si supera → extraer funciones con nombres descriptivos
- [ ] Cada función hace UNA sola cosa (Single Responsibility)

### 2. Nomenclatura
```
✅ CORRECTO:
  Clases:    PascalCase   → UserRepository, OrderService
  Funciones: camelCase    → getUserById, calculateTotal
  Variables: camelCase    → userName, totalAmount
  Constantes: UPPER_SNAKE → MAX_RETRY_ATTEMPTS, DEFAULT_TIMEOUT
  Archivos:  kebab-case   → user-repository.ts, order-service.ts

❌ INCORRECTO (refactorizar siempre):
  x, data, temp, aux, obj, val, res, resp, result, flag
```

### 3. Principios SOLID
- [ ] **S** — Una clase = una responsabilidad
- [ ] **O** — Extender por herencia/composición, no modificar la clase base
- [ ] **L** — Las subclases sustituyen a las clases padre sin romper funcionalidad
- [ ] **I** — Interfaces pequeñas y específicas, no genéricas
- [ ] **D** — Depender de abstracciones, no de implementaciones concretas

### 4. Código Duplicado
- [ ] No existe lógica repetida en más de 2 lugares
- [ ] Si existe → extraer a función/helper compartida
- [ ] No existe copia-pega de bloques de código con ligeras variaciones

### 5. Números Mágicos y Strings Literales
```typescript
// ❌ INCORRECTO
if (user.role === 3) ...
setTimeout(fn, 86400000)

// ✅ CORRECTO
const ADMIN_ROLE_ID = 3;
const ONE_DAY_MS = 86_400_000;
if (user.role === ADMIN_ROLE_ID) ...
setTimeout(fn, ONE_DAY_MS)
```

### 6. Condiciones Complejas
```typescript
// ❌ INCORRECTO
if (user.age >= 18 && user.status === 'active' && !user.banned && user.credits > 0)

// ✅ CORRECTO
const isEligibleUser = user.age >= 18
  && user.status === 'active'
  && !user.banned
  && user.credits > 0;
if (isEligibleUser)
```

### 7. Manejo de Errores
- [ ] No usar `catch(e) {}` vacío — siempre loguear o relanzar
- [ ] No capturar excepciones solo para silenciarlas
- [ ] Usar excepciones tipadas del dominio, no strings genéricos

---

## Proceso de Revisión

```
PASO 1 → Escanear todos los archivos de lógica de negocio
PASO 2 → Identificar violaciones por categoría
PASO 3 → Reportar violaciones con ubicación exacta (archivo:línea)
PASO 4 → Aplicar refactoring en orden: longitud → nombres → SOLID → duplicado
PASO 5 → Verificar que los tests existentes siguen pasando
PASO 6 → Reportar cambios aplicados
```

## Reporte de Revisión

```
🔵 CLEAN-CODE-REVIEWER [BACKEND] — REPORTE
════════════════════════════════════════════════
Archivos analizados:              X
Violaciones encontradas:          X
  Funciones > 20 líneas:          X → refactorizadas: X
  Nombres confusos:               X → renombrados: X
  Violaciones SOLID:              X → corregidas: X
  Código duplicado:               X → extraído a helpers: X
  Números mágicos:                X → convertidos a constantes: X
  Manejo de errores vacío:        X → corregidos: X

Tests ejecutados post-refactoring: X → pasando: X / fallando: X
════════════════════════════════════════════════
```
