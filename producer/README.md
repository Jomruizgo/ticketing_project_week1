# Producer - Microservicio de Publicación de Tickets

## 📋 Descripción

El **Producer** es un microservicio .NET que expone un endpoint HTTP para recibir solicitudes de reserva de tickets y las publica como eventos en **RabbitMQ**.

No realiza consultas a base de datos, solo orquesta la publicación de mensajes.

---

## 🏗️ Arquitectura

Sigue los principios SOLID y está estructurado como:

```
Producer/
├── Models/
│   ├── ReserveTicketRequest.cs       # DTO de entrada
│   └── TicketReservedEvent.cs        # Evento publicado a RabbitMQ
├── Services/
│   ├── ITicketPublisher.cs           # Interfaz del publicador
│   └── RabbitMQTicketPublisher.cs    # Implementación
├── Controllers/
│   └── TicketsController.cs          # Expone endpoints HTTP
├── Extensions/
│   └── RabbitMQExtensions.cs         # Registro de servicios
├── Configurations/
│   └── RabbitMQOptions.cs            # Opciones de configuración
├── Program.cs                        # Bootstrap
├── appsettings.json                  # Configuración producción
└── appsettings.Development.json      # Configuración desarrollo
```

---

## 🚀 Inicio rápido

### Requisitos

- .NET 8.0 o superior
- RabbitMQ corriendo (en el compose del proyecto)

### Compilar

```bash
cd producer/Producer
dotnet build
```

### Ejecutar en desarrollo

```bash
dotnet run
```

La API estará disponible en `https://localhost:7001` (o `http://localhost:5001` si no usas HTTPS).

### Con Docker

```bash
docker build -t producer:latest .
docker run -p 8080:8080 producer:latest
```

---

## 📡 Endpoints

### `POST /api/tickets/reserve`

Reserva un ticket y publica el evento a RabbitMQ.

**Request:**
```json
{
  "eventId": 123,
  "ticketId": 456,
  "orderId": "ORD-2026-001",
  "reservedBy": "usuario@example.com",
  "expiresInSeconds": 300
}
```

**Response (202 Accepted):**
```json
{
  "message": "Reserva procesada",
  "ticketId": 456
}
```

### `GET /api/tickets/health`

Health check del servicio.

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2026-02-10T17:50:00Z"
}
```

---

## ⚙️ Configuración

### `appsettings.json` (Producción)

```json
{
  "RabbitMQ": {
    "Host": "rabbitmq",           // Hostname de RabbitMQ
    "Port": 5672,                 // Puerto AMQP
    "Username": "guest",          // Usuario
    "Password": "guest",          // Contraseña
    "VirtualHost": "/",           // VirtualHost
    "ExchangeName": "tickets",    // Exchange donde publica
    "TicketReservedRoutingKey": "ticket.reserved"  // Routing key
  }
}
```

### `appsettings.Development.json` (Desarrollo Local)

```json
{
  "RabbitMQ": {
    "Host": "localhost"           // Para conectar a RabbitMQ local
  }
}
```

---

## 🔌 RabbitMQ

El producer publica mensajes en:
- **Exchange:** `tickets` (tipo: topic)
- **Routing Key:** `ticket.reserved`
- **Cola:** `q.ticket.reserved` (creada automáticamente por rabbitmq-setup)

### Estructura del mensaje

```json
{
  "ticketId": 456,
  "eventId": 123,
  "orderId": "ORD-2026-001",
  "reservedBy": "usuario@example.com",
  "expiresAt": "2026-02-10T18:00:00Z",
  "createdAt": "2026-02-10T17:55:00Z"
}
```

---

## 🛠️ Principios Aplicados

- **Single Responsibility (SRP):** Cada clase tiene una única responsabilidad
  - `TicketsController` → Maneja HTTP
  - `RabbitMQTicketPublisher` → Publica a RabbitMQ
  
- **Open/Closed (OCP):** El código es extensible
  - `ITicketPublisher` permite agregar nuevos publicadores

- **Dependency Inversion (DIP):** Todo se inyecta por constructor
  - Usa interfaces, no implementaciones concretas

- **Single Source of Truth:** Configuración centralizada en `RabbitMQOptions`

---

## ✅ Validaciones

El endpoint valida:
- `EventId > 0`
- `TicketId > 0`
- `OrderId` no está vacío
- `ReservedBy` no está vacío
- `ExpiresInSeconds > 0`

Devuelve `400 Bad Request` si alguna validación falla.

---

## 📊 Logging

El servicio registra:
- ✅ Publicaciones exitosas
- ❌ Errores y excepciones
- 📝 Detalles del ticket y orden

Ej:
```
Evento de ticket reservado publicado. TicketId: 456, OrderId: ORD-2026-001
```

---

## 🔧 Extensibilidad

Para agregar un nuevo tipo de evento:

1. Crear modelo en `Models/` (ej: `TicketPaymentEvent.cs`)
2. Crear interfaz en `Services/` (ej: `IPaymentPublisher.cs`)
3. Implementar servicio (ej: `RabbitMQPaymentPublisher.cs`)
4. Registrar en `RabbitMQExtensions.cs`
5. Agregar endpoint en `TicketsController.cs`

---

## 📚 Tecnologías

- **.NET 8.0**
- **ASP.NET Core** Web API
- **RabbitMQ.Client 6.8.1**
- **Microsoft.Extensions** para DI y Logging

---

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

