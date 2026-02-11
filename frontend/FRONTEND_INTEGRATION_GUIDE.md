# 🚀 Frontend Integration Guide

## Resumen Ejecutivo

Este documento proporciona a los desarrolladores frontend todo lo necesario para consumir la API del sistema de ticketing. Incluye patrones recomendados, configuración y ejemplos listos para copiar-pegar.

---

## 📌 Quick Start

### URLs Base (Desarrollo Local)
```javascript
const API_URLS = {
  CRUD: 'http://localhost:8002',
  PRODUCER: 'http://localhost:8001'
};
```

### Entidades Principales
```javascript
// Evento (lectura)
Event {
  id: number,
  name: string,
  startsAt: ISO8601DateTime,
  availableTickets: number,
  reservedTickets: number,
  paidTickets: number
}

// Ticket (lectura/escritura)
Ticket {
  id: number,
  eventId: number,
  status: 'available' | 'reserved' | 'paid' | 'released' | 'cancelled',
  reservedAt: ISO8601DateTime | null,
  expiresAt: ISO8601DateTime | null,
  paidAt: ISO8601DateTime | null,
  orderId: string | null,
  reservedBy: string | null,
  version: number  // Para optimistic locking
}
```

---

## 🏗️ Arquitectura de Integración

```
┌─────────────────┐
│   Frontend App  │
│  (React/Vue)    │
└────────┬────────┘
         │
    ┌────┴─────┐
    │           │
    ▼           ▼
 CRUD API  PRODUCER API
   :8002      :8001
    │           │
    ├────┬──────┤
    │    │      │
    ▼    ▼      ▼
  Events Tickets Reserve
  (GET/POST/PUT/DELETE)
```

### Flujo de Datos

**Creación de Evento (Synchronous):**
```
Frontend POST /api/events → CRUD Service → PostgreSQL
         ← 201 Created (sync response) ←
```

**Reserva de Ticket (Asynchronous):**
```
Frontend POST /api/tickets/reserve → Producer Service → RabbitMQ
         ← 202 Accepted (async) ← 
              ↓
        [RabbitMQ distributes]
              ↓
        [Future consumers process]
```

---

## 🛠️ Setup para Frontend

### 1. Instalación de Dependencias

```bash
# Si usas Fetch API (nativo, recomendado para proyectos pequeños)
# No necesitas dependencias

# Si prefieres Axios
npm install axios

# Si prefieres Fetch con mejoras
npm install node-fetch
```

### 2. Crear Service Layer

