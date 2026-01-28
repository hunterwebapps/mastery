# Experiments Feature Documentation

## Overview

The **Experiments** feature enables structured, hypothesis-driven self-experimentation within the Mastery system. Instead of making vague changes and hoping for improvement, users design deliberate experiments with measurable outcomes, controlled time windows, and clear success criteria.

An experiment tests a single hypothesis: **"If I [change], then [expected outcome] because [rationale]."** Results are captured as before/after metric comparisons, creating a personal playbook of what works.

**Design Philosophy**: Personal development should be treated as an empirical process. Most people change habits by feel and give up when results aren't obvious. Experiments provide the structure to know whether a change actually worked—and if not, why.

---

## Business Context

### Why Experiments Exist

Experiments serve as the adaptive mechanism in Mastery's closed-loop control system. They close the gap between "I think this might help" and "I know this works for me." Specifically:

1. **Hypothesis testing** — Forces users to articulate what they're changing and why, preventing aimless tinkering
2. **Baseline comparison** — Establishes what "normal" looks like before measuring improvement
3. **Compliance awareness** — Tracks whether the change was actually implemented consistently enough to draw conclusions
4. **Guardrail protection** — Monitors secondary metrics to catch unintended side effects (e.g., sleeping less to exercise more)
5. **Outcome classification** — Explicitly classifies results so the Learning Engine can build a personal playbook

### The Control System Analogy

| Control Concept | Experiments Component |
|-----------------|----------------------|
| **Hypothesis** | If/Then/Because structured prediction |
| **Independent Variable** | The deliberate change being made |
| **Dependent Variable** | Primary metric under observation |
| **Baseline** | Pre-experiment metric values (baseline window) |
| **Treatment** | Post-change metric values (run window) |
| **Guardrails** | Secondary metrics monitored for side effects |
| **Outcome** | Classified result feeding the Learning Engine |

### Single Active Experiment Constraint

A core business rule: **each user may have at most one active experiment at a time.** This constraint is enforced at both the application level and the database level (unique filtered index). The rationale:

- Running multiple experiments simultaneously introduces confounding variables
- Focus on one change at a time produces clearer signal
- Prevents cognitive overload from managing too many changes
- Aligns with the "one experiment per weekly review" coaching pattern

### Origin Sources

Experiments can be created from four sources, tracked via `CreatedFrom`:

| Source | Description |
|--------|-------------|
| `Manual` | User designs the experiment themselves |
| `WeeklyReview` | System recommends during the weekly review loop |
| `Diagnostic` | Diagnostic Engine identifies an issue and proposes an experiment |
| `Coaching` | Coaching Engine suggests a targeted behavioral change |

---

## Domain Model

### Entity Hierarchy

```
Experiment (Aggregate Root)
├── Id (Guid)
├── UserId (string) ────────────── Owner
├── Title (string, max 200) ────── Display name
├── Description? (string, max 2000) ─ Detailed description
├── Category (enum) ────────────── Area of change
├── Status (enum) ──────────────── Lifecycle state
├── CreatedFrom (enum) ─────────── Origin source
├── Hypothesis (Value Object) ──── The testable prediction
│   ├── Change (string, max 500) ── What's being changed
│   ├── ExpectedOutcome (string, max 500) ── What should happen
│   └── Rationale? (string, max 1000) ── Why it should work
├── MeasurementPlan (Value Object) ── How to measure
│   ├── PrimaryMetricDefinitionId (Guid) ── What to measure
│   ├── PrimaryAggregation (MetricAggregation) ── How to aggregate
│   ├── BaselineWindowDays (int, 1-90) ── Pre-experiment window
│   ├── RunWindowDays (int, 1-90) ── Experiment duration
│   ├── GuardrailMetricDefinitionIds[] ── Side-effect monitors
│   └── MinComplianceThreshold (decimal, 0-1) ── Validity threshold
├── StartDate? (DateOnly) ─────── When the run started
├── EndDatePlanned? (DateOnly) ── When it should end
├── EndDateActual? (DateOnly) ──── When it actually ended
├── LinkedGoalIds[] (Guid) ─────── Associated goals
│
├── Notes[] (Child Entities) ───── Observations during the run
│   └── ExperimentNote
│       ├── Id (Guid)
│       ├── ExperimentId (FK)
│       ├── Content (string, max 2000)
│       └── CreatedAt (DateTime)
│
└── Result? (Child Entity) ─────── Measured outcome
    └── ExperimentResult
        ├── Id (Guid)
        ├── ExperimentId (FK, unique)
        ├── BaselineValue? (decimal)
        ├── RunValue? (decimal)
        ├── Delta? (decimal) ────── RunValue - BaselineValue
        ├── DeltaPercent? (decimal) ── Delta / BaselineValue * 100
        ├── OutcomeClassification (ExperimentOutcome)
        ├── ComplianceRate? (decimal, 0-1)
        ├── NarrativeSummary? (string, max 4000)
        └── ComputedAt (DateTime)
```

