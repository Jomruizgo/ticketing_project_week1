"use client"

import { useEffect, useRef } from "react"

const CRUD_URL = process.env.NEXT_PUBLIC_API_CRUD || "http://localhost:8002"

export interface WaitlistSseEvent {
  type: "opportunity_activated" | "opportunity_expired"
  data: unknown
}

export function useWaitlistSse(
  email: string,
  onEvent: (event: WaitlistSseEvent) => void
) {
  const onEventRef = useRef(onEvent)
  onEventRef.current = onEvent

  useEffect(() => {
    if (!email) return

    const url = `${CRUD_URL}/api/waitlist/stream?email=${encodeURIComponent(email)}`
    const source = new EventSource(url)

    const handleActivated = (e: MessageEvent) => {
      try {
        const data = JSON.parse(e.data)
        onEventRef.current({ type: "opportunity_activated", data })
      } catch {
        // Ignore malformed data
      }
    }

    const handleExpired = (e: MessageEvent) => {
      try {
        const data = JSON.parse(e.data)
        onEventRef.current({ type: "opportunity_expired", data })
      } catch {
        // Ignore malformed data
      }
    }

    source.addEventListener("opportunity_activated", handleActivated)
    source.addEventListener("opportunity_expired", handleExpired)

    return () => {
      source.close()
    }
  }, [email])
}
