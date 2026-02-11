"use client"

import Link from "next/link"
import { ArrowLeft } from "lucide-react"
import { AppHeader } from "@/components/app-header"
import { EventsList } from "@/components/events-list"
import { CreateEventDialog } from "@/components/create-event-dialog"
import { StatsOverview } from "@/components/stats-overview"
import { Button } from "@/components/ui/button"

export default function AdminPage() {
  return (
    <div className="flex min-h-screen flex-col">
      <AppHeader />
      <main className="mx-auto w-full max-w-7xl flex-1 px-6 py-8">
        <div className="flex flex-col gap-8">
          <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
            <div className="flex items-center gap-4">
              <Link href="/">
                <Button variant="outline" size="sm" className="border-border">
                  <ArrowLeft className="h-4 w-4" />
                  Volver
                </Button>
              </Link>
              <div>
                <h1 className="text-2xl font-bold tracking-tight text-foreground">
                  Gestión de Eventos
                </h1>
                <p className="text-sm text-muted-foreground">
                  Crea y administra eventos y tickets
                </p>
              </div>
            </div>
            <CreateEventDialog />
          </div>

          <StatsOverview />

          <EventsList />
        </div>
      </main>
    </div>
  )
}
