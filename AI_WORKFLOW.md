# AI Workflow - TicketRush MVP

Este documento define la estrategia de interacción con herramientas de IA para el desarrollo del proyecto TicketRush.

## 1. Metodología

### 1.1 Enfoque AI-First
La IA actúa como **Developer Senior** que produce código de alta calidad. El equipo humano asume el rol de **Arquitectos y Revisores con autoridad final**.

| Rol | Responsabilidad |
|-----|-----------------|
| IA | Generar código de calidad, proponer soluciones robustas, seguir mejores prácticas |
| Humano | Tomar decisiones finales, aprobar/rechazar propuestas, definir arquitectura, revisar seguridad |

### 1.2 Reglas de Oro
1. **Nunca aceptar código sin entenderlo** - Si no entiendes qué hace, no lo integres
2. **Prohibido boilerplate manual** - La IA genera estructuras repetitivas
3. **Validación obligatoria** - Todo código crítico lleva comentario `// HUMAN CHECK`
4. **Sin secretos en código** - Variables de entorno para credenciales, siempre

### 1.3 Ciclo de Trabajo
```
[Definir tarea] → [Prompt a IA] → [Revisar output] → [Ajustar/Corregir] → [Integrar] → [PR + Code Review]
```

## 2. Herramientas de IA Utilizadas

| Herramienta | Uso Principal | Responsable |
|-------------|---------------|-------------|
| Claude Code | Desarrollo backend, arquitectura, debugging | Jorge |
| [Agregar otras herramientas del equipo] | | |

## 3. Interacciones Clave

### 3.1 Generación de Código
- **Contexto obligatorio**: Siempre proporcionar schema de BD, estructura del proyecto, y convenciones antes de pedir código
- **Iteración**: Pedir primero estructura/esqueleto, luego implementación detallada
- **Fragmentación**: Dividir tareas complejas en subtareas pequeñas

### 3.2 Debugging
- Proporcionar: mensaje de error completo, código relevante, y contexto de ejecución
- Pedir explicación del problema antes de la solución

### 3.3 Code Review asistido
- Usar IA para detectar code smells, vulnerabilidades, y mejoras
- El humano tiene la última palabra

## 4. Documentos Clave y Contextualización

Archivos que deben compartirse con la IA al iniciar sesión de trabajo:

| Documento | Propósito |
|-----------|-----------|
| `scripts/schema.sql` | Estructura de base de datos |
| `compose.yml` | Configuración de infraestructura |
| `scripts/rabbitmq-definitions.json` | Configuración de colas y exchanges |
| Backlog del sprint | Tareas asignadas y criterios de aceptación |

### 4.1 Prompt de Contextualización Inicial
```
Estamos trabajando en TicketRush, un MVP de sistema de ticketing para eventos.

Stack: .NET 8 (LTS), PostgreSQL, RabbitMQ, Docker
Arquitectura: Microservicios con comunicación asíncrona (event-driven)

Microservicios:
- Producer API: Recibe peticiones HTTP, publica eventos a RabbitMQ
- Consumer Service 1 (Reservations): Procesa reservas de tickets
- Consumer Service 2 (Payments & TTL): Procesa pagos y expiración

Eventos RabbitMQ:
- ticket.reserved
- ticket.payments.approved
- ticket.payments.rejected
- ticket.expired

[Adjuntar schema.sql y archivos relevantes]
```

## 5. Dinámicas de Interacción

### 5.1 Antes de cada sesión
1. Revisar estado actual del código (git status, últimos commits)
2. Identificar tarea específica a realizar
3. Preparar contexto necesario para la IA

### 5.2 Durante la sesión
1. **Un objetivo por prompt** - Evitar prompts con múltiples tareas no relacionadas
2. **Validar incrementalmente** - No esperar a tener todo para probar
3. **Documentar decisiones** - Si la IA propone algo que rechazas, documéntalo

### 5.3 Al finalizar
1. Revisar código generado contra criterios de aceptación
2. Agregar comentarios `// HUMAN CHECK` donde corresponda
3. Commit con mensaje descriptivo
4. PR para revisión de pares

## 6. Convenciones de Comentarios

### 6.1 HUMAN CHECK
Para código crítico donde el humano validó/modificó la sugerencia de IA:

```csharp
// HUMAN CHECK:
// La IA sugirió [descripción de lo que sugirió].
// Se modificó porque [razón del cambio].
```

### 6.2 AI-GENERATED
Para código generado por IA sin modificaciones significativas:

```csharp
// AI-GENERATED: Estructura base del controller
```

## 7. Registro de Decisiones

| Fecha | Decisión | Contexto | Responsable |
|-------|----------|----------|-------------|
| 2026-02-10 | Usar .NET 8 | LTS, consistencia en el equipo | Equipo |
| 2026-02-10 | Exchange tipo topic | Permite routing flexible por eventos | Jostin |
| | | | |

---

*Documento vivo - Actualizar conforme el proyecto evolucione*
