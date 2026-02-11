"use client"

import { useEvents } from "@/hooks/use-ticketing"
import { EventCard } from "@/components/event-card"
import { Skeleton } from "@/components/ui/skeleton"
import { CalendarX } from "lucide-react"

export function EventsList() {
  const { data: events, isLoading, error } = useEvents()

  if (isLoading) {
    return (
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {Array.from({ length: 6 }).map((_, i) => (
          <Skeleton key={i} className="h-56 rounded-xl bg-secondary" />
        ))}
      </div>
    )
  }

  if (error) {
    return (
      <div className="flex flex-col items-center justify-center gap-3 py-20 text-center">
        <div className="flex h-12 w-12 items-center justify-center rounded-full bg-destructive/10">
          <CalendarX className="h-6 w-6 text-destructive" />
        </div>
        <p className="text-sm text-muted-foreground">
          No se pudieron cargar los eventos. Verifica que los servicios backend esten corriendo.
        </p>
      </div>
    )
  }

  if (!events || events.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center gap-3 py-20 text-center">
        <div className="flex h-12 w-12 items-center justify-center rounded-full bg-secondary">
          <CalendarX className="h-6 w-6 text-muted-foreground" />
        </div>
        <div>
          <p className="font-medium text-foreground">No hay eventos</p>
          <p className="text-sm text-muted-foreground">
            Crea tu primer evento para comenzar
          </p>
        </div>
      </div>
    )
  }

  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {events.map((event) => (
        <EventCard key={event.id} event={event} />
      ))}
    </div>
  )
}
