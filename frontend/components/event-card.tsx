"use client"

import Link from "next/link"
import { Calendar, Ticket, ArrowRight } from "lucide-react"
import { Card, CardContent } from "@/components/ui/card"
import type { Event } from "@/lib/types"

function formatDate(iso: string) {
  const d = new Date(iso)
  return d.toLocaleDateString("es-ES", {
    day: "numeric",
    month: "short",
    year: "numeric",
  })
}

function formatTime(iso: string) {
  const d = new Date(iso)
  return d.toLocaleTimeString("es-ES", {
    hour: "2-digit",
    minute: "2-digit",
  })
}

export function EventCard({ event }: { event: Event }) {
  const total =
    event.availableTickets + event.reservedTickets + event.paidTickets
  const soldPercent =
    total > 0
      ? Math.round(((event.reservedTickets + event.paidTickets) / total) * 100)
      : 0

  return (
    <Link href={`/events/${event.id}`}>
      <Card className="group border-border bg-card transition-all hover:border-primary/40 hover:bg-card/80">
        <CardContent className="flex flex-col gap-4 p-5">
          <div className="flex items-start justify-between">
            <div className="flex flex-col gap-1">
              <h3 className="text-base font-semibold text-foreground group-hover:text-primary transition-colors">
                {event.name}
              </h3>
              <div className="flex items-center gap-1.5 text-sm text-muted-foreground">
                <Calendar className="h-3.5 w-3.5" />
                <span>
                  {formatDate(event.startsAt)} - {formatTime(event.startsAt)}
                </span>
              </div>
            </div>
            <ArrowRight className="h-4 w-4 text-muted-foreground opacity-0 transition-opacity group-hover:opacity-100" />
          </div>

          <div className="flex flex-col gap-2">
            <div className="flex items-center justify-between text-sm">
              <span className="text-muted-foreground">Ocupacion</span>
              <span className="font-mono text-sm font-medium text-foreground">
                {soldPercent}%
              </span>
            </div>
            <div className="h-1.5 w-full overflow-hidden rounded-full bg-secondary">
              <div
                className="h-full rounded-full bg-primary transition-all"
                style={{ width: `${soldPercent}%` }}
              />
            </div>
          </div>

          <div className="grid grid-cols-3 gap-3">
            <div className="flex flex-col items-center rounded-lg bg-secondary px-3 py-2">
              <span className="font-mono text-lg font-bold text-emerald-400">
                {event.availableTickets}
              </span>
              <span className="text-[11px] text-muted-foreground">
                Disponibles
              </span>
            </div>
            <div className="flex flex-col items-center rounded-lg bg-secondary px-3 py-2">
              <span className="font-mono text-lg font-bold text-blue-400">
                {event.reservedTickets}
              </span>
              <span className="text-[11px] text-muted-foreground">
                Reservados
              </span>
            </div>
            <div className="flex flex-col items-center rounded-lg bg-secondary px-3 py-2">
              <span className="font-mono text-lg font-bold text-amber-400">
                {event.paidTickets}
              </span>
              <span className="text-[11px] text-muted-foreground">
                Pagados
              </span>
            </div>
          </div>

          <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
            <Ticket className="h-3.5 w-3.5" />
            <span>
              {total} tickets totales
            </span>
          </div>
        </CardContent>
      </Card>
    </Link>
  )
}
