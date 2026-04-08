# Quickstart: Waitlist Enrollment UI

**Feature**: 007-waitlist-enrollment-ui
**Date**: 2026-04-08

## Prerrequisitos

1. Backend corriendo (CRUD Service con endpoint waitlist):
   ```bash
   docker compose up -d --build
   ```

2. Frontend en desarrollo:
   ```bash
   cd frontend && npm install && npm run dev
   ```

3. Datos de prueba (opcional):
   ```bash
   psql -h localhost -U postgres -d ticketing -f scripts/insert-test-data.sql
   ```

## Archivos a crear/modificar

| Archivo | Acción | Descripción |
|---------|--------|-------------|
| `frontend/lib/types.ts` | Modificar | Agregar `WaitlistEntryDto` |
| `frontend/lib/api.ts` | Modificar | Agregar `enrollInWaitlist()` |
| `frontend/components/waitlist-enroll-form.tsx` | Crear | Componente de formulario |
| `frontend/app/buy/[id]/page.tsx` | Modificar | Renderizado condicional |

## Orden de implementación (TDD lógico)

### Paso 1: Tipo WaitlistEntryDto
Agregar en `frontend/lib/types.ts`:
```typescript
export interface WaitlistEntryDto {
  id: number
  eventId: number
  buyerEmail: string
  status: string
  enrolledAt: string
}
```

### Paso 2: Función API enrollInWaitlist
Agregar en `frontend/lib/api.ts` dentro del objeto `api`:
```typescript
async enrollInWaitlist(eventId: number, buyerEmail: string): Promise<WaitlistEntryDto> {
  // POST al CRUD Service, manejo específico de 201/409/422/404
}
```

### Paso 3: Componente WaitlistEnrollForm
Crear `frontend/components/waitlist-enroll-form.tsx`:
- Props: `eventId: number`
- Input de email con validación
- Botón "Unirse a la lista de espera"
- Estados: idle → loading → success/error
- Mensajes diferenciados por código HTTP

### Paso 4: Lógica condicional en página de compra
Modificar `frontend/app/buy/[id]/page.tsx`:
- Si `availableTickets > 0` → flujo de compra existente
- Si `availableTickets === 0 && startsAt > now` → `<WaitlistEnrollForm />`
- Si `startsAt <= now` → mensaje de lista cerrada

## Validación

### TC-HU7-01: Lista de espera visible cuando no hay disponibilidad
1. Crear un evento con `availableTickets: 0` y fecha futura.
2. Navegar a `/buy/{eventId}`.
3. Verificar que se muestra el formulario de waitlist y no el de compra.

### TC-HU7-02: Inscripción exitosa
1. En la página del paso anterior, ingresar un email válido.
2. Click en "Unirse a la lista de espera".
3. Verificar mensaje de confirmación con estado activo.

### TC-HU7-03: Lista cerrada para evento pasado
1. Crear un evento con `availableTickets: 0` y fecha pasada.
2. Navegar a `/buy/{eventId}`.
3. Verificar que no se muestra el formulario y sí el mensaje de lista cerrada.

### TC-HU7-04: Rechazo de duplicado
1. Inscribir un email en un evento.
2. Intentar inscribir el mismo email en el mismo evento.
3. Verificar mensaje informativo de duplicado.
