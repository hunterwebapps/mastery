# Habits Feature Documentation

## Overview

The **Habits** feature is the behavioral foundation of the Mastery system. Habits represent the recurring actions that drive progress toward goals. They are the "lead indicators" in the control loop—predictive behaviors that, when executed consistently, produce desired outcomes.

**Design Philosophy**: Habits are not simple reminders. They are capacity-aware, metric-connected behavioral units with mode variants for graceful degradation and lazy-generated occurrences for efficient storage.

---

## Business Context

### Why Habits Exists

Most habit trackers fail because they treat all days equally. Mastery's Habits feature addresses this by:

1. **Enabling capacity-aware completion** - Mode variants (Full, Maintenance, Minimum) allow maintaining streaks on low-energy days
2. **Connecting behaviors to outcomes** - Metric bindings link habit completions to goal lead indicators
3. **Capturing friction data** - Miss reasons feed the diagnostic engine for personalized interventions
4. **Optimizing for daily loop** - Today view designed for <2 minutes daily input

### The Control System Analogy

In control theory terms:

| Control Concept | Habits Component |
|-----------------|------------------|
| **Actuator** | Habit completion (the action taken) |
| **Sensors** | Occurrence status, miss reasons, mode used |
| **Controller Input** | Streak, adherence rate, friction patterns |
| **Plant Feedback** | Metric observations from completions |
| **Setpoint** | Schedule (when habit should occur) |

### The Minimum Viable Version Pattern

Each habit can have three "versions" (variants):

```
Habit Variants
├── Full Version ─────────── Complete execution (30 min workout)
├── Maintenance Version ──── Reduced but meaningful (15 min workout)
└── Minimum Version ──────── Bare minimum to maintain streak (5 min stretch)
```

**Example**: "Daily Exercise" Habit
- **Full**: 30 min strength training, 5 energy cost
- **Maintenance**: 15 min cardio, 3 energy cost
- **Minimum**: 5 min stretching, 1 energy cost

This prevents all-or-nothing thinking that breaks streaks during low-capacity periods.

---

## Domain Model

### Entity Hierarchy

```
Habit (Aggregate Root)
├── Id (Guid)
├── UserId (string) ────────────── Owner
├── Title (string, max 200) ────── Display name
├── Description? (string) ──────── Detailed description
├── Why? (string) ─────────────── Motivation (for coaching)
├── Status (enum) ─────────────── Lifecycle state
├── DisplayOrder (int) ─────────── Position in lists
├── Schedule (Value Object) ────── When habit is due
│   ├── Type ──────────────────── Daily / DaysOfWeek / WeeklyFrequency / Interval
│   ├── DaysOfWeek[]? ─────────── For DaysOfWeek type
│   ├── FrequencyPerWeek? ──────── For WeeklyFrequency type
│   ├── IntervalDays? ──────────── For Interval type
│   ├── StartDate ─────────────── When schedule begins
│   ├── EndDate? ──────────────── Optional end date
│   └── PreferredTimes[]? ──────── Suggested execution times
│
├── Policy (Value Object) ──────── Completion rules
│   ├── AllowLateCompletion ───── Can complete after day ends
│   ├── LateCutoffTime? ────────── Cutoff for late (e.g., 3 AM)
│   ├── AllowSkip ─────────────── Can explicitly skip
│   ├── RequireMissReason ──────── Requires reason when missed
│   ├── AllowBackfill ──────────── Can complete past dates
│   └── MaxBackfillDays ─────────  Max days back (0-30)
│
├── DefaultMode (enum) ─────────── Full / Maintenance / Minimum
├── RoleIds[] (JSON) ───────────── Associated user roles
├── ValueIds[] (JSON) ──────────── Aligned user values
├── GoalIds[] (JSON) ───────────── Contributing to these goals
│
├── MetricBindings[] (Child Entities)
│   └── HabitMetricBinding
│       ├── Id (Guid)
│       ├── MetricDefinitionId (FK)
│       ├── ContributionType ───── BooleanAs1 / FixedValue / UseEnteredValue
│       ├── FixedValue? ────────── For FixedValue type
│       └── Notes? ─────────────── Context
│
├── Variants[] (Child Entities) ── Mode versions
│   └── HabitVariant
│       ├── Id (Guid)
│       ├── Mode ──────────────── Full / Maintenance / Minimum
│       ├── Label ─────────────── User-friendly name
│       ├── DefaultValue ──────── Metric contribution
│       ├── EstimatedMinutes ──── Time estimate
│       ├── EnergyCost (1-5) ──── Energy expenditure
│       └── CountsAsCompletion ── Affects streak?
│
├── Occurrences[] (Child Entities) ── Lazy-generated instances
│   └── HabitOccurrence
│       ├── Id (Guid)
│       ├── ScheduledOn (DateOnly) ── Due date
│       ├── Status (enum) ─────────── Pending / Completed / Missed / Skipped / Rescheduled
│       ├── CompletedAt? (DateTime) ── UTC completion time
│       ├── CompletedOn? (DateOnly) ── User-perceived date
│       ├── ModeUsed? (enum) ───────── Mode at completion
│       ├── EnteredValue? (decimal) ── User-entered value
│       ├── MissReason? (enum) ──────── Categorized reason
│       ├── Note? (string) ──────────── Optional note
│       └── RescheduledTo? (DateOnly) ─ New date if rescheduled
│
├── CurrentStreak (int) ────────── Computed: consecutive completions
└── AdherenceRate7Day (decimal) ── Computed: 7-day completion rate
```

