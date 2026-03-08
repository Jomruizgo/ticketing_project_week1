---
description: 'Skill especializada en generación de tests de UI con Playwright. Genera tests E2E de flujos críticos de usuario, pruebas de componentes y validaciones de formularios siguiendo el lineamiento de testing del proyecto.'
---

# Skill: ui-test-generator [FRONTEND]

## Responsabilidad
Generar tests de UI y E2E para cubrir los flujos críticos de usuario
identificados en el SPEC y los criterios de aceptación de las HU del frontend.

---

## Estándar de Tests de UI (Playwright)

### Estructura base de un test E2E
```typescript
import { test, expect } from '@playwright/test';

test.describe('[Nombre del flujo]: [nombre del recurso]', () => {

  test.beforeEach(async ({ page }) => {
    // Autenticar si el flujo lo requiere
    await page.goto('/login');
    await page.fill('[data-testid="email"]', 'test@example.com');
    await page.fill('[data-testid="password"]', 'TestPassword123');
    await page.click('[data-testid="submit-btn"]');
    await expect(page).toHaveURL('/dashboard');
  });

  test('given_[contexto]_when_[acción]_then_[resultado]', async ({ page }) => {
    // GIVEN — navegar al punto de inicio del flujo
    await page.goto('/recurso');

    // WHEN — ejecutar la interacción del usuario
    await page.fill('[data-testid="nombre-campo"]', 'valor de prueba');
    await page.click('[data-testid="btn-accion"]');

    // THEN — verificar el resultado esperado
    await expect(page.locator('[data-testid="mensaje-exito"]'))
      .toBeVisible();
    await expect(page.locator('[data-testid="mensaje-exito"]'))
      .toHaveText('Operación completada exitosamente');
    await expect(page).toHaveURL('/recurso/detalle');
  });
});
```

---

## Regla de Selectores: Siempre usar data-testid

```tsx
// ❌ INCORRECTO — frágiles ante cambios de diseño
page.locator('.btn-primary')
page.locator('button:nth-child(2)')
page.locator('//div[@class="form"]/input[1]')
page.getByText('Guardar')  // ambiguo si aparece en múltiples lugares

// ✅ CORRECTO — estables y declarativos
page.locator('[data-testid="guardar-btn"]')
page.locator('[data-testid="nombre-input"]')
page.locator('[data-testid="error-mensaje"]')
```

**Al generar tests**: Si los componentes no tienen `data-testid`, agrégalos primero.

---

## Flujos Críticos a Cubrir (desde el SPEC)

Por cada HU de frontend genera al menos estos tests:

### Flujos de Autenticación (si aplica)
```
given_valid_credentials_when_login_then_redirects_to_dashboard
given_invalid_credentials_when_login_then_shows_error_message
given_empty_fields_when_login_then_shows_validation_errors
given_authenticated_user_when_logout_then_redirects_to_login
```

### Flujos de Formularios
```
given_valid_form_data_when_submit_then_shows_success_and_redirects
given_missing_required_fields_when_submit_then_shows_validation_errors
given_invalid_format_when_typing_then_shows_inline_error
given_server_error_when_submit_then_shows_error_notification
```

### Flujos de Navegación
```
given_user_on_[page]_when_click_[link]_then_navigates_to_[target]
given_unauthenticated_user_when_access_[protected_route]_then_redirects_to_login
given_authenticated_user_when_access_[page]_then_renders_correctly
```

### Flujos de Listas/Tablas
```
given_data_exists_when_visit_list_page_then_shows_items
given_no_data_when_visit_list_page_then_shows_empty_state
given_search_term_when_filter_then_shows_filtered_results
given_items_when_paginate_then_shows_correct_page
```

---

## Tests de Componentes Aislados

Para componentes críticos o reutilizables:

```typescript
import { test, expect } from '@playwright/experimental-ct-react';
import { Button } from './Button';

test('given_loading_state_when_render_then_shows_spinner', async ({ mount }) => {
  const component = await mount(<Button isLoading={true}>Guardar</Button>);
  await expect(component.locator('[data-testid="spinner"]')).toBeVisible();
  await expect(component.locator('button')).toBeDisabled();
});
```

---

## Proceso de Generación

```
PASO 1 → Identificar flujos críticos en el SPEC y criterios de aceptación HU
PASO 2 → Verificar que los componentes tienen data-testid apropiados
PASO 3 → Agregar data-testid donde falten
PASO 4 → Generar tests E2E de flujos críticos
PASO 5 → Generar tests de componentes para UI reutilizable
PASO 6 → Configurar playwright.config.ts si no existe
PASO 7 → Ejecutar suite y reportar resultados
```

## Reporte de Generación

```
🧪 UI-TEST-GENERATOR [FRONTEND] — REPORTE
════════════════════════════════════════════════
Flujos críticos identificados:    X
Tests E2E generados:              X
  Happy paths:                    X
  Error paths:                    X
  Edge cases:                     X
Tests de componentes generados:   X
data-testid agregados:            X

Ejecución:
  Tests pasando:                  X
  Tests fallando:                 X (requieren revisión)

Cobertura de flujos críticos:     X%
════════════════════════════════════════════════
```
