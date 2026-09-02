# PlanVest development guide

Read [`docs/PlanVest_MVP_PRD_v1.md`](docs/PlanVest_MVP_PRD_v1.md) and
[`docs/IMPLEMENTATION.md`](docs/IMPLEMENTATION.md) before changing product behaviour.

## Non-negotiable product rules

- PlanVest is educational planning software. Never add trade execution, brokerage
  connectivity, tax advice, return guarantees, or individualized security
  recommendations.
- All demo data must be synthetic. Never commit credentials, tokens, real account
  data, or production connection strings.
- Every query for a user-owned account, holding, transaction, assessment, or goal
  must be scoped by the authenticated user ID. A resource ID is not authorization.
- Persist money and quantities as `decimal`; do not use `float` or `double` for
  financial values.
- Keep `main` unchanged until the product owner explicitly approves a merge.

## Engineering workflow

1. Work on a `codex/*` branch and keep the pull request reviewable.
2. Update API contracts, implementation, tests, and documentation together.
3. Keep endpoint handlers thin. Put reusable calculations in `apps/api/Services`.
4. Return RFC 7807 problem responses for failures and never expose whether a
   resource belongs to another user.
5. Before handing off, run `dotnet test PlanVest.sln`, `pnpm lint`, and
   `pnpm build` (or the equivalent npm commands used by CI).

## Definition of done for a feature

- The protected happy path works through the web UI and API.
- Empty, loading, validation, unauthorized, and server-error states are clear.
- User ownership is enforced server-side and covered by tests when applicable.
- Financial formulas have deterministic boundary tests.
- README and implementation status remain accurate.
