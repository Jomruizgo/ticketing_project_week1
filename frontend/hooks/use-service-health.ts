/**
 * Hook para monitorear la salud de los servicios
 * Demuestra observabilidad en arquitectura distribuida
 */

"use client"

import { useEffect, useState } from "react"
import { api } from "@/lib/api"

export interface ServiceHealth {
  crudService: boolean
  producerService: boolean
  lastChecked: Date | null
  isChecking: boolean
}

const HEALTH_CHECK_INTERVAL = 30000 // 30 segundos

export function useServiceHealth() {
  const [health, setHealth] = useState<ServiceHealth>({
    crudService: true,
    producerService: true,
    lastChecked: null,
    isChecking: true,
  })

  useEffect(() => {
    let interval: NodeJS.Timeout

    const checkHealth = async () => {
      try {
        setHealth((prev) => ({ ...prev, isChecking: true }))

        // Check both services in parallel
        const [crudOk, producerOk] = await Promise.all([
          api
            .healthCrud()
            .then(() => true)
            .catch(() => false),
          api
            .healthProducer()
            .then(() => true)
            .catch(() => false),
        ])

        setHealth({
          crudService: crudOk,
          producerService: producerOk,
          lastChecked: new Date(),
          isChecking: false,
        })
      } catch (error) {
        console.warn("Health check failed:", error)
        setHealth((prev) => ({
          ...prev,
          isChecking: false,
        }))
      }
    }

    // Check immediately
    checkHealth()

    // Then check periodically
    interval = setInterval(checkHealth, HEALTH_CHECK_INTERVAL)

    return () => clearInterval(interval)
  }, [])

  return health
}
