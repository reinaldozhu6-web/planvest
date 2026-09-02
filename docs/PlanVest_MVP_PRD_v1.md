# PlanVest MVP — Product Requirements Document

**Version:** 1.0  
**Status:** Approved; implementation in progress on PR #1 — September 1, 2026
**Product owner:** Reinaldo Pang  
**Product manager:** Codex  
**Target:** Backend/full-stack software engineering interviews  

## 1. Product summary

PlanVest is an educational personal-investment planning platform. A user can create an account, record simulated investment accounts and holdings, assess risk tolerance, set financial goals, compare the current portfolio with a generic target allocation, and model future contributions.

The MVP does not connect to a brokerage, move money, execute trades, provide tax advice, or claim personalized financial advice. Prices and transactions are user-entered or generated as demo data.

## 2. Product objective

### User objective

Help an early-stage investor answer four questions in one place:

1. What do I currently own?
2. How is my portfolio allocated?
3. What risk profile matches my stated time horizon and loss tolerance?
4. Am I progressing toward my financial goals?

### Interview objective

Demonstrate that the candidate can build and explain:

- secure registration and authentication
- REST API design
- relational data modelling and EF Core migrations
- authorization and user-level data isolation
- financial calculations using deterministic business rules
- React/Next.js state and dashboard integration
- validation, error handling, testing, documentation, and deployment

## 3. Target users

### Primary persona

An investor who has several ETFs, stocks, or cash positions but does not have a clear view of asset allocation, risk level, or goal progress.

### Demo persona

`Alex Chen`, age 27, investing in CAD through a TFSA and non-registered account, with a long-term growth goal and moderate tolerance for market losses.

The demo persona and all included financial data are synthetic.

## 4. Product assumptions requiring approval

Unless the product owner requests a change, Codex will build with these assumptions:

1. Base currency is CAD.
2. The MVP supports TFSA, RRSP, FHSA, non-registered, and cash account labels, but performs no tax calculations.
3. Asset prices are manually entered; no paid or rate-limited market-data API is required.
4. Authentication uses email and password with secure password hashing and HttpOnly authentication cookies or a short-lived JWT design.
5. The risk result is an explainable questionnaire score, not an investment recommendation.
6. Target allocations use broad asset classes such as equity, fixed income, and cash—not individual security recommendations.
7. Future-value projections use user-selected assumptions and clearly state that returns are not guaranteed.
8. The application is a portfolio and interview project, not a production financial product.

## 5. MVP scope

### Must have

- User registration, login, logout, and current-user session
- Protected user dashboard
- Create, edit, and delete investment accounts
- Add holdings with symbol, asset name, asset class, quantity, and price
- Record buy, sell, deposit, and withdrawal transactions
- Portfolio market value and allocation calculations
- Risk-assessment questionnaire and stored result
- Conservative, Balanced, and Growth model allocations
- Comparison between current and model allocation
- Financial goal creation and progress calculation
- Monthly-contribution future-value simulator
- Demo account with synthetic data
- Responsive dashboard and accessible forms
- API validation and consistent error responses
- Unit and integration tests for important business logic
- Professional README and deployment instructions

### Should have

- Rebalancing-gap table showing target percentage, current percentage, and approximate dollar difference
- CSV export of holdings and transactions
- Dashboard filtering by account
- Risk-assessment history
- Dark visual theme suitable for a financial dashboard

### Could have after MVP

- Public market-price API
- Historical performance chart
- Dividend tracking
- Watchlist
- Multi-currency conversion
- Passkeys or social login
- Email verification and password reset
- PDF financial-plan summary
- AI-generated natural-language explanations based only on calculated results

### Out of scope

- Real brokerage connectivity
- Bank connectivity
- Real-money trading
- Payment processing
- Individual buy/sell recommendations
- Guaranteed-return claims
- Canadian tax calculation or filing advice
- Options, margin, crypto custody, or derivatives trading

## 6. Core user journeys

### Journey A — First-time onboarding

1. User registers with name, email, and password.
2. User logs in and sees an empty-state onboarding checklist.
3. User creates an investment account.
4. User adds at least one holding.
5. Dashboard displays total portfolio value and allocation.
6. User completes the risk questionnaire.
7. Dashboard compares current allocation with the model allocation.

### Journey B — Portfolio management

1. Authenticated user selects an account.
2. User records a transaction or edits a holding.
3. Backend validates ownership and numeric values.
4. Portfolio totals and allocation update.
5. User can export a CSV summary.

### Journey C — Goal planning

1. User creates a goal with target amount and date.
2. User enters current amount and intended monthly contribution.
3. User selects an assumed annual return.
4. System calculates projected future value and required monthly contribution.
5. Results display assumptions and a no-guarantee disclaimer.

### Journey D — Returning demo reviewer

1. Recruiter selects “Use demo account.”
2. Application loads a synthetic portfolio without requiring personal information.
3. Recruiter can explore dashboards, risk results, goals, and simulations.

## 7. Functional requirements

