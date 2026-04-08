import type {
  Event,
  Ticket,
  CreateEventPayload,
  CreateTicketsPayload,
  CreateTicketsResponse,
  ReserveTicketPayload,
  UpdateTicketPayload,
  WaitlistEntryDto,
  WaitlistStatusResponse,
  ClaimOpportunityResponse,
} from "./types"

const CRUD_URL = process.env.NEXT_PUBLIC_API_CRUD || "http://localhost:8002"
const PRODUCER_URL = process.env.NEXT_PUBLIC_API_PRODUCER || "http://localhost:8001"

function normalizeTicket(ticket: Ticket): Ticket {
  return {
    ...ticket,
    status: ticket.status.toLowerCase() as Ticket["status"],
  }
}

function normalizeTickets(tickets: Ticket[]): Ticket[] {
  return tickets.map(normalizeTicket)
}

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
    const tickets = await handleResponse<Ticket[]>(res)
    return normalizeTickets(tickets)
  },

  async getTicket(id: number): Promise<Ticket> {
    const res = await fetch(`${CRUD_URL}/api/tickets/${id}`)
    const ticket = await handleResponse<Ticket>(res)
    return normalizeTicket(ticket)
  },

  async createTickets(payload: CreateTicketsPayload): Promise<CreateTicketsResponse> {
    const res = await fetch(`${CRUD_URL}/api/tickets/bulk`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    })
    const tickets = normalizeTickets(await handleResponse<Ticket[]>(res))
    return {
      createdCount: tickets.length,
      tickets,
    }
  },

  async updateTicketStatus(id: number, payload: UpdateTicketPayload): Promise<Ticket> {
    const res = await fetch(`${CRUD_URL}/api/tickets/${id}/status`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    })
    const ticket = await handleResponse<Ticket>(res)
    return normalizeTicket(ticket)
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
  }): Promise<{ message: string; ticketId: number; eventId: number }> {
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

  // ─── Waitlist ──────────────────────────────────────────
  async enrollInWaitlist(eventId: number, buyerEmail: string): Promise<WaitlistEntryDto> {
    let res: Response
    try {
      res = await fetch(`${CRUD_URL}/api/waitlist/entries`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ eventId, buyerEmail }),
      })
    } catch {
      throw new ApiError(0, "Error de red al conectar con el servidor")
    }

    if (res.status === 201) {
      return res.json()
    }

    if (res.status === 409) {
      throw new ApiError(409, "Ya tienes una inscripción activa")
    }

    if (res.status === 422) {
      throw new ApiError(422, "La lista de espera ya cerró")
    }

    if (res.status === 404) {
      throw new ApiError(404, "Evento no encontrado")
    }

    throw new ApiError(res.status, `Error ${res.status}`)
  },

  async getWaitlistStatus(eventId: number, email: string): Promise<WaitlistStatusResponse> {
    const res = await fetch(
      `${CRUD_URL}/api/waitlist/entries?eventId=${eventId}&email=${encodeURIComponent(email)}`
    )
    return handleResponse<WaitlistStatusResponse>(res)
  },

  async claimOpportunity(opportunityId: number, buyerEmail: string): Promise<ClaimOpportunityResponse> {
    let res: Response
    try {
      res = await fetch(`${CRUD_URL}/api/waitlist/opportunities/${opportunityId}/claim`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ buyerEmail }),
      })
    } catch {
      throw new ApiError(0, "Error de red al conectar con el servidor")
    }

    return handleResponse<ClaimOpportunityResponse>(res)
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
