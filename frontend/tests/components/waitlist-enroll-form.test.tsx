import { describe, it, expect, vi, beforeEach } from "vitest"
import { render, screen, waitFor } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { WaitlistEnrollForm } from "@/components/waitlist-enroll-form"
import { api, ApiError } from "@/lib/api"

vi.mock("@/lib/api", () => ({
  api: {
    enrollInWaitlist: vi.fn(),
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

const mockEnroll = vi.mocked(api.enrollInWaitlist)

describe("WaitlistEnrollForm", () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it("renders email input and submit button", () => {
    render(<WaitlistEnrollForm eventId={1} />)

    expect(screen.getByLabelText(/email/i)).toBeInTheDocument()
    expect(
      screen.getByRole("button", { name: /unirse a la lista de espera/i })
    ).toBeInTheDocument()
  })

  it("validates empty email", async () => {
    const user = userEvent.setup()
    render(<WaitlistEnrollForm eventId={1} />)

    await user.click(
      screen.getByRole("button", { name: /unirse a la lista de espera/i })
    )

    expect(mockEnroll).not.toHaveBeenCalled()
    expect(screen.getByText(/email es requerido/i)).toBeInTheDocument()
  })

  it("validates invalid email format", async () => {
    const user = userEvent.setup()
    render(<WaitlistEnrollForm eventId={1} />)

    await user.type(screen.getByLabelText(/email/i), "not-an-email")
    await user.click(
      screen.getByRole("button", { name: /unirse a la lista de espera/i })
    )

    expect(mockEnroll).not.toHaveBeenCalled()
    expect(screen.getByText(/email.*válido/i)).toBeInTheDocument()
  })

  it("calls enrollInWaitlist on valid submit", async () => {
    const user = userEvent.setup()
    mockEnroll.mockResolvedValue({
      id: 1,
      eventId: 5,
      buyerEmail: "test@example.com",
      status: "active",
      enrolledAt: "2026-04-08T10:00:00Z",
    })

    render(<WaitlistEnrollForm eventId={5} />)

    await user.type(screen.getByLabelText(/email/i), "test@example.com")
    await user.click(
      screen.getByRole("button", { name: /unirse a la lista de espera/i })
    )

    expect(mockEnroll).toHaveBeenCalledWith(5, "test@example.com")
  })

  it("shows success confirmation on 201", async () => {
    const user = userEvent.setup()
    mockEnroll.mockResolvedValue({
      id: 1,
      eventId: 5,
      buyerEmail: "test@example.com",
      status: "active",
      enrolledAt: "2026-04-08T10:00:00Z",
    })

    render(<WaitlistEnrollForm eventId={5} />)

    await user.type(screen.getByLabelText(/email/i), "test@example.com")
    await user.click(
      screen.getByRole("button", { name: /unirse a la lista de espera/i })
    )

    await waitFor(() => {
      expect(screen.getByText(/inscripción exitosa/i)).toBeInTheDocument()
    })
    expect(screen.getByText(/active/i)).toBeInTheDocument()
  })

  it("shows informative message on 409 duplicate", async () => {
    const user = userEvent.setup()
    mockEnroll.mockRejectedValue(
      new ApiError(409, "Ya tienes una inscripción activa")
    )

    render(<WaitlistEnrollForm eventId={5} />)

    await user.type(screen.getByLabelText(/email/i), "dup@example.com")
    await user.click(
      screen.getByRole("button", { name: /unirse a la lista de espera/i })
    )

    await waitFor(() => {
      expect(
        screen.getByText(/ya tienes una inscripción activa/i)
      ).toBeInTheDocument()
    })
  })

  it("shows closed message on 422", async () => {
    const user = userEvent.setup()
    mockEnroll.mockRejectedValue(
      new ApiError(422, "La lista de espera ya cerró")
    )

    render(<WaitlistEnrollForm eventId={5} />)

    await user.type(screen.getByLabelText(/email/i), "user@example.com")
    await user.click(
      screen.getByRole("button", { name: /unirse a la lista de espera/i })
    )

    await waitFor(() => {
      expect(
        screen.getByText(/la lista de espera ya cerró/i)
      ).toBeInTheDocument()
    })
  })

  it("shows generic error on network failure", async () => {
    const user = userEvent.setup()
    mockEnroll.mockRejectedValue(new Error("Failed to fetch"))

    render(<WaitlistEnrollForm eventId={5} />)

    await user.type(screen.getByLabelText(/email/i), "user@example.com")
    await user.click(
      screen.getByRole("button", { name: /unirse a la lista de espera/i })
    )

    await waitFor(() => {
      expect(screen.getByText(/error/i)).toBeInTheDocument()
    })
  })

  it("disables button during loading", async () => {
    const user = userEvent.setup()
    mockEnroll.mockImplementation(
      () => new Promise((resolve) => setTimeout(resolve, 5000))
    )

    render(<WaitlistEnrollForm eventId={5} />)

    await user.type(screen.getByLabelText(/email/i), "user@example.com")
    await user.click(
      screen.getByRole("button", { name: /unirse a la lista de espera/i })
    )

    expect(
      screen.getByRole("button", { name: /unirse a la lista de espera|procesando/i })
    ).toBeDisabled()
  })
})