---

## Enums Reference

### HabitStatus

Controls habit lifecycle and what operations are permitted:

| Status | Description | Allowed Transitions |
|--------|-------------|---------------------|
| `Active` | Being tracked, shows in Today view | → Paused, → Archived |
| `Paused` | Temporarily inactive | → Active, → Archived |
| `Archived` | Soft-deleted | (terminal state) |

**Business Rules**:
- Cannot update an Archived habit
- Can only Pause from Active
- Can only Activate from Paused
- Archive is available from any non-archived state

### HabitOccurrenceStatus

State machine for individual occurrences:

| Status | Description | Allowed From |
|--------|-------------|--------------|
| `Pending` | Scheduled but not actioned | (initial state) |
| `Completed` | Successfully finished | Pending |
| `Missed` | Past due without completion | Pending |
| `Skipped` | Explicitly skipped | Pending |
| `Rescheduled` | Moved to different date | Pending |

**State Transitions**:
```
                    ┌─────────────┐
                    │   Pending   │
                    └──────┬──────┘
           ┌───────────────┼───────────────┬─────────────┐
           ▼               ▼               ▼             ▼
    ┌──────────┐    ┌──────────┐    ┌──────────┐  ┌────────────┐
    │Completed │◄───│  Skipped │    │  Missed  │  │Rescheduled │
    └────┬─────┘    └──────────┘    └──────────┘  └────────────┘
         │
         ▼ (Undo)
    ┌──────────┐
    │ Pending  │
    └──────────┘
```

### ScheduleType

How the habit recurrence is configured:

| Type | Description | Parameters | IsDueOn Logic |
|------|-------------|------------|---------------|
| `Daily` | Every day | None | Always true |
| `DaysOfWeek` | Specific weekdays | `DaysOfWeek[]` | Date.DayOfWeek in list |
| `WeeklyFrequency` | X times per week | `FrequencyPerWeek` | Always true (user chooses) |
| `Interval` | Every N days | `IntervalDays` | (date - startDate) % N == 0 |

### HabitMode

Effort levels for capacity-aware completion:

| Mode | Description | Typical Use |
|------|-------------|-------------|
| `Full` | Complete version | Normal days |
| `Maintenance` | Reduced version | Moderate capacity |
| `Minimum` | Bare minimum | Low capacity, maintain streak |

### MissReason

Categorized reasons for friction analysis:

| Reason | Label | Use Case |
|--------|-------|----------|
| `TooTired` | Too tired | Energy/fatigue issues |
| `NoTime` | No time | Scheduling conflicts |
| `Forgot` | Forgot | Memory/awareness issues |
| `Environment` | Environment | Location/context barriers |
| `Conflict` | Conflict | Competing priorities |
| `Sickness` | Sick | Health issues |
| `Other` | Other | Uncategorized |

**Diagnostic Use**: Patterns in miss reasons inform coaching interventions (e.g., "You've been too tired 5 times this week—consider switching to Minimum mode").

### HabitContributionType

How habit completions affect metrics:

| Type | Behavior | Example |
|------|----------|---------|
| `BooleanAs1` | Completion adds 1 | "Workout sessions per week" |
| `FixedValue` | Completion adds configured value | "30 minutes per workout" |
| `UseEnteredValue` | User enters value at completion | "Actual minutes exercised" |

---

## Value Objects

### HabitSchedule

