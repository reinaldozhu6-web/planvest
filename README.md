# PlanVest

PlanVest is an educational portfolio-planning application built to demonstrate secure full-stack product development. Users can record simulated accounts and holdings, assess risk tolerance, and plan toward financial goals.

> PlanVest is a portfolio project. It does not connect to brokerages, execute trades, or provide financial, tax, or investment advice.

## Status

Milestone 1 is under active development on `codex/mvp-foundation`. The default branch will remain unchanged until the project owner approves the pull request.

## Stack

- Next.js, React, TypeScript, Recharts
- ASP.NET Core Web API, C#, Entity Framework Core
- SQLite locally; PostgreSQL-compatible production design
- xUnit and GitHub Actions

## Repository layout

```text
apps/web/      Next.js client
apps/api/      ASP.NET Core API
tests/         Backend tests
docs/          Product and architecture documentation
```

## Local development

### Web

```bash
cd apps/web
npm install
npm run dev
```

### API

Requires .NET 8 SDK.

```bash
dotnet restore
dotnet run --project apps/api/PlanVest.Api.csproj
```

The API reads `Jwt:Key` from configuration. Use user secrets or an environment variable in non-development environments; never commit a production secret.

## Current milestone

- [x] Approved product requirements
- [x] Monorepo foundation
- [x] Registration, login, logout, and current-user API contract
- [x] Protected dashboard shell and demo experience
- [ ] Portfolio account and holding persistence
- [ ] Risk assessment and goal planning
- [ ] Full integration and security test suite