**services/ticketingApi.js**
```javascript
export const ticketingApi = {
  CRUD: 'http://localhost:8002',
  PRODUCER: 'http://localhost:8001',

  // Events
  async getEvents() {
    const res = await fetch(`${this.CRUD}/api/events`);
    if (!res.ok) throw new Error('Failed to fetch events');
    return res.json();
  },

  async getEventById(id) {
    const res = await fetch(`${this.CRUD}/api/events/${id}`);
    if (!res.ok) throw new Error(`Event ${id} not found`);
    return res.json();
  },

  async createEvent(name, startsAt) {
    const res = await fetch(`${this.CRUD}/api/events`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name, startsAt })
    });
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  },

  async updateEvent(id, updates) {
    const res = await fetch(`${this.CRUD}/api/events/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(updates)
    });
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  },

  async deleteEvent(id) {
    const res = await fetch(`${this.CRUD}/api/events/${id}`, {
      method: 'DELETE'
    });
    if (!res.ok) throw new Error(`Failed to delete event ${id}`);
  },

  // Tickets
  async getTicketsByEvent(eventId) {
    const res = await fetch(`${this.CRUD}/api/tickets/event/${eventId}`);
    if (!res.ok) throw new Error(`Failed to fetch tickets for event ${eventId}`);
    return res.json();
  },

  async getTicketById(id) {
    const res = await fetch(`${this.CRUD}/api/tickets/${id}`);
    if (!res.ok) throw new Error(`Ticket ${id} not found`);
    return res.json();
  },

  async createTickets(eventId, quantity) {
    const res = await fetch(`${this.CRUD}/api/tickets/bulk`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ eventId, quantity })
    });
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  },

  async updateTicketStatus(id, newStatus, reason = '') {
    const res = await fetch(`${this.CRUD}/api/tickets/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ newStatus, reason })
    });
    if (!res.ok) {
      if (res.status === 409) {
        throw new Error('Version conflict - ticket was modified');
      }
      throw new Error(await res.text());
    }
    return res.json();
  },

  // Producer - Reserve
  async reserveTicket(eventId, ticketId, orderId, reservedBy, expiresInSeconds = 300) {
    const res = await fetch(`${this.PRODUCER}/api/tickets/reserve`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        eventId,
        ticketId,
        orderId,
        reservedBy,
        expiresInSeconds
      })
    });
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  }
};
```

### 3. Usar en Componentes

**React Example:**
```javascript
import { useEffect, useState } from 'react';
import { ticketingApi } from './services/ticketingApi';

export function EventsPage() {
  const [events, setEvents] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    ticketingApi
      .getEvents()
      .then(setEvents)
      .catch(setError)
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <div>Cargando eventos...</div>;
  if (error) return <div>Error: {error.message}</div>;

  return (
    <div>
      {events.map(event => (
        <div key={event.id}>
          <h3>{event.name}</h3>
          <p>Disponibles: {event.availableTickets}</p>
        </div>
      ))}
    </div>
  );
}
```

---

## 🔄 Patrones Comunes

### 1. Obtener Lista y Filtrar

```javascript
// Obtener todos los eventos y mostrar solo los futuros
async function getUpcomingEvents() {
  const events = await ticketingApi.getEvents();
  const now = new Date();
  return events.filter(e => new Date(e.startsAt) > now);
}
```

### 2. Crear Recurso y Esperar Confirmación

```javascript
async function createEventWithTickets(name, startsAt, ticketCount) {
  // 1. Crear evento (sync)
  const event = await ticketingApi.createEvent(name, startsAt);
  
  // 2. Crear tickets (sync)
  const ticketsResult = await ticketingApi.createTickets(event.id, ticketCount);
  
  return { event, tickets: ticketsResult.tickets };
}
```

### 3. Reservar y Esperar (Polling)

```javascript
async function reserveTicketAndWait(eventId, ticketId, userEmail) {
  // 1. Enviar reserva (async - 202)
  await ticketingApi.reserveTicket(
    eventId,
    ticketId,
    `ORD-${Date.now()}`,
    userEmail,
    600 // 10 minutos
  );

  // 2. Esperar a que se actualice (polling)
  let ticket = null;
  let attempts = 0;
  const maxAttempts = 20; // 20 * 500ms = 10 segundos

  while (attempts < maxAttempts) {
    await new Promise(r => setTimeout(r, 500));
    ticket = await ticketingApi.getTicketById(ticketId);
    
    if (ticket.status === 'reserved') {
      return ticket; // Éxito
    }
    
    if (ticket.status === 'cancelled') {
      throw new Error('Reserva fue cancelada');
    }
    
    attempts++;
  }

  throw new Error('Timeout esperando confirmación de reserva');
}
```

### 4. Manejar Concurrencia Optimista

```javascript
async function releaseTicket(ticketId, reason) {
  let ticket = await ticketingApi.getTicketById(ticketId);
  
  let retries = 3;
  while (retries > 0) {
    try {
      const updated = await ticketingApi.updateTicketStatus(
        ticketId,
        'released',
        reason
      );
      return updated;
    } catch (error) {
      if (error.message.includes('Version conflict')) {
        // Recargar y reintentar
        ticket = await ticketingApi.getTicketById(ticketId);
        retries--;
        await new Promise(r => setTimeout(r, 100 * (4 - retries)));
      } else {
        throw error;
      }
    }
  }
  
  throw new Error('Falló después de múltiples intentos');
}
```

### 5. Invalidación de Cache

```javascript
class EventCache {
  constructor() {
    this.cache = new Map();
    this.ttl = 5 * 60 * 1000; // 5 minutos
  }

  async getEvents() {
    if (this.cache.has('events')) {
      const { data, timestamp } = this.cache.get('events');
      if (Date.now() - timestamp < this.ttl) {
        return data;
      }
    }

    const data = await ticketingApi.getEvents();
    this.cache.set('events', { data, timestamp: Date.now() });
    return data;
  }

  invalidate() {
    this.cache.clear();
  }
}
```

---

## 🧪 Casos de Uso Completos

### Caso 1: Flujo de Compra

```javascript
async function purchaseTickets(eventId, ticketCount, userEmail) {
  try {
    // 1. Obtener event
    const event = await ticketingApi.getEventById(eventId);
    console.log(`Comprando ${ticketCount} tickets para ${event.name}`);

    // 2. Obtener tickets disponibles
    const allTickets = await ticketingApi.getTicketsByEvent(eventId);
    const available = allTickets.filter(t => t.status === 'available').slice(0, ticketCount);
    
    if (available.length < ticketCount) {
      throw new Error(`Solo ${available.length} tickets disponibles`);
    }

    // 3. Reservar cada ticket
    const orderId = `ORD-${Date.now()}`;
    const reservations = await Promise.all(
      available.map(ticket =>
        ticketingApi
          .reserveTicket(eventId, ticket.id, orderId, userEmail, 600)
          .then(() => ticket.id)
      )
    );

    console.log(`${reservations.length} tickets reservados`);

    // 4. Esperar confirmación
    await Promise.all(
      reservations.map(ticketId => waitForReservation(ticketId))
    );

    return {
      orderId,
      ticketCount: reservations.length,
      timestamp: new Date()
    };
  } catch (error) {
    console.error('Error en compra:', error);
    throw error;
  }
}

async function waitForReservation(ticketId, timeout = 10000) {
  const start = Date.now();
  while (Date.now() - start < timeout) {
    const ticket = await ticketingApi.getTicketById(ticketId);
    if (ticket.status === 'reserved') return ticket;
    if (ticket.status !== 'available') throw new Error(`Ticket status: ${ticket.status}`);
    await new Promise(r => setTimeout(r, 500));
  }
  throw new Error(`Timeout reservando ticket ${ticketId}`);
}
```

### Caso 2: Administración de Eventos

```javascript
async function manageEvent(eventData) {
  // Crear evento
  const event = await ticketingApi.createEvent(
    eventData.name,
    eventData.startsAt
  );
  console.log(`Evento creado: ${event.id}`);

  // Crear tickets
  const ticketsResult = await ticketingApi.createTickets(
    event.id,
    eventData.ticketQuantity
  );
  console.log(`${ticketsResult.createdCount} tickets creados`);

  // Obtener estado
  const eventState = await ticketingApi.getEventById(event.id);
  console.log(
    `Estado: ${eventState.availableTickets} disp, ` +
    `${eventState.reservedTickets} reservados, ` +
    `${eventState.paidTickets} pagados`
  );

  return event;
}
```

### Caso 3: Monitoreo de Disponibilidad

```javascript
async function monitorTicketAvailability(eventId, checkInterval = 5000) {
  console.log(`Monitoreando disponibilidad del evento ${eventId}`);

  const interval = setInterval(async () => {
    try {
      const event = await ticketingApi.getEventById(eventId);
      console.log(
        `[${new Date().toLocaleTimeString()}] ` +
        `Disponibles: ${event.availableTickets}, ` +
        `Reservados: ${event.reservedTickets}, ` +
        `Pagados: ${event.paidTickets}`
      );

      if (event.availableTickets === 0) {
        console.warn('⚠️  Evento agotado!');
        clearInterval(interval);
      }
    } catch (error) {
      console.error('Error monitoreando:', error);
    }
  }, checkInterval);

  return () => clearInterval(interval);
}
```

---

## 📊 Transformación de Datos

### Formato de Respuesta CRUD → UI

```javascript
// API Response
{
  id: 1,
  name: "Concert 2026",
  startsAt: "2026-03-15T20:00:00Z",
  availableTickets: 5,
  reservedTickets: 2,
  paidTickets: 1
}

// Transformación para UI
function formatEventForDisplay(event) {
  return {
    id: event.id,
    title: event.name,
    date: new Date(event.startsAt).toLocaleDateString('es-ES'),
    time: new Date(event.startsAt).toLocaleTimeString('es-ES'),
    stats: {
      available: event.availableTickets,
      reserved: event.reservedTickets,
      paid: event.paidTickets,
      total: event.availableTickets + event.reservedTickets + event.paidTickets
    }
  };
}
```

---

## ⚠️ Manejo de Errores

### Error Handling Strategy

```javascript
function getErrorMessage(error, defaultMessage = 'Unknown error') {
  if (error instanceof Error) {
    return error.message;
  }
  if (typeof error === 'string') {
    return error;
  }
  return defaultMessage;
}

async function safeApiCall(apiFunction, errorContext) {
  try {
    return await apiFunction();
  } catch (error) {
    const message = getErrorMessage(error);
    console.error(`${errorContext}: ${message}`);
    
    // Mapear errores comunes
    if (message.includes('not found')) {
      throw new Error('El recurso solicitado no existe');
    }
    if (message.includes('Version conflict')) {
      throw new Error('Otro usuario modificó este recurso. Recarga e intenta nuevamente');
    }
    if (message.includes('mayor a 0')) {
      throw new Error('Entrada inválida. Verifica los valores');
    }
    
    throw error;
  }
}
```

### User-Friendly Errors

```javascript
const ERROR_MESSAGES = {
  'not found': 'El recurso no existe',
  'Version conflict': 'Otro usuario lo modificó, intenta nuevamente',
  'mayor a 0': 'Valor inválido',
  'requerido': 'Campo obligatorio',
  'debe estar entre': 'Valor fuera de rango válido'
};

function getUserFriendlyError(apiError) {
  for (const [key, message] of Object.entries(ERROR_MESSAGES)) {
    if (apiError.toLowerCase().includes(key.toLowerCase())) {
      return message;
    }
  }
  return 'Error inesperado. Intenta nuevamente más tarde';
}
```

---

## 🔐 Seguridad

### Validación de Input

```javascript
function validateEventInput(name, startsAt) {
  if (!name || name.trim().length === 0) {
    throw new Error('El nombre del evento es requerido');
  }
  if (name.length > 200) {
    throw new Error('El nombre no puede exceder 200 caracteres');
  }
  
  const date = new Date(startsAt);
  if (isNaN(date.getTime())) {
    throw new Error('Fecha inválida');
  }
  if (date <= new Date()) {
    throw new Error('El evento debe ser en el futuro');
  }
}

function validateTicketInput(eventId, quantity) {
  if (!Number.isInteger(eventId) || eventId <= 0) {
    throw new Error('ID de evento inválido');
  }
  if (!Number.isInteger(quantity) || quantity < 1 || quantity > 1000) {
    throw new Error('Cantidad debe estar entre 1 y 1000');
  }
}
```

### CORS Handling (si es necesario)

```javascript
// Si el frontend está en diferente puerto/dominio:
const apiConfig = {
  headers: {
    'Content-Type': 'application/json'
  },
  mode: 'cors',
  credentials: 'omit' // omit | include | same-origin
};

const response = await fetch(url, { ...apiConfig, method: 'GET' });
```

---

## 📱 Responsive UI Examples

### Componente Event List (React)

```javascript
export function EventsList() {
  const [events, setEvents] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => {
    setLoading(true);
    ticketingApi
      .getEvents()
      .then(setEvents)
      .catch(err => setError(getErrorMessage(err)))
      .finally(() => setLoading(false));
  }, []);

  if (error) return <Alert>{error}</Alert>;

  return (
    <div className="events-grid">
      {loading ? (
        <Spinner />
      ) : events.length === 0 ? (
        <EmptyState />
      ) : (
        events.map(event => (
          <EventCard key={event.id} event={event} />
        ))
      )}
    </div>
  );
}
```

---

## 🚢 Deployment Configuration

### Environment Variables (Frontend)

```env
# .env.development
VITE_API_CRUD=http://localhost:8002
VITE_API_PRODUCER=http://localhost:8001

# .env.production
VITE_API_CRUD=https://api.ticketing.prod/crud
VITE_API_PRODUCER=https://api.ticketing.prod/producer
```

### Usage in Code

```javascript
const API_URLS = {
  CRUD: import.meta.env.VITE_API_CRUD,
  PRODUCER: import.meta.env.VITE_API_PRODUCER
};
```

---

## 📞 Support & Debugging

### Logging Helper

```javascript
const logger = {
  log: (msg, data) => console.log(`[API] ${msg}`, data),
  error: (msg, error) => console.error(`[API ERROR] ${msg}`, error),
  warn: (msg, data) => console.warn(`[API WARN] ${msg}`, data)
};

// Usage
logger.log('Fetching events');
try {
  const events = await ticketingApi.getEvents();
  logger.log('Events fetched', events);
} catch (error) {
  logger.error('Failed to fetch events', error);
}
```

### Health Check Utility

```javascript
async function checkHealthStatus() {
  try {
    const crudHealth = await fetch(`${API_URLS.CRUD}/health`).then(r => r.json());
    const producerHealth = await fetch(`${API_URLS.PRODUCER}/health`).then(r => r.json());
    
    return {
      crud: crudHealth.status === 'healthy',
      producer: producerHealth.status === 'healthy',
      timestamp: new Date()
    };
  } catch (error) {
    return {
      crud: false,
      producer: false,
      error: error.message
    };
  }
}
```

---

## 📋 Checklist para Frontend

- [ ] Service layer implementado (ticketingApi)
- [ ] Manejo de errores en todos los endpoints
- [ ] Validación de input antes de API calls
- [ ] Polling para cambios asincronos
- [ ] Cache para datos que cambian frecuentemente
- [ ] Retry logic para fallos de red
- [ ] Loading states en UI
- [ ] Error messages user-friendly
- [ ] Tests para service layer
- [ ] Environment variables configuradas
- [ ] Health checks implementados
- [ ] Logging para debugging

---

**Última actualización:** Febrero 10, 2026

Contacta al equipo backend para preguntas sobre API behavior.