Encapsulates all schedule logic with factory methods:

```csharp
// Factory methods
HabitSchedule.Daily()
HabitSchedule.OnDays(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday)
HabitSchedule.TimesPerWeek(4)
HabitSchedule.EveryNDays(3)

// With date bounds
HabitSchedule.Daily(startDate: new DateOnly(2026, 1, 1))
HabitSchedule.OnDays(..., endDate: new DateOnly(2026, 12, 31))

// Query methods
schedule.IsDueOn(date)              // Is habit due on this date?
schedule.GetNextDueDate(fromDate)   // When is it next due?
schedule.GetExpectedCountInRange(start, end)  // How many in range?
```

**Schedule Behavior Details**:
- **Daily**: `IsDueOn()` returns true for all dates in range
- **DaysOfWeek**: `IsDueOn()` checks if date's weekday is in configured list
- **WeeklyFrequency**: `IsDueOn()` returns true (user decides which days)
- **Interval**: Uses modulo arithmetic from start date

### HabitPolicy

Encapsulates completion rules with presets:

```csharp
// Presets
HabitPolicy.Default()   // Late til 3 AM, skip allowed, backfill 7 days
HabitPolicy.Strict()    // No late, no skip, requires reason, no backfill
HabitPolicy.Flexible()  // Late til 6 AM, all allowed, backfill 14 days

// Custom
HabitPolicy.Create(
    allowLateCompletion: true,
    lateCutoffTime: new TimeOnly(3, 0),
    allowSkip: true,
    requireMissReason: false,
    allowBackfill: true,
    maxBackfillDays: 7
)
```

---

## Domain Events

### Habit Events

| Event | Trigger | Typical Handler Action |
|-------|---------|------------------------|
| `HabitCreatedEvent` | New habit created | Initialize stats, suggest to goals |
| `HabitUpdatedEvent` | Habit details changed | Audit log |
| `HabitStatusChangedEvent` | Active ↔ Paused ↔ Archived | Update dashboard counts |
| `HabitArchivedEvent` | Habit archived | Hide from active views |
| `HabitCompletedEvent` | Occurrence completed | **Create metric observations** |
| `HabitUndoneEvent` | Completion reversed | **Create metric corrections** |
| `HabitSkippedEvent` | Occurrence skipped | Analytics tracking |
| `HabitMissedEvent` | Occurrence marked missed | Friction analysis, diagnostic |
| `HabitStreakMilestoneEvent` | Milestone reached (7, 30, 100...) | Celebration notification |
| `HabitModeSuggestedEvent` | System suggests mode change | Coaching notification |

### Critical Event Handlers

**HabitCompletedEventHandler**:
1. Loads habit with metric bindings
2. For each binding, calculates contribution value
3. Creates `MetricObservation` with correlation ID `HabitOccurrence:{id}`
4. Updates streak calculation

**HabitUndoneEventHandler**:
1. Finds observations with correlation ID
2. Creates correction entries (value 0) to negate
3. Updates streak calculation

---

## Key Design Patterns

### 1. Lazy Occurrence Generation

**Problem**: Pre-generating occurrences for multi-year daily habits would create millions of records.

**Solution**: Create occurrence records on-demand when user interacts with a specific date.

**Implementation**:
```csharp
// In Habit aggregate
public HabitOccurrence GetOrCreateOccurrence(DateOnly date)
{
    var existing = Occurrences.FirstOrDefault(o => o.ScheduledOn == date);
    if (existing != null) return existing;

    var newOccurrence = HabitOccurrence.Create(Id, date);
    _occurrences.Add(newOccurrence);
    return newOccurrence;
}
```

**Database Constraint**: Unique index on `(HabitId, ScheduledOn)` prevents duplicates.

**EF Core Consideration**: When using filtered includes, new occurrences must be explicitly added to the change tracker:
```csharp
await _habitRepository.AddOccurrenceAsync(newOccurrence, cancellationToken);
```

### 2. Mode Variants for Capacity Awareness

**Problem**: All-or-nothing completion breaks streaks during low-capacity periods.

**Solution**: Define multiple effort levels per habit with configurable streak impact.

**Configuration**:
```json
{
  "variants": [
    { "mode": "Full", "label": "Full workout", "estimatedMinutes": 30, "energyCost": 5, "countsAsCompletion": true },
    { "mode": "Maintenance", "label": "Quick cardio", "estimatedMinutes": 15, "energyCost": 3, "countsAsCompletion": true },
    { "mode": "Minimum", "label": "Stretch only", "estimatedMinutes": 5, "energyCost": 1, "countsAsCompletion": true }
  ]
}
```