### Aggregate Root: Experiment

The `Experiment` entity is the aggregate root. All mutations go through it, and it enforces business invariants.

**Key behaviors**:
- Created in `Draft` status — never directly in `Active`
- Only `Draft` experiments can have their details edited
- Status transitions are explicit methods with guard clauses
- Notes can be added regardless of status
- Result is set exactly once, at completion time
- One-to-one relationship with `ExperimentResult` (enforced by unique index)

### Value Object: Hypothesis

Encapsulates the structured prediction following the format:

> **"If I** [change]**, then** [expected outcome] **because** [rationale]**."**

| Field | Required | Max Length | Description |
|-------|----------|-----------|-------------|
| `Change` | Yes | 500 | The independent variable — what the user will do differently |
| `ExpectedOutcome` | Yes | 500 | The predicted effect on the dependent variable |
| `Rationale` | No | 1000 | The reasoning or evidence behind the prediction |

The `ToString()` method produces a human-readable sentence: `"If I meditate for 10 minutes each morning, then my stress scores will decrease by 20% because mindfulness has been shown to reduce cortisol."`

A server-computed `Summary` field is returned in the DTO for display purposes.

### Value Object: MeasurementPlan

Defines how the experiment will be evaluated quantitatively.

| Field | Required | Range | Default | Description |
|-------|----------|-------|---------|-------------|
| `PrimaryMetricDefinitionId` | Yes | valid GUID | — | The metric being measured |
| `PrimaryAggregation` | Yes | MetricAggregation enum | — | How to roll up observations (Sum, Average, Max, Min, Count, Latest) |
| `BaselineWindowDays` | Yes | 1–90 | 7 | Days of pre-experiment data for comparison |
| `RunWindowDays` | Yes | 1–90 | 7 | Duration of the experiment |
| `GuardrailMetricDefinitionIds` | No | GUID[] | [] | Metrics monitored for side effects |
| `MinComplianceThreshold` | Yes | 0.0–1.0 | 0.7 | Minimum data coverage for valid results |

**Compliance threshold**: If a user sets this to 0.7 and the experiment runs for 14 days, at least 10 days must have metric observations for the results to be considered meaningful.

### Child Entity: ExperimentNote

Notes capture observations, insights, or context during the experiment run.

| Field | Required | Max Length | Description |
|-------|----------|-----------|-------------|
| `Content` | Yes | 2000 | The note text |
| `CreatedAt` | Auto | — | UTC timestamp |

Notes can be added at any time, regardless of experiment status.

### Child Entity: ExperimentResult

Captures the measured outcome when the experiment completes.

| Field | Required | Range | Description |
|-------|----------|-------|-------------|
| `BaselineValue` | No | decimal | Aggregated metric value before the experiment |
| `RunValue` | No | decimal | Aggregated metric value during the experiment |
| `Delta` | Auto | decimal | `RunValue - BaselineValue` (computed if both provided) |
| `DeltaPercent` | Auto | decimal | `Delta / BaselineValue * 100` (computed if baseline non-zero) |
| `OutcomeClassification` | Yes | ExperimentOutcome enum | User's assessment of the result |
| `ComplianceRate` | No | 0.0–1.0 | How consistently the change was applied |
| `NarrativeSummary` | No | max 4000 | Human-readable or AI-generated summary |
| `ComputedAt` | Auto | DateTime | UTC timestamp of when the result was recorded |

