# Data Model: Waitlist Opportunity Status & Claim UI

**Feature**: 008-waitlist-opportunity-ui
**Date**: 2026-04-08

## Entidades existentes (sin cambios en BD)

### WaitlistOpportunity (existente)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| id | bigserial | PK |
| waitlist_entry_id | bigint | FK → waitlist_entries |
| ticket_id | bigint | FK → tickets |
| status | waitlist_opportunity_status | pending, active, consumed, expired, failed |
| activated_at | timestamptz | Momento de activación |
| expires_at | timestamptz | Momento de expiración |
| expired_at | timestamptz | Momento de expiración efectiva (nullable) |
| expiration_reason | varchar(100) | Motivo de expiración (nullable) |

**Transición relevante**: `active → consumed` (via `TransitionTo(Consumed)`)

### WaitlistEntry (existente)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| id | bigserial | PK |
| event_id | bigint | FK → events |
| buyer_email | varchar(255) | Email del comprador |
| status | waitlist_entry_status | active, consumed, expired |
| enrolled_at | timestamptz | Momento de inscripción |

## Nuevos tipos (solo código, no BD)

### Backend — Application Layer

```
ClaimOpportunityCommand
  ├── OpportunityId: long
  └── BuyerEmail: string

ClaimOpportunityResult
  ├── Type: ClaimOpportunityResultType (Claimed | NotFound | Expired | Forbidden)
  └── Response: ClaimOpportunityResponse? (solo cuando Type = Claimed)

ClaimOpportunityResponse
  ├── OpportunityId: long
  ├── TicketId: long
  ├── EventId: long
  └── Status: string ("consumed")
```

### Frontend — TypeScript

```
WaitlistOpportunityDto
  ├── id: number
  ├── ticketId: number
  ├── status: string ("active" | "consumed" | "expired")
  ├── activatedAt: string (ISO 8601)
  ├── expiresAt: string (ISO 8601)
  └── remainingMinutes: number (solo cuando active)

WaitlistStatusResponse
  ├── entry: WaitlistEntryDto
  └── opportunity: WaitlistOpportunityDto | null

ClaimOpportunityResponse
  ├── opportunityId: number
  ├── ticketId: number
  ├── eventId: number
  └── status: string ("consumed")
```

## Diagrama de transiciones de estado (solo la transición que introduce esta feature)

```
WaitlistOpportunity:
  [active] --claim--> [consumed]

  (Las transiciones pending→active, pending→failed, active→expired
   ya están implementadas en HU3 y HU6)
```
