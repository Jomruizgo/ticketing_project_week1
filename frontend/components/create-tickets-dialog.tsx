"use client"

import React from "react"

import { useState } from "react"
import { useSWRConfig } from "swr"
import { toast } from "sonner"
import { Plus } from "lucide-react"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { api } from "@/lib/api"

export function CreateTicketsDialog({ eventId }: { eventId: number }) {
  const [open, setOpen] = useState(false)
  const [quantity, setQuantity] = useState("10")
  const [loading, setLoading] = useState(false)
  const { mutate } = useSWRConfig()

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    const qty = Number(quantity)
    if (!Number.isInteger(qty) || qty < 1 || qty > 1000) {
      toast.error("La cantidad debe estar entre 1 y 1000")
      return
    }

    setLoading(true)
    try {
      const result = await api.createTickets({ eventId, quantity: qty })
      toast.success(`${result.createdCount} tickets creados`)
      mutate(`tickets-${eventId}`)
      mutate(`event-${eventId}`)
      mutate("events")
      setOpen(false)
      setQuantity("10")
    } catch (err) {
      toast.error(
        err instanceof Error ? err.message : "Error al crear tickets"
      )
    } finally {
      setLoading(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button size="sm">
          <Plus className="mr-2 h-4 w-4" />
          Crear Tickets
        </Button>
      </DialogTrigger>
      <DialogContent className="bg-card border-border">
        <DialogHeader>
          <DialogTitle>Crear Tickets</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="flex flex-col gap-4 pt-2">
          <div className="flex flex-col gap-2">
            <Label htmlFor="ticket-quantity">Cantidad de tickets</Label>
            <Input
              id="ticket-quantity"
              type="number"
              min={1}
              max={1000}
              value={quantity}
              onChange={(e) => setQuantity(e.target.value)}
              placeholder="Ej: 100"
              className="bg-secondary border-border"
            />
            <p className="text-xs text-muted-foreground">
              Minimo 1, maximo 1000 tickets por lote
            </p>
          </div>
          <Button type="submit" disabled={loading} className="mt-2">
            {loading ? "Creando..." : "Crear Tickets"}
          </Button>
        </form>
      </DialogContent>
    </Dialog>
  )
}