**UI Behavior**: User can override default mode at completion time.

### 3. Metric Bindings for Lead Indicators

**Problem**: Habit completions should automatically feed goal progress metrics.

**Solution**: Bind habits to metric definitions with contribution rules.

**Flow**:
```
Habit Completion
       │
       ▼
HabitCompletedEvent
       │
       ▼
HabitCompletedEventHandler
       │
       ├── For each MetricBinding:
       │   ├── Calculate value (Boolean/Fixed/Entered)
       │   └── Create MetricObservation
       │
       └── Update streak
```

**Correlation ID**: `HabitOccurrence:{occurrenceId}` links observations to completions for undo support.

### 4. Optimized Today View

**Problem**: Loading all occurrences for all habits is expensive and unnecessary.

**Solution**: Specialized query and DTO for daily loop.

**Query Optimization**:
```csharp
// Only include today's occurrence, not all occurrences
.Include(h => h.Occurrences.Where(o => o.ScheduledOn == today))
```

**TodayHabitDto** includes only:
- Habit basics (id, title, description)
- Today's occurrence (if exists)
- Variants (for mode selection)
- Computed stats (streak, adherence)
- Goal impact tags (names, not full goals)
- Whether value entry is required

### 5. Streak Calculation Algorithm

```
Start from today, work backwards:
  For each day:
    If not scheduled (per Schedule) → skip day
    If completed or skipped → continue
    If pending (today only) → continue (grace period)
    Else → break streak

  Stop at 365 days (performance limit)
  Return count of consecutive completed days
```

**Special Cases**:
- WeeklyFrequency: Only checks days with actual occurrences
- Skipped occurrences don't break streak (if policy allows)
- Today gets grace period (not yet missed)

---

## API Endpoints

### Habit Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/habits` | List habits (query: status) |
| `GET` | `/api/habits/today` | Today's habits (optimized) |
| `GET` | `/api/habits/{id}` | Get habit with full details |
| `GET` | `/api/habits/{id}/history` | History (query: fromDate, toDate) |
| `POST` | `/api/habits` | Create new habit |
| `PUT` | `/api/habits/{id}` | Update habit details |
| `PUT` | `/api/habits/{id}/status` | Change status |
| `DELETE` | `/api/habits/{id}` | Archive (soft delete) |

### Occurrence Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/habits/{id}/occurrences/{date}/complete` | Mark complete |
| `POST` | `/api/habits/{id}/occurrences/{date}/undo` | Undo completion |
| `POST` | `/api/habits/{id}/occurrences/{date}/skip` | Skip occurrence |

### Request/Response Examples

**Create Habit (POST /api/habits)**
```json
{
  "title": "Daily Exercise",
  "description": "Stay active every day",
  "why": "Health is the foundation for everything else",
  "schedule": {
    "type": "Daily",
    "startDate": "2026-01-01"
  },
  "policy": {
    "allowLateCompletion": true,
    "lateCutoffTime": "03:00",
    "allowSkip": true,
    "requireMissReason": false,
    "allowBackfill": true,
    "maxBackfillDays": 7
  },
  "defaultMode": "Full",
  "variants": [
    { "mode": "Full", "label": "Full workout", "defaultValue": 1, "estimatedMinutes": 30, "energyCost": 5, "countsAsCompletion": true },
    { "mode": "Minimum", "label": "Quick stretch", "defaultValue": 1, "estimatedMinutes": 5, "energyCost": 1, "countsAsCompletion": true }
  ],
  "metricBindings": [
    { "metricDefinitionId": "...", "contributionType": "BooleanAs1" }
  ],
  "goalIds": ["..."]
}
```

**Today Habits Response (GET /api/habits/today)**
```json
[
  {
    "id": "...",
    "title": "Daily Exercise",
    "description": "Stay active every day",
    "isDue": true,
    "defaultMode": "Full",
    "todayOccurrence": {
      "id": "...",
      "scheduledOn": "2026-01-26",
      "status": "Pending"
    },
    "variants": [...],
    "currentStreak": 14,
    "adherenceRate7Day": 1.0,
    "goalImpactTags": ["Get Fit"],
    "requiresValueEntry": false,
    "displayOrder": 0
  }
]
```

