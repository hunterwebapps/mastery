# Goals & Metrics Feature Documentation

## Overview

The **Goals & Metrics** feature is the core tracking system in Mastery. Goals represent what users want to achieve, while Metrics provide the measurement framework. Together, they form a "Scoreboard" pattern that turns vague intentions into measurable progress.

**Design Philosophy**: Goals are not just todo items—they're measurable objectives with leading indicators, outcome metrics, and guardrails. This design is inspired by control systems theory and OKR (Objectives and Key Results) methodologies.

---

## Business Context

### Why Goals & Metrics Exists

Most productivity apps fail because they help you plan but not execute. Mastery's Goals & Metrics feature addresses this by:

1. **Making progress measurable** - Every goal has concrete metrics, not just descriptions
2. **Distinguishing inputs from outputs** - Lead metrics (behaviors) predict Lag metrics (outcomes)
3. **Protecting what matters** - Constraint metrics prevent tunnel vision
4. **Enabling course correction** - Regular observation data feeds the diagnostic engine

### The Control System Analogy

In control theory terms:

| Control Concept | Goals & Metrics Component |
|-----------------|--------------------------|
| **Setpoint** | Goal target + deadline |
| **Sensors** | Metric observations (time-series data) |
| **Controller Input** | Aggregated metric values vs. targets |
| **Plant Output** | Current goal progress/health |
| **Feedback Loop** | Daily/weekly metric tracking |

### The Scoreboard Pattern

Each goal has a "scoreboard" with recommended composition:

```
Goal Scoreboard
├── 1 Lag Metric (Outcome) ────── What you're trying to achieve
├── 2 Lead Metrics (Inputs) ───── Behaviors that predict success
└── 1 Constraint Metric ─────── What you won't sacrifice
```

