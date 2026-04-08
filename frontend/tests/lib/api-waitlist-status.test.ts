import { describe, it, expect, vi, beforeEach } from "vitest"

const CRUD_URL = "http://localhost:8002"

describe("getWaitlistStatus", () => {
  beforeEach(() => {
    vi.resetAllMocks()
    globalThis.fetch = vi.fn()
  })

  it("calls GET /api/waitlist/entries with eventId and email", async () => {
    const mockResponse = {
      entry: { id: 1, eventId: 42, buyerEmail: "buyer@test.com", status: "active", enrolledAt: "2026-04-07T10:00:00Z" },
      opportunity: null,
    }
    ;(globalThis.fetch as ReturnType<typeof vi.fn>).mockResolvedValue({
      ok: true, status: 200, json: () => Promise.resolve(mockResponse),
    })

    const { api } = await import("@/lib/api")
    const result = await api.getWaitlistStatus(42, "buyer@test.com")

    expect(globalThis.fetch).toHaveBeenCalledWith(
      `${CRUD_URL}/api/waitlist/entries?eventId=42&email=buyer%40test.com`
    )
    expect(result).toEqual(mockResponse)
  })

  it("returns entry with opportunity when opportunity exists", async () => {
    const mockResponse = {
      entry: { id: 1, eventId: 42, buyerEmail: "buyer@test.com", status: "active", enrolledAt: "2026-04-07T10:00:00Z" },
      opportunity: { id: 5, ticketId: 100, status: "active", activatedAt: "2026-04-07T11:00:00Z", expiresAt: "2026-04-07T11:15:00Z", remainingMinutes: 12 },
    }
    ;(globalThis.fetch as ReturnType<typeof vi.fn>).mockResolvedValue({
      ok: true, status: 200, json: () => Promise.resolve(mockResponse),
    })

    const { api } = await import("@/lib/api")
    const result = await api.getWaitlistStatus(42, "buyer@test.com")

    expect(result.opportunity).not.toBeNull()
    expect(result.opportunity!.status).toBe("active")
  })

  it("throws ApiError on 404 response", async () => {
    ;(globalThis.fetch as ReturnType<typeof vi.fn>).mockResolvedValue({
      ok: false, status: 404, text: () => Promise.resolve('{"detail":"Not found"}'),
    })

    const { api, ApiError } = await import("@/lib/api")

    await expect(api.getWaitlistStatus(42, "nobody@test.com")).rejects.toThrow(ApiError)
  })
})

describe("claimOpportunity", () => {
  beforeEach(() => {
    vi.resetAllMocks()
    globalThis.fetch = vi.fn()
  })

  it("calls POST /api/waitlist/opportunities/{id}/claim with buyerEmail", async () => {
    const mockResponse = { opportunityId: 5, ticketId: 100, eventId: 42, status: "consumed" }
    ;(globalThis.fetch as ReturnType<typeof vi.fn>).mockResolvedValue({
      ok: true, status: 200, json: () => Promise.resolve(mockResponse),
    })

    const { api } = await import("@/lib/api")
    const result = await api.claimOpportunity(5, "buyer@test.com")

    expect(globalThis.fetch).toHaveBeenCalledWith(
      `${CRUD_URL}/api/waitlist/opportunities/5/claim`,
      {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ buyerEmail: "buyer@test.com" }),
      }
    )
    expect(result).toEqual(mockResponse)
  })

  it("throws ApiError on 409 response", async () => {
    ;(globalThis.fetch as ReturnType<typeof vi.fn>).mockResolvedValue({
      ok: false, status: 409, text: () => Promise.resolve('{"detail":"Expired"}'),
    })

    const { api, ApiError } = await import("@/lib/api")

    await expect(api.claimOpportunity(5, "buyer@test.com")).rejects.toThrow(ApiError)
  })

  it("throws ApiError on 403 response", async () => {
    ;(globalThis.fetch as ReturnType<typeof vi.fn>).mockResolvedValue({
      ok: false, status: 403, text: () => Promise.resolve('{"detail":"Forbidden"}'),
    })

    const { api, ApiError } = await import("@/lib/api")

    await expect(api.claimOpportunity(5, "wrong@test.com")).rejects.toThrow(ApiError)
  })

  it("throws ApiError on 404 response", async () => {
    ;(globalThis.fetch as ReturnType<typeof vi.fn>).mockResolvedValue({
      ok: false, status: 404, text: () => Promise.resolve('{"detail":"Not found"}'),
    })

    const { api, ApiError } = await import("@/lib/api")

    await expect(api.claimOpportunity(999, "buyer@test.com")).rejects.toThrow(ApiError)
  })

  it("throws ApiError on network failure", async () => {
    ;(globalThis.fetch as ReturnType<typeof vi.fn>).mockRejectedValue(new TypeError("Failed to fetch"))

    const { api, ApiError } = await import("@/lib/api")

    await expect(api.claimOpportunity(5, "buyer@test.com")).rejects.toThrow(ApiError)
  })
})
