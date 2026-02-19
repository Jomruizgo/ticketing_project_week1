# 📐 Arquitectura del Producer (Hexagonal)

## Resumen

El Producer está dividido en cuatro capas para separar responsabilidades y aplicar DIP:

- `Producer.Domain`: eventos y puertos.
- `Producer.Application`: casos de uso (`ReserveTicket`, `RequestPayment`).
- `Producer.Infrastructure`: adaptadores RabbitMQ + wiring DI.
- `Producer.Api`: controllers HTTP, DTOs y composition root.

## Regla de dependencias

```
Producer.Domain <- Producer.Application <- Producer.Infrastructure <- Producer.Api
```

## Estructura

```
producer/
├── src/
│   ├── Producer.Domain/
│   │   ├── Events/
│   │   │   ├── TicketReservedEvent.cs
│   │   │   └── PaymentRequestedEvent.cs
│   │   └── Ports/
│   │       ├── ITicketEventPublisher.cs
│   │       └── IPaymentEventPublisher.cs
│   ├── Producer.Application/
│   │   └── UseCases/
│   │       ├── ReserveTicket/
│   │       └── RequestPayment/
│   ├── Producer.Infrastructure/
│   │   ├── Messaging/
│   │   │   ├── RabbitMQSettings.cs
│   │   │   ├── RabbitMQTicketPublisher.cs
│   │   │   └── RabbitMQPaymentPublisher.cs
│   │   └── DependencyInjection.cs
│   └── Producer.Api/
│       ├── Controllers/
│       ├── Models/
│       └── Program.cs
└── tests/
    └── Producer.Application.Tests/
```

## Flujo de reserva

1. `POST /api/tickets/reserve` llega a `TicketsController`.
2. Controller valida entrada y crea `ReserveTicketCommand`.
3. `ReserveTicketCommandHandler` mapea a `TicketReservedEvent`.
4. `ITicketEventPublisher` publica vía `RabbitMQTicketPublisher`.
5. Mensaje sale por exchange `tickets` con routing key `ticket.reserved`.

## Flujo de pago

1. `POST /api/payments/process` llega a `PaymentsController`.
2. Controller valida entrada y crea `RequestPaymentCommand`.
3. `RequestPaymentCommandHandler` mapea a `PaymentRequestedEvent`.
4. Si `TransactionRef` viene null, se genera `TXN-{Guid}`.
5. `IPaymentEventPublisher` publica por routing key `ticket.payment.requested`.

## Notas operativas

- La topología RabbitMQ sigue centralizada en `scripts/setup-rabbitmq.sh`.
- `Program.cs` conserva CORS `AllowAll` para MVP (con `// HUMAN CHECK`).
- El Producer no contiene lógica de decisión de pago ni persistencia.
