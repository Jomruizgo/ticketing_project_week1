import { describe, it, expect, vi, beforeEach, afterEach } from "vitest"
import { api, ApiError } from "@/lib/api"

const CRUD_URL = "http://localhost:8002"

describe("enrollInWaitlist", () => {
  const originalFetch = globalThis.fetch

  beforeEach(() => {
    globalThis.fetch = vi.fn()
  })

  afterEach(() => {
    globalThis.fetch = originalFetch
  })

  const mockFetch = () => vi.mocked(globalThis.fetch)

  it("sends POST with correct payload", async () => {
    mockFetch().mockResolvedValue(
      new Response(
        JSON.stringify({
          id: 1,
          eventId: 3,
          buyerEmail: "test@example.com",
          status: "active",
          enrolledAt: "2026-04-08T10:00:00Z",
        }),
        { status: 201, headers: { "Content-Type": "application/json" } }
      )
    )

    await api.enrollInWaitlist(3, "test@example.com")

    expect(mockFetch()).toHaveBeenCalledWith(
      `${CRUD_URL}/api/waitlist/entries`,
      {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ eventId: 3, buyerEmail: "test@example.com" }),
      }
    )
  })

  it("returns WaitlistEntryDto on 201", async () => {
    const dto = {
      id: 1,
      eventId: 3,
      buyerEmail: "test@example.com",
      status: "active",
      enrolledAt: "2026-04-08T10:00:00Z",
    }

    mockFetch().mockResolvedValue(
      new Response(JSON.stringify(dto), {
        status: 201,
        headers: { "Content-Type": "application/json" },
      })
    )

    const result = await api.enrollInWaitlist(3, "test@example.com")
    expect(result).toEqual(dto)
  })

  it("throws ApiError with status 409 on duplicate", async () => {
    mockFetch().mockResolvedValue(
      new Response(JSON.stringify({ message: "Already enrolled" }), {
        status: 409,
      })
    )

    await expect(
      api.enrollInWaitlist(3, "dup@example.com")
    ).rejects.toThrow()

    try {
      await api.enrollInWaitlist(3, "dup@example.com")
    } catch (e) {
      expect(e).toBeInstanceOf(ApiError)
      expect((e as ApiError).status).toBe(409)
    }
  })

  it("throws ApiError with status 422 on closed", async () => {
    mockFetch().mockResolvedValue(
      new Response(JSON.stringify({ message: "Waitlist closed" }), {
        status: 422,
      })
    )

    await expect(
      api.enrollInWaitlist(3, "user@example.com")
    ).rejects.toThrow()

    try {
      await api.enrollInWaitlist(3, "user@example.com")
    } catch (e) {
      expect(e).toBeInstanceOf(ApiError)
      expect((e as ApiError).status).toBe(422)
    }
  })

  it("throws ApiError with status 404 on not found", async () => {
    mockFetch().mockResolvedValue(
      new Response(JSON.stringify({ message: "Not found" }), { status: 404 })
    )

    await expect(
      api.enrollInWaitlist(999, "user@example.com")
    ).rejects.toThrow()

    try {
      await api.enrollInWaitlist(999, "user@example.com")
    } catch (e) {
      expect(e).toBeInstanceOf(ApiError)
      expect((e as ApiError).status).toBe(404)
    }
  })

  it("throws ApiError on network error", async () => {
    mockFetch().mockRejectedValue(new TypeError("Failed to fetch"))

    await expect(
      api.enrollInWaitlist(3, "user@example.com")
    ).rejects.toThrow()

    try {
      await api.enrollInWaitlist(3, "user@example.com")
    } catch (e) {
      expect(e).toBeInstanceOf(ApiError)
      expect((e as ApiError).status).toBe(0)
    }
  })
})
