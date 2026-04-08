"use client"

import { useState, useEffect, useCallback, useRef } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { api, ApiError } from "@/lib/api"
import { toast } from "sonner"
import { useWaitlistSse } from "@/hooks/use-waitlist-sse"
import type { WaitlistStatusResponse, WaitlistOpportunityDto } from "@/lib/types"
import { Clock, CheckCircle2, XCircle, Loader2 } from "lucide-react"

interface WaitlistStatusProps {
  eventId: number
  initialEmail?: string
  onClaimSuccess?: (ticketId: number, eventId: number) => void
}

function formatCountdown(totalSeconds: number): string {
  const clamped = Math.max(0, totalSeconds)
  const minutes = Math.floor(clamped / 60)
  const seconds = clamped % 60
  return `${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}`
}

type ViewState =
  | { kind: "form" }
  | { kind: "loading" }
  | { kind: "waiting"; email: string }
  | { kind: "active"; email: string; opportunity: WaitlistOpportunityDto }
  | { kind: "consumed"; email: string }
  | { kind: "expired"; email: string }
  | { kind: "not-found" }
  | { kind: "error"; message: string }

export function WaitlistStatus({ eventId, initialEmail, onClaimSuccess }: WaitlistStatusProps) {
  const [emailInput, setEmailInput] = useState("")
  const [viewState, setViewState] = useState<ViewState>(
    initialEmail ? { kind: "loading" } : { kind: "form" }
  )
  const [claiming, setClaiming] = useState(false)
  const [countdown, setCountdown] = useState<number | null>(null)
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null)
  const activeEmail = viewState.kind === "form" || viewState.kind === "loading" || viewState.kind === "not-found" || viewState.kind === "error"
    ? ""
    : "email" in viewState ? viewState.email : ""

  const processStatusResponse = useCallback((response: WaitlistStatusResponse, email: string) => {
    const opp = response.opportunity

    if (!opp) {
      setViewState({ kind: "waiting", email })
      return
    }

    if (opp.status === "consumed") {
      setViewState({ kind: "consumed", email })
      return
    }

    if (opp.status === "expired") {
      setViewState({ kind: "expired", email })
      return
    }

    if (opp.status === "active") {
      setViewState({ kind: "active", email, opportunity: opp })
      startCountdown(opp.expiresAt)
      return
    }

    setViewState({ kind: "waiting", email })
  }, [])

  function startCountdown(expiresAt: string | null) {
    if (intervalRef.current) clearInterval(intervalRef.current)

    if (!expiresAt) {
      setCountdown(0)
      return
    }

    const calcRemaining = () => Math.max(0, Math.floor((new Date(expiresAt).getTime() - Date.now()) / 1000))

    setCountdown(calcRemaining())

    intervalRef.current = setInterval(() => {
      const remaining = calcRemaining()
      setCountdown(remaining)
      if (remaining <= 0 && intervalRef.current) {
        clearInterval(intervalRef.current)
        intervalRef.current = null
      }
    }, 1000)
  }

  useEffect(() => {
    return () => {
      if (intervalRef.current) clearInterval(intervalRef.current)
    }
  }, [])

  useEffect(() => {
    if (!initialEmail) return

    async function fetchStatus() {
      try {
        const response = await api.getWaitlistStatus(eventId, initialEmail!)
        processStatusResponse(response, initialEmail!)
      } catch (err) {
        if (err instanceof ApiError && err.status === 404) {
          setViewState({ kind: "not-found" })
        } else {
          setViewState({ kind: "error", message: "Error al conectar con el servidor" })
        }
      }
    }

    fetchStatus()
  }, [eventId, initialEmail, processStatusResponse])

  const handleSseEvent = useCallback((event: { type: string; data: unknown }) => {
    if (event.type === "opportunity_activated") {
      const data = event.data as { opportunityId: number; ticketId: number; eventId: number; expiresAt: string; remainingMinutes: number }
      const opportunity: WaitlistOpportunityDto = {
        id: data.opportunityId, ticketId: data.ticketId,
        status: "active", activatedAt: new Date().toISOString(),
        expiresAt: data.expiresAt, remainingMinutes: data.remainingMinutes,
      }
      setViewState((prev) => {
        const email = "email" in prev ? prev.email : initialEmail || ""
        return { kind: "active", email, opportunity }
      })
      startCountdown(data.expiresAt)
    }

    if (event.type === "opportunity_expired") {
      setViewState((prev) => {
        const email = "email" in prev ? prev.email : initialEmail || ""
        return { kind: "expired", email }
      })
      if (intervalRef.current) {
        clearInterval(intervalRef.current)
        intervalRef.current = null
      }
      setCountdown(null)
    }
  }, [initialEmail])

  useWaitlistSse(activeEmail, handleSseEvent)

  async function handleQuery(e: React.FormEvent) {
    e.preventDefault()
    const trimmed = emailInput.trim()
    if (!trimmed) return

    setViewState({ kind: "loading" })
    try {
      const response = await api.getWaitlistStatus(eventId, trimmed)
      processStatusResponse(response, trimmed)
    } catch (err) {
      if (err instanceof ApiError && err.status === 404) {
        setViewState({ kind: "not-found" })
      } else {
        setViewState({ kind: "error", message: "Error al conectar con el servidor" })
      }
    }
  }

  async function handleClaim() {
    if (viewState.kind !== "active") return

    setClaiming(true)
    try {
      const result = await api.claimOpportunity(viewState.opportunity.id, viewState.email)
      setViewState({ kind: "consumed", email: viewState.email })
      if (intervalRef.current) {
        clearInterval(intervalRef.current)
        intervalRef.current = null
      }
      setCountdown(null)
      onClaimSuccess?.(result.ticketId, result.eventId)
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.status === 409) {
          toast.error("La oportunidad ya no está activa")
          setViewState({ kind: "expired", email: viewState.email })
          if (intervalRef.current) clearInterval(intervalRef.current)
          setCountdown(null)
        } else if (err.status === 403) {
          toast.error("Esta oportunidad no pertenece al comprador indicado")
        } else if (err.status === 404) {
          toast.error("Oportunidad no encontrada")
        } else {
          toast.error("Error al conectar con el servidor")
        }
      } else {
        toast.error("Error al conectar con el servidor")
      }
    } finally {
      setClaiming(false)
    }
  }

  // ─── Render ──────────────────────────────────────────

  if (viewState.kind === "form") {
    return (
      <div className="flex flex-col gap-4">
        <h2 className="text-lg font-semibold text-foreground">Consultar estado de lista de espera</h2>
        <form onSubmit={handleQuery} className="flex flex-col gap-3" noValidate>
          <Input
            type="email"
            placeholder="Tu email"
            value={emailInput}
            onChange={(e) => setEmailInput(e.target.value)}
          />
          <Button type="submit">Consultar estado</Button>
        </form>
      </div>
    )
  }

  if (viewState.kind === "loading") {
    return (
      <div className="flex items-center justify-center gap-2 py-6">
        <Loader2 className="h-5 w-5 animate-spin text-primary" />
        <span className="text-muted-foreground">Consultando estado...</span>
      </div>
    )
  }

  if (viewState.kind === "not-found") {
    return (
      <div className="flex flex-col items-center gap-3 py-6">
        <XCircle className="h-8 w-8 text-muted-foreground" />
        <p className="text-sm text-muted-foreground">No existe inscripción para este comprador</p>
      </div>
    )
  }

  if (viewState.kind === "error") {
    return (
      <div className="flex flex-col items-center gap-3 py-6">
        <XCircle className="h-8 w-8 text-destructive" />
        <p className="text-sm text-destructive">{viewState.message}</p>
      </div>
    )
  }

  if (viewState.kind === "waiting") {
    return (
      <div className="flex flex-col items-center gap-3 py-6">
        <Clock className="h-8 w-8 text-blue-400" />
        <span className="rounded-full bg-blue-500/10 px-3 py-1 text-sm font-medium text-blue-400">
          En espera
        </span>
        <p className="text-sm text-muted-foreground">
          Estás inscrito en la lista de espera. Te notificaremos cuando haya una entrada disponible.
        </p>
      </div>
    )
  }

  if (viewState.kind === "active") {
    const isExpired = countdown !== null && countdown <= 0

    return (
      <div className="flex flex-col items-center gap-4 py-6">
        <span className="rounded-full bg-emerald-500/10 px-3 py-1 text-sm font-medium text-emerald-400">
          Oportunidad activa
        </span>
        {countdown !== null && (
          <div className="text-3xl font-bold tabular-nums text-foreground">
            {formatCountdown(countdown)}
          </div>
        )}
        <p className="text-sm text-muted-foreground">
          Tienes una entrada reservada temporalmente. Avanza al pago antes de que expire.
        </p>
        <Button
          onClick={handleClaim}
          disabled={claiming || isExpired}
          className="mt-2"
        >
          {claiming ? (
            <>
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              Procesando...
            </>
          ) : (
            "Avanzar al pago"
          )}
        </Button>
      </div>
    )
  }

  if (viewState.kind === "consumed") {
    return (
      <div className="flex flex-col items-center gap-3 py-6">
        <CheckCircle2 className="h-8 w-8 text-emerald-400" />
        <span className="rounded-full bg-emerald-500/10 px-3 py-1 text-sm font-medium text-emerald-400">
          Oportunidad utilizada
        </span>
        <p className="text-sm text-muted-foreground">
          Ya avanzaste al flujo de pago con esta oportunidad.
        </p>
      </div>
    )
  }

  if (viewState.kind === "expired") {
    return (
      <div className="flex flex-col items-center gap-3 py-6">
        <XCircle className="h-8 w-8 text-orange-400" />
        <span className="rounded-full bg-orange-500/10 px-3 py-1 text-sm font-medium text-orange-400">
          Oportunidad expirada
        </span>
        <p className="text-sm text-muted-foreground">
          La oportunidad de compra venció sin ser utilizada.
        </p>
      </div>
    )
  }

  return null
}
