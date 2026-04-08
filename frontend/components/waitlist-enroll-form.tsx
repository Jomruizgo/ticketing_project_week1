"use client"

import { useState } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { api, ApiError } from "@/lib/api"
import type { WaitlistEntryDto } from "@/lib/types"

type FormState = "idle" | "loading" | "success" | "error"

const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

export function WaitlistEnrollForm({ eventId }: { eventId: number }) {
  const [email, setEmail] = useState("")
  const [state, setState] = useState<FormState>("idle")
  const [message, setMessage] = useState("")
  const [entry, setEntry] = useState<WaitlistEntryDto | null>(null)
  const [validationError, setValidationError] = useState("")

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setValidationError("")

    const trimmed = email.trim()

    if (!trimmed) {
      setValidationError("El email es requerido")
      return
    }

    if (!EMAIL_REGEX.test(trimmed)) {
      setValidationError("Ingresa un email válido")
      return
    }

    setState("loading")
    setMessage("")

    try {
      const result = await api.enrollInWaitlist(eventId, trimmed)
      setEntry(result)
      setState("success")
    } catch (err) {
      setState("error")
      if (err instanceof ApiError) {
        setMessage(err.message)
      } else {
        setMessage("Error al conectar con el servidor")
      }
    }
  }

  if (state === "success" && entry) {
    return (
      <div className="flex flex-col gap-4">
        <h2 className="text-lg font-semibold text-foreground">
          ¡Inscripción exitosa!
        </h2>
        <p className="text-sm text-muted-foreground">
          Te avisaremos a <span className="font-medium">{entry.buyerEmail}</span> cuando
          haya entradas disponibles.
        </p>
        <div className="rounded-lg bg-secondary/50 px-3 py-2 text-sm">
          Estado: <span className="font-medium">{entry.status}</span>
        </div>
      </div>
    )
  }

  return (
    <form onSubmit={handleSubmit} noValidate className="flex flex-col gap-4">
      <h2 className="text-lg font-semibold text-foreground">
        Lista de espera
      </h2>
      <p className="text-sm text-muted-foreground">
        No hay entradas disponibles. Inscríbete para recibir una notificación
        cuando se liberen.
      </p>

      <div className="flex flex-col gap-2">
        <Label htmlFor="waitlist-email">Tu email</Label>
        <Input
          id="waitlist-email"
          type="email"
          placeholder="tu@email.com"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          className="bg-secondary border-border"
        />
        {validationError && (
          <p className="text-xs text-destructive">{validationError}</p>
        )}
      </div>

      {state === "error" && message && (
        <p className="text-sm text-destructive">{message}</p>
      )}

      <Button
        type="submit"
        disabled={state === "loading"}
      >
        {state === "loading" ? "Procesando..." : "Unirse a la lista de espera"}
      </Button>
    </form>
  )
}
