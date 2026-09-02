# PlanVest interview guide

## Resume bullets

- Built PlanVest, a Dockerized Next.js and ASP.NET Core investment-planning
  application with JWT authentication, server-side logout invalidation,
  PostgreSQL migrations, and per-user authorization verified by real HTTP tests.
- Implemented decimal portfolio allocation, a versioned 100-point risk model,
  generic allocation-gap analysis, and monthly-compounding goal projections in
  tested C# services, with a responsive TypeScript dashboard and synthetic demo.

## 60-second introduction

PlanVest is an educational investment-planning workspace I built to demonstrate a
complete, security-conscious product slice. A user can register or open an
isolated demo, manage accounts and holdings, see calculated allocation, complete
an explainable risk questionnaire, compare the portfolio with a generic model,
and model a financial goal. The frontend is Next.js and TypeScript; the API is
ASP.NET Core with EF Core, PostgreSQL in production, and SQLite for local setup. I deliberately kept financial math on the
server using decimal arithmetic, filtered every owned query by the authenticated
user, and made logout invalidate tokens through a database-backed token version.
The test suite includes real Kestrel workflows against both SQLite and a freshly
migrated PostgreSQL database, not only mocked services.

## Five-minute walkthrough

1. **Open the synthetic demo.** Show that no email or financial account is needed
   and explain why every demo click creates a unique workspace.
2. **Edit the portfolio.** Add a holding and show total value and allocation update
   from the server response. Point to account/holding ownership checks.
3. **Complete risk assessment.** Explain seven deterministic questions, the
   versioned answer map, the 0–35 / 36–70 / 71–100 thresholds, and the disclaimer.
4. **Show the plan.** Connect the current allocation, model target, percentage
   difference, and approximate dollar gap without presenting a trade recommendation.
5. **Create a goal and run the simulator.** Explain decimal math, monthly
   compounding, the 0% branch, input bounds, and non-guarantee language.
6. **Open tests and CI.** Highlight logout invalidation, cross-user `404`, fresh
   migrations, risk boundaries, and production frontend build.

## Important trade-offs

### Why a short-lived JWT instead of a cookie?

It keeps a split-origin local Next.js/API demo easy to inspect. The server checks a
token version, so logout is real rather than merely deleting browser state. For a
public financial application I would move authentication to an HttpOnly, Secure,
SameSite cookie through a same-origin backend-for-frontend or use a managed
identity provider.

### Why manual prices?

Market-data providers add API keys, rate limits, symbol ambiguity, stale-price
rules, and licensing concerns that do not strengthen this MVP's core engineering
story. Manual synthetic prices make calculations deterministic and the demo
reliable.

### Why SQLite?

It gives a reviewer one-command local startup while PostgreSQL remains the
production provider. This keeps onboarding simple without accepting SQLite's
single-writer and single-instance limitations in the deployed architecture.

### Why an aggregate dashboard endpoint?

The UI needs accounts, portfolio totals, latest risk result, comparison, and goals
together. One read endpoint avoids loading-state waterfalls. Mutations stay as
small REST resources, so the aggregate does not become a command API.

## Debugging story

The first real SQLite integration run returned a dashboard 500 even though unit
tests passed. The cause was an EF Core query ordering by `DateTimeOffset`; SQLite
can store that value but cannot translate its ordering expression. The fix was to
retain the security-critical user filter in SQL, materialize only that user's
small result set, and order it in memory. I added the real-process integration
test to keep migrations and provider-specific query behaviour covered. This is a
useful example of why an in-memory provider alone is insufficient.

## Likely follow-up questions

- **How is IDOR prevented?** Every account, holding, transaction, goal, and risk
  query combines the resource key with the JWT subject; tests attempt a cross-user
  account read and expect `404`.
- **How does logout invalidate a stateless token?** JWTs contain a token-version
  claim. Middleware compares it with the user's current database version, and
  logout increments that version.
- **Why `decimal`?** Binary floating point cannot exactly represent many decimal
  monetary values. Persisted amounts and backend formulas use `decimal`, with an
  explicit midpoint rounding policy for money.
- **What would you add before broader production use?** Managed identity or secure
  cookies, email verification/password reset, refresh/revocation policy,
  structured telemetry, stronger secret rotation, CSRF review, and a privacy/legal
  review.
- **What is deliberately not advice?** The questionnaire, model allocations, gap
  display, and projections are deterministic educational examples. The system
  never names a security to buy or sell and never guarantees a result.