**Complete Occurrence (POST /api/habits/{id}/occurrences/{date}/complete)**
```json
{
  "mode": "Full",
  "value": null,
  "note": "Great session today!"
}
```

**History Response (GET /api/habits/{id}/history)**
```json
{
  "habitId": "...",
  "fromDate": "2025-07-26",
  "toDate": "2026-01-26",
  "occurrences": [...],
  "totalDue": 180,
  "totalCompleted": 165,
  "totalMissed": 10,
  "totalSkipped": 5
}
```

---

## Implementation Architecture

### Layer Responsibilities

```
┌─────────────────────────────────────────────────────────────────┐
│                         API Layer                               │
│  Controllers/HabitsController.cs                                │
│  Contracts/Habits/Requests.cs                                   │
│  - HTTP endpoints & routing                                     │
│  - Request/response mapping                                     │
│  - Input validation (structural)                                │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                     Application Layer                           │
│  Features/Habits/Commands/                                      │
│  Features/Habits/Queries/                                       │
│  Features/Habits/EventHandlers/                                 │
│  - Use case orchestration (CQRS via MediatR)                   │
│  - Business validation (FluentValidation)                       │
│  - DTO mapping                                                  │
│  - Event handling (metric integration)                          │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                       Domain Layer                              │
│  Entities/Habit/                                                │
│  ValueObjects/HabitSchedule.cs, HabitPolicy.cs                 │
│  Enums/, Events/                                                │
│  - Business rules & invariants                                  │
│  - Status transitions                                           │
│  - Schedule logic                                               │
│  - Domain events                                                │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                   Infrastructure Layer                          │
│  Data/Configurations/HabitConfiguration.cs                      │
│  Repositories/HabitRepository.cs                                │
│  - EF Core mapping (incl. JSON columns)                        │
│  - Database operations                                          │
│  - Optimized queries (filtered includes)                       │
│  - Streak calculation                                           │
└─────────────────────────────────────────────────────────────────┘
```

### Key Files Reference

| Layer | File | Purpose |
|-------|------|---------|
| Domain | `Entities/Habit/Habit.cs` | Aggregate root with business logic |
| Domain | `Entities/Habit/HabitOccurrence.cs` | Occurrence entity |
| Domain | `Entities/Habit/HabitVariant.cs` | Mode variant entity |
| Domain | `Entities/Habit/HabitMetricBinding.cs` | Metric connection entity |
| Domain | `ValueObjects/HabitSchedule.cs` | Schedule with IsDueOn logic |
| Domain | `ValueObjects/HabitPolicy.cs` | Completion policy rules |
| Domain | `Enums/HabitStatus.cs` | Lifecycle states |
| Domain | `Enums/HabitOccurrenceStatus.cs` | Occurrence states |
| Domain | `Enums/HabitMode.cs` | Effort levels |
| Domain | `Enums/ScheduleType.cs` | Schedule types |
| Domain | `Enums/MissReason.cs` | Friction categories |
| Domain | `Events/HabitEvents.cs` | Domain events |
| Domain | `Interfaces/IHabitRepository.cs` | Repository contract |
| Application | `Features/Habits/Commands/CompleteOccurrence/` | Completion use case |
| Application | `Features/Habits/Queries/GetTodayHabits/` | Today view query |
| Application | `Features/Habits/EventHandlers/` | Metric integration |
| Application | `Features/Habits/Models/HabitDto.cs` | All DTOs |
| Infrastructure | `Data/Configurations/HabitConfiguration.cs` | EF mapping |
| Infrastructure | `Repositories/HabitRepository.cs` | Data access |
| API | `Controllers/HabitsController.cs` | HTTP endpoints |
| API | `Contracts/Habits/` | Request DTOs |

### Database Schema

