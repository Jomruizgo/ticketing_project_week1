import { describe, it, expect, vi, beforeEach, afterEach } from "vitest"
import { render, screen, waitFor, act } from "@testing-library/react"
import userEvent from "@testing-library/user-event"

vi.mock("@/lib/api", () => ({
  api: {
    getWaitlistStatus: vi.fn(),
    claimOpportunity: vi.fn(),
  },
  ApiError: class ApiError extends Error {
    status: number
    constructor(status: number, message: string) {
      super(message)
      this.status = status
      this.name = "ApiError"
    }
  },
}))

vi.mock("sonner", () => ({
  toast: { error: vi.fn(), success: vi.fn() },
}))

vi.mock("@/hooks/use-waitlist-sse", () => ({
  useWaitlistSse: vi.fn(),
}))

import { WaitlistStatus } from "@/components/waitlist-status"
import { api, ApiError } from "@/lib/api"
import { toast } from "sonner"
import { useWaitlistSse } from "@/hooks/use-waitlist-sse"

const mockGetWaitlistStatus = api.getWaitlistStatus as ReturnType<typeof vi.fn>
const mockClaimOpportunity = api.claimOpportunity as ReturnType<typeof vi.fn>
const mockUseWaitlistSse = useWaitlistSse as ReturnType<typeof vi.fn>