One experiment has at most one result (enforced by unique index on `ExperimentId`).

---

## Enums Reference

### ExperimentStatus

Controls the lifecycle of an experiment.

| Value | Description | Can Transition To |
|-------|-------------|-------------------|
| `Draft` | Being designed, not yet running | `Active` |
| `Active` | Currently running | `Paused`, `Completed`, `Abandoned` |
| `Paused` | Temporarily on hold | `Active`, `Completed`, `Abandoned` |
| `Completed` | Concluded with results recorded | `Archived` |
| `Abandoned` | Stopped early without full results | `Archived` |
| `Archived` | Hidden from default views | *(terminal)* |

### ExperimentCategory

Categorizes the area of change. Combines user-facing life-area categories with system-diagnostic categories.

**Life-area categories** (user-initiated):

| Value | Description |
|-------|-------------|
| `Habit` | Habit formation, scaling, or modification |
| `Routine` | Routine or schedule changes |
| `Environment` | Context or environment changes |
| `Mindset` | Cognitive or motivation strategies |
| `Productivity` | Workflow or planning changes |
| `Health` | Health, energy, or sleep changes |
| `Social` | Social or accountability strategies |

**System-diagnostic categories** (typically surfaced by engines):

| Value | Description | Surfaced When |
|-------|-------------|---------------|
| `PlanRealism` | Testing whether plans are realistic | Overcommitment detected |
| `FrictionReduction` | Reducing barriers to action | Friction events are frequent |
| `CheckInConsistency` | Improving daily check-in habits | Check-in completion rate drops |
| `Top1FollowThrough` | Improving #1 priority execution | Top-1 completion rate is low |

**Catch-all**:

| Value | Description |
|-------|-------------|
| `Other` | Custom experiment that doesn't fit other categories |

### ExperimentOutcome (Backend)

Classifies the result of a completed experiment.

| Value | Description |
|-------|-------------|
| `Positive` | Clear positive effect observed |
| `Neutral` | No meaningful change detected |
| `Negative` | Metrics worsened |
| `Inconclusive` | Insufficient data or too many confounds |

### ExperimentCreatedFrom

Tracks the origin of the experiment.

| Value | Description |
|-------|-------------|
| `Manual` | User-created |
| `WeeklyReview` | Weekly review recommendation |
| `Diagnostic` | Diagnostic Engine recommendation |
| `Coaching` | Coaching Engine suggestion |

---

## Status Lifecycle & Business Rules

### State Machine

```
                    ┌──────────────┐
                    │    Draft     │
                    └──────┬───────┘
                           │ Start()
                           ▼
                    ┌──────────────┐
              ┌────►│    Active    │◄────┐
              │     └──┬───┬───┬──┘     │
              │        │   │   │        │
              │  Pause │   │   │ Resume │
              │        ▼   │   │        │
              │  ┌─────────┴┐  │        │
              │  │  Paused  ├──┘        │
              │  └──┬───┬───┘           │
              │     │   │               │
              └─────┘   │               │
                        │               │
         Complete()     │    Complete() or Abandon()
         or Abandon()   │    (from Active)
                        ▼
              ┌──────────────┐    ┌──────────────┐
              │  Completed   │    │  Abandoned    │
              └──────┬───────┘    └──────┬───────┘
                     │ Archive()         │ Archive()
                     ▼                   ▼
              ┌──────────────────────────────┐
              │           Archived           │
              └──────────────────────────────┘
```

### Transition Rules

| Transition | Source Status | Target Status | Side Effects |
|------------|-------------|---------------|-------------- |
| **Start** | Draft | Active | Sets `StartDate` to today (if null). Sets `EndDatePlanned` to `StartDate + RunWindowDays` (if null). Checks no other active experiment exists. |
| **Pause** | Active | Paused | — |
| **Resume** | Paused | Active | Checks no other active experiment exists. |
| **Complete** | Active, Paused | Completed | Sets `EndDateActual` to today. Requires `ExperimentResult`. Auto-computes `Delta` and `DeltaPercent` if both baseline and run values provided. |
| **Abandon** | Active, Paused | Abandoned | Sets `EndDateActual` to today. Optional `reason` string. |
| **Archive** | Completed, Abandoned | Archived | — |

