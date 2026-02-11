"use client"

import Link from "next/link"
import { Calendar, Tickets, ArrowRight } from "lucide-react"
import { Card } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import type { Event } from "@/lib/types"

export function BuyerEventCard({ event }: { event: Event }) {
  const startsAt = new Date(event.startsAt)
  const isUpcoming = startsAt > new Date()

  const formattedDate = startsAt.toLocaleDateString("es-ES", {
    weekday: "long",
    day: "numeric",
    month: "long",
    year: "numeric",
  })

  const formattedTime = startsAt.toLocaleTimeString("es-ES", {
    hour: "2-digit",
    minute: "2-digit",
  })

  return (
    <Card className="flex flex-col gap-4 border-border bg-card p-6 transition-all hover:shadow-lg">
      {/* Status Badge */}
      {!isUpcoming && (
        <div className="inline-flex w-fit rounded-full bg-gray-500/15 px-3 py-1">
          <span className="text-xs font-medium text-gray-400">
            Evento finalizado
          </span>
        </div>
      )}

      {/* Event Name */}
      <div>
        <h3 className="text-lg font-semibold text-foreground">{event.name}</h3>
      </div>

      {/* Date & Time */}
      <div className="flex flex-col gap-2">
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <Calendar className="h-4 w-4" />
          {formattedDate}
        </div>
        <div className="text-sm font-medium text-foreground">
          {formattedTime}
        </div>
      </div>

      {/* Tickets Info */}
      <div className="flex items-center gap-2 rounded-lg bg-secondary/50 px-3 py-2">
        <Tickets className="h-4 w-4 text-blue-400" />
        <div className="text-sm">
          <span className="font-semibold text-foreground">
            {event.availableTickets}
          </span>
          <span className="text-muted-foreground"> disponibles</span>
        </div>
      </div>

      {/* CTA Button */}
      <Link href={`/buy/${event.id}`} className="mt-auto">
        <Button
          className="w-full"
          disabled={!isUpcoming || event.availableTickets === 0}
        >
          {!isUpcoming ? (
            "Evento finalizado"
          ) : event.availableTickets === 0 ? (
            "Sin tickets disponibles"
          ) : (
            <>
              Comprar tickets
              <ArrowRight className="ml-2 h-4 w-4" />
            </>
          )}
        </Button>
      </Link>
    </Card>
  )
}
