# Auditoría Técnica — Semana 3 QA & TDD

> Formato: defensa técnica en vivo según rúbrica Semana 2 — IA-Native Quality & TDD (Mid Level)

---

## 1. Estrategia QA (`TESTING_STRATEGY.md`)

> *"Diferenciamos QA de Testing: QA es la decisión de qué proteger y por qué. Testing es la ejecución que lo demuestra."*

**Pirámide aplicada:**

| Nivel | Alcance | Ejemplo |
|---|---|---|
| Unit | Lógica aislada con mocks (NSubstitute) | `PaymentValidationServiceTests`, `TicketStatusConsumerTests` |
| Component | Pipeline interno sin I/O externo | `TicketStatusPipelineTests` |
| Integration | Contrato entre servicios (SSE, RabbitMQ payload) | `SseContractIntegrationTests` |
| E2E | Scripts Docker reproducibles | `scripts/verify-e2e.sh`, `scripts/verify-devA-expiration.sh` |

**Resultado: 92 tests en verde, 0 regresiones.**

---

## 2. Test que VERIFICA — arquitectura e interfaz de puerto

**Archivo:** `paymentService/MsPaymentService.Worker.Tests/PaymentApprovedEventHandlerTests.cs`

```csharp
[Fact]
public async Task HandleAsync_ValidJson_DelegatesToValidationService()
{
    // ARRANGE
    var json = JsonSerializer.Serialize(new PaymentApprovedEvent { TicketId = 1, ... });
    _validationService
        .ValidateAndProcessApprovedPaymentAsync(Arg.Any<PaymentApprovedEvent>())
        .Returns(ValidationResult.Success());

    // ACT
    var result = await _sut.HandleAsync(json);

    // ASSERT — verifica que el handler llama al puerto correcto, 1 sola vez
    await _validationService.Received(1)
        .ValidateAndProcessApprovedPaymentAsync(
            Arg.Is<PaymentApprovedEvent>(e => e.TicketId == 1));
}
```

**¿Qué verifica?**
`PaymentApprovedEventHandler` depende de `IPaymentValidationService` (puerto de entrada hexagonal) y no de ninguna implementación concreta. Si el handler llamara directamente a PostgreSQL o a RabbitMQ, este test fallaría porque el mock no recibiría la llamada y `Received(1)` rompería. La arquitectura hexagonal queda protegida.

**Dependencias mockeadas:**
- `IPaymentValidationService` → aísla la lógica de validación de negocio
- `IStatusChangedPublisher` → aísla la publicación a RabbitMQ

**Mutación que lo rompería:** eliminar la llamada a `_validationService` en el handler → `Received(1)` falla con 0 invocaciones.

---

## 3. Test que VALIDA — regla de negocio crítica

**Archivo:** `paymentService/MsPaymentService.Worker.Tests/PaymentValidationServiceTests.cs`

```csharp
[Fact]
public async Task ApprovedPayment_AlreadyPaid_ReturnsAlreadyProcessed()
{
    // ARRANGE — ticket ya está en estado "paid"
    var evt = CreateApprovedEvent(ticketId: 1);
    _ticketRepository.GetByIdAsync(1)
        .Returns(new Ticket { Id = 1, Status = TicketStatus.paid });

    // ACT
    var result = await _sut.ValidateAndProcessApprovedPaymentAsync(evt);

    // ASSERT — la regla de negocio: no procesar un pago duplicado
    Assert.True(result.IsAlreadyProcessed);
    Assert.False(result.IsSuccess);
}
```

**¿Qué regla de negocio valida?**
Un ticket ya pagado no puede procesarse dos veces. Sin este test, un mensaje duplicado de RabbitMQ podría cobrarle dos veces a un usuario y el sistema no lo detectaría. Es la protección contra idempotencia rota en el flujo de pagos.

**Segundo caso crítico:**

```csharp
[Fact]
public async Task ApprovedPayment_InvalidStatus_Available_ReturnsFailure()
```

Valida que un ticket en estado `available` (no reservado) no puede recibir un pago aprobado — protege la máquina de estados del dominio: no se puede pagar lo que nunca se reservó.

**Dependencias mockeadas:**
- `ITicketRepository` → retorna el estado actual del ticket sin tocar DB real
- `IPaymentRepository` → aísla persistencia del pago
- `ITicketStateService` → aísla la transición de estado

---

## 4. Human Check — Estructura de respuesta ante el instructor

Si el instructor elige un test aleatorio del repositorio, la explicación sigue este orden:

### Paso 1 — Identificar qué se está probando
> "Este test prueba `[nombre del método]` de `[clase]` que implementa `[interfaz/puerto]`."

### Paso 2 — Explicar cada mock
> "Se mockea `[interfaz]` porque `[clase concreta]` requeriría `[DB/RabbitMQ/red]` que no queremos en un test unitario. El mock devuelve `[valor]` para simular el escenario `[descripción]`."

### Paso 3 — Conectar el Assert con la regla
> "El `Assert` verifica que `[condición]` — lo que protege la regla de negocio: `[regla en lenguaje de negocio]`."

### Paso 4 — Demostrar que no es teatro de calidad
> "Si cambio `[línea concreta del código de producción]`, este test falla porque `[razón]`."

---

## 5. Trazabilidad TDD — Dev A (commits atómicos verificables)

```
f676b8a  🔴 RED    — test(red): add expiration use case tests including technical failure
83eccf1  🟢 GREEN  — feat(green): implement automatic reservation expiration flow
3f4638c  🔵 REFACTOR — refactor: decouple status publishing and harden expiration consumer
9ba2bcd  ✅ E2E   — test(integration): add repeatable docker verification for Dev A expiration flow
```

**Cómo demostrarlo en vivo:**
```bash
git show f676b8a --stat   # test escrito primero, sin implementación
git show 83eccf1 --stat   # implementación mínima que hace pasar el test
git show 3f4638c --stat   # refactor sin nuevos tests añadidos
```

---

## 6. Reportes automatizados

Ubicación: `docs/evidencias/semana3/artifacts/`

| Suite | Archivo | Tests |
|---|---|---|
| ReservationService | `reservation/reservation-week3.trx` | 9 ✅ |
| CrudService | `crud/crud-week3.trx` | 58 ✅ |
| PaymentService | `payment/payment-week3.trx` | 25 ✅ |

Comando reproducible para cualquier suite:
```bash
dotnet test --logger "trx;LogFileName=resultado.trx" --results-directory ./artifacts
```