```sql
-- Habits table
CREATE TABLE Habits (
    Id uniqueidentifier PRIMARY KEY,
    UserId nvarchar(256) NOT NULL,
    Title nvarchar(200) NOT NULL,
    Description nvarchar(1000) NULL,
    Why nvarchar(500) NULL,
    Status nvarchar(20) NOT NULL,
    DisplayOrder int NOT NULL DEFAULT 0,
    DefaultMode nvarchar(20) NOT NULL,
    Schedule nvarchar(max) NOT NULL,        -- JSON
    Policy nvarchar(max) NOT NULL,          -- JSON
    RoleIds nvarchar(max) NOT NULL,         -- JSON array
    ValueIds nvarchar(max) NOT NULL,        -- JSON array
    GoalIds nvarchar(max) NOT NULL,         -- JSON array
    CurrentStreak int NOT NULL DEFAULT 0,
    AdherenceRate7Day decimal(5,2) NOT NULL DEFAULT 0,
    CreatedAt datetime2 NOT NULL,
    CreatedBy nvarchar(256) NULL,
    ModifiedAt datetime2 NULL,
    ModifiedBy nvarchar(256) NULL
);

-- HabitOccurrences table
CREATE TABLE HabitOccurrences (
    Id uniqueidentifier PRIMARY KEY,
    HabitId uniqueidentifier NOT NULL,
    ScheduledOn date NOT NULL,
    Status nvarchar(20) NOT NULL,
    CompletedAt datetime2 NULL,
    CompletedOn date NULL,
    ModeUsed nvarchar(20) NULL,
    EnteredValue decimal(18,4) NULL,
    MissReason nvarchar(20) NULL,
    Note nvarchar(500) NULL,
    RescheduledTo date NULL,
    CreatedAt datetime2 NOT NULL,

    CONSTRAINT FK_Occurrences_Habits FOREIGN KEY (HabitId)
        REFERENCES Habits(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_Occurrence_Date UNIQUE (HabitId, ScheduledOn)
);

-- HabitVariants table
CREATE TABLE HabitVariants (
    Id uniqueidentifier PRIMARY KEY,
    HabitId uniqueidentifier NOT NULL,
    Mode nvarchar(20) NOT NULL,
    Label nvarchar(100) NOT NULL,
    DefaultValue decimal(18,4) NOT NULL DEFAULT 1,
    EstimatedMinutes int NOT NULL DEFAULT 15,
    EnergyCost int NOT NULL DEFAULT 3,
    CountsAsCompletion bit NOT NULL DEFAULT 1,
    CreatedAt datetime2 NOT NULL,
    ModifiedAt datetime2 NULL,

    CONSTRAINT FK_Variants_Habits FOREIGN KEY (HabitId)
        REFERENCES Habits(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_Variant_Mode UNIQUE (HabitId, Mode)
);

-- HabitMetricBindings table
CREATE TABLE HabitMetricBindings (
    Id uniqueidentifier PRIMARY KEY,
    HabitId uniqueidentifier NOT NULL,
    MetricDefinitionId uniqueidentifier NOT NULL,
    ContributionType nvarchar(20) NOT NULL,
    FixedValue decimal(18,4) NULL,
    Notes nvarchar(500) NULL,
    CreatedAt datetime2 NOT NULL,
    ModifiedAt datetime2 NULL,

    CONSTRAINT FK_Bindings_Habits FOREIGN KEY (HabitId)
        REFERENCES Habits(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Bindings_Metrics FOREIGN KEY (MetricDefinitionId)
        REFERENCES MetricDefinitions(Id) ON DELETE RESTRICT,
    CONSTRAINT UQ_Binding_Metric UNIQUE (HabitId, MetricDefinitionId)
);

-- Indexes
CREATE INDEX IX_Habits_UserId ON Habits(UserId);
CREATE INDEX IX_Habits_UserId_Status ON Habits(UserId, Status);
CREATE INDEX IX_Habits_UserId_DisplayOrder ON Habits(UserId, DisplayOrder);
CREATE INDEX IX_Occurrences_HabitId_Status ON HabitOccurrences(HabitId, Status);
CREATE INDEX IX_Occurrences_ScheduledOn ON HabitOccurrences(ScheduledOn);
CREATE INDEX IX_Variants_HabitId ON HabitVariants(HabitId);
CREATE INDEX IX_Bindings_HabitId ON HabitMetricBindings(HabitId);
CREATE INDEX IX_Bindings_MetricId ON HabitMetricBindings(MetricDefinitionId);
```

---

## UI Implementation

### Pages & Routes

| Route | Component | Purpose |
|-------|-----------|---------|
| `/habits` | `HabitsPage` | Main hub with Today/All tabs |
| `/habits/new` | `CreateHabitPage` | Multi-step wizard |
| `/habits/:id` | `HabitDetailPage` | Detail view with history |
| `/habits/:id/edit` | `EditHabitPage` | Edit using wizard |

### Component Structure

