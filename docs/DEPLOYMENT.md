# PlanVest deployment runbook

This runbook describes the approved low-cost target architecture. It does not
contain production credentials and does not authorize creating public resources.

## Target architecture

| Component | Platform | Plan |
| --- | --- | --- |
| Next.js web client | Vercel | Hobby for a personal, non-commercial portfolio |
| ASP.NET Core API | Railway | Hobby, one replica, Docker deployment |
| Database | Railway PostgreSQL 16 | Private networking in the API region |

Use the platform-provided `vercel.app` and `up.railway.app` domains initially.
Place the API and database in the same Railway region and set a workspace spend
limit before enabling automatic deployment.

## Required variables

### Vercel web project

Set the project root directory to `apps/web` and configure this variable for the
Production environment:

| Variable | Example |
| --- | --- |
| `NEXT_PUBLIC_API_URL` | `https://planvest-api.up.railway.app` |

This is a public build-time value, not a secret. Changing it requires a new web
deployment. Do not point preview deployments at production unless their exact
origins are also intentionally allowed by the API.

### Railway API service

Build from the repository root with `apps/api/Dockerfile`. The checked-in
`railway.json` supplies the Dockerfile path, watch paths, health check, and
restart policy. Railway supplies `PORT`; do not hard-code its value.

| Variable | Required value |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `Database__Provider` | `PostgreSql` |
| `ConnectionStrings__DefaultConnection` | Npgsql keyword connection string assembled from Railway PostgreSQL reference variables |
| `Jwt__Key` | Secret random value of at least 64 bytes |
| `Jwt__Issuer` | `PlanVest.Api` |
| `Jwt__Audience` | `PlanVest.Web` |
| `WebOrigin` | Exact Vercel production origin, with no trailing slash |
| `Hosting__Provider` | `Railway` |
| `RateLimiting__AuthPermitLimit` | `10` unless load testing justifies another value |

An Npgsql connection string has this shape:

```text
Host=<PGHOST>;Port=<PGPORT>;Database=<PGDATABASE>;Username=<PGUSER>;Password=<PGPASSWORD>;SSL Mode=Require
```

Use Railway reference variables in the dashboard so credentials remain inside
the platform. Never copy the resolved connection string, JWT key, or database
password into GitHub, Vercel, logs, screenshots, or this file.

## Provisioning order

1. Create the Vercel project and reserve its stable production URL.
2. Create one Railway project, then add PostgreSQL 16 and the API service in the
   same region. Keep PostgreSQL private; do not add a public TCP proxy.
3. Add the Railway API variables, health-check path `/api/health`, and a hard
   spending limit. Deploy only a reviewed commit from `main`.
4. Set Vercel `NEXT_PUBLIC_API_URL` to the healthy Railway URL and deploy the web
   project.
5. Confirm Railway `WebOrigin` exactly matches the final Vercel origin. Redeploy
   the API if the origin changed.
6. Enable automatic deployment from `main` only after the acceptance checks pass.

## Release validation

- Both public URLs use HTTPS and HTTP does not create a redirect loop.
- `/api/health` returns `200` only when PostgreSQL is reachable.
- Anonymous protected requests return `401`.
- Registration, login, logout, demo session, portfolio, risk, goals, simulation,
  and dashboard smoke tests pass through the Vercel origin.
- Requests from different client IPs have independent authentication rate-limit
  buckets, and a single client receives `429` when its bucket is exhausted.
- An unapproved origin fails CORS preflight.
- A Railway restart preserves data; a fresh database receives the full migration
  chain exactly once.
- Application logs contain no bearer tokens, passwords, connection strings, or
  full request bodies.

## Backups and rollback

Enable scheduled Railway volume backups for PostgreSQL and create a manual backup
before every schema migration. Test restoring a backup before calling the service
production-ready.

Vercel and Railway application releases can be rolled back independently. Do not
roll an application back across an incompatible database migration. Prefer
backward-compatible expand/migrate/contract schema changes and restore the
database only for a confirmed data-loss incident.

Rotating `Jwt__Key` invalidates every existing session. Rotate the database
password and JWT key immediately if either value appears in a log or repository.

## SQLite production fallback

For a short-lived private demo only, the API can use `Database__Provider=Sqlite`
with `Data Source=/data/planvest.db` and a Railway volume mounted at `/data`.
This prevents file loss but disables replicas, introduces deployment downtime,
and retains SQLite's single-writer constraints. PostgreSQL is the supported
public deployment path.
