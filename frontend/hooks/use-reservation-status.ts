/**
 * Hook para monitorear el estado de una reserva asincrónica
 * Demuestra pattern de polling inteligente en sistemas distribuidos
 */

"use client"

import { useState, useCallback, useRef } from "react"
import { api } from "@/lib/api"
import { waitForTicketReservation } from "@/lib/polling"

export interface UseReservationStatusOptions {
  onSuccess?: () => void
  onError?: (error: Error) => void
}

export function useReservationStatus(options: UseReservationStatusOptions = {}) {
  const [isPolling, setIsPolling] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const abortControllerRef = useRef<AbortController | null>(null)

  const startPolling = useCallback(
    async (ticketId: number, timeoutMs: number = 10000) => {
      setIsPolling(true)
      setError(null)

      try {
        // Wait for ticket to be reserved with exponential backoff polling
        await waitForTicketReservation(
          (id) => api.getTicket(id),
          ticketId,
          timeoutMs
        )

        setIsPolling(false)
        options.onSuccess?.()
        return true
      } catch (err) {
        const errorMessage = err instanceof Error ? err.message : "Unknown error"
        setError(errorMessage)
        setIsPolling(false)
        options.onError?.(err instanceof Error ? err : new Error(errorMessage))
        return false
      }
    },
    [options]
  )

  const cancel = useCallback(() => {
    abortControllerRef.current?.abort()
    setIsPolling(false)
  }, [])

  return {
    isPolling,
    error,
    startPolling,
    cancel,
  }
}