```
features/habits/
├── api/
│   └── habits-api.ts              # API client
├── hooks/
│   └── use-habits.ts              # React Query hooks (with optimistic updates)
├── schemas/
│   └── habit-schema.ts            # Zod validation schemas
├── utils/
│   └── habit-form-utils.ts        # Form data transformations
├── components/
│   ├── today-view/
│   │   ├── today-habit-card.tsx   # One-tap completion card
│   │   ├── today-habits-list.tsx  # Today list with progress
│   │   ├── mode-selector.tsx      # Full/Maintenance/Minimum picker
│   │   ├── value-entry-dialog.tsx # Input for UseEnteredValue
│   │   ├── skip-dialog.tsx        # Skip with reason selection
│   │   └── completion-celebration.tsx # Milestone animations
│   ├── habit-list/
│   │   ├── habit-card.tsx         # Summary card
│   │   └── habits-list.tsx        # All habits list
│   ├── habit-form/
│   │   ├── habit-wizard.tsx       # Multi-step form (create/edit)
│   │   ├── step-basics.tsx        # Title, description, why
│   │   ├── step-schedule.tsx      # Schedule configuration
│   │   ├── step-variants.tsx      # Mode variants
│   │   └── step-review.tsx        # Review before submit
│   ├── habit-detail/
│   │   └── habit-calendar.tsx     # GitHub-style contribution calendar
│   └── common/
│       └── streak-badge.tsx       # Animated streak counter
└── pages/
    ├── habits-page.tsx            # Main page with tabs
    ├── create-habit-page.tsx      # Create wrapper
    ├── edit-habit-page.tsx        # Edit wrapper
    └── habit-detail-page.tsx      # Detail view
```

### Key UI Patterns

**1. One-Tap Completion (Today View)**
- Large tap target for instant completion
- Mode selector appears on tap for override
- Optimistic update with celebration animation
- Undo available immediately after completion

**2. Multi-Step Wizard (Create/Edit)**
- Step 1 (Basics): Title, description, why
- Step 2 (Schedule): Type selection with visual pickers
- Step 3 (Variants): Mode variant configuration
- Step 4 (Review): Preview all settings
- Shared between create and edit (mode prop)

**3. Contribution Calendar (Detail View)**
- GitHub-style grid showing completion history
- Color-coded: Green (completed), Red (missed), Yellow (skipped)
- Hover tooltips with date and status
- Navigation to view older history

**4. Optimistic Updates**
```typescript
// Immediately update cache, rollback on error
onMutate: async ({ habitId, date }) => {
  await queryClient.cancelQueries({ queryKey: habitKeys.today() })
  const previous = queryClient.getQueryData(habitKeys.today())
  queryClient.setQueryData(habitKeys.today(), (old) => {
    // Update occurrence status, increment streak
  })
  return { previous }
},
onError: (err, variables, context) => {
  queryClient.setQueryData(habitKeys.today(), context?.previous)
},
onSettled: () => {
  queryClient.invalidateQueries({ queryKey: habitKeys.today() })
}
```

### State Management

```typescript
// Query Keys (TanStack Query)
export const habitKeys = {
  all: ['habits'] as const,
  lists: () => [...habitKeys.all, 'list'] as const,
  list: (status?: HabitStatus) => [...habitKeys.lists(), { status }] as const,
  today: () => [...habitKeys.all, 'today'] as const,
  details: () => [...habitKeys.all, 'detail'] as const,
  detail: (id: string) => [...habitKeys.details(), id] as const,
  history: (id: string) => [...habitKeys.all, 'history', id] as const,
}
```

### UI Helpers

```typescript
// Status styling
export const habitStatusInfo: Record<HabitStatus, { label, color, bgColor }> = {
  Active: { label: 'Active', color: 'text-green-400', bgColor: 'bg-green-500/10' },
  Paused: { label: 'Paused', color: 'text-yellow-400', bgColor: 'bg-yellow-500/10' },
  Archived: { label: 'Archived', color: 'text-muted-foreground', bgColor: 'bg-muted/50' },
}

// Mode styling
export const habitModeInfo: Record<HabitMode, { label, description, color }> = {
  Full: { label: 'Full', description: 'Complete version', color: 'text-blue-400' },
  Maintenance: { label: 'Maintenance', description: 'Reduced version', color: 'text-yellow-400' },
  Minimum: { label: 'Minimum', description: 'Bare minimum', color: 'text-orange-400' },
}

// Miss reason info
export const missReasonInfo: Record<MissReason, { label, emoji }> = {
  TooTired: { label: 'Too tired', emoji: '😴' },
  NoTime: { label: 'No time', emoji: '⏰' },
  Forgot: { label: 'Forgot', emoji: '🤔' },
  // ...
}
```

