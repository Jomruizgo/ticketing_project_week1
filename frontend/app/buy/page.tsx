"use client"

import { useEvents } from "@/hooks/use-ticketing"
import { Skeleton } from "@/components/ui/skeleton"
import { BuyerEventCard } from "@/components/buyer-event-card"

export default function BuyPage() {
  const { data: events, isLoading } = useEvents()

  return (
    <div className="flex min-h-screen flex-col bg-background">
      {/* Header */}
      <header className="border-b border-border">
        <div className="mx-auto w-full max-w-7xl px-6 py-8">
          <div>
            <h1 className="text-3xl font-bold tracking-tight text-foreground">
              Compra de Tickets
            </h1>
            <p className="mt-2 text-lg text-muted-foreground">
              Reserva y compra tickets para tus eventos favoritos
            </p>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className="mx-auto w-full max-w-7xl flex-1 px-6 py-8">
        {isLoading ? (
          <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
            {Array.from({ length: 6 }).map((_, i) => (
              <Skeleton key={i} className="h-64 rounded-lg bg-secondary" />
            ))}
          </div>
        ) : !events || events.length === 0 ? (
          <div className="flex flex-col items-center justify-center gap-4 rounded-xl border border-border bg-card py-16">
            <p className="text-lg font-medium text-foreground">
              No hay eventos disponibles
            </p>
            <p className="text-sm text-muted-foreground">
              Vuelve más tarde para nuevos eventos
            </p>
          </div>
        ) : (
          <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
            {events.map((event) => (
              <BuyerEventCard key={event.id} event={event} />
            ))}
          </div>
        )}
      </main>
    </div>
  )
}
