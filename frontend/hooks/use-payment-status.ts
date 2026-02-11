/**
 * Hook para monitorear el estado de pago
 * Usa polling para esperar a que el estado del ticket cambie a "paid"
 */

import { useEffect, useState } from "react"

interface UsePaymentStatusOptions {
  ticketId?: number
  onPaymentConfirmed?: () => void
  onPaymentRejected?: (reason: string) => void
  maxDuration?: number // en segundos, default 10
}

export function usePaymentStatus({
  ticketId,
  onPaymentConfirmed,
  onPaymentRejected,
  maxDuration = 10,
}: UsePaymentStatusOptions) {
  const [isPolling, setIsPolling] = useState(false)
  const [elapsedTime, setElapsedTime] = useState(0)

  useEffect(() => {
    if (!ticketId || !isPolling) {
      return
    }

    const startTime = Date.now()
    const maxTime = maxDuration * 1000
    let pollInterval: NodeJS.Timeout | null = null

    const pollPaymentStatus = async () => {
      try {
        const response = await fetch(`/api/tickets/${ticketId}`)
        if (!response.ok) {
          throw new Error("Failed to fetch ticket")
        }

        const ticket = await response.json()
        const elapsed = Date.now() - startTime

        setElapsedTime(Math.round(elapsed / 1000))

        // Si el pago fue aprobado
        if (ticket.status === "paid") {
          if (pollInterval) clearInterval(pollInterval)
          setIsPolling(false)
          onPaymentConfirmed?.()
          return
        }

        // Si pasó el tiempo máximo, asumimos que fue rechazado
        if (elapsed > maxTime) {
          if (pollInterval) clearInterval(pollInterval)
          setIsPolling(false)
          onPaymentRejected?.("El pago tardó demasiado en confirmarse")
          return
        }
      } catch (error) {
        console.error("Error polling payment status:", error)
        if (pollInterval) clearInterval(pollInterval)
        setIsPolling(false)
        onPaymentRejected?.("Error verificando estado del pago")
      }
    }

    pollInterval = setInterval(pollPaymentStatus, 500) // Poll cada 500ms
    
    // Hacer una llamada inmediata
    pollPaymentStatus()

    return () => {
      if (pollInterval) clearInterval(pollInterval)
    }
  }, [ticketId, isPolling, onPaymentConfirmed, onPaymentRejected, maxDuration])

  return {
    isPolling,
    startPolling: () => setIsPolling(true),
    stopPolling: () => setIsPolling(false),
    elapsedTime,
  }
}
