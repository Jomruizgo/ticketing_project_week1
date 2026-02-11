/**
 * Header Component with Service Status
 * Demonstrates distributed system observability
 */

"use client"

import Link from "next/link"
import { ServiceStatusIndicator } from "./service-status-indicator"

export function Header() {
  return (
    <header className="sticky top-0 z-40 border-b border-border bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
      <div className="container flex h-14 max-w-screen-2xl items-center justify-between">
        <div className="flex items-center gap-6">
          <Link href="/" className="font-bold text-lg">
            🎟️ Ticketing
          </Link>

          <nav className="hidden md:flex gap-4">
            <Link href="/buy" className="text-sm text-muted-foreground hover:text-foreground">
              Comprar Tickets
            </Link>
          </nav>
        </div>

        <div className="flex items-center gap-4">
          <ServiceStatusIndicator />
        </div>
      </div>
    </header>
  )
}
