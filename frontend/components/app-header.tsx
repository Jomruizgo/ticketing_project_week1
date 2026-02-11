"use client"

import Link from "next/link"
import { Ticket, Activity } from "lucide-react"

export function AppHeader() {
  return (
    <header className="sticky top-0 z-50 border-b border-border bg-background/80 backdrop-blur-md">
      <div className="mx-auto flex max-w-7xl items-center justify-between px-6 py-4">
        <Link href="/" className="flex items-center gap-3">
          <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-primary">
            <Ticket className="h-5 w-5 text-primary-foreground" />
          </div>
          <span className="text-lg font-semibold text-foreground">
            Ticketing
          </span>
        </Link>
        <nav className="flex items-center gap-6">
          <Link
            href="/"
            className="flex items-center gap-2 text-sm font-medium text-muted-foreground transition-colors hover:text-foreground"
          >
            <Activity className="h-4 w-4" />
            Eventos
          </Link>
        </nav>
      </div>
    </header>
  )
}