**Example**: "Improve Fitness" Goal
- **Lag**: Body weight (kg) - Target: 75kg
- **Lead 1**: Workout sessions per week - Target: ≥4
- **Lead 2**: Daily steps - Target: ≥8000
- **Constraint**: Sleep hours - Target: ≥7 (don't sacrifice sleep for exercise)

---

## Domain Model

### Entity Hierarchy

```
Goal (Aggregate Root)
├── Id (Guid)
├── UserId (string) ────────────── Owner
├── Title (string, max 200) ─────── Display name
├── Description? (string) ──────── Detailed description
├── Why? (string) ──────────────── Motivation (for coaching)
├── Status (enum) ──────────────── Lifecycle state
├── Priority (1-5) ─────────────── Importance ranking
├── Deadline? (DateOnly) ───────── Target completion date
├── SeasonId? (FK) ─────────────── Associated season
├── RoleIds[] (JSON) ───────────── Associated user roles
├── ValueIds[] (JSON) ──────────── Aligned user values
├── DependencyIds[] (JSON) ──────── Blocked-by goals
│
├── Metrics[] (Child Entities) ─── The Scoreboard
│   └── GoalMetric
│       ├── Id (Guid)
│       ├── MetricDefinitionId (FK) ─ References reusable definition
│       ├── Kind (enum) ──────────── Lag / Lead / Constraint
│       ├── Target (Value Object) ── Target configuration
│       │   ├── Type ───────────── AtLeast / AtMost / Between / Exactly
│       │   ├── Value ──────────── Primary target value
│       │   └── MaxValue? ──────── For Between type
│       ├── EvaluationWindow (VO) ── Time window for evaluation
│       │   ├── Type ───────────── Daily / Weekly / Monthly / Rolling
│       │   ├── RollingDays? ──── For Rolling type (1-365)
│       │   └── StartDay? ──────── For Weekly (customize start)
│       ├── Aggregation (enum) ──── Sum / Average / Max / Min / Count / Latest
│       ├── SourceHint (enum) ───── Expected data source
│       ├── Weight (0.0-1.0) ────── Importance in goal health
│       ├── DisplayOrder (int) ──── Position in scoreboard
│       ├── Baseline? (decimal) ─── Starting point for progress
│       └── MinimumThreshold? ───── Drift warning threshold
│
├── CompletionNotes? (string) ──── Notes when completed
└── CompletedAt? (DateTime) ────── Completion timestamp


MetricDefinition (Aggregate Root) ─ Reusable metric template
├── Id (Guid)
├── UserId (string) ────────────── Owner
├── Name (string, max 100) ──────── Display name
├── Description? (string) ──────── What this measures
├── DataType (enum) ────────────── Number / Boolean / Duration / etc.
├── Unit (Value Object) ─────────── Unit of measurement
│   ├── UnitType ───────────────── Category
│   ├── DisplayLabel ───────────── Singular (e.g., "min")
│   └── PluralLabel? ───────────── Plural (e.g., "mins")
├── Direction (enum) ───────────── Increase / Decrease / Maintain
├── DefaultCadence (enum) ──────── Suggested observation window
├── DefaultAggregation (enum) ──── Suggested aggregation
├── IsArchived (bool) ──────────── Soft-delete flag
└── Tags[] (JSON) ──────────────── Categorization


MetricObservation (Entity) ──────── Time-series data point
├── Id (Guid)
├── MetricDefinitionId (FK)
├── UserId (string)
├── ObservedAt (DateTime) ──────── Exact UTC timestamp
├── ObservedOn (DateOnly) ──────── Date in user timezone (for queries)
├── Value (decimal) ────────────── Numeric value
├── Source (enum) ──────────────── Manual / Habit / Task / etc.
├── CorrelationId? (string) ────── Link to source event
├── Note? (string)
├── CorrectedObservationId? ────── If this corrects another
└── IsCorrected (bool) ─────────── Superseded by correction
```

---

## Enums Reference

### GoalStatus

Controls goal lifecycle and what operations are permitted:

| Status | Description | Allowed Transitions |
|--------|-------------|---------------------|
| `Draft` | Being defined, not active | → Active, → Archived |
| `Active` | Actively working toward | → Paused, → Completed, → Archived |
| `Paused` | Temporarily on hold | → Active, → Completed, → Archived |
| `Completed` | Goal achieved | → Archived |
| `Archived` | Soft-deleted | (terminal state) |

**Business Rules**:
- Cannot update a Completed or Archived goal
- Completing a goal requires Active or Paused status
- Archive is available from any non-archived state

### MetricKind

Defines the role a metric plays in the goal scoreboard:

| Kind | Purpose | Recommended Count | Example |
|------|---------|-------------------|---------|
| `Lag` | Outcome metric - the result | 1 per goal | Revenue, Weight, Test Score |
| `Lead` | Leading indicator - predictive behavior | 2 per goal | Sales Calls, Workouts, Study Hours |
| `Constraint` | Guardrail - what not to sacrifice | 1 per goal | Sleep, Family Time, Health |

### TargetType

How to evaluate if a metric is on track:

| Type | Meaning | Example |
|------|---------|---------|
| `AtLeast` | Value must be >= target | "At least 5 workouts/week" |
| `AtMost` | Value must be <= target | "At most 2000 calories/day" |
| `Between` | Value must be in range | "Body fat 15-20%" |
| `Exactly` | Value must match target | "Exactly 8 hours sleep" |

### WindowType

Time period for aggregating observations:

| Type | Window | Use Case |
|------|--------|----------|
| `Daily` | Single day | Daily habits, quick feedback |
| `Weekly` | Mon-Sun (configurable) | Most behaviors, balanced view |
| `Monthly` | 1st to last day | Business metrics, long cycles |
| `Rolling` | Last N days | Smoothed averages, trends |

### MetricDataType

What kind of values this metric accepts:

| Type | Value Range | Example Metrics |
|------|-------------|-----------------|
| `Number` | Any decimal | Weight, Revenue, Hours |
| `Boolean` | 0 or 1 | "Did I exercise?" |
| `Duration` | Minutes | Deep work time, Sleep |
| `Percentage` | 0-100 | Completion rate |
| `Count` | Positive integers | Steps, Reps, Items |
| `Rating` | 1-5 | Mood, Energy level |

### MetricDirection

What direction is "good" for this metric:

| Direction | Meaning | Progress Calculation |
|-----------|---------|---------------------|
| `Increase` | Higher is better | (current - baseline) / (target - baseline) |
| `Decrease` | Lower is better | (baseline - current) / (baseline - target) |
| `Maintain` | Stay in range | 1.0 if in range, 0.0 otherwise |

### MetricAggregation

How to combine multiple observations in a window:

| Aggregation | Use Case |
|-------------|----------|
| `Sum` | Cumulative (total hours, total steps) |
| `Average` | Mean value (average mood, average calories) |
| `Max` | Peak value (best workout, highest score) |
| `Min` | Floor value (minimum sleep) |
| `Count` | Number of observations (workout sessions) |
| `Latest` | Most recent value (current weight) |

### MetricSourceType

Where observation data comes from:

| Source | Description |
|--------|-------------|
| `Manual` | User entered directly |
| `Habit` | From habit completion |
| `Task` | From task completion |
| `CheckIn` | From daily check-in |
| `Integration` | From external integration |
| `Computed` | Calculated from other metrics |

---

## Value Objects

### Target

Encapsulates target configuration with validation and progress calculation:

```csharp
Target.AtLeast(5)           // >= 5
Target.AtMost(2000)         // <= 2000
Target.Between(15, 20)      // 15 <= x <= 20
Target.Exactly(8)           // == 8

target.IsMet(actualValue)   // Returns bool
target.GetProgress(actual, baseline) // Returns 0.0-1.0+
```

**Progress Calculation**:
- AtLeast: `(actual - baseline) / (target - baseline)`
- AtMost: `(baseline - actual) / (baseline - target)`
- Between: 1.0 if in range, else 0.0
- Exactly: 1.0 if met, else 0.0

### EvaluationWindow

Defines the time window for metric evaluation:

```csharp
EvaluationWindow.Daily()
EvaluationWindow.Weekly()           // Mon-Sun
EvaluationWindow.Weekly(startDay: 0) // Sun-Sat
EvaluationWindow.Monthly()
EvaluationWindow.Rolling(days: 30)

window.GetDateRange(referenceDate) // Returns (start, end)
```

### MetricUnit

Handles unit display and formatting:

```csharp
MetricUnit.Minutes   // "min" / "mins"
MetricUnit.Hours     // "hour" / "hours"
MetricUnit.Dollars   // "$" prefix
MetricUnit.Percentage // "%" suffix

unit.Format(value)  // "5 hours", "$100.00", "85%"
```

**Pre-defined Units**: Minutes, Hours, Count, Sessions, Percentage, Dollars, Pounds, Kilograms, Rating, Boolean, Steps, Calories, None

---

## Domain Events

### Goal Events

| Event | Trigger | Typical Handler Action |
|-------|---------|------------------------|
| `GoalCreatedEvent` | New goal created | Update user stats, suggest habits |
| `GoalUpdatedEvent` | Goal details changed | Audit log |
| `GoalStatusChangedEvent` | Status transition | Update dashboard, notify |
| `GoalCompletedEvent` | Goal marked complete | Celebration notification, retrospective prompt |
| `GoalScoreboardUpdatedEvent` | Metrics changed | Recalculate goal health |

### Metric Events

| Event | Trigger | Typical Handler Action |
|-------|---------|------------------------|
| `MetricDefinitionCreatedEvent` | New metric defined | Suggest to relevant goals |
| `MetricDefinitionUpdatedEvent` | Definition changed | Recalculate affected goals |
| `MetricDefinitionArchivedEvent` | Definition archived | Warn if used in active goals |
| `MetricObservationRecordedEvent` | New observation | Update goal progress, check thresholds |
| `MetricObservationCorrectedEvent` | Observation corrected | Recalculate affected aggregations |

---

## API Endpoints

### Goal Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/goals` | List goals (filterable by status) |
| `GET` | `/api/goals/{id}` | Get goal with full details |
| `POST` | `/api/goals` | Create new goal with optional metrics |
| `PUT` | `/api/goals/{id}` | Update goal details (not metrics) |
| `PUT` | `/api/goals/{id}/status` | Change goal status |
| `PUT` | `/api/goals/{id}/scoreboard` | Replace entire metric scoreboard |
| `DELETE` | `/api/goals/{id}` | Archive goal (soft delete) |

### Metric Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/metrics` | List metric definitions |
| `POST` | `/api/metrics` | Create metric definition |
| `PUT` | `/api/metrics/{id}` | Update metric definition |
| `POST` | `/api/metrics/{id}/observations` | Record observation |
| `GET` | `/api/metrics/{id}/observations` | Get observations (date range) |

### Request/Response Examples

**Create Goal with Metrics (POST /api/goals)**
```json
{
  "title": "Get Fit for Summer",
  "description": "Improve overall fitness and reach target weight",
  "why": "Feel confident and have energy for activities with family",
  "priority": 2,
  "deadline": "2026-06-01",
  "metrics": [
    {
      "metricDefinitionId": "...",
      "kind": "Lag",
      "target": { "type": "AtMost", "value": 75 },
      "evaluationWindow": { "windowType": "Weekly" },
      "aggregation": "Latest",
      "sourceHint": "Manual"
    },
    {
      "metricDefinitionId": "...",
      "kind": "Lead",
      "target": { "type": "AtLeast", "value": 4 },
      "evaluationWindow": { "windowType": "Weekly" },
      "aggregation": "Count",
      "sourceHint": "Habit"
    }
  ]
}
```

**Goal Response (GET /api/goals/{id})**
```json
{
  "id": "a1b2c3d4-...",
  "userId": "auth0|123456",
  "title": "Get Fit for Summer",
  "description": "...",
  "why": "...",
  "status": "Active",
  "priority": 2,
  "deadline": "2026-06-01",
  "metrics": [
    {
      "id": "m1m2m3m4-...",
      "metricDefinitionId": "...",
      "metricName": "Body Weight",
      "kind": "Lag",
      "target": { "type": "AtMost", "value": 75, "maxValue": null },
      "evaluationWindow": { "windowType": "Weekly" },
      "aggregation": "Latest",
      "weight": 1.0,
      "sourceHint": "Manual",
      "displayOrder": 0,
      "unit": { "type": "weight", "label": "kg" }
    }
  ],
  "createdAt": "2026-01-26T..."
}
```

**Create Metric Definition (POST /api/metrics)**
```json
{
  "name": "Deep Work Hours",
  "description": "Focused, uninterrupted work time",
  "dataType": "Duration",
  "unit": { "type": "duration", "label": "hours" },
  "direction": "Increase",
  "defaultCadence": "Daily",
  "defaultAggregation": "Sum",
  "tags": ["productivity", "focus"]
}
```

**Record Observation (POST /api/metrics/{id}/observations)**
```json
{
  "value": 3.5,
  "observedOn": "2026-01-26",
  "source": "Manual",
  "note": "Good focus session this morning"
}
```

---

## Implementation Architecture

### Layer Responsibilities

```
┌─────────────────────────────────────────────────────────────────┐
│                         API Layer                               │
│  Controllers/GoalsController.cs, MetricsController.cs           │
│  - HTTP endpoints & routing                                     │
│  - Request/response mapping                                     │
│  - Input validation (structural)                                │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                     Application Layer                           │
│  Features/Goals/Commands/, Features/Goals/Queries/              │
│  Features/Metrics/Commands/, Features/Metrics/Queries/          │
│  - Use case orchestration (CQRS via MediatR)                   │
│  - Business validation (FluentValidation)                       │
│  - DTO mapping                                                  │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                       Domain Layer                              │
│  Entities/Goal/, Entities/Metrics/                              │
│  ValueObjects/, Enums/, Events/                                 │
│  - Business rules & invariants                                  │
│  - Status transitions                                           │
│  - Domain events                                                │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                   Infrastructure Layer                          │
│  Data/Configurations/, Repositories/                            │
│  - EF Core mapping (incl. JSON columns)                        │
│  - Database operations                                          │
│  - Repository implementations                                   │
└─────────────────────────────────────────────────────────────────┘
```

### Key Files Reference

| Layer | File | Purpose |
|-------|------|---------|
| Domain | `Entities/Goal/Goal.cs` | Goal aggregate with business logic |
| Domain | `Entities/Goal/GoalMetric.cs` | Scoreboard metric entity |
| Domain | `Entities/Metrics/MetricDefinition.cs` | Reusable metric template |
| Domain | `Entities/Metrics/MetricObservation.cs` | Time-series observation |
| Domain | `ValueObjects/Target.cs` | Target with progress calculation |
| Domain | `ValueObjects/EvaluationWindow.cs` | Window with date range logic |
| Domain | `ValueObjects/MetricUnit.cs` | Unit with formatting |
| Domain | `Enums/GoalStatus.cs` | Goal lifecycle states |
| Domain | `Enums/MetricKind.cs` | Lag/Lead/Constraint |
| Domain | `Events/GoalEvents.cs` | Goal domain events |
| Domain | `Events/MetricEvents.cs` | Metric domain events |
| Application | `Features/Goals/Commands/CreateGoal/` | Goal creation use case |
| Application | `Features/Goals/Queries/GetGoals/` | Goal listing (excludes archived) |
| Application | `Features/Metrics/Commands/RecordObservation/` | Observation recording |
| Infrastructure | `Data/Configurations/GoalConfiguration.cs` | EF mapping for Goal |
| Infrastructure | `Repositories/GoalRepository.cs` | Goal data access |
| API | `Controllers/GoalsController.cs` | Goal HTTP endpoints |
| API | `Contracts/Goals/Requests.cs` | Goal request DTOs |

### Database Schema

```sql
-- Goals table
CREATE TABLE Goals (
    Id uniqueidentifier PRIMARY KEY,
    UserId nvarchar(256) NOT NULL,
    Title nvarchar(200) NOT NULL,
    Description nvarchar(max) NULL,
    Why nvarchar(1000) NULL,
    Status nvarchar(20) NOT NULL,
    Priority int NOT NULL DEFAULT 3,
    Deadline date NULL,
    SeasonId uniqueidentifier NULL,
    RoleIds nvarchar(max) NOT NULL,        -- JSON array
    ValueIds nvarchar(max) NOT NULL,       -- JSON array
    DependencyIds nvarchar(max) NOT NULL,  -- JSON array
    CompletionNotes nvarchar(max) NULL,
    CompletedAt datetime2 NULL,
    CreatedAt datetime2 NOT NULL,
    CreatedBy nvarchar(256) NULL,
    ModifiedAt datetime2 NULL,
    ModifiedBy nvarchar(256) NULL
);

-- GoalMetrics table (child of Goals)
CREATE TABLE GoalMetrics (
    Id uniqueidentifier PRIMARY KEY,
    GoalId uniqueidentifier NOT NULL,
    MetricDefinitionId uniqueidentifier NOT NULL,
    Kind nvarchar(20) NOT NULL,
    TargetType nvarchar(20) NOT NULL,
    TargetValue decimal(18,4) NOT NULL,
    TargetMaxValue decimal(18,4) NULL,
    WindowType nvarchar(20) NOT NULL,
    RollingDays int NULL,
    StartDay int NULL,
    Aggregation nvarchar(20) NOT NULL,
    SourceHint nvarchar(20) NOT NULL,
    Weight decimal(5,4) NOT NULL DEFAULT 1.0,
    DisplayOrder int NOT NULL DEFAULT 0,
    Baseline decimal(18,4) NULL,
    MinimumThreshold decimal(18,4) NULL,
    CreatedAt datetime2 NOT NULL,
    ModifiedAt datetime2 NULL,

    CONSTRAINT FK_GoalMetrics_Goals FOREIGN KEY (GoalId)
        REFERENCES Goals(Id) ON DELETE CASCADE,
    CONSTRAINT FK_GoalMetrics_MetricDefinitions FOREIGN KEY (MetricDefinitionId)
        REFERENCES MetricDefinitions(Id)
);

-- MetricDefinitions table
CREATE TABLE MetricDefinitions (
    Id uniqueidentifier PRIMARY KEY,
    UserId nvarchar(256) NOT NULL,
    Name nvarchar(100) NOT NULL,
    Description nvarchar(max) NULL,
    DataType nvarchar(20) NOT NULL,
    UnitType nvarchar(50) NULL,
    UnitLabel nvarchar(20) NULL,
    UnitPluralLabel nvarchar(20) NULL,
    Direction nvarchar(20) NOT NULL,
    DefaultCadence nvarchar(20) NOT NULL,
    DefaultAggregation nvarchar(20) NOT NULL,
    IsArchived bit NOT NULL DEFAULT 0,
    Tags nvarchar(max) NOT NULL,           -- JSON array
    CreatedAt datetime2 NOT NULL,
    CreatedBy nvarchar(256) NULL,
    ModifiedAt datetime2 NULL,
    ModifiedBy nvarchar(256) NULL
);

-- MetricObservations table
CREATE TABLE MetricObservations (
    Id uniqueidentifier PRIMARY KEY,
    MetricDefinitionId uniqueidentifier NOT NULL,
    UserId nvarchar(256) NOT NULL,
    ObservedAt datetime2 NOT NULL,
    ObservedOn date NOT NULL,              -- Denormalized for queries
    Value decimal(18,4) NOT NULL,
    Source nvarchar(20) NOT NULL,
    CorrelationId nvarchar(256) NULL,
    Note nvarchar(500) NULL,
    CorrectedObservationId uniqueidentifier NULL,
    IsCorrected bit NOT NULL DEFAULT 0,
    CreatedAt datetime2 NOT NULL,

    CONSTRAINT FK_Observations_Definitions FOREIGN KEY (MetricDefinitionId)
        REFERENCES MetricDefinitions(Id)
);

-- Indexes
CREATE INDEX IX_Goals_UserId ON Goals(UserId);
CREATE INDEX IX_Goals_UserId_Status ON Goals(UserId, Status);
CREATE INDEX IX_GoalMetrics_GoalId ON GoalMetrics(GoalId);
CREATE INDEX IX_MetricDefinitions_UserId ON MetricDefinitions(UserId);
CREATE INDEX IX_Observations_MetricId_Date ON MetricObservations(MetricDefinitionId, ObservedOn);
CREATE INDEX IX_Observations_UserId_Date ON MetricObservations(UserId, ObservedOn);
```

---

## UI Implementation

### Pages & Routes

| Route | Component | Purpose |
|-------|-----------|---------|
| `/goals` | `GoalsPage` | Goal list with status tabs |
| `/goals/new` | `CreateGoalPage` | Multi-step goal wizard |
| `/goals/:id` | `GoalDetailPage` | Goal view with scoreboard |
| `/goals/:id/edit` | `EditGoalPage` | Edit goal details |

### Component Structure

```
features/goals/
├── api/
│   ├── goals-api.ts          # Goal API calls
│   └── metrics-api.ts        # Metrics API calls
├── hooks/
│   ├── use-goals.ts          # React Query hooks for goals
│   └── use-metrics.ts        # React Query hooks for metrics
├── schemas/
│   ├── goal-schema.ts        # Zod validation schemas
│   └── metric-schema.ts
├── components/
│   ├── goal-list/
│   │   ├── goal-card.tsx     # Goal summary card
│   │   └── goals-list.tsx    # Goal list with empty state
│   ├── goal-detail/
│   │   ├── goal-header.tsx   # Title, status, actions
│   │   ├── goal-scoreboard.tsx # Metric cards with edit dialog
│   │   ├── metric-card.tsx   # Individual metric display
│   │   └── add-metric-dialog.tsx # Add metric to scoreboard
│   ├── goal-form/
│   │   ├── goal-wizard.tsx   # Multi-step form container
│   │   ├── step-basics.tsx   # Step 1: Title, description, etc.
│   │   ├── step-metrics.tsx  # Step 2: Configure scoreboard
│   │   └── step-review.tsx   # Step 3: Review and create
│   └── metric-library/
│       ├── metric-form.tsx   # Create/edit metric definition
│       └── metric-library-dialog.tsx # Browse metric definitions
└── pages/
    ├── goals-page.tsx        # List page with tabs
    ├── create-goal-page.tsx  # Wizard wrapper
    ├── goal-detail-page.tsx  # Detail view
    └── edit-goal-page.tsx    # Edit form
```

### Key UI Patterns

**1. Goal Wizard (3-Step)**
- Step 1 (Basics): Title, description, why, priority, deadline
- Step 2 (Metrics): Add metrics from library or create new
- Step 3 (Review): Preview all settings, create goal

**2. Scoreboard Editing**
- Click metric card to edit (role, target, window, aggregation)
- "Remove" button in edit dialog to remove from goal
- "Add Metric" button to add from library or create new

**3. Metric Library**
- Global "Metric Library" button on goals page
- Create reusable metric definitions
- Metrics can be used across multiple goals

**4. Status Management**
- Activate/Pause/Complete buttons based on current status
- Archive via menu dropdown
- Status badge with color coding

### State Management

```typescript
// Query Keys (TanStack Query)
export const goalKeys = {
  all: ['goals'] as const,
  lists: () => [...goalKeys.all, 'list'] as const,
  list: (status?: GoalStatus) => [...goalKeys.lists(), { status }] as const,
  details: () => [...goalKeys.all, 'detail'] as const,
  detail: (id: string) => [...goalKeys.details(), id] as const,
}

export const metricKeys = {
  all: ['metrics'] as const,
  lists: () => [...metricKeys.all, 'list'] as const,
  list: (includeArchived?: boolean) => [...metricKeys.lists(), { includeArchived }] as const,
  observations: (id: string) => [...metricKeys.all, 'observations', id] as const,
}
```

### UI Helpers

```typescript
// Goal status styling
export const goalStatusInfo: Record<GoalStatus, { label, color, bgColor }> = {
  Draft: { label: 'Draft', color: 'text-gray-400', bgColor: 'bg-gray-500/10' },
  Active: { label: 'Active', color: 'text-green-400', bgColor: 'bg-green-500/10' },
  Paused: { label: 'Paused', color: 'text-yellow-400', bgColor: 'bg-yellow-500/10' },
  Completed: { label: 'Completed', color: 'text-blue-400', bgColor: 'bg-blue-500/10' },
  Archived: { label: 'Archived', color: 'text-zinc-500', bgColor: 'bg-zinc-500/10' },
}

// Metric kind styling
export const metricKindInfo: Record<MetricKind, { label, description, color }> = {
  Lag: { label: 'Outcome', color: 'text-purple-400', description: 'The result you want' },
  Lead: { label: 'Leading', color: 'text-blue-400', description: 'Predictive behaviors' },
  Constraint: { label: 'Constraint', color: 'text-orange-400', description: 'What not to sacrifice' },
}
```

---

## Extension Guide

### Adding a New Goal Field

1. **Domain**: Add property to `Goal.cs`
   ```csharp
   public string? NewField { get; private set; }
   ```

2. **Domain**: Add to `Create()` and `Update()` methods

3. **Infrastructure**: Update `GoalConfiguration.cs`
   ```csharp
   builder.Property(g => g.NewField).HasMaxLength(200);
   ```

4. **Application**: Update `GoalDto` and request types

5. **API**: Update `Requests.cs` and controller mapping

6. **Migration**: Generate and apply
   ```bash
   dotnet ef migrations add AddGoalNewField --project Mastery.Infrastructure --startup-project Mastery.Api
   ```

7. **Frontend**: Update TypeScript types and forms

### Adding a New Metric Aggregation

1. **Domain**: Add to `MetricAggregation` enum

2. **Application**: Update any aggregation calculation logic

3. **Frontend**: Add to Select options in metric forms

### Adding a New Goal Status

1. **Domain**: Add to `GoalStatus` enum

2. **Domain**: Update `Goal.cs` transition methods

3. **Application**: Update validation rules

4. **Frontend**: Add to `goalStatusInfo` helper

### Implementing Metric Aggregation Service

The system needs a service to aggregate observations:

```csharp
public interface IMetricAggregationService
{
    Task<decimal?> GetAggregatedValueAsync(
        Guid metricDefinitionId,
        string userId,
        EvaluationWindow window,
        MetricAggregation aggregation,
        DateOnly referenceDate,
        CancellationToken cancellationToken);
}
```

This service would:
1. Get the date range from `EvaluationWindow.GetDateRange()`
2. Query observations in that range
3. Apply aggregation function
4. Return the result (null if no observations)

---

## Testing Considerations

### Unit Tests (Domain)

- `Goal.Create()` validates required fields
- `Goal.Activate()` rejects non-Draft/Paused goals
- `Goal.Complete()` sets CompletedAt timestamp
- `Goal.AddMetric()` rejects duplicates
- `Target.IsMet()` for all target types
- `Target.GetProgress()` calculation accuracy
- `EvaluationWindow.GetDateRange()` date boundaries

### Integration Tests (Application)

- `CreateGoalCommand` creates goal with metrics
- `UpdateGoalStatusCommand` enforces transitions
- `UpdateGoalScoreboardCommand` replaces all metrics
- `RecordObservationCommand` handles corrections
- Archived goals excluded from default listing

### API Tests

- POST `/api/goals` with valid data returns 201
- PUT `/api/goals/{id}/status` enforces valid transitions
- GET `/api/goals` excludes archived by default
- GET `/api/goals?status=Archived` returns only archived

---

## Future Considerations

### Potential Enhancements

1. **Goal Templates**: Pre-built goal configurations for common objectives
2. **Smart Defaults**: AI-suggested metrics based on goal title
3. **Progress Notifications**: Alert when metrics deviate from targets
4. **Goal Dependencies**: Block goals until dependencies complete
5. **Metric Correlations**: Analyze which lead metrics best predict lag outcomes
6. **Historical Analysis**: Compare progress across completed goals
7. **Team Goals**: Shared goals with role-based contributions

### Performance Considerations

- Goals are frequently queried; consider caching active goals
- Observation queries by date range are common; index `(MetricDefinitionId, ObservedOn)`
- Scoreboard updates are full replacements; optimize for read-heavy patterns
- Consider materialized views for aggregated metric values

### Data Migration Path

If changing metric storage significantly:
1. Add new columns with defaults
2. Backfill data transformation
3. Update application to use new format
4. Remove old columns in subsequent migration

---

## Glossary

| Term | Definition |
|------|------------|
| **Goal** | A measurable objective with deadline and metrics |
| **Scoreboard** | Collection of metrics tracking a goal's progress |
| **Lag Metric** | Outcome metric - the result you're measuring |
| **Lead Metric** | Leading indicator - behavior that predicts outcome |
| **Constraint Metric** | Guardrail - what not to sacrifice for the goal |
| **Metric Definition** | Reusable template defining what to measure |
| **Observation** | Single data point for a metric at a point in time |
| **Target** | Desired value or range for a metric |
| **Evaluation Window** | Time period for aggregating observations |
| **Aggregation** | Method for combining observations (sum, avg, etc.) |