| ID | Requirement | Priority | Acceptance condition |
| --- | --- | --- | --- |
| AUTH-01 | Register with name, email, and password | Must | Valid registration creates one user; duplicate email returns a clear conflict error |
| AUTH-02 | Login and logout | Must | Valid login creates authenticated session; logout invalidates it |
| AUTH-03 | Protect user data | Must | Anonymous requests receive 401; users cannot read or mutate another user’s records |
| PORT-01 | Manage investment accounts | Must | User can create, rename, classify, and delete an owned account |
| PORT-02 | Manage holdings | Must | User can add, edit, and remove a holding with validated quantity and price |
| PORT-03 | Record transactions | Must | Buy, sell, deposit, and withdrawal records appear in account history |
| PORT-04 | Calculate portfolio value | Must | Total equals the sum of quantity × current price across selected holdings |
| PORT-05 | Calculate allocation | Must | Asset-class percentages total approximately 100% for non-empty portfolios |
| RISK-01 | Complete questionnaire | Must | All required questions must be answered before submission |
| RISK-02 | Calculate explainable score | Must | Result includes total score, profile, category subscores, and plain-language rationale |
| RISK-03 | Store assessment | Should | User can view the most recent result and assessment date |
| PLAN-01 | Display model allocation | Must | Conservative, Balanced, or Growth allocation corresponds to stored risk profile |
| PLAN-02 | Compare allocation | Must | Table displays current %, target %, and percentage-point difference |
| PLAN-03 | Calculate dollar gap | Should | Gap uses current portfolio value and target percentages |
| GOAL-01 | Manage goals | Must | User can create, edit, archive, and delete owned goals |
| GOAL-02 | Calculate progress | Must | Progress displays current amount ÷ target amount with a 0–100% visual cap |
| SIM-01 | Future-value projection | Must | User can vary principal, monthly contribution, years, and expected annual return |
| SIM-02 | Required-contribution calculation | Must | System estimates contribution required to reach a target under stated assumptions |
| DEMO-01 | Demo access | Must | Reviewer can access synthetic portfolio without registering personal data |
| EXPORT-01 | CSV export | Should | Holdings and transactions download in a documented format |

## 8. Risk-assessment rules

### Question categories

- Time horizon
- Income stability
- Emergency-fund readiness
- Investment knowledge
- Reaction to a hypothetical market decline
- Need for liquidity
- Primary investment objective

Each answer receives a documented numeric score. The score is stored with the exact answers and scoring version.

### Initial profile thresholds

| Score | Profile | Generic model allocation |
| --- | --- | --- |
| 0–35 | Conservative | 35% equity, 55% fixed income, 10% cash |
| 36–70 | Balanced | 65% equity, 30% fixed income, 5% cash |
| 71–100 | Growth | 85% equity, 10% fixed income, 5% cash |

These allocations are demonstration rules, not personalized advice. The UI must show this limitation next to every result.

## 9. Financial calculations

### Holding market value

`marketValue = quantity × currentPrice`

### Portfolio allocation

`allocationPct = assetClassValue ÷ totalPortfolioValue × 100`

### Goal progress

`progressPct = currentSaved ÷ targetAmount × 100`

### Future value

Use monthly compounding with user-provided annual rate:

`FV = principal × (1 + r)^n + contribution × (((1 + r)^n - 1) ÷ r)`

Where `r` is the monthly rate and `n` is the number of months. Handle a 0% rate without division by zero.

All monetary values must use decimal arithmetic on the backend. Do not use binary floating-point values for persisted money.

## 10. Information architecture

### Public routes

- `/` — product explanation and demo entry
- `/login`
- `/register`
- `/disclaimer`

### Protected routes

- `/dashboard`
- `/accounts`
- `/accounts/[id]`
- `/risk-assessment`
- `/plan`
- `/goals`
- `/simulator`
- `/settings`

## 11. Data model

### User

- Id
- DisplayName
- Email
- PasswordHash
- CreatedAt
- LastLoginAt

### InvestmentAccount

- Id
- UserId
- Name
- AccountType
- BaseCurrency
- CreatedAt

### Holding

- Id
- InvestmentAccountId
- Symbol
- AssetName
- AssetClass
- Quantity
- AverageCost
- CurrentPrice
- UpdatedAt

### Transaction

- Id
- InvestmentAccountId
- HoldingId, optional
- Type
- Quantity
- Price
- Amount
- TransactionDate
- Note

### RiskAssessment

- Id
- UserId
- ScoringVersion
- AnswersJson
- TotalScore
- RiskProfile
- CreatedAt

### FinancialGoal

- Id
- UserId
- Name
- GoalType
- TargetAmount
- CurrentAmount
- TargetDate
- MonthlyContribution
- AssumedAnnualReturn
- Status

## 12. API surface

### Authentication

- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/logout`
- `GET /api/auth/me`

### Accounts and portfolio

- `GET /api/accounts`
- `POST /api/accounts`
- `GET /api/accounts/{id}`
- `PUT /api/accounts/{id}`
- `DELETE /api/accounts/{id}`
- `POST /api/accounts/{id}/holdings`
- `PUT /api/holdings/{id}`
- `DELETE /api/holdings/{id}`
- `POST /api/accounts/{id}/transactions`
- `GET /api/portfolio/summary`
- `GET /api/portfolio/allocation`

### Risk and planning

- `GET /api/risk/questions`
- `POST /api/risk/assessments`
- `GET /api/risk/latest`
- `GET /api/plan/allocation-comparison`

### Goals and simulation

- `GET /api/goals`
- `POST /api/goals`
- `PUT /api/goals/{id}`
- `DELETE /api/goals/{id}`
- `POST /api/simulations/future-value`
- `POST /api/simulations/required-contribution`

## 13. Technology architecture

### Frontend

- Next.js
- React
- TypeScript
- Recharts
- Accessible component primitives

### Backend

- ASP.NET Core Web API
- C#
- Entity Framework Core
- ASP.NET Core Identity or an equivalent secure identity implementation
- FluentValidation or explicit request validators
- OpenAPI/Swagger

### Database

- SQLite for simple local onboarding
- PostgreSQL-compatible production configuration
- EF Core migrations committed to source

### Repository structure

```text
planvest/
  apps/
    web/
    api/
  docs/
    PRD.md
    architecture.md
  tests/
  README.md
```

## 14. Security and privacy

- Hash passwords using the framework’s approved password hasher.
- Never store plaintext passwords.
- Prefer HttpOnly, Secure, SameSite cookies when deployment architecture permits.
- Validate authorization for every user-owned resource.
- Rate-limit authentication endpoints.
- Validate numeric ranges and reject negative quantities or prices where invalid.
- Do not log passwords, authentication tokens, or full sensitive request bodies.
- Do not commit secrets, connection strings, `.env`, or production credentials.
- Demo data must be synthetic.

## 15. Non-functional requirements

| Area | Requirement |
| --- | --- |
| Accessibility | Keyboard navigation, visible focus, labels, semantic tables, readable contrast |
| Responsiveness | Core workflows usable from 360px mobile through desktop |
| Performance | Dashboard returns promptly for at least 1,000 synthetic transactions per user |
| Reliability | Clear loading, empty, validation, unauthorized, and server-error states |
| Maintainability | Typed contracts, service-layer business logic, limited dependencies, migrations |
| Observability | Structured backend logs and safe error correlation ID |

## 16. Test plan

### Backend unit tests

- Risk-score thresholds and boundary values
- Asset-allocation calculations
- Future-value calculation, including 0% return
- Required-contribution calculation
- Goal-progress calculation

### Backend integration tests

- Registration and login
- Unauthorized access returns 401
- Cross-user record access is denied
- Account and holding CRUD
- Invalid monetary values return validation errors

### Frontend tests

- Login validation
- Empty dashboard
- Portfolio summary display
- Risk-questionnaire completion
- Simulator calculation and error states

### Release checks

- Production frontend build
- Production backend build
- Lint/static analysis
- Automated tests
- Migration from empty database
- Secret scan

## 17. Definition of done

- A recruiter can use the demo workflow without personal data.
- Registration and protected user workflow function end to end.
- Portfolio totals and allocations are correct for test fixtures.
- Risk result is explainable and clearly labelled educational.
- Goal and simulation results show assumptions and limitations.
- Users cannot access another user’s records.
- Frontend and backend builds pass.
- Automated tests pass.
- README documents setup, architecture, security, and limitations.
- Repository contains no unused starter template or secrets.
- Reviewable PR is open; `main` is not modified without product-owner approval.
- Live demo is published only after explicit public-access approval.

## 18. Development milestones

### Milestone 1 — Foundation

- Monorepo structure
- Database and migrations
- Registration/login
- Protected dashboard shell

### Milestone 2 — Portfolio

- Accounts, holdings, transactions
- Portfolio summary and allocation
- Demo seed data

### Milestone 3 — Planning

- Risk questionnaire
- Model allocation comparison
- Goals and simulator

### Milestone 4 — Quality and delivery

- Tests and security checks
- Responsive polish
- README and architecture notes
- Deployment and GitHub PR
- Resume bullets and interview walkthrough

## 19. Interview evidence produced

The finished project must generate:

- Two accurate resume bullets
- A 60-second project introduction
- A 5-minute technical walkthrough
- Architecture diagram
- Database relationship diagram
- Explanation of authentication and authorization
- Explanation of decimal financial calculations
- One documented debugging story
- Answers to likely security and trade-off questions

## 20. Product approval

Approve the following statement to begin implementation:

> I approve PlanVest MVP PRD v1 with CAD as the base currency, simulated/manual market data, educational risk assessment, Next.js frontend, ASP.NET Core backend, SQLite local database, and PostgreSQL-compatible production design. Codex may create a new GitHub repository and development branch, but may not merge into `main` or publish private data without separate approval.
