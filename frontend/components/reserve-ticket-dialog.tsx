"use client"

import React from "react"

import { useState, useEffect, useCallback } from "react"
import { useSWRConfig } from "swr"
import { toast } from "sonner"
import { Loader2, CheckCircle2, XCircle } from "lucide-react"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { api } from "@/lib/api"
import type { Ticket } from "@/lib/types"

type ReserveStep = "form" | "polling" | "queued" | "success" | "error"

export function ReserveTicketDialog({
  ticket,
  eventId,
  open,
  onOpenChange,
}: {
  ticket: Ticket
  eventId: number
  open: boolean
  onOpenChange: (open: boolean) => void
}) {
  const [step, setStep] = useState<ReserveStep>("form")
  const [email, setEmail] = useState("")
  const [expiresIn, setExpiresIn] = useState("300")
  const [loading, setLoading] = useState(false)
  const [errorMsg, setErrorMsg] = useState("")
  const { mutate } = useSWRConfig()

  // Reset when dialog opens
  useEffect(() => {
    if (open) {
      setStep("form")
      setEmail("")
      setExpiresIn("300")
      setErrorMsg("")
    }
  }, [open])

  const pollForReservation = useCallback(
    async (ticketId: number) => {
      const maxAttempts = 20
      for (let i = 0; i < maxAttempts; i++) {
        await new Promise((r) => setTimeout(r, 500))
        try {
          const t = await api.getTicket(ticketId)
          const status = (t.status as string).toLowerCase()
          if (status === "reserved") {
            setStep("success")
            mutate(`tickets-${eventId}`)
            mutate(`event-${eventId}`)
            mutate("events")
            return
          }
          if (status !== "available") {
            setStep("error")
            setErrorMsg(`El ticket cambio a estado: ${status}`)
            return
          }
        } catch {
          // continue polling
        }
      }
      // Después de 10 segundos, mostrar estado "encolado" en lugar de error
      // La reserva ya fue aceptada por RabbitMQ, solo esperar más tiempo
      setStep("queued")
    },
    [eventId, mutate]
  )

  async function handleReserve(e: React.FormEvent) {
    e.preventDefault()
    if (!email.trim()) {
      toast.error("El email es requerido")
      return
    }

    const seconds = Number(expiresIn)
    if (!seconds || seconds <= 0) {
      toast.error("El tiempo de expiracion debe ser mayor a 0")
      return
    }

    setLoading(true)
    try {
      const orderId = `ORD-${Date.now()}`
      await api.reserveTicket({
        eventId,
        ticketId: ticket.id,
        orderId,
        reservedBy: email.trim(),
        expiresInSeconds: seconds,
      })

      setStep("polling")
      pollForReservation(ticket.id)
    } catch (err) {
      toast.error(
        err instanceof Error ? err.message : "Error al enviar reserva"
      )
    } finally {
      setLoading(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="bg-card border-border">
        <DialogHeader>
          <DialogTitle>
            Reservar Ticket #{ticket.id}
          </DialogTitle>
          <DialogDescription className="text-muted-foreground">
            La reserva se procesa de forma asincrona. Al enviar, recibiras una
            confirmacion cuando se complete.
          </DialogDescription>
        </DialogHeader>

        {step === "form" && (
          <form onSubmit={handleReserve} className="flex flex-col gap-4 pt-2">
            <div className="flex flex-col gap-2">
              <Label htmlFor="reserve-email">Email del comprador</Label>
              <Input
                id="reserve-email"
                type="email"
                placeholder="usuario@ejemplo.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                maxLength={120}
                className="bg-secondary border-border"
              />
            </div>
            <div className="flex flex-col gap-2">
              <Label htmlFor="reserve-expires">
                Tiempo de expiracion (segundos)
              </Label>
              <Input
                id="reserve-expires"
                type="number"
                min={1}
                value={expiresIn}
                onChange={(e) => setExpiresIn(e.target.value)}
                className="bg-secondary border-border"
              />
              <p className="text-xs text-muted-foreground">
                La reserva expirara automaticamente despues de este tiempo
              </p>
            </div>
            <Button type="submit" disabled={loading} className="mt-2">
              {loading ? "Enviando..." : "Reservar Ticket"}
            </Button>
          </form>
        )}

        {step === "polling" && (
          <div className="flex flex-col items-center gap-4 py-8">
            <Loader2 className="h-10 w-10 animate-spin text-primary" />
            <div className="text-center">
              <p className="font-medium text-foreground">
                Procesando reserva...
              </p>
              <p className="text-sm text-muted-foreground">
                Esperando confirmacion del servidor
              </p>
            </div>
          </div>
        )}

        {step === "queued" && (
          <div className="flex flex-col items-center gap-4 py-8">
            <div className="flex h-14 w-14 items-center justify-center rounded-full bg-blue-500/10">
              <CheckCircle2 className="h-8 w-8 text-blue-400" />
            </div>
            <div className="text-center">
              <p className="font-medium text-foreground">
                Reserva encolada ✓
              </p>
              <p className="text-sm text-muted-foreground">
                Tu solicitud fue recibida y está siendo procesada.
                <br />
                El ticket se actualizará en breve.
              </p>
            </div>
            <Button
              onClick={() => onOpenChange(false)}
              className="mt-2"
            >
              Cerrar
            </Button>
          </div>
        )}

        {step === "success" && (
          <div className="flex flex-col items-center gap-4 py-8">
            <div className="flex h-14 w-14 items-center justify-center rounded-full bg-emerald-500/10">
              <CheckCircle2 className="h-8 w-8 text-emerald-400" />
            </div>
            <div className="text-center">
              <p className="font-medium text-foreground">
                Reserva confirmada
              </p>
              <p className="text-sm text-muted-foreground">
                El ticket #{ticket.id} fue reservado exitosamente para {email}
              </p>
            </div>
            <Button
              onClick={() => onOpenChange(false)}
              className="mt-2"
            >
              Cerrar
            </Button>
          </div>
        )}

        {step === "error" && (
          <div className="flex flex-col items-center gap-4 py-8">
            <div className="flex h-14 w-14 items-center justify-center rounded-full bg-destructive/10">
              <XCircle className="h-8 w-8 text-destructive" />
            </div>
            <div className="text-center">
              <p className="font-medium text-foreground">
                Error en la reserva
              </p>
              <p className="text-sm text-muted-foreground">{errorMsg}</p>
            </div>
            <div className="flex gap-2">
              <Button
                variant="outline"
                onClick={() => setStep("form")}
                className="border-border"
              >
                Reintentar
              </Button>
              <Button onClick={() => onOpenChange(false)}>Cerrar</Button>
            </div>
          </div>
        )}
      </DialogContent>
    </Dialog>
  )
}
