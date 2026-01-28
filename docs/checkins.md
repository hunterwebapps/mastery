# Check-Ins Feature Documentation

## Overview

The **Check-Ins** feature implements the daily control loop—the core feedback mechanism in Mastery. Each day has two check-ins: a **Morning** check-in (set intention) and an **Evening** check-in (capture reality). Together, they close the loop: intent → action → reality capture → compare → diagnose → adjust.

**Design Constraint**: <2 minutes total daily input for both check-ins combined.

**Design Philosophy**: Check-ins are not journals. They are structured sensor readings that feed the diagnostic engine, planning engine, and coaching engine. Every field exists because it maps to a downstream system decision.

---

## Business Context

### Why Check-Ins Exist

The daily loop is the highest-leverage intervention point in the system. Without it:
- The planning engine has no energy signal to adapt plans
- The diagnostic engine can't detect drift or blockers
- The coaching engine can't personalize recommendations
- Streak/consistency data doesn't exist

### The Control System Analogy

| Control Concept | Check-In Component |
|-----------------|-------------------|
| **Sensor Reading** | Energy level (AM/PM), stress level |
| **Setpoint Declaration** | Top 1 priority, day mode, intention |
| **Error Signal** | Top 1 completion (did reality match intent?) |
| **Disturbance Detection** | Blocker category + note |
| **State Estimation** | Reflection (qualitative context for quantitative signals) |
| **Loop Completion** | Streak tracking (is the feedback loop running?) |

### Daily Loop Flow

```
Morning Check-In                    Evening Check-In
┌──────────────┐                    ┌──────────────┐
│ Energy (1-5) │                    │ Top 1 Done?  │
│ Day Mode     │──── Day ────────►  │ Energy PM    │
│ Top 1 Pick   │    Passes         │ Stress Level │
│ Intention    │                    │ Blockers     │
└──────────────┘                    │ Reflection   │
       │                            └──────────────┘
       ▼                                   │
  Planning Engine                          ▼
  adapts today's plan              Diagnostic Engine
  based on energy + mode           detects patterns
```

---

## Domain Model

### Entity: CheckIn (Aggregate Root)

A single entity with a `Type` discriminator (Morning/Evening). Morning and evening fields are all nullable in the database; the domain layer enforces required fields per type via factory methods.

```
CheckIn (Aggregate Root)
├── Id (Guid)
├── UserId (string) ────────────── Owner
├── CheckInDate (DateOnly) ─────── User-local date
├── Type (CheckInType) ─────────── Morning | Evening
├── Status (CheckInStatus) ─────── Draft | Completed | Skipped
├── CompletedAt (DateTime?) ────── Submission timestamp
│
├── Morning Fields
│   ├── EnergyLevel (int?, 1-5) ── Self-rated energy
│   ├── SelectedMode (HabitMode?)── Full | Maintenance | Minimum
│   ├── Top1Type (Top1Type?) ───── Task | Habit | Project | FreeText
│   ├── Top1EntityId (Guid?) ───── FK to task/habit/project
│   ├── Top1FreeText (string?) ─── Max 200 chars
│   └── Intention (string?) ────── Max 500 chars
│
├── Evening Fields
│   ├── EnergyLevelPm (int?, 1-5)─ End-of-day energy
│   ├── StressLevel (int?, 1-5) ── Self-rated stress
│   ├── Top1Completed (bool?) ──── Did you finish your #1?
│   ├── BlockerCategory (enum?) ── What got in the way
│   ├── BlockerNote (string?) ──── Max 500 chars
│   └── Reflection (string?) ───── Max 1000 chars
│
└── Audit
    ├── CreatedAt, CreatedBy
    └── ModifiedAt, ModifiedBy
```

