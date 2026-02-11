/**
 * Payment Form Component
 * Formulario para procesar pagos de tickets
 */

"use client"

import { useState } from "react"
import { Loader2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Card } from "@/components/ui/card"
import { toast } from "sonner"
import { api } from "@/lib/api"

interface PaymentFormProps {
  ticket: {
    id: number
    price: number
    currency?: string
  }
  eventId: number
  email: string
  onPaymentStart: () => void
  onPaymentSuccess: (ticketId: number, transactionRef: string) => void
  onPaymentError: (error: string) => void
}

export function PaymentForm({
  ticket,
  eventId,
  email,
  onPaymentStart,
  onPaymentSuccess,
  onPaymentError,
}: PaymentFormProps) {
  const [isLoading, setIsLoading] = useState(false)
  const [cardNumber, setCardNumber] = useState("")
  const [cardHolder, setCardHolder] = useState("")
  const [expiryDate, setExpiryDate] = useState("")
  const [cvv, setCvv] = useState("")

  // Formatear número de tarjeta (4 dígitos separados por espacios)
  const formatCardNumber = (value: string) => {
    const cleaned = value.replace(/\s/g, "").slice(0, 16)
    const formatted = cleaned.replace(/(\d{4})/g, "$1 ").trim()
    return formatted
  }

  // Formatear fecha de vencimiento (MM/YY)
  const formatExpiryDate = (value: string) => {
    const cleaned = value.replace(/\D/g, "").slice(0, 4)
    if (cleaned.length >= 2) {
      return `${cleaned.slice(0, 2)}/${cleaned.slice(2)}`
    }
    return cleaned
  }

  const handlePaymentSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    // Validaciones
    if (!cardNumber.replace(/\s/g, "") || cardNumber.replace(/\s/g, "").length !== 16) {
      toast.error("Número de tarjeta inválido (16 dígitos)")
      return
    }

    if (!cardHolder.trim()) {
      toast.error("Nombre del titular requerido")
      return
    }

    if (!expiryDate || expiryDate.length !== 5) {
      toast.error("Fecha de vencimiento inválida (MM/YY)")
      return
    }

    if (!cvv || cvv.length !== 3) {
      toast.error("CVV inválido (3 dígitos)")
      return
    }

    // Validar que la fecha no esté expirada
    const [month, year] = expiryDate.split("/")
    const currentDate = new Date()
    const currentYear = currentDate.getFullYear() % 100
    const currentMonth = currentDate.getMonth() + 1

    const expYear = parseInt(`20${year}`)
    const expMonth = parseInt(month)

    if (expYear < currentDate.getFullYear() || (expYear === currentDate.getFullYear() && expMonth < currentMonth)) {
      toast.error("Tarjeta expirada")
      return
    }

    setIsLoading(true)
    onPaymentStart()

    try {
      const transactionRef = `TXN-${Date.now()}`
      
      // Procesar pago
      const response = await api.processPayment({
        ticketId: ticket.id,
        eventId: eventId,
        amountCents: Math.round(ticket.price), // Ya está en centavos desde backend
        currency: ticket.currency || "USD",
        paymentBy: email,
        paymentMethodId: `card_${cardNumber.replace(/\s/g, "").slice(-4)}`, // Usar últimos 4 dígitos
        transactionRef: transactionRef,
      })

      toast.success("💳 Pago procesado. Confirmando con el servidor...")

      onPaymentSuccess(ticket.id, transactionRef)
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : "Error al procesar el pago"
      onPaymentError(errorMessage)
      toast.error(`❌ ${errorMessage}`)
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <Card className="border-border bg-card p-6">
      <h2 className="mb-6 text-xl font-semibold text-foreground">Información de Pago</h2>

      <form onSubmit={handlePaymentSubmit} className="space-y-4">
        {/* Número de Tarjeta */}
        <div className="space-y-2">
          <Label htmlFor="cardNumber" className="text-sm">
            Número de Tarjeta
          </Label>
          <Input
            id="cardNumber"
            placeholder="1234 5678 9012 3456"
            value={cardNumber}
            onChange={(e) => setCardNumber(formatCardNumber(e.target.value))}
            disabled={isLoading}
            maxLength={19}
            className="bg-secondary border-border font-mono text-lg tracking-widest"
          />
          <p className="text-xs text-muted-foreground">
            {cardNumber.replace(/\s/g, "").length}/16 dígitos
          </p>
        </div>

        {/* Nombre del Titular */}
        <div className="space-y-2">
          <Label htmlFor="cardHolder" className="text-sm">
            Nombre del Titular
          </Label>
          <Input
            id="cardHolder"
            placeholder="JUAN PÉREZ"
            value={cardHolder}
            onChange={(e) => setCardHolder(e.target.value.toUpperCase())}
            disabled={isLoading}
            className="bg-secondary border-border uppercase"
          />
        </div>

        {/* Fecha de Vencimiento y CVV */}
        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-2">
            <Label htmlFor="expiryDate" className="text-sm">
              Vencimiento (MM/YY)
            </Label>
            <Input
              id="expiryDate"
              placeholder="12/25"
              value={expiryDate}
              onChange={(e) => setExpiryDate(formatExpiryDate(e.target.value))}
              disabled={isLoading}
              maxLength={5}
              className="bg-secondary border-border font-mono text-center"
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="cvv" className="text-sm">
              CVV
            </Label>
            <Input
              id="cvv"
              type="password"
              placeholder="123"
              value={cvv}
              onChange={(e) => setCvv(e.target.value.replace(/\D/g, "").slice(0, 3))}
              disabled={isLoading}
              maxLength={3}
              className="bg-secondary border-border font-mono text-center tracking-widest"
            />
          </div>
        </div>

        {/* Botón de Pago */}
        <Button type="submit" disabled={isLoading} className="mt-6 w-full">
          {isLoading ? (
            <>
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              Procesando...
            </>
          ) : (
            `Pagar $${(ticket.price / 100).toFixed(2)}`
          )}
        </Button>

        {/* Nota de Seguridad */}
        <p className="text-xs text-muted-foreground">
          🔒 Los datos de tu tarjeta son procesados de forma segura. Esta es una demostración, no se cobran datos reales.
        </p>
      </form>
    </Card>
  )
}
