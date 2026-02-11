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

export function CreateEventDialog() {
  const [open, setOpen] = useState(false)
  const [name, setName] = useState("")
  const [startsAt, setStartsAt] = useState("")
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
      await api.createEvent({ name: name.trim(), startsAt: new Date(startsAt).toISOString() })
      toast.success("Evento creado exitosamente")
      mutate("events")
      setOpen(false)
      setName("")
      setStartsAt("")
    } catch (err) {
      toast.error(err instanceof Error ? err.message : "Error al crear evento")
    } finally {
      setLoading(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button>
          <Plus className="mr-2 h-4 w-4" />
          Nuevo Evento
        </Button>
      </DialogTrigger>
      <DialogContent className="bg-card border-border">
        <DialogHeader>
          <DialogTitle>Crear Evento</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="flex flex-col gap-4 pt-2">
          <div className="flex flex-col gap-2">
            <Label htmlFor="event-name">Nombre del evento</Label>
            <Input
              id="event-name"
              placeholder="Ej: Concierto 2026"
              value={name}
              onChange={(e) => setName(e.target.value)}
              maxLength={200}
              className="bg-secondary border-border"
            />
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor="event-date">Fecha y hora</Label>
            <Input
              id="event-date"
              type="datetime-local"
              value={startsAt}
              onChange={(e) => setStartsAt(e.target.value)}
              className="bg-secondary border-border"
            />
          </div>
          <Button type="submit" disabled={loading} className="mt-2">
            {loading ? "Creando..." : "Crear Evento"}
          </Button>
        </form>
      </DialogContent>
    </Dialog>
  )
}