### Key Invariants

1. **Single active experiment per user** — Enforced at:
   - Database level: unique filtered index `IX_Experiments_UserId_ActiveUnique` where `Status = 'Active'`
   - Application level: `HasActiveExperimentAsync()` check in Start and Resume handlers

2. **Draft-only editing** — `Update()` throws `DomainException` if `Status != Draft`

3. **Result requires completion** — `ExperimentResult` can only be attached via `Complete()` on an Active or Paused experiment

4. **One result per experiment** — Unique index on `ExperimentResult.ExperimentId`

5. **User isolation** — All queries and commands verify the experiment belongs to the current user

6. **Date auto-population on Start**:
   - `StartDate` defaults to `DateOnly.FromDateTime(DateTime.UtcNow)` if not pre-set
   - `EndDatePlanned` defaults to `StartDate + RunWindowDays` if not pre-set

---

## Domain Events

All status transitions emit domain events for downstream processing (diagnostics, coaching, learning engine).

| Event | Payload | Emitted When |
|-------|---------|-------------- |
| `ExperimentCreatedEvent` | ExperimentId, UserId, Title | `Create()` factory method |
| `ExperimentStartedEvent` | ExperimentId, UserId | `Start()` transition |
| `ExperimentPausedEvent` | ExperimentId, UserId | `Pause()` transition |
| `ExperimentResumedEvent` | ExperimentId, UserId | `Resume()` transition |
| `ExperimentCompletedEvent` | ExperimentId, UserId, Outcome | `Complete()` transition |
| `ExperimentAbandonedEvent` | ExperimentId, UserId, Reason? | `Abandon()` transition |

---

## API Endpoints

Base path: `api/experiments`

### Queries

| Method | Path | Description | Response |
|--------|------|-------------|----------|
| `GET` | `/` | List experiments (optional `?status=` filter) | `ExperimentSummaryDto[]` |
| `GET` | `/{id}` | Get experiment with full details | `ExperimentDto` |
| `GET` | `/active` | Get the current active experiment | `ExperimentDto` or `204 No Content` |

**List filtering**: When no `status` query parameter is provided, all experiments except `Archived` are returned. When a valid status string is provided, only experiments with that status are returned.

### Commands

| Method | Path | Body | Description | Response |
|--------|------|------|-------------|----------|
| `POST` | `/` | `CreateExperimentRequest` | Create a new experiment (Draft) | `201` + Guid |
| `PUT` | `/{id}` | `UpdateExperimentRequest` | Update a draft experiment | `204` |
| `PUT` | `/{id}/start` | — | Start experiment (Draft → Active) | `204` |
| `PUT` | `/{id}/pause` | — | Pause experiment (Active → Paused) | `204` |
| `PUT` | `/{id}/resume` | — | Resume experiment (Paused → Active) | `204` |
| `PUT` | `/{id}/complete` | `CompleteExperimentRequest` | Complete with results | `204` |
| `PUT` | `/{id}/abandon` | `AbandonExperimentRequest` | Abandon experiment | `204` |
| `POST` | `/{id}/notes` | `AddExperimentNoteRequest` | Add observation note | `201` + Guid |

### Request/Response Types

**CreateExperimentRequest**:
```json
{
  "title": "Morning meditation experiment",
  "category": "Mindset",
  "createdFrom": "Manual",
  "hypothesis": {
    "change": "Meditate for 10 minutes every morning",
    "expectedOutcome": "Reduce average daily stress score by 20%",
    "rationale": "Mindfulness reduces cortisol and improves emotional regulation"
  },
  "measurementPlan": {
    "primaryMetricDefinitionId": "guid",
    "primaryAggregation": "Average",
    "baselineWindowDays": 7,
    "runWindowDays": 14,
    "guardrailMetricDefinitionIds": ["guid"],
    "minComplianceThreshold": 0.7
  },
  "description": "optional detailed description",
  "linkedGoalIds": ["guid"],
  "startDate": "2026-02-01",
  "endDatePlanned": "2026-02-15"
}
```

