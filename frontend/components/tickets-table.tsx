"use client"

import { useState } from "react"
import { useSWRConfig } from "swr"
import { toast } from "sonner"
import { MoreHorizontal, Clock } from "lucide-react"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { Button } from "@/components/ui/button"
import { Skeleton } from "@/components/ui/skeleton"
import { StatusBadge } from "@/components/status-badge"
import { ReserveTicketDialog } from "@/components/reserve-ticket-dialog"
import type { Ticket } from "@/lib/types"
import { api } from "@/lib/api"

function formatDateTime(iso: string | null) {
  if (!iso) return "-"
  return new Date(iso).toLocaleString("es-ES", {
    day: "2-digit",
    month: "2-digit",
    year: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  })
}

function isExpired(expiresAt: string | null) {
  if (!expiresAt) return false
  return new Date(expiresAt) < new Date()
}

export function TicketsTable({
  tickets,
  loading,
  eventId,
}: {
  tickets: Ticket[]
  loading: boolean
  eventId: number
}) {
  const { mutate } = useSWRConfig()
  const [reserveTicket, setReserveTicket] = useState<Ticket | null>(null)

  async function handleStatusChange(
    ticketId: number,
    newStatus: "released" | "cancelled" | "paid",
    reason: string
  ) {
    try {
      await api.updateTicketStatus(ticketId, { newStatus, reason })
      toast.success(`Ticket #${ticketId} actualizado a ${newStatus}`)
      mutate(`tickets-${eventId}`)
      mutate(`event-${eventId}`)
      mutate("events")
    } catch (err) {
      toast.error(
        err instanceof Error ? err.message : "Error al actualizar ticket"
      )
    }
  }

  if (loading) {
    return (
      <div className="flex flex-col gap-2">
        {Array.from({ length: 5 }).map((_, i) => (
          <Skeleton key={i} className="h-12 w-full rounded-lg bg-secondary" />
        ))}
      </div>
    )
  }

  if (tickets.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center gap-2 rounded-xl border border-border bg-card py-16">
        <p className="text-sm text-muted-foreground">
          No hay tickets para este evento
        </p>
        <p className="text-xs text-muted-foreground">
          Crea tickets usando el boton de arriba
        </p>
      </div>
    )
  }

  return (
    <>
      <div className="overflow-hidden rounded-xl border border-border bg-card">
        <Table>
          <TableHeader>
            <TableRow className="border-border hover:bg-transparent">
              <TableHead className="text-muted-foreground">ID</TableHead>
              <TableHead className="text-muted-foreground">Estado</TableHead>
              <TableHead className="hidden text-muted-foreground md:table-cell">
                Reservado por
              </TableHead>
              <TableHead className="hidden text-muted-foreground lg:table-cell">
                Order ID
              </TableHead>
              <TableHead className="hidden text-muted-foreground md:table-cell">
                Expira
              </TableHead>
              <TableHead className="text-muted-foreground">Version</TableHead>
              <TableHead className="w-12 text-muted-foreground" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {tickets.map((ticket) => (
              <TableRow
                key={ticket.id}
                className="border-border hover:bg-secondary/50"
              >
                <TableCell className="font-mono text-sm text-foreground">
                  #{ticket.id}
                </TableCell>
                <TableCell>
                  <div className="flex items-center gap-2">
                    <StatusBadge status={ticket.status} />
                    {ticket.status === "reserved" &&
                      isExpired(ticket.expiresAt) && (
                        <Clock className="h-3.5 w-3.5 text-destructive" />
                      )}
                  </div>
                </TableCell>
                <TableCell className="hidden text-sm text-muted-foreground md:table-cell">
                  {ticket.reservedBy || "-"}
                </TableCell>
                <TableCell className="hidden font-mono text-xs text-muted-foreground lg:table-cell">
                  {ticket.orderId || "-"}
                </TableCell>
                <TableCell className="hidden text-sm text-muted-foreground md:table-cell">
                  {formatDateTime(ticket.expiresAt)}
                </TableCell>
                <TableCell className="font-mono text-xs text-muted-foreground">
                  v{ticket.version}
                </TableCell>
                <TableCell>
                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <Button
                        variant="ghost"
                        size="sm"
                        className="h-8 w-8 p-0 text-muted-foreground hover:text-foreground"
                      >
                        <span className="sr-only">Acciones</span>
                        <MoreHorizontal className="h-4 w-4" />
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent
                      align="end"
                      className="bg-card border-border"
                    >
                      {ticket.status?.toLowerCase() === "available" && (
                        <DropdownMenuItem
                          onClick={() => setReserveTicket(ticket)}
                          className="text-foreground focus:bg-secondary focus:text-foreground"
                        >
                          Reservar
                        </DropdownMenuItem>
                      )}
                      {ticket.status?.toLowerCase() === "reserved" && (
                        <>
                          <DropdownMenuItem
                            onClick={() =>
                              handleStatusChange(
                                ticket.id,
                                "paid",
                                "Pago confirmado"
                              )
                            }
                            className="text-foreground focus:bg-secondary focus:text-foreground"
                          >
                            Marcar como pagado
                          </DropdownMenuItem>
                          <DropdownMenuItem
                            onClick={() =>
                              handleStatusChange(
                                ticket.id,
                                "released",
                                "Liberado manualmente"
                              )
                            }
                            className="text-foreground focus:bg-secondary focus:text-foreground"
                          >
                            Liberar
                          </DropdownMenuItem>
                        </>
                      )}
                      {(ticket.status?.toLowerCase() === "available" ||
                        ticket.status?.toLowerCase() === "reserved" ||
                        ticket.status?.toLowerCase() === "paid") && (
                        <DropdownMenuItem
                          onClick={() =>
                            handleStatusChange(
                              ticket.id,
                              "cancelled",
                              "Cancelado manualmente"
                            )
                          }
                          className="text-destructive focus:bg-destructive/10 focus:text-destructive"
                        >
                          Cancelar
                        </DropdownMenuItem>
                      )}
                    </DropdownMenuContent>
                  </DropdownMenu>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>

      {reserveTicket && (
        <ReserveTicketDialog
          ticket={reserveTicket}
          eventId={eventId}
          open={!!reserveTicket}
          onOpenChange={(open) => {
            if (!open) setReserveTicket(null)
          }}
        />
      )}
    </>
  )
}
