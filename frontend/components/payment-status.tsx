/**
 * Payment Status Component
 * Muestra el estado del pago (procesando, éxito, error)
 */

"use client"

import { CheckCircle2, XCircle, Loader2 } from "lucide-react"
import { Card } from "@/components/ui/card"
import { Button } from "@/components/ui/button"

type PaymentStatus = "idle" | "processing" | "success" | "error"

interface PaymentStatusProps {
  status: PaymentStatus
  error?: string
  ticketId?: number
  onReset: () => void
}

export function PaymentStatus({ status, error, ticketId, onReset }: PaymentStatusProps) {
  if (status === "idle") {
    return null
  }

  return (
    <Card className="border-border bg-card p-6">
      {status === "processing" && (
        <div className="space-y-4 text-center">
          <div className="flex justify-center">
            <Loader2 className="h-12 w-12 animate-spin text-blue-500" />
          </div>
          <div>
            <h3 className="text-lg font-semibold text-foreground">Procesando Pago</h3>
            <p className="mt-2 text-sm text-muted-foreground">
              Por favor espera mientras procesamos tu pago...
            </p>
            <p className="mt-4 text-xs text-muted-foreground">
              ⏱️ Esto puede tomar hasta 10 segundos
            </p>
          </div>
        </div>
      )}

      {status === "success" && (
        <div className="space-y-4 text-center">
          <div className="flex justify-center">
            <div className="flex h-16 w-16 items-center justify-center rounded-full bg-emerald-500/10">
              <CheckCircle2 className="h-10 w-10 text-emerald-500" />
            </div>
          </div>
          <div>
            <h3 className="text-lg font-semibold text-foreground">¡Pago Aprobado!</h3>
            <p className="mt-2 text-sm text-muted-foreground">
              Tu ticket #{ticketId} ha sido confirmado
            </p>
            <p className="mt-1 text-xs text-muted-foreground">
              Se ha enviado un email con los detalles de tu compra
            </p>
          </div>
          <Button onClick={onReset} className="mt-4 w-full">
            Volver a Eventos
          </Button>
        </div>
      )}

      {status === "error" && (
        <div className="space-y-4 text-center">
          <div className="flex justify-center">
            <div className="flex h-16 w-16 items-center justify-center rounded-full bg-destructive/10">
              <XCircle className="h-10 w-10 text-destructive" />
            </div>
          </div>
          <div>
            <h3 className="text-lg font-semibold text-foreground">Pago Rechazado</h3>
            <p className="mt-2 text-sm text-muted-foreground">{error || "Ocurrió un error al procesar el pago"}</p>
            <p className="mt-1 text-xs text-muted-foreground">
              El ticket ha sido liberado y está disponible para otros usuarios
            </p>
          </div>
          <Button onClick={onReset} variant="outline" className="mt-4 w-full">
            Intentar de Nuevo
          </Button>
        </div>
      )}
    </Card>
  )
}
