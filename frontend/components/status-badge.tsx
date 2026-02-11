import { cn } from "@/lib/utils"
import type { TicketStatus } from "@/lib/types"

const statusConfig: Record<
  TicketStatus,
  { label: string; className: string }
> = {
  available: {
    label: "Disponible",
    className: "bg-emerald-500/15 text-emerald-400 border-emerald-500/20",
  },
  reserved: {
    label: "Reservado",
    className: "bg-blue-500/15 text-blue-400 border-blue-500/20",
  },
  paid: {
    label: "Pagado",
    className: "bg-amber-500/15 text-amber-400 border-amber-500/20",
  },
  released: {
    label: "Liberado",
    className: "bg-gray-500/15 text-gray-400 border-gray-500/20",
  },
  cancelled: {
    label: "Cancelado",
    className: "bg-red-500/15 text-red-400 border-red-500/20",
  },
}

export function StatusBadge({ status }: { status: TicketStatus | undefined }) {
  const config = statusConfig[status as TicketStatus]
  
  if (!config) {
    console.warn('Unknown ticket status:', status)
    return (
      <span className="inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-medium bg-gray-500/15 text-gray-400 border-gray-500/20">
        {status || "Desconocido"}
      </span>
    )
  }
  
  return (
    <span
      className={cn(
        "inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-medium",
        config.className
      )}
    >
      {config.label}
    </span>
  )
}
