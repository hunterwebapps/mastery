# Mastery - AI-Guided Personal Mastery System

## Project Overview

Mastery is a closed-loop control system for personal development that treats goal achievement as a feedback control problem: **intent → action → reality capture → compare → diagnose → adjust**.

**Core constraint**: <2 minutes/day manual input for 90% of users.

**Product thesis**: Most productivity apps fail because they help you plan but not execute. Mastery is different because it:
1. Makes planning **possible** (capacity-aware, not just "correct")
2. Coaches from **signals**, not vibes (deterministic constraints + AI explanation)
3. Adapts to **what works for you** (learning engine + personalized playbook)

---

## Repository Structure

```
/mastery
├── src/
│   ├── web/                          # React SPA (Vite + TypeScript + PWA)
│   │   ├── src/
│   │   │   ├── app/                  # App setup, providers, router
│   │   │   ├── components/           # Shared UI components
│   │   │   ├── features/             # Feature modules (co-located)
│   │   │   │   ├── dashboard/
│   │   │   │   ├── goals/
│   │   │   │   ├── habits/
│   │   │   │   └── check-ins/
│   │   │   ├── hooks/                # Shared hooks
│   │   │   ├── lib/                  # Utilities, API client
│   │   │   ├── stores/               # Zustand stores
│   │   │   └── types/                # Shared TypeScript types
│   │   └── vite.config.ts
│   │
│   └── api/                          # .NET 10 Backend (Clean Architecture)
│       ├── Mastery.Domain/           # Domain layer (innermost)
│       │   ├── Common/               # Base classes (BaseEntity, ValueObject, etc.)
│       │   ├── Entities/             # Domain entities
│       │   ├── ValueObjects/         # Value objects
│       │   ├── Events/               # Domain events
│       │   ├── Interfaces/           # Repository interfaces
│       │   └── Exceptions/           # Domain exceptions
│       │
│       ├── Mastery.Application/      # Application layer
│       │   ├── Common/
│       │   │   ├── Behaviors/        # MediatR pipeline behaviors
│       │   │   ├── Interfaces/       # Application interfaces
│       │   │   └── Models/           # DTOs and view models
│       │   ├── Features/             # Use cases organized by feature
│       │   │   ├── Goals/
│       │   │   ├── Habits/
│       │   │   ├── CheckIns/
│       │   │   └── Users/
│       │   └── DependencyInjection.cs
│       │
│       ├── Mastery.Infrastructure/   # Infrastructure layer
│       │   ├── Data/
│       │   │   ├── Configurations/   # EF entity configurations
│       │   │   ├── Migrations/
│       │   │   └── MasteryDbContext.cs
│       │   ├── Repositories/
│       │   ├── Services/
│       │   └── DependencyInjection.cs
│       │
│       ├── Mastery.Api/              # API layer (outermost)
│       │   ├── Controllers/
│       │   ├── Middleware/
│       │   ├── Services/
│       │   └── Program.cs
│       │
│       └── Mastery.sln
│
├── docs/                             # Documentation
└── CLAUDE.md                         # This file
```

---

## Architecture Principles

### 1. Deterministic Constraints First, LLM Second
The system computes capacity, feasibility, and candidate actions using **code**. The LLM's job is to:
- Select among pre-computed candidates
- Generate rationale and explanations
- Propose experiments
- Communicate empathetically

### 2. Event-Sourced Behavioral Ledger
Everything the user does becomes a domain event. Derived scores and plans are projections that can be recomputed.

### 3. Explainability as First-Class Artifact
Store a "recommendation trace" so the product can show "why" and developers can debug.

### 4. Clean Architecture
- **Domain**: Pure business logic, no external dependencies
- **Application**: Use cases, orchestration via MediatR (CQRS)
- **Infrastructure**: External concerns (database, external services)
- **API**: HTTP interface, authentication, middleware

---

## Closed-Loop Control System Mapping

| Control Concept | Mastery Implementation |
|-----------------|------------------------|
| **Setpoint** | Goals + lead indicator targets + seasonal priorities |
| **Sensors** | Check-ins, completions/misses, calendar load, optional passive metrics |
| **State Estimator** | Derived scores (consistency, capacity health, friction index) |
| **Controller** | Planning engine + diagnostic engine + coaching engine |
| **Actuators** | Plan adjustments, habit scaling, next-action suggestions, notifications |
| **Plant** | The human + their environment |
| **Feedback** | Daily/weekly loops |

---

## Domain Model (Core Entities)

### UserProfile
Values, roles, season, preferences, constraints.

