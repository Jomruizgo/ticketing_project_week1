/**
 * Hook para escuchar cambios de estado de un ticket via Server-Sent Events.
 * Reemplaza el polling de reserva (reserve-ticket-dialog) y el polling de pago (use-payment-status).
 *
 * Flujo:
 *  1. El componente llama a waitForStatus(ticketId, expectedStatuses)
 *  2. El hook abre un EventSource hacia GET /api/tickets/{id}/stream
 *  3. Cuando llega el evento, resuelve con el status recibido
 *  4. Cierra la conexion automaticamente
 */

"use client"

import { useCallback, useRef } from "react"

const CRUD_URL = process.env.NEXT_PUBLIC_API_CRUD || "http://localhost:8002"
const SSE_TIMEOUT_MS = 30_000

export type TicketStatusEvent = {
  ticketId: number
  status: string
}

export function useTicketStatusSse() {
  const sourceRef = useRef<EventSource | null>(null)

  const waitForStatus = useCallback(
    (
      ticketId: number,
      onStatusReceived: (status: string) => void,
      onTimeout: () => void
    ) => {
      // Cerrar conexion previa si existe
      sourceRef.current?.close()

      const url = `${CRUD_URL}/api/tickets/${ticketId}/stream`
      const source = new EventSource(url)
      sourceRef.current = source

      const timer = setTimeout(() => {
        source.close()
        sourceRef.current = null
        onTimeout()
      }, SSE_TIMEOUT_MS)

      source.onmessage = (event) => {
        clearTimeout(timer)
        source.close()
        sourceRef.current = null

        try {
          const data: TicketStatusEvent = JSON.parse(event.data)
          onStatusReceived(data.status)
        } catch {
          onTimeout()
        }
      }

      source.onerror = () => {
        clearTimeout(timer)
        source.close()
        sourceRef.current = null
        onTimeout()
      }
    },
    []
  )

  const cancel = useCallback(() => {
    sourceRef.current?.close()
    sourceRef.current = null
  }, [])

  return { waitForStatus, cancel }
}
