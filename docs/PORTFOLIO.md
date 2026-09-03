# PlanVest portfolio presentation

This document keeps presentation assets and job-application copy aligned with
what the deployed project actually supports. It is not a product roadmap or a
claim that planned work has shipped.

## README screenshot plan

Use production-safe synthetic demo data only. Capture the same isolated demo
session where possible, hide browser extensions and account chrome, and never
include tokens, deployment variables, database identifiers, or platform billing
details.

| Order | README position | View to capture | Target path | Recommended crop |
| --- | --- | --- | --- | --- |
| 1 | After the opening description | Desktop dashboard with totals, allocation, risk, and goal summary | `docs/images/dashboard-desktop.webp` | 1440 × 900, full application shell |
| 2 | After Core functionality | Portfolio accounts, holdings, and allocation chart | `docs/images/portfolio-allocation.webp` | 1200 × 750, emphasize data relationships |
| 3 | After Core functionality | Completed risk assessment and generic model comparison | `docs/images/risk-assessment.webp` | 1200 × 750, retain rationale text |
| 4 | After Core functionality | Goal progress and projection controls | `docs/images/goals-planning.webp` | 1200 × 750, use plausible synthetic values |
| 5 | After Quick product tour | Open mobile navigation drawer | `docs/images/mobile-navigation.webp` | 390 × 844, include menu and page context |

Prefer WebP at 80–85% quality and keep each image below roughly 350 KB. Add
meaningful alt text describing the engineering workflow shown, not decorative
phrasing. Once the images are reviewed, use the desktop dashboard as the README
hero and place the three workflow images in a compact table. Keep the mobile
capture beside the quick-tour instructions.

## Resume description

**PlanVest — Full-Stack Investment Planning Application**

Next.js, React, TypeScript, ASP.NET Core, C#, EF Core, PostgreSQL, Docker, GitHub Actions

- Built and deployed a responsive full-stack planning application with JWT
  authentication, server-enforced per-user data isolation, portfolio and
  transaction management, explainable risk scoring, asset allocation, and goal
  projections.
- Designed an ASP.NET Core API with decimal-safe financial calculations, RFC 7807
  error handling, SQLite/PostgreSQL provider separation, EF Core migrations, and
  real HTTP integration tests covering authentication, authorization, and core
  workflows.
- Automated web/API builds, xUnit tests, Docker image validation, PostgreSQL
  integration tests, and fresh-migration checks in GitHub Actions; deployed the
  Next.js frontend to Vercel and the containerized API/database to Railway.

## LinkedIn project description

PlanVest is a deployed full-stack investment-planning project I built to
demonstrate production-minded web engineering. The responsive Next.js and
TypeScript frontend connects to an ASP.NET Core 8 API backed by PostgreSQL in
production and SQLite for local development. It includes registration and login,
server-side logout invalidation, isolated demo users, portfolio and transaction
management, asset-allocation analysis, explainable risk scoring, and financial
goal projections. I also implemented per-user authorization, decimal-safe
financial calculations, Docker deployment, EF Core migrations, strict CORS and
proxy-aware rate limiting, plus GitHub Actions checks for the frontend, API,
PostgreSQL integration, and clean database initialization. PlanVest is an
educational tool using synthetic data; it does not connect to brokerages or
provide investment advice.

Live demo: https://planvest-pi.vercel.app

Source: https://github.com/reinaldozhu6-web/planvest

## Honest follow-up opportunities

These are suitable interview discussion points, not shipped features:

- Replace session-storage bearer tokens with an HttpOnly cookie/BFF or audited
  identity provider before handling sensitive real-world data.
- Add Playwright coverage for critical desktop and mobile workflows.
- Add production telemetry, structured dashboards, alerting, and documented SLOs.
- Add password reset and email verification if the project evolves beyond a
  portfolio demonstration.
- Add market-data ingestion only with clear provenance, caching, failure modes,
  and product/legal review; do not add brokerage execution to the educational MVP.