**Why single entity, not two tables?** Morning and evening check-ins for the same date are conceptually linked (the evening reviews the morning's intention). A single entity with type discriminator simplifies queries like "get today's state" and keeps the unique constraint straightforward.

### Uniqueness Constraint

```
UNIQUE INDEX: {UserId, CheckInDate, Type}
```

One morning and one evening per user per date. Enforced at both the domain level (existence check before creation) and the database level (unique composite index).

---

## Enums Reference

### CheckInType

| Value | Purpose |
|-------|---------|
| `Morning` | Energy rating, Top 1 selection, day mode |
| `Evening` | Completion review, blocker capture, reflection |

### CheckInStatus

| Value | Description | System Behavior |
|-------|-------------|-----------------|
| `Draft` | Started but not completed | Reserved for future "save progress" |
| `Completed` | Fully submitted | Counts toward streak, triggers events |
| `Skipped` | Explicitly skipped by user | Breaks streak, recorded for diagnostics |

**Business Rule**: Only `Completed` check-ins count toward the streak. `Skipped` is distinct from "missing"—a skip is intentional and recorded, while a missing check-in is simply absent.

### Top1Type

The user's #1 priority can reference an existing entity or be free-text:

| Value | Description | Data Field |
|-------|-------------|------------|
| `Task` | A specific task to complete today | `Top1EntityId` → Task ID |
| `Habit` | A habit to focus on today | `Top1EntityId` → Habit ID |
| `Project` | A project to advance today | `Top1EntityId` → Project ID |
| `FreeText` | Custom text priority | `Top1FreeText` → string |

**Business Rules**:
- If `Top1Type` is Task, Habit, or Project → `Top1EntityId` is required
- If `Top1Type` is FreeText → `Top1FreeText` is required (1-200 chars)
- Top 1 is optional in the morning check-in (user may skip this step)

### BlockerCategory

Categories aligned with the diagnostic engine's MissReason taxonomy:

| Value | Label | Diagnostic Signal |
|-------|-------|-------------------|
| `TooTired` | Too tired | Energy management issue |
| `NoTime` | No time | Capacity/planning issue |
| `Forgot` | Forgot | System/reminder issue |
| `Environment` | Environment | Context/location issue |
| `Conflict` | Conflict | Scheduling issue |
| `Sickness` | Sick | Health issue |
| `Other` | Other | Captured in blocker note |

**Business Rule**: Blocker is optional in the evening check-in. When selected, the optional `BlockerNote` (max 500 chars) provides additional context.

---

## Morning Check-In Business Rules

### Required Fields
- **Energy Level** (1-5): Self-rated morning energy. Maps to planning engine capacity.
- **Selected Mode** (Full/Maintenance/Minimum): Sets the day's ambition level.

### Optional Fields
- **Top 1 Priority**: The single most important thing for today.
- **Intention** (max 500 chars): Free-text morning intention.

### Mode Selection Logic

The UI suggests a mode based on energy level:

| Energy Level | Suggested Mode | Rationale |
|-------------|---------------|-----------|
| 1-2 (Exhausted/Low) | Minimum | Protect essentials, don't overcommit |
| 3 (Moderate) | Maintenance | Steady state, hold ground |
| 4-5 (Good/Peak) | Full | Maximize output on high-energy days |

The suggestion is advisory—the user always has final choice.

### Top 1 Selection

When selecting a Task, Habit, or Project as Top 1:
- **Tasks**: Shows today's tasks (not completed/cancelled), with overdue badges, due dates, time estimates, and project association
- **Habits**: Shows today's due habits (not yet completed), with streak count and 7-day adherence rate
- **Projects**: Shows active projects, with completion percentage, stuck/deadline warnings, and goal association
- **Task Creation**: Users can create a new task directly from the Top 1 step via a slide-out sheet containing the full task form (with project, goal, context tags, etc.)

### Energy → Metrics Bridge

When a morning check-in is submitted, the `MorningCheckInSubmittedEvent` handler:
1. Looks up the user's "Energy Level" metric definition
2. If found, creates a `MetricObservation` with:
   - Value: energy level (1-5)
   - Source: `CheckIn`
   - Correlation: `CheckIn:{id}`
3. This feeds energy trends into the goal metrics system

---

## Evening Check-In Business Rules

### All Fields Optional

The evening check-in is designed for maximum flexibility. Users can submit with any combination of fields—even an empty evening check-in counts as completed.

### Top 1 Review

- Shows the morning's Top 1 (resolved to the actual entity name, not just "Task priority")
- Binary Yes/No: "Did you complete your #1?"
- This is the core error signal—intent vs. reality

### Evening Energy & Stress

- **Energy PM** (1-5): End-of-day energy for AM/PM comparison
- **Stress Level** (1-5): Separate from energy—high stress + high energy is a different state than high stress + low energy

### Blocker Capture

- Category selection from 7 predefined options
- Optional free-text note for context
- Toggle behavior: tap to select, tap again to deselect

### Reflection

- Free-text (max 1000 chars)
- Prompt: "e.g., Good deep work session but lost focus after lunch"
- Optional—can be skipped entirely

---

## Streak Calculation

The streak counts consecutive days with at least one `Completed` check-in (morning or evening):

1. Query completed check-in dates (up to 365 days back)
2. If today has a completed check-in, start counting from today
3. If today has no completed check-in, start counting from yesterday
4. Count backwards through consecutive days until a gap is found
5. Return the count (0 if no completed check-ins)

**Business Rules**:
- Skipped check-ins do NOT count toward the streak
- Missing days break the streak
- Only one completed check-in per day is needed (morning OR evening)

---

## Domain Events

| Event | Trigger | Downstream Actions |
|-------|---------|-------------------|
| `MorningCheckInSubmittedEvent` | Morning check-in completed | Create energy MetricObservation, trigger planning engine |
| `EveningCheckInSubmittedEvent` | Evening check-in completed | Update adherence projections, precompute next day plan |
| `CheckInUpdatedEvent` | Check-in modified after submission | Re-process affected signals |
| `CheckInSkippedEvent` | User explicitly skips | Record gap for diagnostic engine |

### Event Payloads

```csharp
// Morning: includes energy + mode for planning engine
MorningCheckInSubmittedEvent(CheckInId, UserId, CheckInDate, EnergyLevel, SelectedMode)

// Evening: includes Top 1 completion for intent-vs-reality tracking
EveningCheckInSubmittedEvent(CheckInId, UserId, CheckInDate, Top1Completed?)

// Update: tracks which section changed for selective reprocessing
CheckInUpdatedEvent(CheckInId, ChangedSection)

// Skip: captures type for per-type gap analysis
CheckInSkippedEvent(CheckInId, UserId, CheckInDate, CheckInType)
```

---

## Implementation Architecture

### Layer Responsibilities

```
┌─────────────────────────────────────────────────────────────────┐
│                         API Layer                               │
│  Controllers/CheckInsController.cs                              │
│  Contracts/CheckIns/Requests.cs                                 │
│  - HTTP endpoints & routing                                     │
│  - Request/response mapping                                     │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                     Application Layer                           │
│  Features/CheckIns/Commands/ (4 command sets)                   │
│  Features/CheckIns/Queries/ (3 query sets)                      │
│  Features/CheckIns/EventHandlers/                               │
│  Features/CheckIns/Models/                                      │
│  - Use case orchestration (CQRS via MediatR)                   │
│  - Business validation (FluentValidation)                       │
│  - DTO mapping                                                  │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                       Domain Layer                              │
│  Entities/CheckIn/CheckIn.cs                                    │
│  Enums/ (4 enums), Events/CheckInEvents.cs                      │
│  Interfaces/ICheckInRepository.cs                               │
│  - Factory methods (CreateMorning, CreateEvening, CreateSkipped) │
│  - Update methods (UpdateMorning, UpdateEvening)                │
│  - Domain events on all state changes                           │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                   Infrastructure Layer                          │
│  Data/Configurations/CheckInConfiguration.cs                    │
│  Repositories/CheckInRepository.cs                              │
│  - EF Core mapping with string enums                           │
│  - Unique composite index enforcement                           │
│  - Streak calculation query                                     │
└─────────────────────────────────────────────────────────────────┘
```

### Key Files Reference

| Layer | File | Purpose |
|-------|------|---------|
| Domain | `Entities/CheckIn/CheckIn.cs` | Aggregate root with factory + update methods |
| Domain | `Enums/CheckInType.cs` | Morning / Evening |
| Domain | `Enums/CheckInStatus.cs` | Draft / Completed / Skipped |
| Domain | `Enums/Top1Type.cs` | Task / Habit / Project / FreeText |
| Domain | `Enums/BlockerCategory.cs` | 7 blocker categories |
| Domain | `Events/CheckInEvents.cs` | 4 domain events |
| Domain | `Interfaces/ICheckInRepository.cs` | Repository contract |
| Application | `Features/CheckIns/Models/CheckInDto.cs` | DTOs + mapping |
| Application | `Features/CheckIns/Commands/SubmitMorningCheckIn/` | Morning submission |
| Application | `Features/CheckIns/Commands/SubmitEveningCheckIn/` | Evening submission |
| Application | `Features/CheckIns/Commands/UpdateCheckIn/` | Post-submission edit |
| Application | `Features/CheckIns/Commands/SkipCheckIn/` | Explicit skip |
| Application | `Features/CheckIns/Queries/GetTodayCheckInState/` | Today's state + streak |
| Application | `Features/CheckIns/Queries/GetCheckIns/` | Date range history |
| Application | `Features/CheckIns/Queries/GetCheckInById/` | Single check-in detail |
| Application | `Features/CheckIns/EventHandlers/` | Energy → Metrics bridge |
| Infrastructure | `Data/Configurations/CheckInConfiguration.cs` | EF Core mapping |
| Infrastructure | `Repositories/CheckInRepository.cs` | Data access + streak query |
| API | `Controllers/CheckInsController.cs` | 7 HTTP endpoints |
| API | `Contracts/CheckIns/Requests.cs` | 4 request DTOs |

### Database Schema

```sql
CREATE TABLE CheckIns (
    Id uniqueidentifier PRIMARY KEY,
    UserId nvarchar(256) NOT NULL,
    CheckInDate date NOT NULL,
    Type nvarchar(20) NOT NULL,          -- Morning | Evening
    Status nvarchar(20) NOT NULL,        -- Draft | Completed | Skipped
    CompletedAt datetime2 NULL,

    -- Morning fields (nullable)
    EnergyLevel int NULL,                -- 1-5
    SelectedMode nvarchar(20) NULL,      -- Full | Maintenance | Minimum
    Top1Type nvarchar(20) NULL,          -- Task | Habit | Project | FreeText
    Top1EntityId uniqueidentifier NULL,
    Top1FreeText nvarchar(200) NULL,
    Intention nvarchar(500) NULL,

    -- Evening fields (nullable)
    EnergyLevelPm int NULL,              -- 1-5
    StressLevel int NULL,                -- 1-5
    Reflection nvarchar(1000) NULL,
    BlockerCategory nvarchar(20) NULL,
    BlockerNote nvarchar(500) NULL,
    Top1Completed bit NULL,

    -- Audit
    CreatedAt datetime2 NOT NULL,
    CreatedBy nvarchar(256) NULL,
    ModifiedAt datetime2 NULL,
    ModifiedBy nvarchar(256) NULL
);

-- Indexes
CREATE INDEX IX_CheckIns_UserId ON CheckIns(UserId);
CREATE UNIQUE INDEX IX_CheckIns_UserId_Date_Type ON CheckIns(UserId, CheckInDate, Type);
CREATE INDEX IX_CheckIns_UserId_Status ON CheckIns(UserId, Status);
CREATE INDEX IX_CheckIns_UserId_Date ON CheckIns(UserId, CheckInDate);
```

---

## API Endpoints

| Method | Route | Description | Returns |
|--------|-------|-------------|---------|
| `POST` | `/api/check-ins/morning` | Submit morning check-in | 201 + ID |
| `POST` | `/api/check-ins/evening` | Submit evening check-in | 201 + ID |
| `GET` | `/api/check-ins/today` | Today's state (morning + evening + streak) | 200 |
| `GET` | `/api/check-ins` | History (optional `fromDate`, `toDate`) | 200 |
| `GET` | `/api/check-ins/{id}` | Single check-in detail | 200 / 404 |
| `PUT` | `/api/check-ins/{id}` | Update existing check-in | 204 / 400 / 404 |
| `POST` | `/api/check-ins/skip` | Skip a check-in | 201 + ID |

### Request/Response Examples

**Submit Morning (POST /api/check-ins/morning)**
```json
{
  "energyLevel": 4,
  "selectedMode": "Full",
  "top1Type": "Task",
  "top1EntityId": "a1b2c3d4-...",
  "intention": "Ship the check-ins feature"
}
```

**Submit Evening (POST /api/check-ins/evening)**
```json
{
  "top1Completed": true,
  "energyLevelPm": 3,
  "stressLevel": 2,
  "reflection": "Good deep work session, shipped check-ins on time",
  "blockerCategory": null,
  "blockerNote": null
}
```

**Today's State (GET /api/check-ins/today)**
```json
{
  "morningCheckIn": {
    "id": "...",
    "checkInDate": "2026-01-27",
    "type": "Morning",
    "status": "Completed",
    "energyLevel": 4,
    "selectedMode": "Full",
    "top1Type": "Task",
    "top1EntityId": "a1b2c3d4-...",
    "intention": "Ship the check-ins feature"
  },
  "eveningCheckIn": null,
  "morningStatus": "Completed",
  "eveningStatus": "Pending",
  "checkInStreakDays": 5
}
```

**Skip (POST /api/check-ins/skip)**
```json
{
  "type": "Evening"
}
```

---

## Validation Rules

### Morning Check-In

| Field | Rule |
|-------|------|
| `energyLevel` | Required, integer 1-5 |
| `selectedMode` | Required, one of: Full, Maintenance, Minimum |
| `top1Type` | Optional, one of: Task, Habit, Project, FreeText |
| `top1EntityId` | Required if top1Type is Task/Habit/Project |
| `top1FreeText` | Required if top1Type is FreeText, max 200 chars |
| `intention` | Optional, max 500 chars |
| `checkInDate` | Optional (defaults to today), format yyyy-MM-dd |

### Evening Check-In

| Field | Rule |
|-------|------|
| `top1Completed` | Optional, boolean |
| `energyLevelPm` | Optional, integer 1-5 |
| `stressLevel` | Optional, integer 1-5 |
| `reflection` | Optional, max 1000 chars |
| `blockerCategory` | Optional, one of: TooTired, NoTime, Forgot, Environment, Conflict, Sickness, Other |
| `blockerNote` | Optional, max 500 chars |
| `checkInDate` | Optional (defaults to today), format yyyy-MM-dd |

### Skip

| Field | Rule |
|-------|------|
| `type` | Required, one of: Morning, Evening |
| `checkInDate` | Optional (defaults to today) |

### Cross-Cutting Rules

- **Uniqueness**: Cannot submit a morning/evening check-in if one already exists for that user+date+type
- **Ownership**: Users can only read/update their own check-ins
- **Idempotency**: Skip creates a new check-in record (not an update to an existing one)

---

## UI Implementation

### Pages & Routes

| Route | Component | Purpose |
|-------|-----------|---------|
| `/check-in` | `CheckInPage` | Dashboard + flow launcher |

### Component Structure

```
features/check-ins/
├── api/
│   └── check-ins-api.ts              # API client (7 methods)
├── hooks/
│   └── use-check-ins.ts              # React Query hooks (4 queries + 4 mutations)
├── schemas/
│   ├── check-in-schema.ts            # Zod schemas (morning + evening)
│   └── index.ts
├── components/
│   ├── morning/
│   │   ├── energy-step.tsx            # 5-button energy selector (1-5)
│   │   ├── mode-step.tsx              # Full/Maintenance/Minimum cards
│   │   ├── top1-step.tsx              # Entity picker + task creation sheet
│   │   ├── morning-summary.tsx        # Confirmation view
│   │   └── morning-check-in-flow.tsx  # 5-step orchestrator
│   ├── evening/
│   │   ├── top1-review-step.tsx       # Yes/No with entity name display
│   │   ├── evening-energy-step.tsx    # PM energy + stress dual scale
│   │   ├── blocker-step.tsx           # Category grid + optional note
│   │   ├── reflection-step.tsx        # Textarea with counter
│   │   ├── evening-summary.tsx        # Full evening recap
│   │   └── evening-check-in-flow.tsx  # 5-step orchestrator
│   └── common/
│       ├── step-progress.tsx          # Mobile bar + desktop dots
│       ├── energy-badge.tsx           # Color-coded energy display
│       ├── check-in-status-banner.tsx # Morning/evening status card
│       ├── check-in-history-list.tsx  # Grouped history with click-to-detail
│       └── check-in-detail-sheet.tsx  # Slide-out detail view
├── pages/
│   └── check-in-page.tsx             # Main page (dashboard + flows)
└── index.ts                           # Barrel exports
```

### Morning Flow (5 Steps)

| Step | Component | Required | Auto-Advance |
|------|-----------|----------|--------------|
| 0 | EnergyStep | Yes (1-5) | Yes (300ms) |
| 1 | ModeStep | Yes | Yes (300ms) |
| 2 | Top1Step | No | No |
| 3 | Intention (textarea) | No | No |
| 4 | MorningSummary | — | Submit |

**Navigation**: Back, Next, Skip (step 0 skips entire flow), Skip Step (steps 2-3).

### Evening Flow (5 Steps)

| Step | Component | Required | Auto-Advance |
|------|-----------|----------|--------------|
| 0 | Top1ReviewStep | No | Yes (300ms) |
| 1 | EveningEnergyStep | No | No |
| 2 | BlockerStep | No | No |
| 3 | ReflectionStep | No | No |
| 4 | EveningSummary | — | Submit |

**Navigation**: Back, Next, Skip (step 0 skips entire flow), Skip Step (steps 1-3).

### Dashboard View

The main `/check-in` page shows three states:

1. **Active Flow**: Full-screen morning or evening flow (replaces dashboard)
2. **Pending Check-Ins**: Status banners with "Start" buttons
3. **All Done**: Green completion card when both are done

Below the status area: **Recent History** showing the last 7 days, grouped by date, with morning/evening columns. Each completed check-in is clickable and opens a slide-out Sheet with full detail.

### Detail Sheet

When a history entry is clicked, a right-side Sheet slides in showing:

- **Header**: Type icon (Sun/Moon), date, completion time
- **Morning Detail**: Energy badge, day mode badge, #1 priority (with resolved entity name + type icon), intention
- **Evening Detail**: Top 1 completion (Yes/No), PM energy badge, stress level, blocker (emoji + category + note), reflection

The Top 1 entity name is resolved lazily using `useTask`, `useHabit`, or `useProject` hooks based on the stored `Top1Type` and `Top1EntityId`.

### State Management

```typescript
// Query Keys (TanStack Query)
checkInKeys = {
  all: ['check-ins'],
  today: () => ['check-ins', 'today'],
  lists: () => ['check-ins', 'list'],
  list: (from?, to?) => ['check-ins', 'list', { from, to }],
  details: () => ['check-ins', 'detail'],
  detail: (id) => ['check-ins', 'detail', id],
}
```

All mutations invalidate `today()` and `lists()` on success, ensuring the dashboard and history refresh.

### UI Helpers

```typescript
// Energy level display (1-5)
energyLevelInfo[level] = { label, color, bgColor, emoji }
// 1: "Exhausted" 🔴, 2: "Low" 🟠, 3: "Moderate" 🟡, 4: "Good" 🟢, 5: "Peak" ⚡

// Blocker categories
blockerCategoryInfo[category] = { label, emoji }
// TooTired: 😴, NoTime: ⏰, Forgot: 🤔, Environment: 🏠, Conflict: ⚡, Sickness: 🤒, Other: ❓

// Top 1 type metadata
top1TypeInfo[type] = { label, description }
```

---

## Integration Points

### Check-Ins → Metrics System

Morning energy is automatically recorded as a `MetricObservation` via the `MorningCheckInSubmittedEventHandler`. This bridges the check-in into the goals/metrics system, enabling energy trend analysis on goal scoreboards.

### Check-Ins → Tasks/Habits/Projects

The Top 1 step fetches live data from:
- `useTodayTasks()` — tasks scheduled for today (excluding completed/cancelled)
- `useTodayHabits()` — habits due today (excluding completed)
- `useProjects({ status: 'Active' })` — active projects

New tasks can be created inline via the full `TaskForm` component (embedded in a Sheet), which supports project and goal association.

### Check-Ins → Planning Engine (Downstream)

Morning energy + mode feed into the planning engine:
- Energy 1-2 → reduce planned commitments
- Minimum mode → only non-negotiables
- Maintenance mode → standard plan
- Full mode → maximize output

### Check-Ins → Diagnostic Engine (Downstream)

Evening data feeds pattern detection:
- Repeated blocker categories → systemic issue diagnosis
- Top 1 miss patterns → intention-reality gap analysis
- AM/PM energy delta → energy management insights
- Stress trends → burnout risk detection

---

## Extension Guide

### Adding a New Morning Field

1. **Domain**: Add nullable property to `CheckIn.cs`, update `CreateMorning()` and `UpdateMorning()`
2. **Infrastructure**: Update `CheckInConfiguration.cs` with column mapping
3. **Application**: Update `SubmitMorningCheckInCommand`, validator, handler, and DTOs
4. **API**: Update `SubmitMorningCheckInRequest` in `Requests.cs`
5. **Frontend**: Update `check-in.ts` types, `check-in-schema.ts`, morning flow components
6. **Migration**: `dotnet ef migrations add AddCheckInField --project Mastery.Infrastructure --startup-project Mastery.Api`

### Adding a New Blocker Category

1. **Domain**: Add to `BlockerCategory` enum
2. **Application**: Update validator's allowed values list
3. **Frontend**: Add to `BlockerCategory` type and `blockerCategoryInfo` map
4. **UI**: Category will automatically appear in blocker step grid

### Adding a New Top1Type

1. **Domain**: Add to `Top1Type` enum
2. **Application**: Update validators and command handlers for new entity resolution
3. **Frontend**: Add to `Top1Type` type, `top1TypeInfo`, and `typeOptions` in `top1-step.tsx`
4. **UI**: Add data fetching hook and suggestion mapping in `top1-step.tsx`

---

## Testing Considerations

### Unit Tests (Domain)

- `CheckIn.CreateMorning()` validates energy 1-5, requires mode
- `CheckIn.CreateEvening()` accepts all-null fields
- `CheckIn.CreateSkipped()` sets status to Skipped, no other fields
- `CheckIn.UpdateMorning()` emits `CheckInUpdatedEvent`
- Top1 cross-validation: FreeText requires text, entity types require ID

### Integration Tests (Application)

- Morning submission creates check-in + emits event
- Duplicate morning for same date returns error
- Evening submission with no fields succeeds
- Skip creates Skipped check-in
- Today state returns correct statuses for each combination
- Streak calculation across consecutive days
- Streak breaks on missing day

### API Tests

- POST `/api/check-ins/morning` with valid data returns 201
- POST `/api/check-ins/morning` duplicate returns 400
- POST `/api/check-ins/evening` with empty body returns 201
- GET `/api/check-ins/today` returns both check-ins + streak
- GET `/api/check-ins/{id}` for another user's check-in returns 404
- PUT `/api/check-ins/{id}` updates fields and emits event

---

## Future Considerations

### Potential Enhancements

1. **Draft/Resume**: Save partial check-in progress (Draft status)
2. **Habit Sweep**: Evening batch habit completion (Done/Missed/Skipped per habit)
3. **Task Sweep**: Evening batch task completion (Done/Snooze/Blocked/Cancel per task)
4. **Adaptive Steps**: Skip steps the user never fills in (learn from history)
5. **Voice Input**: Speech-to-text for reflection and intention
6. **Notification Triggers**: Push notifications at configured check-in times
7. **Weekly Digest**: Aggregated view of the week's check-ins
8. **Check-In Editing**: Edit completed check-ins from the detail sheet

### Performance Considerations

- Today's state is queried frequently—consider response caching
- Streak calculation queries up to 365 days—index on `{UserId, CheckInDate}` is critical
- History list defaults to 7 days—pagination not needed for MVP
- Entity name resolution in detail sheet uses individual hooks—acceptable for single-item views

---

## Glossary

| Term | Definition |
|------|------------|
| **Check-In** | A structured daily sensor reading (morning or evening) |
| **Morning Check-In** | Intent-setting: energy, mode, Top 1, intention |
| **Evening Check-In** | Reality-capture: Top 1 review, energy, stress, blocker, reflection |
| **Top 1** | The single most important priority for the day |
| **Day Mode** | Full / Maintenance / Minimum — sets ambition level |
| **Energy Level** | Self-rated 1-5 scale (Exhausted → Peak) |
| **Blocker** | What prevented progress (category + optional note) |
| **Streak** | Consecutive days with at least one completed check-in |
| **Sweep** | Batch completion of habits/tasks during evening check-in (future) |
