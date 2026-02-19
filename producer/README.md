# Producer - API de publicación de eventos

## 📋 Descripción

El **Producer** es una API HTTP en .NET 8 que recibe solicitudes de reserva y pago, y publica eventos en RabbitMQ.

- No tiene base de datos.
- Responde `202 Accepted` y delega el procesamiento a los consumers.

---

## 🏗️ Arquitectura actual (hexagonal)

```
producer/
├── src/
│   ├── Producer.Domain/
│   │   ├── Events/
│   │   └── Ports/
│   ├── Producer.Application/
│   │   └── UseCases/
│   │       ├── ReserveTicket/
│   │       └── RequestPayment/
│   ├── Producer.Infrastructure/
│   │   ├── Messaging/
│   │   └── DependencyInjection.cs
│   └── Producer.Api/
│       ├── Controllers/
│       ├── Models/
│       ├── Program.cs
│       └── appsettings.json
├── tests/
│   └── Producer.Application.Tests/
└── Dockerfile
```

Regla de dependencias:

`Producer.Domain` ← `Producer.Application` ← `Producer.Infrastructure` ← `Producer.Api`

---

## 🚀 Inicio rápido

### Requisitos

- .NET 8.0+
- RabbitMQ activo (idealmente con `docker compose` del repo)

### Compilar

```bash
dotnet build producer/src/Producer.Api/Producer.Api.csproj
```

### Ejecutar en desarrollo

```bash
dotnet run --project producer/src/Producer.Api/Producer.Api.csproj
```

### Tests de Application

```bash
dotnet test producer/tests/Producer.Application.Tests/Producer.Application.Tests.csproj
```

### Con Docker

```bash
docker build -t producer:latest producer
docker run -p 8080:8080 producer:latest
```

---

## 📡 Endpoints

### `POST /api/tickets/reserve`

Publica `ticket.reserved`.

### `POST /api/payments/process`

Publica `ticket.payment.requested`.

### `GET /health`

Health check general del servicio.

### `GET /api/tickets/health`

Health check compatible con Dockerfile actual.

---

## ⚙️ RabbitMQ

Configuración en `Producer.Api/appsettings.json` bajo sección `RabbitMQ`.

Eventos publicados por el Producer:

- `ticket.reserved`
- `ticket.payment.requested`

No se declaran exchanges/colas aquí; la topología la centraliza `scripts/setup-rabbitmq.sh`.

---

## ✅ Principios de diseño aplicados

- Validación HTTP en controllers (`Producer.Api`).
- Casos de uso y mapeos en handlers (`Producer.Application`).
- Publicación RabbitMQ en adaptadores (`Producer.Infrastructure`).
- Puertos y eventos sin dependencias externas (`Producer.Domain`).

## 🧪 Testing (Recomendado)

```csharp
[Fact]
public async Task PublishTicketReservedAsync_WithValidEvent_PublishesMessage()
{
    // Arrange
    var mockConnection = new Mock<IConnection>();
    var mockChannel = new Mock<IModel>();
    mockConnection.Setup(c => c.CreateModel()).Returns(mockChannel.Object);
    
    var publisher = new RabbitMQTicketPublisher(
        mockConnection.Object,
        Options.Create(new RabbitMQOptions()),
        Mock.Of<ILogger<RabbitMQTicketPublisher>>()
    );
    
    var ticketEvent = new TicketReservedEvent { /* ... */ };
    
    // Act
    await publisher.PublishTicketReservedAsync(ticketEvent);
    
    // Assert
    mockChannel.Verify(ch => ch.BasicPublish(...), Times.Once);
}
```

---

## 📖 Notas

- El Producer **solo publica**, no consume ni procesa
- La configuración se carga automáticamente desde `appsettings.json`
- RabbitMQ debe estar disponible antes de iniciar la aplicación
- Los mensajes son **persistentes** (DeliveryMode = 2)

---

## 🤝 Contribuciones

Mantén el código:
- ✅ Simple y claro
- ✅ Testeable
- ✅ Respetando SOLID
- ✅ Documentado

