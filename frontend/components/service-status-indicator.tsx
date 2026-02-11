/**
 * Service Status Indicator Component
 * Demonstrates observability in distributed systems
 */

"use client"

import { useServiceHealth } from "@/hooks/use-service-health"
import { cn } from "@/lib/utils"

export function ServiceStatusIndicator() {
  const health = useServiceHealth()

  const statusDot = (isHealthy: boolean) => (
    <div
      className={cn("h-2.5 w-2.5 rounded-full", isHealthy ? "bg-green-500" : "bg-red-500")}
      title={isHealthy ? "Connected" : "Disconnected"}
    />
  )

  return (
    <div className="flex items-center gap-3 text-xs">
      {/* CRUD Service Status */}
      <div className="flex items-center gap-1.5">
        {statusDot(health.crudService)}
        <span className="text-muted-foreground">CRUD</span>
      </div>

      {/* Producer Service Status */}
      <div className="flex items-center gap-1.5">
        {statusDot(health.producerService)}
        <span className="text-muted-foreground">Producer</span>
      </div>

      {/* Health Check Status */}
      {health.isChecking && <span className="text-xs text-muted-foreground">Checking...</span>}

      {/* Last Check Time */}
      {health.lastChecked && (
        <span className="text-xs text-muted-foreground">
          Last: {health.lastChecked.toLocaleTimeString()}
        </span>
      )}
    </div>
  )
}