describe("WaitlistStatus", () => {
  const user = userEvent.setup()

  beforeEach(() => {
    vi.resetAllMocks()
    mockUseWaitlistSse.mockImplementation(() => {})
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  // ─── US1: "En espera" state ────────────────────────────

  describe("US1: status query - en espera", () => {
    it("renders email input form for status query", () => {
      render(<WaitlistStatus eventId={42} />)
      expect(screen.getByPlaceholderText(/email/i)).toBeInTheDocument()
      expect(screen.getByRole("button", { name: /consultar/i })).toBeInTheDocument()
    })

    it("shows 'En espera' badge when entry is active without opportunity", async () => {
      mockGetWaitlistStatus.mockResolvedValue({
        entry: { id: 1, eventId: 42, buyerEmail: "buyer@test.com", status: "active", enrolledAt: "2026-04-07T10:00:00Z" },
        opportunity: null,
      })

      render(<WaitlistStatus eventId={42} />)

      await user.type(screen.getByPlaceholderText(/email/i), "buyer@test.com")
      await user.click(screen.getByRole("button", { name: /consultar/i }))

      await waitFor(() => {
        expect(screen.getByText(/en espera/i)).toBeInTheDocument()
      })

      expect(screen.queryByText(/avanzar al pago/i)).not.toBeInTheDocument()
    })
  })

  // ─── US2: Active opportunity with countdown ───────────

  describe("US2: active opportunity with countdown and claim", () => {
    it("shows countdown in MM:SS format when opportunity is active", async () => {
      vi.useFakeTimers({ shouldAdvanceTime: true })
      const expiresAt = new Date(Date.now() + 12 * 60 * 1000).toISOString()

      mockGetWaitlistStatus.mockResolvedValue({
        entry: { id: 1, eventId: 42, buyerEmail: "buyer@test.com", status: "active", enrolledAt: "2026-04-07T10:00:00Z" },
        opportunity: { id: 5, ticketId: 100, status: "active", activatedAt: "2026-04-07T11:00:00Z", expiresAt, remainingMinutes: 12 },
      })

      render(<WaitlistStatus eventId={42} initialEmail="buyer@test.com" />)

      // Initial countdown should be 12:00 (MM:SS format)
      await waitFor(() => {
        expect(screen.getByText("12:00")).toBeInTheDocument()
      })

      // Advance 1 second and verify countdown decrements
      act(() => { vi.advanceTimersByTime(1000) })

      await waitFor(() => {
        expect(screen.getByText("11:59")).toBeInTheDocument()
      })

      expect(screen.getByRole("button", { name: /avanzar al pago/i })).toBeEnabled()
    })

    it("disables claim button when countdown reaches 00:00", async () => {
      vi.useFakeTimers({ shouldAdvanceTime: true })
      const expiresAt = new Date(Date.now() + 2000).toISOString()

      mockGetWaitlistStatus.mockResolvedValue({
        entry: { id: 1, eventId: 42, buyerEmail: "buyer@test.com", status: "active", enrolledAt: "2026-04-07T10:00:00Z" },
        opportunity: { id: 5, ticketId: 100, status: "active", activatedAt: "2026-04-07T11:00:00Z", expiresAt, remainingMinutes: 0 },
      })

      render(<WaitlistStatus eventId={42} initialEmail="buyer@test.com" />)

      await waitFor(() => {
        expect(screen.getByRole("button", { name: /avanzar al pago/i })).toBeInTheDocument()
      })

      act(() => { vi.advanceTimersByTime(3000) })

      await waitFor(() => {
        expect(screen.getByText("00:00")).toBeInTheDocument()
        expect(screen.getByRole("button", { name: /avanzar al pago/i })).toBeDisabled()
      })
    })

    it("redirects to payment flow on successful claim", async () => {
      mockGetWaitlistStatus.mockResolvedValue({
        entry: { id: 1, eventId: 42, buyerEmail: "buyer@test.com", status: "active", enrolledAt: "2026-04-07T10:00:00Z" },
        opportunity: { id: 5, ticketId: 100, status: "active", activatedAt: "2026-04-07T11:00:00Z", expiresAt: new Date(Date.now() + 600000).toISOString(), remainingMinutes: 10 },
      })
      mockClaimOpportunity.mockResolvedValue({ opportunityId: 5, ticketId: 100, eventId: 42, status: "consumed" })
      const onClaim = vi.fn()

      render(<WaitlistStatus eventId={42} initialEmail="buyer@test.com" onClaimSuccess={onClaim} />)

      await waitFor(() => {
        expect(screen.getByRole("button", { name: /avanzar al pago/i })).toBeInTheDocument()
      })

      await user.click(screen.getByRole("button", { name: /avanzar al pago/i }))

      await waitFor(() => {
        expect(mockClaimOpportunity).toHaveBeenCalledWith(5, "buyer@test.com")
        expect(onClaim).toHaveBeenCalledWith(100, 42)
      })
    })

    it("transitions to active state on SSE opportunity_activated event", async () => {
      let sseCallback: ((event: { type: string; data: unknown }) => void) | undefined

      mockUseWaitlistSse.mockImplementation((_email: string, onEvent: (event: { type: string; data: unknown }) => void) => {
        sseCallback = onEvent
      })

      mockGetWaitlistStatus.mockResolvedValue({
        entry: { id: 1, eventId: 42, buyerEmail: "buyer@test.com", status: "active", enrolledAt: "2026-04-07T10:00:00Z" },
        opportunity: null,
      })

      render(<WaitlistStatus eventId={42} initialEmail="buyer@test.com" />)

      await waitFor(() => {
        expect(screen.getByText(/en espera/i)).toBeInTheDocument()
      })

      act(() => {
        sseCallback?.({
          type: "opportunity_activated",
          data: { opportunityId: 5, ticketId: 100, eventId: 42, expiresAt: new Date(Date.now() + 900000).toISOString(), remainingMinutes: 15 },
        })
      })

      await waitFor(() => {
        expect(screen.getByRole("button", { name: /avanzar al pago/i })).toBeInTheDocument()
      })
    })
  })

  // ─── US3: Expired opportunity ─────────────────────────

  describe("US3: expired opportunity", () => {
    it("shows 'Oportunidad expirada' badge with no actions", async () => {
      mockGetWaitlistStatus.mockResolvedValue({
        entry: { id: 1, eventId: 42, buyerEmail: "buyer@test.com", status: "active", enrolledAt: "2026-04-07T10:00:00Z" },
        opportunity: { id: 5, ticketId: 100, status: "expired", activatedAt: "2026-04-07T11:00:00Z", expiresAt: "2026-04-07T11:15:00Z", remainingMinutes: 0 },
      })

      render(<WaitlistStatus eventId={42} initialEmail="buyer@test.com" />)

      await waitFor(() => {
        expect(screen.getByText(/oportunidad expirada/i)).toBeInTheDocument()
      })

      expect(screen.queryByRole("button", { name: /avanzar al pago/i })).not.toBeInTheDocument()
    })

    it("transitions to expired on SSE opportunity_expired event", async () => {
      let sseCallback: ((event: { type: string; data: unknown }) => void) | undefined

      mockUseWaitlistSse.mockImplementation((_email: string, onEvent: (event: { type: string; data: unknown }) => void) => {
        sseCallback = onEvent
      })

      mockGetWaitlistStatus.mockResolvedValue({
        entry: { id: 1, eventId: 42, buyerEmail: "buyer@test.com", status: "active", enrolledAt: "2026-04-07T10:00:00Z" },
        opportunity: { id: 5, ticketId: 100, status: "active", activatedAt: "2026-04-07T11:00:00Z", expiresAt: new Date(Date.now() + 600000).toISOString(), remainingMinutes: 10 },
      })

      render(<WaitlistStatus eventId={42} initialEmail="buyer@test.com" />)

      await waitFor(() => {
        expect(screen.getByRole("button", { name: /avanzar al pago/i })).toBeInTheDocument()
      })

      act(() => {
        sseCallback?.({ type: "opportunity_expired", data: { opportunityId: 5, eventId: 42, reason: "timeout" } })
      })

      await waitFor(() => {
        expect(screen.getByText(/oportunidad expirada/i)).toBeInTheDocument()
        expect(screen.queryByRole("button", { name: /avanzar al pago/i })).not.toBeInTheDocument()
      })
    })
  })

  // ─── US4: Consumed opportunity ────────────────────────

  describe("US4: consumed opportunity", () => {
    it("shows 'Oportunidad utilizada' badge with no actions", async () => {
      mockGetWaitlistStatus.mockResolvedValue({
        entry: { id: 1, eventId: 42, buyerEmail: "buyer@test.com", status: "consumed", enrolledAt: "2026-04-07T10:00:00Z" },
        opportunity: { id: 5, ticketId: 100, status: "consumed", activatedAt: "2026-04-07T11:00:00Z", expiresAt: "2026-04-07T11:15:00Z", remainingMinutes: 0 },
      })

      render(<WaitlistStatus eventId={42} initialEmail="buyer@test.com" />)

      await waitFor(() => {
        expect(screen.getByText(/oportunidad utilizada/i)).toBeInTheDocument()
      })

      expect(screen.queryByRole("button", { name: /avanzar al pago/i })).not.toBeInTheDocument()
    })
  })

  // ─── US5: Claim 409 ──────────────────────────────────

  describe("US5: claim 409 error", () => {
    it("shows expiration toast on 409 and transitions to expired state", async () => {
      mockGetWaitlistStatus.mockResolvedValue({
        entry: { id: 1, eventId: 42, buyerEmail: "buyer@test.com", status: "active", enrolledAt: "2026-04-07T10:00:00Z" },
        opportunity: { id: 5, ticketId: 100, status: "active", activatedAt: "2026-04-07T11:00:00Z", expiresAt: new Date(Date.now() + 600000).toISOString(), remainingMinutes: 10 },
      })
      mockClaimOpportunity.mockRejectedValue(new ApiError(409, "Opportunity is no longer active."))

      render(<WaitlistStatus eventId={42} initialEmail="buyer@test.com" />)

      await waitFor(() => {
        expect(screen.getByRole("button", { name: /avanzar al pago/i })).toBeInTheDocument()
      })

      await user.click(screen.getByRole("button", { name: /avanzar al pago/i }))

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledWith("La oportunidad ya no está activa")
      })
    })
  })

  // ─── US6: Claim 403 ──────────────────────────────────

  describe("US6: claim 403 error", () => {
    it("shows forbidden toast on 403", async () => {
      mockGetWaitlistStatus.mockResolvedValue({
        entry: { id: 1, eventId: 42, buyerEmail: "buyer@test.com", status: "active", enrolledAt: "2026-04-07T10:00:00Z" },
        opportunity: { id: 5, ticketId: 100, status: "active", activatedAt: "2026-04-07T11:00:00Z", expiresAt: new Date(Date.now() + 600000).toISOString(), remainingMinutes: 10 },
      })
      mockClaimOpportunity.mockRejectedValue(new ApiError(403, "Forbidden"))

      render(<WaitlistStatus eventId={42} initialEmail="buyer@test.com" />)

      await waitFor(() => {
        expect(screen.getByRole("button", { name: /avanzar al pago/i })).toBeInTheDocument()
      })

      await user.click(screen.getByRole("button", { name: /avanzar al pago/i }))

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledWith("Esta oportunidad no pertenece al comprador indicado")
      })
    })
  })

  // ─── US7: 404 errors ─────────────────────────────────

  describe("US7: 404 errors", () => {
    it("shows 'no inscripción' message on status query 404", async () => {
      mockGetWaitlistStatus.mockRejectedValue(new ApiError(404, "Not found"))

      render(<WaitlistStatus eventId={42} />)

      await user.type(screen.getByPlaceholderText(/email/i), "nobody@test.com")
      await user.click(screen.getByRole("button", { name: /consultar/i }))

      await waitFor(() => {
        expect(screen.getByText(/no existe inscripción/i)).toBeInTheDocument()
      })
    })

    it("shows error toast on claim 404", async () => {
      mockGetWaitlistStatus.mockResolvedValue({
        entry: { id: 1, eventId: 42, buyerEmail: "buyer@test.com", status: "active", enrolledAt: "2026-04-07T10:00:00Z" },
        opportunity: { id: 5, ticketId: 100, status: "active", activatedAt: "2026-04-07T11:00:00Z", expiresAt: new Date(Date.now() + 600000).toISOString(), remainingMinutes: 10 },
      })
      mockClaimOpportunity.mockRejectedValue(new ApiError(404, "Not found"))

      render(<WaitlistStatus eventId={42} initialEmail="buyer@test.com" />)

      await waitFor(() => {
        expect(screen.getByRole("button", { name: /avanzar al pago/i })).toBeInTheDocument()
      })

      await user.click(screen.getByRole("button", { name: /avanzar al pago/i }))

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledWith("Oportunidad no encontrada")
      })
    })
  })

  // ─── FR-016: Network errors ───────────────────────────

  describe("FR-016: network and 5xx errors", () => {
    it("shows generic error on network failure during status query", async () => {
      mockGetWaitlistStatus.mockRejectedValue(new ApiError(0, "Error de red"))

      render(<WaitlistStatus eventId={42} />)

      await user.type(screen.getByPlaceholderText(/email/i), "buyer@test.com")
      await user.click(screen.getByRole("button", { name: /consultar/i }))

      await waitFor(() => {
        expect(screen.getByText(/error.*conectar|error.*red|error.*servidor/i)).toBeInTheDocument()
      })
    })

    it("shows generic error toast on network failure during claim", async () => {
      mockGetWaitlistStatus.mockResolvedValue({
        entry: { id: 1, eventId: 42, buyerEmail: "buyer@test.com", status: "active", enrolledAt: "2026-04-07T10:00:00Z" },
        opportunity: { id: 5, ticketId: 100, status: "active", activatedAt: "2026-04-07T11:00:00Z", expiresAt: new Date(Date.now() + 600000).toISOString(), remainingMinutes: 10 },
      })
      mockClaimOpportunity.mockRejectedValue(new ApiError(0, "Error de red"))

      render(<WaitlistStatus eventId={42} initialEmail="buyer@test.com" />)

      await waitFor(() => {
        expect(screen.getByRole("button", { name: /avanzar al pago/i })).toBeInTheDocument()
      })

      await user.click(screen.getByRole("button", { name: /avanzar al pago/i }))

      await waitFor(() => {
        expect(toast.error).toHaveBeenCalledWith(expect.stringMatching(/error/i))
      })
    })
  })
})
