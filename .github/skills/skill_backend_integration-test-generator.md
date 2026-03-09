---
description: 'Skill especializada en generación de tests de integración para endpoints y servicios del backend. Genera tests con patrón AAA, nombres given_when_then y cobertura de happy, error y edge cases.'
---

# Skill: integration-test-generator [BACKEND]

## Responsabilidad
Generar tests de integración para endpoints REST y servicios del backend
siguiendo el patrón AAA y nomenclatura `given_when_then` del lineamiento.

---

## Estándar de Tests de Integración

### Estructura obligatoria de cada test
```typescript
describe('[Controller/Service]: [nombre del recurso]', () => {

  // GIVEN — setup del contexto
  beforeEach(async () => {
    // preparar base de datos de test
    // inicializar mocks de dependencias externas
    // autenticar si el endpoint lo requiere
  });

  afterEach(async () => {
    // limpiar datos insertados en el test
    // resetear mocks
  });

  it('given_[contexto]_when_[acción]_then_[resultado esperado]', async () => {
    // GIVEN — preparar datos específicos del test
    const payload = { ... };

    // WHEN — ejecutar la acción
    const response = await request(app)
      .post('/api/v1/recurso')
      .send(payload)
      .set('Authorization', `Bearer ${token}`);

    // THEN — verificar el resultado
    expect(response.status).toBe(201);
    expect(response.body.success).toBe(true);
    expect(response.body.data).toMatchObject({ ... });
  });
});
```

---

## Casos Obligatorios por Endpoint

### Para cada endpoint genera MÍNIMO estos tests:

#### Happy Path
```
given_valid_payload_when_POST_[recurso]_then_returns_201_with_data
given_existing_id_when_GET_[recurso]_then_returns_200_with_entity
given_valid_update_when_PUT_[recurso]_then_returns_200_updated
given_existing_id_when_DELETE_[recurso]_then_returns_204
given_filters_when_GET_[recurso]_list_then_returns_paginated_results
```

#### Error Paths
```
given_missing_required_field_when_POST_then_returns_400_INVALID_DATA
given_invalid_format_when_POST_then_returns_400_with_validation_details
given_no_auth_token_when_GET_protected_then_returns_401_UNAUTHORIZED
given_invalid_token_when_GET_protected_then_returns_401_UNAUTHORIZED
given_nonexistent_id_when_GET_then_returns_404_NOT_FOUND
given_duplicate_entity_when_POST_then_returns_409_CONFLICT
```

#### Edge Cases
```
given_empty_list_when_GET_with_filters_then_returns_200_empty_array
given_max_page_size_when_GET_list_then_returns_correct_pagination
given_boundary_values_when_POST_then_validates_correctly
```

---

## Reglas de Testing

- **Ambiente aislado**: Usar base de datos de test, nunca la de desarrollo
- **Sin dependencias entre tests**: Cada test limpia su propio estado
- **Datos deterministas**: No depender de datos preexistentes
- **Mocks exactos**: Mockear únicamente las dependencias externas (APIs terceros, email, etc.)
- **Cobertura mínima**: 80% de branches cubiertas por endpoint
- **Sin lógica en tests**: Los tests deben ser declarativos, no procedurales

---

## Proceso de Generación

```
PASO 1 → Identificar todos los endpoints del codebase sin tests
PASO 2 → Leer contratos del SPEC para cada endpoint
PASO 3 → Generar tests happy path por endpoint
PASO 4 → Generar tests error paths por endpoint
PASO 5 → Generar tests edge cases relevantes
PASO 6 → Ejecutar suite de tests y verificar que pasan
PASO 7 → Reportar coverage alcanzado
```

## Reporte de Generación

```
🟢 INTEGRATION-TEST-GENERATOR [BACKEND] — REPORTE
════════════════════════════════════════════════════
Endpoints analizados:           X
Endpoints con tests previos:    X
Tests generados:                X
  Happy paths:                  X
  Error paths:                  X
  Edge cases:                   X

Ejecución:
  Tests pasando:                X
  Tests fallando:               X (requieren revisión)

Cobertura alcanzada:            X% (objetivo: >= 80%)
════════════════════════════════════════════════════
```
