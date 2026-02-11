"use client"

import React from "react"

import { useState } from "react"
import Link from "next/link"
import { useSWRConfig } from "swr"
import { toast } from "sonner"
import {
  ArrowLeft,
  Calendar,
  Ticket,
  Trash2,
  Pencil,
  Plus,
} from "lucide-react"
import { useEvent, useTickets } from "@/hooks/use-ticketing"
import { TicketsTable } from "@/components/tickets-table"
import { CreateTicketsDialog } from "@/components/create-tickets-dialog"
import { EditEventDialog } from "@/components/edit-event-dialog"
import { Button } from "@/components/ui/button"
import { Skeleton } from "@/components/ui/skeleton"
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog"
import { api } from "@/lib/api"

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

export function EventDetailView({ eventId }: { eventId: number }) {
  const { data: event, isLoading: eventLoading } = useEvent(eventId)
  const { data: tickets, isLoading: ticketsLoading } = useTickets(eventId)
  const { mutate } = useSWRConfig()
  const [deleting, setDeleting] = useState(false)

  async function handleDelete() {
    setDeleting(true)
    try {
      await api.deleteEvent(eventId)
      toast.success("Evento eliminado")
      mutate("events")
      window.location.href = "/"
    } catch (err) {
      toast.error(
        err instanceof Error ? err.message : "Error al eliminar evento"
      )
    } finally {
      setDeleting(false)
    }
  }

  if (eventLoading) {
    return (
      <div className="flex flex-col gap-6">
        <Skeleton className="h-8 w-48 bg-secondary" />
        <Skeleton className="h-32 w-full rounded-xl bg-secondary" />
        <Skeleton className="h-64 w-full rounded-xl bg-secondary" />
      </div>
    )
  }

  if (!event) {
    return (
      <div className="flex flex-col items-center justify-center gap-4 py-20">
        <p className="text-muted-foreground">Evento no encontrado</p>
        <Link href="/">
          <Button variant="outline">
            <ArrowLeft className="mr-2 h-4 w-4" />
            Volver
          </Button>
        </Link>
      </div>
    )
  }

  const total =
    event.availableTickets + event.reservedTickets + event.paidTickets

  return (
    <div className="flex flex-col gap-6">
      {/* Back link */}
      <Link
        href="/"
        className="inline-flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground transition-colors w-fit"
      >
        <ArrowLeft className="h-4 w-4" />
        Volver a eventos
      </Link>

      {/* Event header */}
      <div className="flex flex-col gap-4 rounded-xl border border-border bg-card p-6 sm:flex-row sm:items-start sm:justify-between">
        <div className="flex flex-col gap-2">
          <h1 className="text-2xl font-bold tracking-tight text-foreground text-balance">
            {event.name}
          </h1>
          <div className="flex items-center gap-2 text-sm text-muted-foreground">
            <Calendar className="h-4 w-4" />
            <span>
              {formatDate(event.startsAt)} a las {formatTime(event.startsAt)}
            </span>
          </div>
        </div>
        <div className="flex items-center gap-2">
          <EditEventDialog event={event} />
          <AlertDialog>
            <AlertDialogTrigger asChild>
              <Button variant="outline" size="sm" className="text-destructive hover:bg-destructive/10 hover:text-destructive border-destructive/30 bg-transparent">
                <Trash2 className="mr-2 h-4 w-4" />
                Eliminar
              </Button>
            </AlertDialogTrigger>
            <AlertDialogContent className="bg-card border-border">
              <AlertDialogHeader>
                <AlertDialogTitle>Eliminar evento</AlertDialogTitle>
                <AlertDialogDescription>
                  Esta accion eliminara permanentemente el evento y todos sus
                  tickets asociados. Esta accion no se puede deshacer.
                </AlertDialogDescription>
              </AlertDialogHeader>
              <AlertDialogFooter>
                <AlertDialogCancel className="bg-secondary border-border text-foreground">Cancelar</AlertDialogCancel>
                <AlertDialogAction
                  onClick={handleDelete}
                  disabled={deleting}
                  className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
                >
                  {deleting ? "Eliminando..." : "Eliminar"}
                </AlertDialogAction>
              </AlertDialogFooter>
            </AlertDialogContent>
          </AlertDialog>
        </div>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        <StatCard
          label="Total"
          value={total}
          icon={<Ticket className="h-5 w-5 text-primary" />}
          bg="bg-primary/10"
        />
        <StatCard
          label="Disponibles"
          value={event.availableTickets}
          icon={<Ticket className="h-5 w-5 text-emerald-400" />}
          bg="bg-emerald-500/10"
        />
        <StatCard
          label="Reservados"
          value={event.reservedTickets}
          icon={<Ticket className="h-5 w-5 text-blue-400" />}
          bg="bg-blue-500/10"
        />
        <StatCard
          label="Pagados"
          value={event.paidTickets}
          icon={<Ticket className="h-5 w-5 text-amber-400" />}
          bg="bg-amber-500/10"
        />
      </div>

      {/* Tickets section */}
      <div className="flex flex-col gap-4">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold text-foreground">Tickets</h2>
          <CreateTicketsDialog eventId={eventId} />
        </div>
        <TicketsTable
          tickets={tickets ?? []}
          loading={ticketsLoading}
          eventId={eventId}
        />
      </div>
    </div>
  )
}

function StatCard({
  label,
  value,
  icon,
  bg,
}: {
  label: string
  value: number
  icon: React.ReactNode
  bg: string
}) {
  return (
    <div className="flex items-center gap-4 rounded-xl border border-border bg-card p-4">
      <div
        className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-lg ${bg}`}
      >
        {icon}
      </div>
      <div className="flex flex-col">
        <span className="font-mono text-xl font-bold text-foreground">
          {value}
        </span>
        <span className="text-xs text-muted-foreground">{label}</span>
      </div>
    </div>
  )
}
