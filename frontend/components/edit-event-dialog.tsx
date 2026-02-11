"use client"

import React from "react"

import { useState } from "react"
import { useSWRConfig } from "swr"
import { toast } from "sonner"
import { Pencil } from "lucide-react"
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
import type { Event } from "@/lib/types"

function toLocalDatetimeValue(iso: string) {
  const d = new Date(iso)
  const offset = d.getTimezoneOffset()
  const local = new Date(d.getTime() - offset * 60000)
  return local.toISOString().slice(0, 16)
}

export function EditEventDialog({ event }: { event: Event }) {
  const [open, setOpen] = useState(false)
  const [name, setName] = useState(event.name)
  const [startsAt, setStartsAt] = useState(
    toLocalDatetimeValue(event.startsAt)
  )
  const [loading, setLoading] = useState(false)
  const { mutate } = useSWRConfig()

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!name.trim() || !startsAt) {
      toast.error("Completa todos los campos")
      return
    }

    setLoading(true)
    try {
      await api.updateEvent(event.id, {
        name: name.trim(),
        startsAt: new Date(startsAt).toISOString(),
      })
      toast.success("Evento actualizado")
      mutate(`event-${event.id}`)
      mutate("events")
      setOpen(false)
    } catch (err) {
      toast.error(
        err instanceof Error ? err.message : "Error al actualizar evento"
      )
    } finally {
      setLoading(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button variant="outline" size="sm" className="border-border bg-transparent">
          <Pencil className="mr-2 h-4 w-4" />
          Editar
        </Button>
      </DialogTrigger>
      <DialogContent className="bg-card border-border">
        <DialogHeader>
          <DialogTitle>Editar Evento</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="flex flex-col gap-4 pt-2">
          <div className="flex flex-col gap-2">
            <Label htmlFor="edit-event-name">Nombre del evento</Label>
            <Input
              id="edit-event-name"
              value={name}
              onChange={(e) => setName(e.target.value)}
              maxLength={200}
              className="bg-secondary border-border"
            />
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="edit-event-date">Fecha y hora</Label>
            <Input
              id="edit-event-date"
              type="datetime-local"
              value={startsAt}
              onChange={(e) => setStartsAt(e.target.value)}
              className="bg-secondary border-border"
            />
          </div>
          <Button type="submit" disabled={loading} className="mt-2">
            {loading ? "Guardando..." : "Guardar Cambios"}
          </Button>
        </form>
      </DialogContent>
    </Dialog>
  )
}
