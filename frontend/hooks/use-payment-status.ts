/**
 * Hook para monitorear el estado de pago via SSE.
 * Reemplaza el polling anterior por una conexion SSE a GET /api/tickets/{id}/stream.
 * El servidor notifica cuando el ticket cambia a "paid" o "released".
 */

"use client"

import { useCallback, useRef, useState } from "react"
import { useTicketStatusSse } from "./use-ticket-status-sse"

interface UsePaymentStatusOptions {
  ticketId?: number
  onPaymentConfirmed?: () => void
  onPaymentRejected?: (reason: string) => void
}

export function usePaymentStatus({
  ticketId,
  onPaymentConfirmed,
  onPaymentRejected,
}: UsePaymentStatusOptions) {
  const [isPolling, setIsPolling] = useState(false)
  const { waitForStatus, cancel } = useTicketStatusSse()
  const startedRef = useRef(false)

  const startPolling = useCallback(() => {
    if (!ticketId || startedRef.current) return

    startedRef.current = true
    setIsPolling(true)

    waitForStatus(
      ticketId,
      (status) => {
        setIsPolling(false)
        startedRef.current = false

        if (status === "paid") {
          onPaymentConfirmed?.()
        } else {
          onPaymentRejected?.(
            status === "released"
              ? "Pago rechazado. El ticket ha sido liberado."
              : `Estado inesperado: ${status}`
          )
        }
      },
      () => {
        setIsPolling(false)
        startedRef.current = false
        onPaymentRejected?.("El pago tardó demasiado en confirmarse")
      }
    )
  }, [ticketId, waitForStatus, onPaymentConfirmed, onPaymentRejected])

  const stopPolling = useCallback(() => {
    cancel()
    setIsPolling(false)
    startedRef.current = false
  }, [cancel])

  return {
    isPolling,
    startPolling,
    stopPolling,
    elapsedTime: 0, // Mantenemos la interfaz compatible
  }
}
