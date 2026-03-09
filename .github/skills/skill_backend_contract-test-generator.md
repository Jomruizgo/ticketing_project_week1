---
description: 'Skill especializada en generación de contract tests entre servicios. Genera contratos consumer-driven con Pact o mocks de servidor para verificar compatibilidad entre productor y consumidor de APIs.'
---

# Skill: contract-test-generator [BACKEND]

## Responsabilidad
Generar y verificar contratos entre servicios para garantizar que
los cambios en un productor no rompen a sus consumidores.

---

## Cuándo Aplica Esta Skill

Actívala cuando se detecte ALGUNA de estas condiciones:
- Arquitectura de microservicios o servicios distribuidos
- Múltiples servicios compartiendo DTOs o modelos
- APIs que son consumidas por otros servicios propios
- Contratos definidos en el SPEC entre componentes distintos

---

## Tipos de Contract Tests a Generar

### 1. Consumer-Driven Contract Tests (Pact)
Para cada relación consumidor → productor:

```typescript
// CONSUMER SIDE — genera en el servicio consumidor
import { Pact } from '@pact-foundation/pact';

describe('OrderService → PaymentService contract', () => {
  const provider = new Pact({
    consumer: 'OrderService',
    provider: 'PaymentService',
    port: 1234,
  });

  beforeAll(() => provider.setup());
  afterAll(() => provider.finalize());
  afterEach(() => provider.verify());

  it('given valid order_when request payment_then returns payment confirmation', async () => {
    // GIVEN — define la interacción esperada
    await provider.addInteraction({
      state: 'a valid order exists',
      uponReceiving: 'a payment request for valid order',
      withRequest: {
        method: 'POST',
        path: '/api/v1/payments',
        headers: { 'Content-Type': 'application/json' },
        body: {
          orderId: like('uuid-123'),
          amount: like(100.50),
          currency: term({ matcher: '^[A-Z]{3}$', generate: 'USD' }),
        },
      },
      willRespondWith: {
        status: 201,
        body: {
          paymentId: like('pay-uuid-456'),
          status: term({ matcher: 'PENDING|APPROVED|REJECTED', generate: 'APPROVED' }),
          processedAt: iso8601DateTime(),
        },
      },
    });

    // WHEN — el consumidor ejecuta la llamada real
    const result = await paymentClient.processPayment({
      orderId: 'uuid-123',
      amount: 100.50,
      currency: 'USD',
    });

    // THEN — valida respuesta según el contrato
    expect(result.paymentId).toBeDefined();
    expect(['PENDING', 'APPROVED', 'REJECTED']).toContain(result.status);
  });
});
```

### 2. Provider Verification (en el servicio productor)
```typescript
// PROVIDER SIDE — genera en el servicio productor
import { Verifier } from '@pact-foundation/pact';

describe('PaymentService provider verification', () => {
  it('verifies all consumer contracts', async () => {
    const verifier = new Verifier({
      provider: 'PaymentService',
      providerBaseUrl: 'http://localhost:3001',
      pactBrokerUrl: process.env.PACT_BROKER_URL,
      publishVerificationResult: true,
      providerVersion: process.env.APP_VERSION,
      stateHandlers: {
        'a valid order exists': async () => {
          // setup del estado en la base de datos de test
          await testDb.orders.create({ id: 'uuid-123', status: 'confirmed' });
        },
      },
    });

    await verifier.verifyProvider();
  });
});
```

---

## Mapa de Relaciones a Documentar

Por cada par consumidor-productor identificado en el SPEC:

```
┌─────────────────────────────────────────────────┐
│ CONSUMIDOR         CONTRATO         PRODUCTOR    │
├─────────────────────────────────────────────────┤
│ [Servicio A]  ──► [endpoint]  ──►  [Servicio B] │
│ [Servicio C]  ──► [endpoint]  ──►  [Servicio D] │
└─────────────────────────────────────────────────┘
```

---

## Reglas de Contract Tests

- El contrato lo define el CONSUMIDOR, no el productor
- Verificar contratos en el pipeline del productor ante CADA PR
- Publicar resultados al Pact Broker (si existe)
- Si no hay Pact Broker → usar archivos `.json` locales con contrato versionado
- Los state handlers deben usar datos de test, nunca de producción

---

## Proceso de Generación

```
PASO 1 → Identificar relaciones consumidor-productor del SPEC y codebase
PASO 2 → Generar contract tests en el lado del consumidor
PASO 3 → Generar provider verification en el lado del productor
PASO 4 → Documentar mapa de relaciones detectadas
PASO 5 → Ejecutar verificación y reportar estado
```

## Reporte

```
🟡 CONTRACT-TEST-GENERATOR [BACKEND] — REPORTE
════════════════════════════════════════════════
Relaciones consumidor→productor detectadas: X
Contratos generados (consumer side):        X
Verificaciones generadas (provider side):   X

Estado de contratos:
  Verificados y pasando:  X
  Fallando (breaking):    X → ATENCIÓN requerida
  Pendientes de ejecutar: X
════════════════════════════════════════════════
```
