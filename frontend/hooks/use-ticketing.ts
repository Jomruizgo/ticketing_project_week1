"use client"

import useSWR from "swr"
import { api } from "@/lib/api"

export function useEvents() {
  return useSWR("events", () => api.getEvents(), {
    refreshInterval: 10000,
  })
}

export function useEvent(id: number | null) {
  return useSWR(id != null ? `event-${id}` : null, () => api.getEvent(id!), {
    refreshInterval: 5000,
  })
}

export function useTickets(eventId: number | null) {
  return useSWR(
    eventId != null ? `tickets-${eventId}` : null,
    async () => {
      const tickets = await api.getTicketsByEvent(eventId!)
      // Normalizar status a minúsculas por si la API devuelve diferentes formatos
      return tickets.map(ticket => ({
        ...ticket,
        status: (ticket.status as string).toLowerCase() as any
      }))
    },
    { refreshInterval: 3000 }
  )
}
