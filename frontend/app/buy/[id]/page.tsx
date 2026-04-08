"use client"

import { useEffect, useState } from "react"
import { useParams } from "next/navigation"
import { Calendar, Tickets, Loader2, CheckCircle2, XCircle } from "lucide-react"
import { useEvent, useTickets } from "@/hooks/use-ticketing"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Skeleton } from "@/components/ui/skeleton"
import { PaymentForm } from "@/components/payment-form"
import { waitForTicketStatusSse } from "@/hooks/use-ticket-status-sse"
import { api } from "@/lib/api"
import { toast } from "sonner"
import { WaitlistEnrollForm } from "@/components/waitlist-enroll-form"
import { WaitlistStatus } from "@/components/waitlist-status"

type PurchaseStep = "form" | "processing" | "reserved" | "success" | "error"
type PaymentProgress = "idle" | "processing" | "success" | "error"

const DEFAULT_TICKET_PRICE_CENTS = 9999

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString("es-ES", {
    weekday: "long",
    day: "numeric",
    month: "long",
    year: "numeric",
  })
}

function formatTime(iso: string) {
  return new Date(iso).toLocaleTimeString("es-ES", {
    hour: "2-digit",
    minute: "2-digit",
  })
}

export default function BuyerEventPage() {
  const params = useParams()
  const eventId = Number(params.id)
  const { data: event, isLoading: eventLoading } = useEvent(eventId)
  const { data: tickets, isLoading: ticketsLoading } = useTickets(eventId)
  const [step, setStep] = useState<PurchaseStep>("form")
  const [quantity, setQuantity] = useState("1")
  const [email, setEmail] = useState("")
  const [expiresIn, setExpiresIn] = useState("300")
  const [loading, setLoading] = useState(false)
  const [errorMsg, setErrorMsg] = useState("")
  const [reservedCount, setReservedCount] = useState(0)
  const [reservedTicketIds, setReservedTicketIds] = useState<number[]>([])
  const [paymentStates, setPaymentStates] = useState<Record<number, PaymentProgress>>({})
  const [paymentErrors, setPaymentErrors] = useState<Record<number, string>>({})

  const availableTickets = tickets?.filter(
    (t) => t.status?.toLowerCase() === "available"
  ) || []

  useEffect(() => {
    if (
      step === "reserved" &&
      reservedTicketIds.length > 0 &&
      reservedTicketIds.every((ticketId) => paymentStates[ticketId] === "success")
    ) {
      setStep("success")
      toast.success("¡Pago confirmado! Tu compra está completa")
    }
  }, [paymentStates, reservedTicketIds, step])

  async function handlePurchase(e: React.FormEvent) {
    e.preventDefault()
    
    const qty = Number(quantity)
    if (!Number.isInteger(qty) || qty < 1) {
      toast.error("La cantidad debe ser mayor a 0")
      return
    }
    
    if (qty > availableTickets.length) {
      toast.error(`Solo hay ${availableTickets.length} tickets disponibles`)
      return
    }
    
    if (!email.trim()) {
      toast.error("El email es requerido")
      return
    }

    const seconds = Number(expiresIn)
    if (!seconds || seconds <= 0) {
      toast.error("El tiempo de expiración debe ser mayor a 0")
      return
    }

    setLoading(true)
    setStep("processing")

    try {
      const selectedTickets = availableTickets.slice(0, qty)
      const orderId = `ORD-${Date.now()}`
      const reservedIds = (
        await Promise.all(
          selectedTickets.map(async (ticket) => {
            try {
              const result = await api.reserveTicket({
                eventId,
                ticketId: ticket.id,
                orderId,
                reservedBy: email.trim(),
                expiresInSeconds: seconds,
              })

              const status = (await waitForTicketStatusSse(result.ticketId)).toLowerCase()
              if (status !== "reserved") {
                throw new Error(`Estado inesperado para ticket ${result.ticketId}: ${status}`)
              }
              return result.ticketId
            } catch (err) {
              console.error("Failed to reserve ticket:", err)
              return null
            }
          })
        )
      ).filter((ticketId): ticketId is number => ticketId !== null)

      if (reservedIds.length === 0) {
        throw new Error("No fue posible reservar los tickets")
      }

      setReservedCount(reservedIds.length)
      setReservedTicketIds(reservedIds)
      setPaymentStates(
        Object.fromEntries(reservedIds.map((ticketId) => [ticketId, "idle" as PaymentProgress]))
      )
      setPaymentErrors({})
      setStep("reserved")
    } catch (err) {
      setStep("error")
      setErrorMsg(
        err instanceof Error ? err.message : "Error al procesar la compra"
      )
    } finally {
      setLoading(false)
    }
  }

  function handlePaymentStart(ticketId: number) {
    setPaymentStates((current) => ({
      ...current,
      [ticketId]: "processing",
    }))
    setPaymentErrors((current) => {
      const next = { ...current }
      delete next[ticketId]
      return next
    })
  }

  function handlePaymentSuccess(ticketId: number) {
    setPaymentStates((current) => ({
      ...current,
      [ticketId]: "success",
    }))
    toast.success(`Pago confirmado para ticket #${ticketId}`)
  }

  function handlePaymentError(ticketId: number, error: string) {
    setPaymentStates((current) => ({
      ...current,
      [ticketId]: "error",
    }))
    setPaymentErrors((current) => ({
      ...current,
      [ticketId]: error,
    }))
    toast.error(`Error en el pago del ticket #${ticketId}: ${error}`)
  }

  function handleReset() {
    setStep("form")
    setQuantity("1")
    setEmail("")
    setExpiresIn("300")
    setReservedCount(0)
    setReservedTicketIds([])
    setPaymentStates({})
    setPaymentErrors({})
    setErrorMsg("")
  }

  if (eventLoading) {
    return (
      <div className="flex min-h-screen flex-col gap-4 bg-background px-6 py-8">
        <Skeleton className="h-10 w-32 rounded-lg bg-secondary" />
        <Skeleton className="h-32 rounded-lg bg-secondary" />
      </div>
    )
  }

  if (!event) {
    return (
      <div className="flex min-h-screen flex-col items-center justify-center gap-4 bg-background">
        <p className="text-lg font-medium text-foreground">
          Evento no encontrado
        </p>
      </div>
    )
  }

  return (
    <div className="flex min-h-screen flex-col bg-background">
      {/* Header */}
      <header className="border-b border-border">
        <div className="mx-auto w-full max-w-4xl px-6 py-8">
          <h1 className="text-3xl font-bold tracking-tight text-foreground">
            {event.name}
          </h1>
        </div>
      </header>

      {/* Main Content */}
      <main className="mx-auto w-full max-w-4xl flex-1 px-6 py-8">
        <div className="flex flex-col gap-8">
          {/* Event Info */}
          <div className="rounded-xl border border-border bg-card p-6">
            <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
              <div className="flex flex-col gap-4">
                <div className="flex items-center gap-2 text-muted-foreground">
                  <Calendar className="h-5 w-5" />
                  <span>{formatDate(event.startsAt)}</span>
                </div>
                <div className="text-lg font-semibold text-foreground">
                  {formatTime(event.startsAt)}
                </div>
              </div>
              <div className="flex flex-col gap-4">
                <div className="text-sm text-muted-foreground">
                  Disponibilidad
                </div>
                <div className="flex items-center gap-2">
                  <Tickets className="h-5 w-5 text-blue-400" />
                  <span className="text-2xl font-bold text-foreground">
                    {event.availableTickets}
                  </span>
                  <span className="text-muted-foreground">
                    de {event.availableTickets + event.reservedTickets + event.paidTickets}
                  </span>
                </div>
              </div>
            </div>
          </div>

          {/* Purchase Form or Status */}
          <div className="rounded-xl border border-border bg-card p-6">
            {availableTickets.length === 0 && step === "form" ? (
              new Date(event.startsAt) > new Date() ? (
                <div className="flex flex-col gap-6">
                  <WaitlistEnrollForm eventId={eventId} />
                  <WaitlistStatus
                    eventId={eventId}
                    onClaimSuccess={(ticketId) => {
                      setReservedCount(1)
                      setReservedTicketIds([ticketId])
                      setPaymentStates({ [ticketId]: "idle" })
                      setPaymentErrors({})
                      setStep("reserved")
                    }}
                  />
                </div>
              ) : (
                <div className="flex flex-col gap-2 py-4">
                  <h2 className="text-lg font-semibold text-foreground">
                    Lista de espera cerrada
                  </h2>
                  <p className="text-sm text-muted-foreground">
                    Este evento ya ha pasado y no es posible inscribirse en la
                    lista de espera.
                  </p>
                </div>
              )
            ) : step === "form" && (
              <form onSubmit={handlePurchase} className="flex flex-col gap-4">
                <h2 className="text-lg font-semibold text-foreground">
                  Compra de Tickets
                </h2>

                <div className="flex flex-col gap-2">
                  <Label htmlFor="quantity">Cantidad de tickets</Label>
                  <Input
                    id="quantity"
                    type="number"
                    min={1}
                    max={availableTickets.length}
                    value={quantity}
                    onChange={(e) => setQuantity(e.target.value)}
                    disabled={ticketsLoading || availableTickets.length === 0}
                    className="bg-secondary border-border"
                  />
                  <p className="text-xs text-muted-foreground">
                    Máximo disponible: {availableTickets.length} tickets
                  </p>
                </div>

                <div className="flex flex-col gap-2">
                  <Label htmlFor="email">Tu email</Label>
                  <Input
                    id="email"
                    type="email"
                    placeholder="tu@email.com"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    className="bg-secondary border-border"
                  />
                </div>

                <div className="flex flex-col gap-2">
                  <Label htmlFor="expires">Tiempo de expiración (segundos)</Label>
                  <Input
                    id="expires"
                    type="number"
                    min={1}
                    value={expiresIn}
                    onChange={(e) => setExpiresIn(e.target.value)}
                    className="bg-secondary border-border"
                  />
                  <p className="text-xs text-muted-foreground">
                    La reserva expirará después de este tiempo
                  </p>
                </div>

                <Button
                  type="submit"
                  disabled={loading || ticketsLoading || availableTickets.length === 0}
                  className="mt-4"
                >
                  {loading ? "Procesando..." : `Comprar ${quantity} Ticket${quantity !== "1" ? "s" : ""}`}
                </Button>
              </form>
            )}

            {step === "processing" && (
              <div className="flex flex-col items-center gap-4 py-8">
                <Loader2 className="h-10 w-10 animate-spin text-primary" />
                <div className="text-center">
                  <p className="font-medium text-foreground">
                    Procesando tu compra...
                  </p>
                  <p className="text-sm text-muted-foreground">
                    Enviando reservas al sistema
                  </p>
                </div>
              </div>
            )}

            {step === "reserved" && event && (
              <div className="flex flex-col gap-6">
                <div>
                  <h2 className="text-lg font-semibold text-foreground">
                    Completar Pago
                  </h2>
                  <p className="mt-2 text-sm text-muted-foreground">
                    {reservedCount} ticket{reservedCount !== 1 ? "s" : ""} reservado{reservedCount !== 1 ? "s" : ""} • Total: ${(DEFAULT_TICKET_PRICE_CENTS * reservedCount / 100).toFixed(2)}
                  </p>
                </div>

                {reservedTicketIds.map((ticketId) => (
                  <PaymentForm
                    key={ticketId}
                    ticket={{
                      id: ticketId,
                      amountCents: DEFAULT_TICKET_PRICE_CENTS,
                      currency: "USD",
                    }}
                    eventId={eventId}
                    email={email}
                    status={paymentStates[ticketId] ?? "idle"}
                    error={paymentErrors[ticketId]}
                    onPaymentStart={handlePaymentStart}
                    onPaymentSuccess={handlePaymentSuccess}
                    onPaymentError={handlePaymentError}
                  />
                ))}
              </div>
            )}

            {step === "success" && (
              <div className="flex flex-col items-center gap-4 py-8">
                <div className="flex h-14 w-14 items-center justify-center rounded-full bg-emerald-500/10">
                  <CheckCircle2 className="h-8 w-8 text-emerald-400" />
                </div>
                <div className="text-center">
                  <p className="font-medium text-foreground">
                    ¡Compra completada!
                  </p>
                  <p className="text-sm text-muted-foreground">
                    {reservedCount} ticket{reservedCount !== 1 ? "s" : ""} pagado{reservedCount !== 1 ? "s" : ""} para {email}
                  </p>
                </div>
                <Button onClick={handleReset} className="mt-4">
                  Volver a Eventos
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
                    Error en la compra
                  </p>
                  <p className="text-sm text-muted-foreground">{errorMsg}</p>
                </div>
                <Button onClick={() => setStep("form")} className="mt-4">
                  Intentar de nuevo
                </Button>
              </div>
            )}
          </div>
        </div>
      </main>
    </div>
  )
}