**CompleteExperimentRequest**:
```json
{
  "outcomeClassification": "Positive",
  "baselineValue": 4.2,
  "runValue": 3.1,
  "complianceRate": 0.85,
  "narrativeSummary": "Stress scores dropped meaningfully. Will adopt as permanent habit."
}
```

**AbandonExperimentRequest**:
```json
{
  "reason": "Got sick and couldn't maintain the protocol"
}
```

**AddExperimentNoteRequest**:
```json
{
  "content": "Day 3: Noticed I feel calmer during morning meetings"
}
```

### DTO Shapes

**ExperimentDto** (full detail):
- All entity fields serialized
- `hypothesis.summary` — auto-generated "If I... then... because..." string
- `notes[]` — all notes with content and timestamps
- `result` — includes computed delta/deltaPercent
- `daysRemaining` — computed from EndDatePlanned vs today (null if no planned end, 0 if overdue)
- `daysElapsed` — computed from StartDate vs today or EndDateActual

**ExperimentSummaryDto** (list view):
- Core fields: id, title, category, status, createdFrom
- `hypothesisSummary` — the formatted hypothesis string
- Date fields: startDate, endDatePlanned, daysRemaining, daysElapsed
- `outcomeClassification` — only present for completed experiments
- `noteCount` — number of attached notes
- `hasResult` — boolean flag

---

## Implementation Architecture

### Backend Layers

```
API Controller (ExperimentsController)
    │ Maps HTTP requests to MediatR commands/queries
    ▼
Application Layer (Commands + Queries via MediatR)
    │ Orchestrates use cases, validates input
    ▼
Domain Layer (Experiment aggregate + value objects)
    │ Enforces business rules and invariants
    ▼
Infrastructure Layer (EF Core + SQL Server)
    │ Persists entities, manages transactions
    ▼
Database (Experiments, ExperimentNotes, ExperimentResults tables)
```

### Command Handlers

| Command | Handler Logic |
|---------|-------------- |
| `CreateExperiment` | Parse enums → create value objects → `Experiment.Create()` → save |
| `UpdateExperiment` | Verify ownership → verify Draft status → `experiment.Update()` → save |
| `StartExperiment` | Verify ownership → check no active experiment → `experiment.Start()` → save |
| `PauseExperiment` | Verify ownership → `experiment.Pause()` → save |
| `ResumeExperiment` | Verify ownership → check no active experiment → `experiment.Resume()` → save |
| `CompleteExperiment` | Verify ownership → compute delta → `ExperimentResult.Create()` → `experiment.Complete(result)` → save |
| `AbandonExperiment` | Verify ownership → `experiment.Abandon(reason)` → save |
| `AddExperimentNote` | Verify ownership → `experiment.AddNote(content)` → save → return note ID |

### Delta Computation (Complete Handler)

When both `baselineValue` and `runValue` are provided on completion:

```
delta = runValue - baselineValue
deltaPercent = (baselineValue != 0) ? (delta / baselineValue) * 100 : null
```

This is computed in the application layer before creating the `ExperimentResult` value object.

### Validation Rules (FluentValidation)

**CreateExperimentCommand**:
- `Title`: required, max 200 characters
- `Category`: must be a valid enum value
- `CreatedFrom`: must be a valid enum value
- `Hypothesis.Change`: required, max 500 characters
- `Hypothesis.ExpectedOutcome`: required, max 500 characters
- `Hypothesis.Rationale`: max 1000 characters
- `MeasurementPlan.PrimaryMetricDefinitionId`: required, valid GUID
- `MeasurementPlan.PrimaryAggregation`: valid MetricAggregation enum
- `MeasurementPlan.BaselineWindowDays`: 1–90
- `MeasurementPlan.RunWindowDays`: 1–90
- `MeasurementPlan.MinComplianceThreshold`: 0.0–1.0

**CompleteExperimentCommand**:
- `OutcomeClassification`: must be a valid enum value
- `ComplianceRate`: 0.0–1.0 (when provided)
- `NarrativeSummary`: max 4000 characters

### Database Schema

