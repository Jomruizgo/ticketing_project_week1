export type TicketStatus = "available" | "reserved" | "paid" | "released" | "cancelled"

export interface Event {
  id: number
  name: string
  startsAt: string
  availableTickets: number
  reservedTickets: number
  paidTickets: number
}

export interface Ticket {
  id: number
  eventId: number
  status: TicketStatus
  reservedAt: string | null
  expiresAt: string | null
  paidAt: string | null
  orderId: string | null
  reservedBy: string | null
  version: number
}

export interface CreateEventPayload {
  name: string
  startsAt: string
}

export interface CreateTicketsPayload {
  eventId: number
  quantity: number
}

export interface CreateTicketsResponse {
  createdCount: number
  tickets: Ticket[]
}

export interface ReserveTicketPayload {
  eventId: number
  ticketId: number
  orderId: string
  reservedBy: string
  expiresInSeconds: number
}

export interface UpdateTicketPayload {
  newStatus: TicketStatus
  reason?: string
}