---

## Extension Guide

### Adding a New Schedule Type

1. **Domain**: Add to `ScheduleType` enum
2. **Domain**: Update `HabitSchedule.IsDueOn()` with logic
3. **Domain**: Update `HabitSchedule.GetExpectedCountInRange()`
4. **Domain**: Add factory method `HabitSchedule.NewType()`
5. **Application**: Update input parsing in commands
6. **Frontend**: Add to `scheduleTypeInfo` and schedule step UI

### Adding a New Occurrence Status

1. **Domain**: Add to `HabitOccurrenceStatus` enum
2. **Domain**: Update `HabitOccurrence` state transition methods
3. **Domain**: Add domain event if needed
4. **Application**: Create command for new transition
5. **API**: Add endpoint for transition
6. **Frontend**: Update status styling and UI

### Adding a New Miss Reason

1. **Domain**: Add to `MissReason` enum
2. **Application**: No changes needed (enum is passed through)
3. **Frontend**: Add to `missReasonInfo` with label and emoji

### Implementing Stats Aggregation Service

The system needs a service for habit analytics:

```csharp
public interface IHabitStatsService
{
    Task<HabitStatsDto> GetStatsAsync(
        Guid habitId,
        string userId,
        CancellationToken cancellationToken);

    Task<int> CalculateStreakAsync(
        Guid habitId,
        DateOnly asOfDate,
        CancellationToken cancellationToken);

    Task<decimal> CalculateAdherenceRateAsync(
        Guid habitId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken);
}
```

---

## Testing Considerations

### Unit Tests (Domain)

- `HabitSchedule.IsDueOn()` for all schedule types
- `HabitSchedule.GetExpectedCountInRange()` boundary conditions
- `Habit.Activate()` / `Pause()` / `Archive()` state transitions
- `HabitOccurrence.Complete()` / `Skip()` / `Undo()` transitions
- `HabitVariant` uniqueness constraint (one per mode)
- `HabitMetricBinding.GetContributionValue()` for all types
- `HabitPolicy` validation rules

### Integration Tests (Application)

- `CompleteOccurrenceCommand` creates metric observations
- `UndoOccurrenceCommand` creates metric corrections
- `GetTodayHabitsQuery` returns only due habits with today's occurrence
- Cascade deletes (habit deletion removes variants, bindings, occurrences)
- Backfill validation respects policy

### API Tests

- POST `/api/habits` with valid data returns 201
- PUT `/api/habits/{id}/status` enforces valid transitions
- POST `/{id}/occurrences/{date}/complete` validates backfill policy
- GET `/api/habits/today` excludes archived habits
- Unique constraint violations return 400

---

## Future Considerations

### Potential Enhancements

1. **Habit Templates**: Pre-built habits for common use cases
2. **Smart Scheduling**: AI-suggested optimal times based on completion patterns
3. **Streak Insurance**: Allow one "free miss" per streak milestone
4. **Habit Chaining**: Trigger one habit after another completes
5. **Social Accountability**: Share habits with accountability partners
6. **Habit Suggestions**: Recommend habits based on goal gaps
7. **Time-Based Triggers**: Location or time-based reminders
8. **Habit Batching**: Group related habits into "routines"

### Performance Considerations

- Lazy occurrence generation prevents storage explosion
- Filtered includes for today's occurrence only
- Composite indexes on common query patterns
- Consider caching streak calculations for frequently accessed habits
- Pagination for history queries

### Data Migration Path

If changing occurrence storage significantly:
1. Add new columns with defaults
2. Backfill data transformation
3. Update application to use new format
4. Remove old columns in subsequent migration

---

## Glossary

| Term | Definition |
|------|------------|
| **Habit** | Recurring behavioral action tracked over time |
| **Occurrence** | Single instance of a habit on a specific date |
| **Schedule** | Configuration defining when habit is due |
| **Policy** | Rules governing completion (late, skip, backfill) |
| **Mode** | Effort level (Full, Maintenance, Minimum) |
| **Variant** | Configuration for a specific mode |
| **Streak** | Consecutive days of completion |
| **Adherence Rate** | Percentage of due occurrences completed |
| **Metric Binding** | Connection between habit and goal metric |
| **Contribution Type** | How completion affects metric value |
| **Miss Reason** | Categorized explanation for non-completion |
| **Backfill** | Completing a habit for a past date |
| **Late Completion** | Completing after the day ends (within cutoff) |
