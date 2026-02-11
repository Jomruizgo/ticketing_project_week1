"use client"

import { useEvents } from "@/hooks/use-ticketing"
import { CalendarDays, Ticket, Clock, CheckCircle2 } from "lucide-react"

export function StatsOverview() {
  const { data: events } = useEvents()

  const stats = {
    totalEvents: events?.length ?? 0,
    totalAvailable: events?.reduce((sum, e) => sum + e.availableTickets, 0) ?? 0,
    totalReserved: events?.reduce((sum, e) => sum + e.reservedTickets, 0) ?? 0,
    totalPaid: events?.reduce((sum, e) => sum + e.paidTickets, 0) ?? 0,
  }

  const cards = [
    {
      label: "Eventos",
      value: stats.totalEvents,
      icon: CalendarDays,
      color: "text-primary",
      bg: "bg-primary/10",
    },
    {
      label: "Disponibles",
      value: stats.totalAvailable,
      icon: Ticket,
      color: "text-emerald-400",
      bg: "bg-emerald-500/10",
    },
    {
      label: "Reservados",
      value: stats.totalReserved,
      icon: Clock,
      color: "text-blue-400",
      bg: "bg-blue-500/10",
    },
    {
      label: "Pagados",
      value: stats.totalPaid,
      icon: CheckCircle2,
      color: "text-amber-400",
      bg: "bg-amber-500/10",
    },
  ]

  return (
    <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
      {cards.map((card) => (
        <div
          key={card.label}
          className="flex items-center gap-4 rounded-xl border border-border bg-card p-4"
        >
          <div
            className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-lg ${card.bg}`}
          >
            <card.icon className={`h-5 w-5 ${card.color}`} />
          </div>
          <div className="flex flex-col">
            <span className="font-mono text-xl font-bold text-foreground">
              {card.value}
            </span>
            <span className="text-xs text-muted-foreground">{card.label}</span>
          </div>
        </div>
      ))}
    </div>
  )
}