**Experiments table**:
- `Id` (PK, Guid)
- `UserId` (nvarchar 256, required, indexed)
- `Title` (nvarchar 200, required)
- `Description` (nvarchar 2000)
- `Category` (nvarchar 30, stored as string)
- `Status` (nvarchar 20, stored as string)
- `CreatedFrom` (nvarchar 30, stored as string)
- `Hypothesis` (nvarchar(max), JSON)
- `MeasurementPlan` (nvarchar(max), JSON)
- `LinkedGoalIds` (nvarchar(max), JSON array)
- `StartDate`, `EndDatePlanned`, `EndDateActual` (DateOnly)
- `CreatedAt`, `ModifiedAt`, `CreatedBy`, `ModifiedBy` (audit fields)

**Indexes**:
- `IX_Experiments_UserId` — fast user lookups
- `IX_Experiments_UserId_Status` — filtered listing
- `IX_Experiments_UserId_ActiveUnique` — unique filtered index where `Status = 'Active'` (enforces single-active rule)

**ExperimentNotes table**:
- `Id` (PK, Guid)
- `ExperimentId` (FK, cascade delete)
- `Content` (nvarchar 2000, required)
- `CreatedAt` (DateTime)

**ExperimentResults table**:
- `Id` (PK, Guid)
- `ExperimentId` (FK, cascade delete, unique index)
- `BaselineValue`, `RunValue`, `Delta`, `DeltaPercent` (decimal 18,4)
- `OutcomeClassification` (nvarchar 30, stored as string)
- `ComplianceRate` (decimal 5,2)
- `NarrativeSummary` (nvarchar 4000)
- `ComputedAt` (DateTime)

**JSON column strategy**: `Hypothesis` and `MeasurementPlan` are stored as JSON in nvarchar(max) columns with EF Core value converters. This avoids schema proliferation for value objects that are always read as a whole.

---

## UI Implementation

### Pages

| Route | Component | Description |
|-------|-----------|-------------|
| `/experiments` | `ExperimentsPage` | Main list with status filter tabs |
| `/experiments/new` | `CreateExperimentPage` | 4-step creation wizard |
| `/experiments/:id` | `ExperimentDetailPage` | Full detail view with actions |
| `/experiments/:id/edit` | `EditExperimentPage` | Edit form (draft only) |

### Creation Wizard (4 Steps)

The experiment creation flow uses a multi-step wizard with per-step validation:

**Step 0 — Basics**:
- Title (required), Description (optional)
- Category (dropdown with all 12 categories)
- Created From (dropdown: Manual, Weekly Review, Diagnostic, Coaching)
- Start Date, Planned End Date (optional date pickers)
- Linked Goals (multi-select Popover with searchable Command list, badge chips)

**Step 1 — Hypothesis**:
- "If I..." — Change field (color-coded blue section)
- "Then..." — Expected Outcome field (color-coded green section)
- "Because..." — Rationale field (color-coded amber section, optional)

**Step 2 — Measurement Plan**:
- Primary Metric — selectable via `MetricLibraryDialog` with inline metric creation
- Aggregation method (Sum, Average, Max, Min, Count, Latest)
- Baseline Window (days slider/input, 1–90)
- Run Window (days slider/input, 1–90)
- Min. Compliance Threshold (percentage input, 0–100%)
- Guardrail Metrics — multi-select Popover with searchable Command list, excludes primary metric, supports inline metric creation via `MetricLibraryDialog`

**Step 3 — Review**:
- Summary cards for each section with metric names resolved (not GUIDs)
- Edit links to jump back to specific steps
- Submit button (explicit click, no form auto-submit)

### Detail Page Components

| Component | Description |
|-----------|-------------|
| `ExperimentHeader` | Title, status/category badges, description, action buttons (Start/Pause/Resume/Complete/Abandon) with confirmation dialogs |
| `HypothesisCard` | Displays If/Then/Because structure with color-coded sections |
| `MeasurementPlanCard` | Primary metric with name/type/direction resolved from metric definitions, baseline/run windows, compliance bar, guardrail metrics list |
| `ExperimentResultsCard` | Outcome badge, baseline vs. run comparison, delta with trend indicators, compliance rate, narrative summary |
| `ExperimentNotes` | Note input with Cmd/Ctrl+Enter shortcut, timeline view with relative timestamps |
| `ExperimentTimeline` | Visual lifecycle timeline (Created → Active → Paused → Completed/Abandoned) with dates |
| `CompleteExperimentDialog` | Modal form for outcome classification, baseline/run values, compliance rate, narrative |

