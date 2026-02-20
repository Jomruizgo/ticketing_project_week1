---                          
  Guion de Exposición — CrudService y PaymentService
                                                                                                                                                                 
  ---
  [El problema: Mock Imposible]                                                                                                                                  
                  
  Cuando aplicamos la metodología Mock Imposible — intentar escribir tests unitarios puros sin Docker, sin base de datos, sin RabbitMQ — nos encontramos con que
  la lógica de negocio era imposible de aislar.

  Los servicios tenían todo mezclado en un solo proyecto: el controlador llamaba al service, el service usaba EF Core directamente. Para testear cualquier regla
  de negocio necesitabas levantar toda la infraestructura. Eso nos reveló violaciones de DIP y SRP, y fue la señal para migrar a arquitectura hexagonal.

  ---
  [CrudService: los smells concretos]

  En el CrudService encontramos tres problemas puntuales:

  Primero, todo el código vivía en un solo proyecto — no había separación entre lógica de negocio e infraestructura.

  Segundo, los cuatro repositorios vivían en un único archivo de 200 líneas — un God File — y las cuatro interfaces en otro. Violación de SRP e ISP al mismo
  tiempo.

  Tercero, los repositorios dependían directamente de TicketingDbContext concreto, haciendo imposible testear la lógica de negocio sin base de datos.

  La solución: separamos en tres capas independientes — Domain, Application, Infrastructure — y el punto de entrada HTTP existente (los controladores) quedó
  desacoplado de la lógica de negocio, dependiendo solo de los handlers de Application. Cada operación tiene su propio handler: cinco para eventos, seis para
  tickets. SRP: un handler, un motivo de cambio. DIP: los handlers dependen de interfaces de Domain, nunca de EF Core. ISP: tres interfaces segregadas, una por
  responsabilidad.

  Resultado: 19 tests unitarios, todos pasando, sin base de datos ni Docker.

  ---
  [PaymentService: complejidad adicional]

  El PaymentService tenía el mismo problema de base, más uno adicional: procesaba mensajes de tres colas de RabbitMQ con lógica de negocio mezclada dentro del
  consumer.

  En la versión original había condicionales para enrutar por tipo de cola. Eso viola OCP: agregar un nuevo tipo de evento obliga a modificar el consumer
  existente.

  La solución fue el patrón Strategy + Dispatcher: cada cola tiene su propio IPaymentEventHandler. El PaymentEventDispatcherImpl recibe todos los handlers por
  inyección de colección y selecciona el correcto por nombre de cola. Agregar un nuevo evento es solo implementar un nuevo handler y registrarlo en DI — sin
  tocar nada existente.

  La lógica de negocio vive en dos command handlers: uno para pago aprobado, uno para pago rechazado. Estos dependen de ITicketStateService — un puerto que
  define la transición de estados. La implementación concreta con EF Core y transacciones queda en Infrastructure. La lógica no toca la base de datos
  directamente.

  Resultado: 25 tests unitarios, todos pasando, sin RabbitMQ ni base de datos.

  ---
  [El test del CTO]

  La pregunta final: si mañana cambiamos RabbitMQ por Kafka, ¿cuánto código de negocio hay que reescribir?

  En CrudService: cero. No tiene ninguna dependencia de mensajería en la lógica de negocio.

  En PaymentService: solo se reescriben TicketPaymentConsumer y RabbitMQConnection en Infrastructure. Los command handlers y el dispatcher — todo lo que contiene
  lógica de negocio — no requiere reescritura. Los event handlers necesitan re-wireado de DI, pero no se toca su lógica.

  Ese es el objetivo de la arquitectura hexagonal: que los detalles técnicos sean intercambiables sin tocar el negocio.

  ---
  [Patrones de diseño aplicados — por categoría]

  CREACIONALES — Singleton

  Usado en ambos servicios. En CrudService, el TicketStatusHub se registra como Singleton porque necesita sobrevivir múltiples requests HTTP para mantener el
  mapa de conexiones SSE activas por ticketId. Si se creara una instancia por request, cada cliente SSE quedaría en un hub distinto y nunca recibiría la
  notificación. En PaymentService, RabbitMQConnection, TicketPaymentConsumer y StatusChangedPublisher son Singleton porque comparten una única conexión física
  al broker. Crear una conexión nueva por mensaje sería costoso e innecesario.

  La justificación técnica es la misma en ambos casos: el estado compartido entre requests o entre mensajes exige una única instancia controlada.

  ESTRUCTURALES — Repository

  Usado en ambos servicios. Cada servicio define interfaces de repositorio en Domain y las implementa con EF Core en Infrastructure. Los handlers de Application
  nunca importan EF Core — solo conocen el contrato.

  La justificación: encapsular el acceso a datos detrás de una abstracción orientada al dominio es lo que hace posible mockear los repositorios en los tests
  unitarios. Sin Repository, los handlers estarían acoplados a DbContext y no se podrían testear sin base de datos.

  ESTRUCTURALES — Adapter

  Usado en PaymentService. Los tres event handlers — PaymentApprovedEventHandler, PaymentRejectedEventHandler y PaymentRequestedEventHandler — son adaptadores
  entre el mundo de RabbitMQ (JSON crudo como string) y el mundo de Application (Commands tipados).

  La justificación: el caso de uso no sabe nada de colas ni de JSON. El adapter traduce el mensaje entrante al contrato que el handler de negocio espera. Si
  mañana el mensaje cambia de formato, solo se modifica el adapter, no la lógica de negocio.

  COMPORTAMIENTO — Strategy (con Dispatcher)

  Usado en PaymentService. IPaymentEventHandler define el contrato — QueueName más HandleAsync. Cada implementación es una estrategia independiente para un tipo
  de evento. PaymentEventDispatcherImpl recibe la colección completa de estrategias por inyección y selecciona la correcta en tiempo de ejecución según el
  nombre de la cola.

  La justificación: sin Strategy, el consumer tendría un switch con un caso por tipo de evento. Agregar un nuevo tipo de evento implica modificar ese switch —
  violación de OCP. Con Strategy, agregar un nuevo evento es solo registrar una nueva implementación en DI. El dispatcher no cambia.

  COMPORTAMIENTO — Command Handler

  Usado en ambos servicios. Cada intención de negocio tiene su propio handler: CreateEventCommandHandler, ProcessApprovedPaymentCommandHandler, etc.

  La justificación: un servicio monolítico con múltiples métodos mezcla responsabilidades y hace los tests más frágiles. Un handler por caso de uso significa
  que cada test prueba exactamente una intención, con el mock mínimo necesario. Es también la base para aplicar SRP de forma medible.

  ---