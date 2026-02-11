"use client"

import { use } from "react"
import { AppHeader } from "@/components/app-header"
import { EventDetailView } from "@/components/event-detail-view"

export default function EventPage({
  params,
}: {
  params: Promise<{ id: string }>
}) {
  const { id } = use(params)
  const eventId = Number(id)

  return (
    <div className="flex min-h-screen flex-col">
      <AppHeader />
      <main className="mx-auto w-full max-w-7xl flex-1 px-6 py-8">
        <EventDetailView eventId={eventId} />
      </main>
    </div>
  )
}
