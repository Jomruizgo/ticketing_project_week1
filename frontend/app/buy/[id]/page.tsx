"use client"

import { useState } from "react"
import { useParams } from "next/navigation"
import Link from "next/link"
import { Calendar, Tickets, Loader2, CheckCircle2, XCircle, ArrowLeft } from "lucide-react"
import { useEvent, useTickets } from "@/hooks/use-ticketing"
import { usePaymentStatus } from "@/hooks/use-payment-status"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Skeleton } from "@/components/ui/skeleton"
import { PaymentForm } from "@/components/payment-form"
import { PaymentStatus } from "@/components/payment-status"
import { api } from "@/lib/api"
import { toast } from "sonner"

type PurchaseStep = "form" | "processing" | "reserved" | "payment" | "confirming" | "success" | "error"

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
  const [paymentStatus, setPaymentStatus] = useState<"idle" | "processing" | "success" | "error">("idle")
  const [paymentError, setPaymentError] = useState("")

  const { isPolling: isPaymentPolling, startPolling: startPaymentPolling } = usePaymentStatus({
    ticketId: reservedTicketIds[0],
    onPaymentConfirmed: () => {
      setPaymentStatus("success")
      setStep("success")
      toast.success("¡Pago confirmado! Tu compra está completa")
    },
    onPaymentRejected: (reason) => {
      setPaymentStatus("error")
      setPaymentError(reason)
      toast.error(`Pago rechazado: ${reason}`)
    },
  })

  const availableTickets = tickets?.filter(
    (t) => t.status?.toLowerCase() === "available"
  ) || []

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
      let successCount = 0
      const reservedIds: number[] = []

      // Enviar reservas en paralelo
      await Promise.all(
        selectedTickets.map((ticket) =>
          api
            .reserveTicket({
              eventId,
              ticketId: ticket.id,
              orderId,
              reservedBy: email.trim(),
              expiresInSeconds: seconds,
            })
            .then((result) => {
              successCount++
              reservedIds.push(result.ticketId)
            })
            .catch((err) => {
              console.error("Failed to reserve ticket:", err)
            })
        )
      )

      if (successCount === 0) {
        throw new Error("No fue posible reservar los tickets")
      }

      setReservedCount(successCount)
      setReservedTicketIds(reservedIds)
      
      // Esperar un poco para que se procesen
      await new Promise(r => setTimeout(r, 2000))
      
      // Mostrar formulario de pago
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

  async function handlePaymentStart() {
    setPaymentStatus("processing")
  }

  async function handlePaymentSuccess(ticketId: number, transactionRef: string) {
    toast.loading("Confirmando pago...")
    setPaymentStatus("processing")
    setStep("confirming")
    
    // Iniciar polling para esperar confirmación del pago
    startPaymentPolling()
  }

  async function handlePaymentError(error: string) {
    setPaymentStatus("error")
    setPaymentError(error)
    toast.error(`Error en el pago: ${error}`)
  }

  function handleReset() {
    setStep("form")
    setQuantity("1")
    setEmail("")
    setExpiresIn("300")
    setReservedCount(0)
    setReservedTicketIds([])
    setPaymentStatus("idle")
    setPaymentError("")
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
            {step === "form" && (
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
                    {reservedCount} ticket{reservedCount !== 1 ? "s" : ""} reservado{reservedCount !== 1 ? "s" : ""} • Total: ${(9999 * reservedCount / 100).toFixed(2)}
                  </p>
                </div>

                {reservedTicketIds.map((ticketId) => (
                  <PaymentForm
                    key={ticketId}
                    ticket={{
                      id: ticketId,
                      price: 9999, // $99.99 en centavos
                      currency: "USD",
                    }}
                    eventId={eventId}
                    email={email}
                    onPaymentStart={handlePaymentStart}
                    onPaymentSuccess={handlePaymentSuccess}
                    onPaymentError={handlePaymentError}
                  />
                ))}
              </div>
            )}

            {step === "confirming" && (
              <PaymentStatus
                status="processing"
                ticketId={reservedTicketIds[0]}
                onReset={handleReset}
              />
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
