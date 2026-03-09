---
description: 'Skill especializada en verificación de accesibilidad web. Revisa cumplimiento WCAG 2.1 AA en todos los componentes: ARIA, contraste, navegación por teclado, lectores de pantalla y formularios accesibles.'
---

# Skill: accessibility-checker [FRONTEND]

## Responsabilidad
Verificar y corregir el cumplimiento de accesibilidad WCAG 2.1 nivel AA
en todos los componentes frontend del proyecto.

---

## Checklist WCAG 2.1 AA

### 1. Semántica HTML y ARIA
```tsx
// ❌ INCORRECTO
<div onClick={handleClick}>Eliminar</div>
<div class="btn">Enviar</div>

// ✅ CORRECTO
<button onClick={handleClick} type="button">Eliminar</button>
<button type="submit">Enviar</button>
```

- [ ] Usar elementos HTML semánticos: `<header>`, `<nav>`, `<main>`, `<footer>`, `<section>`, `<article>`
- [ ] No usar `<div>` y `<span>` para elementos interactivos
- [ ] `aria-label` en elements sin texto visible
- [ ] `aria-describedby` para describir campos de formulario con instrucciones
- [ ] `role` solo cuando el HTML semántico no es suficiente
- [ ] `aria-live` para contenido que cambia dinámicamente

### 2. Contraste de Color (Ratio mínimo)
```
Texto normal (< 18px):    ratio >= 4.5:1
Texto grande (>= 18px):   ratio >= 3:1
Elementos UI e íconos:    ratio >= 3:1

Herramienta de verificación: https://webaim.org/resources/contrastchecker/
```

- [ ] Verificar todos los textos sobre fondos de color
- [ ] Verificar íconos y elementos de UI interactivos
- [ ] No transmitir información SOLO con color (usar también forma, texto o patrón)

### 3. Navegación por Teclado
```tsx
// ❌ INCORRECTO — no navegable por teclado
<div tabIndex="0" onKeyPress={...}>Acción</div>

// ✅ CORRECTO — semántico y accesible
<button onClick={handleClick} type="button">Acción</button>
```

- [ ] Todos los elementos interactivos son alcanzables con `Tab`
- [ ] Orden lógico del foco (se puede seguir visualmente)
- [ ] `Escape` cierra modales, dropdowns y popovers
- [ ] `Enter` y `Espacio` activan botones y links
- [ ] Focus visible y nunca oculto con `outline: none` sin alternativa

### 4. Formularios Accesibles
```tsx
// ❌ INCORRECTO
<input placeholder="Tu email" />
<input type="text" />

// ✅ CORRECTO
<label htmlFor="email">Email</label>
<input
  id="email"
  type="email"
  aria-describedby="email-hint email-error"
  aria-required="true"
  aria-invalid={hasError}
/>
<span id="email-hint">Ejemplo: usuario@dominio.com</span>
<span id="email-error" role="alert">{errorMessage}</span>
```

- [ ] Todo campo de formulario tiene un `<label>` asociado
- [ ] Los errores de validación son anunciados por lectores de pantalla
- [ ] No depender solo del placeholder para indicar el propósito del campo
- [ ] Los campos requeridos marcados con `aria-required="true"`
- [ ] Los campos con error marcados con `aria-invalid="true"`

### 5. Imágenes y Medios
```tsx
// ❌ INCORRECTO
<img src="accion-guardar.png" />
<img src="decorativo.png" />

// ✅ CORRECTO
<img src="accion-guardar.png" alt="Guardar cambios" />
<img src="decorativo.png" alt="" aria-hidden="true" />
```

- [ ] Todas las imágenes informativas con `alt` descriptivo
- [ ] Imágenes decorativas con `alt=""` y/o `aria-hidden="true"`
- [ ] Íconos solos (sin texto) con `aria-label` o `title`
- [ ] Videos con subtítulos y transcripciones si aplica

### 6. Modales y Diálogos
```tsx
// ✅ CORRECTO
<dialog
  role="dialog"
  aria-modal="true"
  aria-labelledby="modal-title"
  aria-describedby="modal-description"
>
  <h2 id="modal-title">Confirmar Eliminación</h2>
  <p id="modal-description">¿Estás seguro de que deseas eliminar este elemento?</p>
  ...
</dialog>
```

- [ ] `role="dialog"` y `aria-modal="true"`
- [ ] Foco se mueve al modal al abrirse
- [ ] Foco queda atrapado dentro del modal mientras está abierto
- [ ] Al cerrar, foco regresa al elemento que abrió el modal
- [ ] `Escape` cierra el modal

---

## Proceso de Verificación

```
PASO 1 → Escanear todos los componentes con criterios WCAG
PASO 2 → Priorizar: interactivos > formularios > imágenes > decorativos
PASO 3 → Verificar herramientas automáticas: axe, Lighthouse accessibility
PASO 4 → Corregir bloqueadores críticos (Level A primero, luego AA)
PASO 5 → Verificar con navegación solo por teclado manualmente
PASO 6 → Reportar estado de cumplimiento
```

## Reporte

```
♿ ACCESSIBILITY-CHECKER [FRONTEND] — REPORTE WCAG 2.1 AA
════════════════════════════════════════════════════════
Componentes auditados:             X

Violaciones encontradas:
  Críticas (Level A):              X → corregidas: X
  Importantes (Level AA):          X → corregidas: X
  Menores (Level AAA):             X → documentadas para backlog

Categorías:
  Semántica/ARIA:                  X violaciones
  Contraste de color:              X violaciones
  Navegación por teclado:          X violaciones
  Formularios:                     X violaciones
  Imágenes:                        X violaciones
  Modales/Diálogos:                X violaciones

Cumplimiento WCAG 2.1 AA: X%
════════════════════════════════════════════════════════
```