### List View

- Status filter tabs: Active, Drafts, Paused, Completed, Abandoned, All
- `ExperimentCard` — color-coded left border by category, status and category badges, title, hypothesis summary, date range, days remaining/elapsed, note count, outcome badge for completed experiments

### Frontend Architecture

```
Types (experiment.ts)
    │ TypeScript interfaces + UI helper objects
    ▼
API Layer (experiments-api.ts)
    │ Axios-based HTTP client functions
    ▼
Hooks (use-experiments.ts)
    │ TanStack Query hooks (queries + mutations)
    │ Query key factory for cache management
    ▼
Schemas (experiment-schema.ts)
    │ Zod validation schemas for forms
    ▼
Components + Pages
    │ React components consuming hooks
    ▼
Router (lazy-loaded routes)
```

---

## Integration Points

### Metrics System

Experiments depend on the Goals & Metrics feature for metric definitions:
- `MeasurementPlan.PrimaryMetricDefinitionId` references a `MetricDefinition`
- `MeasurementPlan.GuardrailMetricDefinitionIds` reference `MetricDefinition` entities
- The UI resolves metric GUIDs to names, data types, directions, and units via `useMetrics()` hook
- Inline metric creation is supported through `MetricLibraryDialog`

### Goals System

Experiments can be linked to goals via `LinkedGoalIds`:
- Links are informational (no cascading behavior)
- The UI displays linked goal titles via `useGoals()` hook
- Goals provide context for why an experiment matters

### Weekly Review (Future)

The weekly review flow will:
1. Show the current active experiment's progress
2. Prompt for notes if none were added that week
3. Suggest completion if the run window has elapsed
4. Propose the next experiment from the Diagnostic Engine's recommendations

### Learning Engine (Future)

Completed experiment results feed the Learning Engine:
- `ExperimentCompletedEvent` triggers result recording
- Outcome + category + compliance rate build a per-user "what works" playbook
- Interventions with `Positive` outcomes get higher weight in future recommendations

---

## Known Considerations

### ExperimentOutcome Enum Mismatch

The backend uses `Positive`, `Neutral`, `Negative`, `Inconclusive` while the frontend uses `Success`, `PartialSuccess`, `NoEffect`, `Negative`, `Inconclusive`. The frontend has five values vs. the backend's four. This will need alignment — the frontend values provide finer granularity that may be preferable.

### Pause Duration Not Tracked

When an experiment is paused and resumed, the pause duration is not subtracted from the total experiment timeline. This means DaysElapsed and DaysRemaining include pause time. A future enhancement could track cumulative pause days.

### No Metric Observation Validation

The system does not currently verify that actual metric observations exist within the baseline or run windows. Compliance rate is manually entered by the user at completion time. Future automation would query the MetricObservation table to compute compliance automatically.

---

## Glossary

| Term | Definition |
|------|------------|
| **Hypothesis** | A structured prediction: "If I [change], then [outcome] because [rationale]" |
| **Measurement Plan** | Configuration defining what metric to watch, over what time windows, with what minimum data coverage |
| **Baseline Window** | The number of days before the experiment starts used to establish "normal" metric values |
| **Run Window** | The number of days the experiment runs to collect treatment data |
| **Compliance Threshold** | The minimum fraction of days with metric data required for results to be valid (e.g., 0.7 = 70% of days) |
| **Guardrail Metric** | A secondary metric monitored for unintended side effects of the experiment |
| **Primary Metric** | The main metric being tracked to determine experiment success |
| **Aggregation** | How multiple metric observations within a window are combined (Sum, Average, Max, Min, Count, Latest) |
| **Delta** | The absolute difference between run and baseline values |
| **Delta Percent** | The percentage change from baseline to run value |
| **Outcome Classification** | The user's assessment of the experiment result (Positive, Neutral, Negative, Inconclusive) |
| **Single Active Constraint** | Business rule: at most one experiment can be in Active status per user at any time |