### Goal
- `metrics_lag[]` - Outcome metrics (what you're trying to achieve)
- `metrics_lead[]` - Leading indicators (predictive behaviors)
- `metric_constraint` - Guardrail metric (what not to sacrifice)
- `baseline`, `target`, `deadline`, `priority`, `why`
- `dependencies[]`

### Habit
- `definition`, `schedule`, `min_version` (minimum viable version)
- `trigger`, `reward`, `difficulty`
- `time_cost`, `context_tags[]`, `failure_modes[]`

### HabitOccurrence
- `habit_id`, `date`, `status`
- `reason` (optional), `context` (optional)

### Task
- `project_id?`, `est_minutes`, `energy_level`
- `context_tags[]`, `due_date?`, `dependency_ids[]`

### CheckIn
- `date`, `energy_am?`, `energy_pm?`, `stress?`
- `intention?`, `reflection?`, `blocker?`

### Experiment
- `hypothesis`, `change`, `metric_to_watch`
- `start_date`, `end_date?`, `result`, `notes`

### Recommendation
- `type`, `payload`, `rationale_text`
- `trace` (inputs, rules triggered, prompt version, output)
- `status` (accepted/dismissed)

---

## Intelligence Layer (The Engines)

### A. Planning Engine (Deterministic + Explainable)
**Inputs**: Priorities, tasks, habits, capacity
**Outputs**: Daily plan, weekly plan, feasibility report

Core: Knapsack-style scheduling + heuristic scoring (NOT LLM).

### B. Diagnostic Engine (Rules First)
**Inputs**: Adherence trends, capacity health, friction events
**Outputs**: Ranked hypotheses with evidence

Transparent rules like:
- "3+ nights under sleep threshold → adherence risk"
- "High overload score + rising misses → switch to maintenance"

### C. Coaching Engine (LLM-Assisted)
**Inputs**: Diagnostic hypotheses, intervention library, user preferences
**Outputs**: Targeted intervention, weekly experiment, reframes

Important: Interventions come from a **curated library**; the LLM selects and adapts language.

### D. Learning Engine (Personalization)
**Inputs**: Intervention history + outcomes, context features
**Outputs**: Per-user playbook ("When X, Y works"), intervention weights

---

## AI Implementation Pattern

**"Controller Pipeline" Pattern**:
1. **Retrieve state** (structured data)
2. **Compute feasibility** (deterministic)
3. **Generate candidates** (deterministic)
4. **LLM selects + explains** (bounded choices only)
5. **Persist recommendation + trace** (auditability)
6. **Collect outcome** (accepted/dismissed + completion)

This avoids the failure mode of LLM planning: confident but infeasible advice.

### Recommendation Trace (What to Store)
- Inputs used: energy, capacity, priority goals, due tasks
- Rules triggered: overload, low sleep risk, weekend adherence gap
- Candidate list: top N actions + scores
- Selected action + explanation
- Prompt version + model version
- User response: accepted/dismissed + completion outcome

---

## Key Data Flows

### Daily Loop (Morning)
1. User submits morning check-in
2. `CheckInSubmitted` event emitted
3. Planning Engine computes available time blocks, feasible plan, candidate actions
4. Diagnostic Engine flags risks
5. Coaching Engine generates intervention + NBA explanation
6. App receives "Today Plan + NBA + Why + Overrides"

### Daily Loop (Evening)
1. User marks habits/tasks done or missed + reflection
2. Events emitted
3. System updates adherence projections, friction index
4. Next day plan is precomputed

### Weekly Review
1. Aggregate last 7 days (goal momentum, lead indicator adherence, capacity health)
2. Diagnostic Engine identifies the constraint
3. Learning Engine selects next experiment
4. Planning Engine proposes capacity-aware targets
5. Coaching Engine produces narrative + "stop doing" recommendation

---

## Technology Stack

### Frontend (React SPA)
- **Vite** + **React 19** + **TypeScript**
- **PWA** with service worker (vite-plugin-pwa)
- **TanStack Query** - Server state management
- **Zustand** - Client state management
- **React Router v7** - Routing with lazy loading
- **Tailwind CSS v4** - Styling
- **React Hook Form + Zod** - Forms and validation

### Backend (.NET 10 API)
- **ASP.NET Core** with Controllers
- **Entity Framework Core 10** + SQL Server
- **MediatR 14** - CQRS pattern
- **FluentValidation** - Input validation
- **Mapster** - Object mapping
- **Serilog** - Structured logging
- **Scalar** - OpenAPI documentation UI

---

## Development Roadmap Phases

### Phase 0 - Foundations (Current)
Domain primitives, event model, smallest viable loop.

### Phase 1 - MVP
Onboarding, daily loop (2-4 min), goal/habit cards, Next Best Action v1, weekly review, basic drift detection.

### Phase 2 - Capacity-Aware Planning
Calendar import, plan realism engine, habit scaling, Mastery Score, explainability upgrades.

### Phase 3 - Personalization
Learning engine, friction heatmaps, voice input, optional integrations.

### Phase 4 - Scale
Accountability features, coach mode, advanced analytics, framework marketplace.

---

## Success Metrics to Instrument

- **Loop completion rate** (morning + evening)
- **Recommendation acceptance rate**
- **Accepted → completed conversion**
- **Overcommitment index** (planned minutes / available minutes)
- **Intervention effectiveness** (before/after adherence)
- **User trust proxy** (dismissals without alternative; "why?" taps)

---

## Development Commands

### Backend
```bash
cd src/api
dotnet build Mastery.sln          # Build all projects
dotnet run --project Mastery.Api  # Run API (https://localhost:5001)
dotnet ef migrations add <Name> --project Mastery.Infrastructure --startup-project Mastery.Api
```

### Frontend
```bash
cd src/web
npm install        # Install dependencies
npm run dev        # Development server (http://localhost:3000)
npm run build      # Production build
npm run preview    # Preview production build
```

### API Documentation
When running in development, visit `/scalar/v1` for interactive API docs.

---

## Key Differentiators (Build These First)

1. **Capacity-aware plan realism** (even with manual capacity rating)
2. **Next Best Action** that respects time/energy/context
3. **Habit minimum-version scaling**
4. **Weekly constraint diagnosis + one experiment**
5. **Explainable recommendation traces**

Most competitors do (1) poorly or not at all - that's where user trust is won.
