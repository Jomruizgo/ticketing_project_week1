import type {
  Event,
  Ticket,
  CreateEventPayload,
  CreateTicketsPayload,
  CreateTicketsResponse,
  ReserveTicketPayload,
  UpdateTicketPayload,
} from "./types"
import { retryWithBackoff } from "./polling"

const CRUD_URL = process.env.NEXT_PUBLIC_API_CRUD || "http://localhost:8002"
const PRODUCER_URL = process.env.NEXT_PUBLIC_API_PRODUCER || "http://localhost:8001"

/**
 * Custom error class for API errors
 * Helps distinguish between different error types in distributed systems
 */
export class ApiError extends Error {
  constructor(
    public status: number,
    public message: string,
    public serviceType: "crud" | "producer" = "crud"
  ) {
    super(message)
    this.name = "ApiError"
  }
}

async function handleResponse<T>(res: Response): Promise<T> {
  // Success case
  if (res.ok) {
    return res.json()
  }

  // 202 Accepted is special - it's not an error
  if (res.status === 202) {
    return res.json()
  }

  // Error case
  const text = await res.text()

  // Parse error message
  let errorMessage = text
  try {
    const json = JSON.parse(text)
    errorMessage = json.message || json.error || text
  } catch {
    // If not JSON, use raw text
  }

  throw new ApiError(res.status, errorMessage || `Error ${res.status}`)
}

export const api = {
  // ─── Events ───────────────────────────────────────────
  async getEvents(): Promise<Event[]> {
    const res = await fetch(`${CRUD_URL}/api/events`)
    return handleResponse<Event[]>(res)
  },

  async getEvent(id: number): Promise<Event> {
    const res = await fetch(`${CRUD_URL}/api/events/${id}`)
    return handleResponse<Event>(res)
  },

  async createEvent(payload: CreateEventPayload): Promise<Event> {
    const res = await fetch(`${CRUD_URL}/api/events`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    })
    return handleResponse<Event>(res)
  },

  async updateEvent(id: number, payload: Partial<CreateEventPayload>): Promise<Event> {
    const res = await fetch(`${CRUD_URL}/api/events/${id}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    })
    return handleResponse<Event>(res)
  },

  async deleteEvent(id: number): Promise<void> {
    const res = await fetch(`${CRUD_URL}/api/events/${id}`, { method: "DELETE" })
    if (!res.ok) throw new Error(`Failed to delete event ${id}`)
  },

  // ─── Tickets ──────────────────────────────────────────
  async getTicketsByEvent(eventId: number): Promise<Ticket[]> {
    const res = await fetch(`${CRUD_URL}/api/tickets/event/${eventId}`)
    return handleResponse<Ticket[]>(res)
  },

  async getTicket(id: number): Promise<Ticket> {
    const res = await fetch(`${CRUD_URL}/api/tickets/${id}`)
    return handleResponse<Ticket>(res)
  },

  async createTickets(payload: CreateTicketsPayload): Promise<CreateTicketsResponse> {
    const res = await fetch(`${CRUD_URL}/api/tickets/bulk`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    })
    return handleResponse<CreateTicketsResponse>(res)
  },

  async updateTicketStatus(id: number, payload: UpdateTicketPayload): Promise<Ticket> {
    const res = await fetch(`${CRUD_URL}/api/tickets/${id}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    })
    return handleResponse<Ticket>(res)
  },

  // ─── Producer ─────────────────────────────────────────
  /**
   * Reserve a ticket (async operation)
   * Returns 202 Accepted - reservation is processed asynchronously
   * Frontend should poll the ticket status to confirm reservation
   */
  async reserveTicket(payload: ReserveTicketPayload): Promise<{ message: string; ticketId: number }> {
    const res = await fetch(`${PRODUCER_URL}/api/tickets/reserve`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    })

    if (res.status === 202) {
      // 202 Accepted - request queued for async processing
      return res.json()
    }

    // Any other status (including errors) goes through error handler
    const text = await res.text()
    throw new ApiError(res.status, text || `Error ${res.status}`, "producer")
  },

  /**
   * Process a payment (async operation)
   * Returns 202 Accepted - payment is processed asynchronously by RabbitMQ
   * Frontend should poll the ticket status to confirm payment approval
   */
  async processPayment(payload: {
    ticketId: number
    eventId: number
    amountCents: number
    currency: string
    paymentBy: string
    paymentMethodId: string
    transactionRef: string
  }): Promise<{ message: string; ticketId: number; eventId: number; status: string }> {
    const res = await fetch(`${PRODUCER_URL}/api/payments/process`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    })

    if (res.status === 202) {
      // 202 Accepted - payment queued for async processing
      return res.json()
    }

    // Any other status (including errors) goes through error handler
    const text = await res.text()
    throw new ApiError(res.status, text || `Error ${res.status}`, "producer")
  },

  // ─── Health ───────────────────────────────────────────
  async healthCrud(): Promise<boolean> {
    try {
      const res = await fetch(`${CRUD_URL}/health`)
      return res.ok
    } catch {
      return false
    }
  },

  async healthProducer(): Promise<boolean> {
    try {
      const res = await fetch(`${PRODUCER_URL}/health`)
      return res.ok
    } catch {
      return false
    }
  },
}
