import { describe, it, expect, vi, beforeEach, afterEach } from "vitest"
import { renderHook, act } from "@testing-library/react"
import { useWaitlistSse } from "@/hooks/use-waitlist-sse"

describe("useWaitlistSse", () => {
  let mockEventSource: {
    addEventListener: ReturnType<typeof vi.fn>
    close: ReturnType<typeof vi.fn>
    removeEventListener: ReturnType<typeof vi.fn>
  }

  beforeEach(() => {
    mockEventSource = {
      addEventListener: vi.fn(),
      close: vi.fn(),
      removeEventListener: vi.fn(),
    }
    vi.stubGlobal("EventSource", vi.fn(function (this: typeof mockEventSource) {
      Object.assign(this, mockEventSource)
    }) as unknown as typeof EventSource)
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it("creates EventSource with correct URL when email is provided", () => {
    const onEvent = vi.fn()
    renderHook(() => useWaitlistSse("buyer@test.com", onEvent))

    expect(EventSource).toHaveBeenCalledWith(
      expect.stringContaining("/api/waitlist/stream?email=buyer%40test.com")
    )
  })

  it("does not create EventSource when email is empty", () => {
    const onEvent = vi.fn()
    renderHook(() => useWaitlistSse("", onEvent))

    expect(EventSource).not.toHaveBeenCalled()
  })

  it("registers listeners for opportunity_activated and opportunity_expired events", () => {
    const onEvent = vi.fn()
    renderHook(() => useWaitlistSse("buyer@test.com", onEvent))

    const registeredEvents = mockEventSource.addEventListener.mock.calls.map((c: unknown[]) => c[0])
    expect(registeredEvents).toContain("opportunity_activated")
    expect(registeredEvents).toContain("opportunity_expired")
  })

  it("calls onEvent callback when opportunity_activated event fires", () => {
    const onEvent = vi.fn()
    renderHook(() => useWaitlistSse("buyer@test.com", onEvent))

    const activatedHandler = mockEventSource.addEventListener.mock.calls.find(
      (c: unknown[]) => c[0] === "opportunity_activated"
    )?.[1]

    const eventData = { opportunityId: 5, ticketId: 100, eventId: 42, expiresAt: "2026-04-07T11:15:00Z", remainingMinutes: 15 }

    act(() => {
      activatedHandler?.({ data: JSON.stringify(eventData) })
    })

    expect(onEvent).toHaveBeenCalledWith({
      type: "opportunity_activated",
      data: eventData,
    })
  })

  it("closes EventSource on unmount", () => {
    const onEvent = vi.fn()
    const { unmount } = renderHook(() => useWaitlistSse("buyer@test.com", onEvent))

    unmount()

    expect(mockEventSource.close).toHaveBeenCalled()
  })
})
