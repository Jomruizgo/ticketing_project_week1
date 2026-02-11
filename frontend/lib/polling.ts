/**
 * Polling utilities with exponential backoff
 * Demonstrates intelligent async polling for distributed systems
 */

interface PollOptions {
  maxAttempts?: number
  initialDelay?: number
  maxDelay?: number
  multiplier?: number
}

const DEFAULT_OPTIONS: Required<PollOptions> = {
  maxAttempts: 20,
  initialDelay: 100,
  maxDelay: 2000,
  multiplier: 1.5,
}

/**
 * Poll a condition with exponential backoff
 * Useful for async operations like reservation confirmation
 */
export async function pollUntilCondition<T>(
  fn: () => Promise<T>,
  condition: (result: T) => boolean,
  options: PollOptions = {}
): Promise<T> {
  const opts = { ...DEFAULT_OPTIONS, ...options }
  let attempt = 0
  let delay = opts.initialDelay

  while (attempt < opts.maxAttempts) {
    try {
      const result = await fn()
      if (condition(result)) {
        return result
      }
    } catch (error) {
      // Continue polling even on error
      console.warn(`Poll attempt ${attempt + 1} failed:`, error)
    }

    attempt++
    if (attempt < opts.maxAttempts) {
      await new Promise((resolve) => setTimeout(resolve, delay))
      delay = Math.min(delay * opts.multiplier, opts.maxDelay)
    }
  }

  throw new Error(
    `Condition not met after ${opts.maxAttempts} attempts (${Math.round(delay)}ms max delay)`
  )
}

/**
 * Wait for a ticket to be reserved with smart polling
 * Demonstrates how to handle async backend operations
 */
export async function waitForTicketReservation(
  getTicket: (id: number) => Promise<{ status: string }>,
  ticketId: number,
  maxWaitMs: number = 10000
): Promise<boolean> {
  const startTime = Date.now()

  try {
    await pollUntilCondition(
      () => getTicket(ticketId),
      (ticket) => ticket.status === "reserved",
      {
        maxAttempts: 20,
        initialDelay: 100,
        maxDelay: 1000,
      }
    )
    return true
  } catch (error) {
    const elapsed = Date.now() - startTime
    throw new Error(
      `Reservation timeout after ${elapsed}ms: ${error instanceof Error ? error.message : "Unknown error"}`
    )
  }
}

/**
 * Retry a function with exponential backoff
 * Useful for flaky network operations
 */
export async function retryWithBackoff<T>(
  fn: () => Promise<T>,
  maxRetries: number = 3,
  initialDelay: number = 1000
): Promise<T> {
  let lastError: Error | unknown

  for (let attempt = 0; attempt < maxRetries; attempt++) {
    try {
      return await fn()
    } catch (error) {
      lastError = error
      if (attempt < maxRetries - 1) {
        const delay = initialDelay * Math.pow(2, attempt)
        console.warn(
          `Attempt ${attempt + 1} failed, retrying in ${delay}ms:`,
          error
        )
        await new Promise((resolve) => setTimeout(resolve, delay))
      }
    }
  }

  throw lastError
}
