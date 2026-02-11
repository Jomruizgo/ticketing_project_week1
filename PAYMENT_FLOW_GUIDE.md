# Guía del Flujo de Pago - Sistema de Tickets

## 🎯 Descripción General

El sistema implementa un flujo de pago distribuido:

1. **Frontend (Next.js)** - Captura datos de pago del usuario
2. **Producer Service** - Publica eventos de pago a RabbitMQ
3. **RabbitMQ** - Enruta eventos según tipo (aprobado/rechazado)
4. **CRUD Service** - Consume eventos y actualiza estado del ticket

## 📋 Flujo de Compra Completo

### Paso 1: Seleccionar Tickets
- Usuario accede a `/buy/[id]`
- Ingresa cantidad de tickets y email
- Click en "Comprar X Ticket(s)"

### Paso 2: Reservar Tickets
```
Frontend → Producer.reserveTicket()
         ↓
Producer: 202 Accepted
         ↓
Frontend: Polling `/api/tickets/{ticketId}` hasta status="reserved"
         ↓
Frontend: Muestra formulario de pago
```

### Paso 3: Procesar Pago
```
Frontend → Producer.processPayment()
         ↓
Producer Controller:
  - Valida datos de pago
  - 80% éxito → Publica PaymentApprovedEvent
  - 20% rechazo → Publica PaymentRejectedEvent
         ↓
RabbitMQ Topic Exchange "tickets":
  - Routing Key: ticket.payments.approved
  - Routing Key: ticket.payments.rejected
         ↓
Frontend: Polling `/api/tickets/{ticketId}` hasta status="paid"
         ↓
Pantalla de éxito
```

## 🛠️ Integración Frontend

### Componentes Nuevos

#### `components/payment-form.tsx`
- Captura: número de tarjeta, nombre, vencimiento, CVV
- Validaciones: formato, dígitos, fecha expirada
- Envía: `api.processPayment(ProcessPaymentRequest)`

#### `components/payment-status.tsx`
- Estados: `processing`, `success`, `error`
- Muestra loader, confirmación o error

### Hooks Nuevos

#### `hooks/use-payment-status.ts`
- Polling automático cada 500ms
- Timeout: 10 segundos por defecto
- Callbacks: `onPaymentConfirmed`, `onPaymentRejected`

### Métodos API Nuevos

#### `lib/api.ts`
```typescript
api.processPayment({
  ticketId: number
  eventId: number
  amountCents: number
  currency: string
  paymentBy: string
  paymentMethodId: string
  transactionRef: string
}): Promise<{
  message: string
  ticketId: number
  eventId: number
  status: string
}>
```

## 🧪 Pruebas Manuales

### 1. Preparar Ambiente
```bash
cd /Users/jostinalvarados/Documents/Proyectos/Sofka/ticketing_project_week0

# Iniciar servicios
docker-compose up -d --build

# Verificar salud
docker-compose ps  # Todos deben mostrar (healthy)
```

### 2. Crear Evento y Tickets
```bash
# Crear evento
curl -X POST http://localhost:8002/api/events \
  -H "Content-Type: application/json" \
  -d '{"name":"Test Event","startsAt":"2026-12-25T20:00:00Z","price":2999}'

# Crear tickets (eventId=1, 10 tickets)
curl -X POST http://localhost:8002/api/tickets/bulk \
  -H "Content-Type: application/json" \
  -d '{"eventId":1,"quantity":10}'
```

### 3. Probar Flujo en Frontend
1. Abrir `http://localhost:3000/buy`
2. Click en evento
3. Seleccionar cantidad: 1
4. Ingresar email: `test@example.com`
5. Click "Comprar 1 Ticket"
6. Esperar confirmación de reserva (2-3 segundos)
7. Ingresar datos de pago:
   - Tarjeta: `4111 1111 1111 1111`
   - Nombre: `TEST USER`
   - Vencimiento: `12/25`
   - CVV: `123`
8. Click "Pagar"
9. Esperar confirmación (hasta 10 segundos)

### 4. Verificar Eventos en RabbitMQ
```bash
# Acceder a Management UI
open http://localhost:15672
# Usuario/Pass: guest/guest

# Verificar:
# - Exchange "tickets" existe
# - Routing keys: ticket.reserved, ticket.payments.approved, ticket.payments.rejected
# - Mensajes en colas
```

### 5. Verificar Estado en CRUD Service
```bash
# Ver ticket después de pago
curl http://localhost:8002/api/tickets/1
# Esperar: status="paid"
```

## 🔄 Flujo de Pasos en Frontend

```typescript
type PurchaseStep = 
  | "form"           // Formulario inicial
  | "processing"     // Procesando reserva
  | "reserved"       // Mostrar formulario de pago
  | "payment"        // (Disponible para estado de pago en progreso)
  | "confirming"     // Polling estado de pago
  | "success"        // Pago aprobado
  | "error"          // Error en compra/pago
```

## 📊 Datos de Pago Simulados

El Producer Service simula pagos con:
- **Éxito (80%)**: Publica `PaymentApprovedEvent`
- **Rechazo (20%)**: Publica `PaymentRejectedEvent`

Las muestras son determinísticas basadas en `transactionRef`.

## 🐛 Troubleshooting

### El polling no termina
- Verificar que la cola de pagos existe en RabbitMQ
- Verificar logs del Producer: `docker logs producer`
- Verificar que CRUD Service está recibiendo eventos

### Formulario de pago no se muestra
- Verificar que la reserva fue exitosa (buscar en network tab)
- Verificar que `reservedTicketIds` está poblado

### Error "El pago tardó demasiado"
- Verificar que el backend procesa el evento (logs)
- Aumentar `maxDuration` en `usePaymentStatus` si es necesario

## 📝 Notas Técnicas

- **Price Format**: Centavos USD (e.g., 2999 = $29.99)
- **Polling**: 500ms interval, 10s max duration
- **RabbitMQ**: Topic Exchange, reconexión automática cada 10s
- **Async**: Todos los endpoints devuelven 202 Accepted
- **Security**: Datos de tarjeta nunca se almacenan (solo demostración)
