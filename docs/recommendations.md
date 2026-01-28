# AI Recommendations Feature Documentation

## Overview

The **AI Recommendations** feature is the "controller output" of Mastery's closed-loop control system. It produces typed, executable recommendation objects via a deterministic pipeline that detects diagnostic signals from user state, generates actionable candidates, ranks them, and persists full audit traces for explainability and learning.

**Design Philosophy**: Deterministic constraints first, LLM second. All signal detection, candidate generation, and ranking happens in code. The LLM's future role is selection refinement and natural language explanation only — never planning.

---

## Business Context

### Why Recommendations Exist

Most productivity apps tell you _what_ to do but not _why_ or _when_. Mastery's Recommendations close the loop:

1. **Signal-driven, not vibe-driven** — Every recommendation is backed by quantitative diagnostic signals with measurable evidence
2. **Capacity-aware** — Recommendations respect scheduled load, energy trends, and hard constraints
3. **Explainable** — Full pipeline traces stored so the user (and developer) can see _why_ any recommendation was made
4. **Bounded choices** — The LLM selects from pre-computed candidates; it cannot invent infeasible advice
5. **Outcome-tracked** — Accept/dismiss/snooze responses feed the learning engine for future personalization

### The Control System Analogy

| Control Concept | Recommendations Component |
|-----------------|--------------------------|
| **Sensors** | Diagnostic signal detectors (11 detectors monitoring state) |
| **State Estimator** | UserStateAssembler (snapshot of all user data) |
| **Controller** | Pipeline: detect → generate → rank → select |
| **Actuators** | Typed recommendation actions (create, update, execute, reflect) |
| **Feedback** | User response (accept/dismiss/snooze) + completion tracking |

### The Controller Pipeline Pattern

The pipeline executes 7 steps in sequence:

```
1. Assemble State ──→ UserStateSnapshot (parallel repo fetches)
2. Detect Signals ──→ DiagnosticSignal[] (11 detectors, continue on error)
3. Persist Signals ──→ Save to DiagnosticSignals table
4. Generate Candidates ──→ RecommendationCandidate[] (9 generators)
5. Rank ──→ Deduplicate + sort by score, take top 5
6. Orchestrate ──→ LLM selects from ranked list (Phase 1: passthrough)
7. Persist + Trace ──→ Recommendation + RecommendationTrace per selection
   └─ Expire Stale ──→ Mark pending >24h as Expired
```

Each step has independent error handling. A failing detector or generator is skipped — the pipeline continues with remaining components.

---

## Domain Model

### Entity Hierarchy

```
Recommendation (Aggregate Root)
├── Id (Guid)
├── UserId (string) ────────────── Owner
├── Type (enum) ────────────────── What kind of recommendation (12 types)
├── Status (enum) ──────────────── Lifecycle state (6 states)
├── Context (enum) ─────────────── When/why generated (6 triggers)
├── Target (Value Object) ──────── What entity is targeted
│   ├── Kind ───────────────────── Entity type (8 kinds)
│   ├── EntityId? ──────────────── ID of target entity
│   └── EntityTitle? ───────────── Display name
├── ActionKind (enum) ──────────── What user should do (7 kinds)
├── Title (string) ─────────────── User-facing title
├── Rationale (string, max 2000) ─ Why this recommendation
├── ActionPayload? (string) ────── JSON-encoded action details
├── Score (decimal, 0-100) ─────── Relevance score
├── ExpiresAt? (DateTime) ─────── Default: CreatedAt + 24h
├── RespondedAt? (DateTime) ────── When user responded
├── DismissReason? (string) ────── Why user dismissed (for learning)
├── SignalIds[] (JSON) ─────────── Contributing diagnostic signal IDs
│
└── Trace (Child Entity) ──────── Full pipeline audit trail
    ├── Id (Guid)
    ├── RecommendationId (FK)
    ├── StateSnapshotJson ──────── Snapshot summary (userId, entity counts)
    ├── SignalsSummaryJson ─────── Detected signals (id, type, severity)
    ├── CandidateListJson ─────── Pre-ranked candidates (type, title, score)
    ├── PromptVersion? ─────────── LLM prompt version (future)
    ├── ModelVersion? ──────────── LLM model version (future)
    ├── RawLlmResponse? ────────── Full LLM response (future)
    └── SelectionMethod ────────── "Deterministic" or LLM model name


DiagnosticSignal (Aggregate Root)
├── Id (Guid)
├── UserId (string)
├── Type (enum) ────────────────── Signal type (11 types)
├── Title (string) ─────────────── Short title
├── Description (string, max 2000) Full description
├── Severity (int, 0-100) ─────── Severity score
├── Evidence (Value Object) ────── Quantitative backing
│   ├── Metric ─────────────────── Metric name (e.g., "ScheduledMinutesToday")
│   ├── CurrentValue ───────────── Actual observed value
│   ├── ThresholdValue? ────────── Threshold being exceeded
│   └── Detail? ────────────────── Contextual detail
├── DetectedOn (DateOnly) ──────── Detection date
├── IsActive (bool) ────────────── Whether signal is still active
└── ResolvedByRecommendationId? ── Recommendation that resolved it
```

---

## Enums Reference

### RecommendationType

Defines the 12 kinds of recommendations the system can produce:

| Type | Description | Typical Action | Score Range |
|------|-------------|----------------|-------------|
| `NextBestAction` | Most impactful action to take now | ExecuteToday | 40-55 |
| `Top1Suggestion` | Suggested daily priority (Top-1) | Update | 70 |
| `HabitModeSuggestion` | Scale habit to minimum mode | Update | 50 |
| `PlanRealismAdjustment` | Fix overambitious daily plan | Defer | — |
| `TaskBreakdownSuggestion` | Break large task into subtasks | Create | — |
| `ScheduleAdjustmentSuggestion` | Suggest schedule changes | Update | — |
| `ProjectStuckFix` | Define next action for stalled project | Create | 55 |
| `ExperimentRecommendation` | Run behavioral experiment | Create | 45 |
| `GoalScoreboardSuggestion` | Add missing metrics to goal scoreboard | Update | 40 |
| `HabitFromLeadMetricSuggestion` | Create habit to drive an orphaned lead metric | Create | 45 |
| `CheckInConsistencyNudge` | Re-establish check-in routine | ReflectPrompt | 55 |
| `MetricObservationReminder` | Record stale manual metric observation | Create | 35 |

### RecommendationStatus

Controls recommendation lifecycle and what operations are permitted:

| Status | Description | Allowed Transitions |
|--------|-------------|---------------------|
| `Pending` | New, awaiting user response | → Accepted, → Dismissed, → Snoozed, → Expired |
| `Accepted` | User acknowledged and accepted | → Executed |
| `Dismissed` | User rejected (with optional reason) | (terminal) |
| `Snoozed` | Deferred for later review | → Accepted, → Dismissed, → Expired |
| `Expired` | 24+ hours old without response | (terminal) |
| `Executed` | Accepted and completed | (terminal) |

**Business Rules**:
- Only `Pending` or `Snoozed` recommendations can be accepted, dismissed, or snoozed
- Expiry is automatic: the pipeline marks all pending/snoozed recommendations older than 24 hours as `Expired`
- Only `Accepted` recommendations can transition to `Executed`
- `DismissReason` is captured for dismissed recommendations to feed the learning engine

### RecommendationContext

When/why a recommendation batch was generated:

| Context | Description | Typical Trigger |
|---------|-------------|-----------------|
| `Onboarding` | Initial setup after profile creation | First-time user |
| `MorningCheckIn` | Start-of-day planning | Morning check-in submission |
| `Midday` | Mid-day course correction | Scheduled or manual |
| `EveningCheckIn` | End-of-day reflection | Evening check-in submission |
| `WeeklyReview` | Weekly planning and review | Scheduled weekly |
| `DriftAlert` | Triggered by goal drift detection | Automatic when lead metrics drift |

### RecommendationTargetKind

What entity a recommendation targets:

| Kind | Description | Example |
|------|-------------|---------|
| `Goal` | Goal entity | "Add a lead metric to your Fitness goal" |
| `Metric` | Metric definition | "Record your body weight observation" |
| `Habit` | Habit entity | "Switch Morning Run to minimum mode" |
| `HabitOccurrence` | Specific habit completion | — |
| `Task` | Task entity | "Complete the API review task" |
| `Project` | Project entity | "Define next action for Website Redesign" |
| `Experiment` | Experiment entity | "Try a 2-week sleep experiment" |
| `UserProfile` | User-level action | "Re-establish your check-in routine" |

### RecommendationActionKind

What the user should do:

| Kind | Description | Example |
|------|-------------|---------|
| `Create` | Create a new entity | "Create a habit for deep work hours" |
| `Update` | Modify an existing entity | "Switch habit to minimum mode" |
| `ExecuteToday` | Complete an action today | "Work on the API review task" |
| `Defer` | Reschedule or postpone | "Move 2 tasks to tomorrow" |
| `Remove` | Delete or archive | — |
| `ReflectPrompt` | Prompt for reflection | "Reflect on your check-in consistency" |
| `LearnPrompt` | Prompt for learning/insight | — |

### SignalType

The 11 diagnostic signals the system can detect:

| Signal | Description | Severity Range |
|--------|-------------|---------------|
| `PlanRealismRisk` | Day is overbooked (>480 scheduled minutes) | 0-100 |
| `Top1FollowThroughLow` | <50% Top-1 completion in last 7 days | 65 |
| `CheckInConsistencyDrop` | Streak broken or <3 morning check-ins/week | 40-60 |
| `HabitAdherenceDrop` | Active habit <60% adherence (7-day) | 40-100 |
| `FrictionHigh` | Task rescheduled >3 times | 45-100 |
| `ProjectStuck` | Active project with no next action defined | 70 |
| `LeadMetricDrift` | Lead metric value <70% of target | 60 |
| `GoalScoreboardIncomplete` | Goal missing lag, lead, or constraint metric | 50 |
| `NoActuatorForLeadMetric` | Manual lead metric with no habit binding | 50 |
| `CapacityOverload` | >10 tasks or >600 minutes scheduled today | 80 |
| `EnergyTrendLow` | Average morning energy <=2/5 over 5 days (min 3 data points) | 70 |

---

## Value Objects

### RecommendationTarget

Identifies what entity a recommendation targets. Stored as flattened columns in the `Recommendations` table.

```csharp
RecommendationTarget.Create(RecommendationTargetKind.Task, taskId, "API Review")
```

**Properties:**
- `Kind` — Entity type
- `EntityId?` — Guid of target entity (null for user-level actions)
- `EntityTitle?` — Display name for UI

### SignalEvidence

Quantitative evidence backing a diagnostic signal. Stored as flattened columns in the `DiagnosticSignals` table.

```csharp
SignalEvidence.Create("ScheduledMinutesToday", 540, 480, "11 tasks totaling 540 minutes")
```

**Properties:**
- `Metric` — What's being measured (e.g., "Adherence7Day", "RescheduleCount")
- `CurrentValue` — Observed value
- `ThresholdValue?` — Threshold being exceeded (null if no fixed threshold)
- `Detail?` — Human-readable context

---

## Domain Events

| Event | Trigger | Typical Handler Action |
|-------|---------|----------------------|
| `RecommendationsGeneratedEvent` | Pipeline completes | Update dashboard badge count |
| `RecommendationAcceptedEvent` | User accepts | Track acceptance rate, trigger action |
| `RecommendationDismissedEvent` | User dismisses | Log dismissal reason for learning |
| `RecommendationSnoozedEvent` | User snoozes | Reschedule notification |
| `DiagnosticSignalDetectedEvent` | Signal detected | Audit log, drift alerting |

### Event Payloads

```csharp
record RecommendationsGeneratedEvent(string UserId, RecommendationContext Context, int Count);
record RecommendationAcceptedEvent(Guid RecommendationId, string UserId, RecommendationType Type);
record RecommendationDismissedEvent(Guid RecommendationId, string UserId, RecommendationType Type, string? Reason);
record RecommendationSnoozedEvent(Guid RecommendationId, string UserId);
record DiagnosticSignalDetectedEvent(Guid SignalId, string UserId, SignalType Type, int Severity);
```

---

## Signal Detectors (11 Implementations)

Signal detectors are the "sensors" of the control system. Each implements `IDiagnosticSignalDetector` and inspects a `UserStateSnapshot` to detect conditions that warrant intervention.

All detectors:
- Are `sealed` classes with parameterless constructors
- Return `IReadOnlyList<DiagnosticSignal>` (0 or more signals)
- Declare which `SignalType`(s) they can produce via `SupportedSignals`
- Are registered via DI as `IEnumerable<IDiagnosticSignalDetector>`
- Are invoked in parallel by the pipeline (errors are caught per-detector)

### 1. PlanRealismRiskDetector

**Signal:** `PlanRealismRisk`

**Logic:** Sums `EstMinutes` of all tasks scheduled for today. Fires if total exceeds 480 minutes (8 hours).

**Severity formula:** `min(100, (totalMinutes - 480) / 5)`

| Evidence Field | Value |
|---------------|-------|
| Metric | `ScheduledMinutesToday` |
| CurrentValue | Total scheduled minutes |
| ThresholdValue | 480 |

### 2. Top1FollowThroughLowDetector

**Signal:** `Top1FollowThroughLow`

**Logic:** Examines last 7 days of morning check-ins. Counts how many had a `Top1EntityId` set and how many of those were completed. Fires if completion rate < 50%.

**Severity:** 65 (fixed)

| Evidence Field | Value |
|---------------|-------|
| Metric | `Top1CompletionRate` |
| CurrentValue | Completion percentage (0-100) |
| ThresholdValue | 50 |

### 3. CheckInConsistencyDropDetector

**Signal:** `CheckInConsistencyDrop` (may fire 1-2 signals)

**Logic:** Two independent checks:
1. **Streak check:** If `CheckInStreak == 0` → fires with severity 60
2. **Frequency check:** If fewer than 3 completed morning check-ins in last 7 days → fires with severity 40 (or 60 if streak is also 0)

| Evidence Field | Value |
|---------------|-------|
| Metric | `CheckInStreak` or `MorningCheckInsLast7Days` |
| CurrentValue | Current streak or count |
| ThresholdValue | 1 or 3 |

### 4. HabitAdherenceDropDetector

**Signal:** `HabitAdherenceDrop` (per active habit)

**Logic:** For each active habit, checks 7-day adherence. Fires if adherence < 60%.

**Severity formula:** `(1 - adherence) * 100`

| Evidence Field | Value |
|---------------|-------|
| Metric | `Adherence7Day` |
| CurrentValue | Adherence rate (0.0-1.0) |
| ThresholdValue | 0.6 |
| Detail | Habit title |

### 5. FrictionHighDetector

**Signal:** `FrictionHigh` (per task)

**Logic:** For each task with `RescheduleCount > 3`, fires a signal.

**Severity formula:** `min(100, rescheduleCount * 15)`

| Evidence Field | Value |
|---------------|-------|
| Metric | `RescheduleCount` |
| CurrentValue | Reschedule count |
| ThresholdValue | 3 |
| Detail | Task title |

### 6. ProjectStuckDetector

**Signal:** `ProjectStuck` (per active project)

**Logic:** For each active project where `NextTaskId` is null (no next action defined), fires a signal.

**Severity:** 70 (fixed)

| Evidence Field | Value |
|---------------|-------|
| Metric | `NextTaskId` |
| CurrentValue | 0 |
| ThresholdValue | 1 |
| Detail | Project title |

### 7. LeadMetricDriftDetector

**Signal:** `LeadMetricDrift` (per lead metric)

**Logic:** For each active goal's lead metrics, checks if `CurrentValue < TargetValue * 0.7`. Fires if the lead metric is drifting below 70% of target.

**Severity:** 60 (fixed)

| Evidence Field | Value |
|---------------|-------|
| Metric | Metric name |
| CurrentValue | Current metric value |
| ThresholdValue | `TargetValue * 0.7` |

### 8. GoalScoreboardIncompleteDetector

**Signal:** `GoalScoreboardIncomplete` (per goal)

**Logic:** For each active goal, checks if the metrics list includes at least one Lag, one Lead, and one Constraint metric. Fires if any are missing.

**Severity:** 50 (fixed)

| Evidence Field | Value |
|---------------|-------|
| Metric | `MissingMetricKinds` |
| CurrentValue | Number of metric kinds present (0-3) |
| ThresholdValue | 3 |
| Detail | Comma-separated missing kinds (e.g., "Lead, Constraint") |

### 9. NoActuatorForLeadMetricDetector

**Signal:** `NoActuatorForLeadMetric` (per orphaned metric)

**Logic:** For each active goal's lead metrics with `SourceHint == Manual`, checks if any habit has a matching `MetricBindingId`. If no habit is bound to the metric, the metric is "orphaned" — there's no automated actuator driving it.

**Severity:** 50 (fixed)

| Evidence Field | Value |
|---------------|-------|
| Metric | Metric name |
| CurrentValue | 0 (no bindings) |
| ThresholdValue | 1 |

### 10. CapacityOverloadDetector

**Signal:** `CapacityOverload`

**Logic:** Counts tasks scheduled for today. Fires if either:
- Task count > 10, OR
- Total estimated minutes > 600

**Severity:** 80 (fixed)

| Evidence Field | Value |
|---------------|-------|
| Metric | `ScheduledTaskCount` or `ScheduledMinutesToday` |
| CurrentValue | Count or total minutes |
| ThresholdValue | 10 or 600 |

### 11. EnergyTrendLowDetector

**Signal:** `EnergyTrendLow`

**Logic:** Looks at morning check-ins from the last 5 days. Requires at least 3 data points. If average energy level is <=2 (on 1-5 scale), fires.

**Severity:** 70 (fixed)

| Evidence Field | Value |
|---------------|-------|
| Metric | `AverageMorningEnergy` |
| CurrentValue | Average energy (1.0-5.0) |
| ThresholdValue | 2 |

---

## Candidate Generators (9 Implementations)

Candidate generators transform detected signals and user state into actionable recommendation candidates. Each implements `IRecommendationCandidateGenerator` and produces typed `RecommendationCandidate` objects with relevance scores.

All generators:
- Are `sealed` classes with parameterless constructors
- Receive the full `UserStateSnapshot`, all detected signals, and the current `RecommendationContext`
- Return `IReadOnlyList<RecommendationCandidate>` (0 or more candidates)
- Declare which `RecommendationType`(s) they can produce via `SupportedTypes`
- Are registered via DI as `IEnumerable<IRecommendationCandidateGenerator>`

### 1. NextBestActionGenerator

**Type:** `NextBestAction`
**Context:** Any
**Action:** `ExecuteToday`
**Target:** Task

**Logic:** Finds tasks that are ready or scheduled for today, then scores each:

| Factor | Points | Condition |
|--------|--------|-----------|
| Priority | (11 - priority) * 10 | Higher priority = more points |
| Urgency | +20 | Due within 2 days |
| Energy fit | +10 | Task energy matches today's check-in energy |
| Goal alignment | +15 | Task is linked to an active goal |

Returns top 3 candidates sorted by score.

### 2. Top1SuggestionGenerator

**Type:** `Top1Suggestion`
**Context:** `MorningCheckIn` only
**Action:** `Update`
**Target:** Task
**Score:** 70

**Logic:** Only fires during morning check-in context AND when today's morning check-in has no `Top1EntityId` set. Selects the highest-priority goal-aligned ready task as the suggested Top-1 for the day.

### 3. HabitModeSuggestionGenerator

**Type:** `HabitModeSuggestion`
**Context:** Any
**Action:** `Update`
**Target:** Habit
**Score:** 50

**Logic:** Checks if any morning check-in from the last 3 days has `EnergyLevel <= 2`. If low energy is detected, suggests switching every active habit that is NOT already in minimum mode to minimum mode (habit scaling).

### 4. ProjectStuckFixGenerator

**Type:** `ProjectStuckFix`
**Context:** Any
**Action:** `Create` (define next action)
**Target:** Project
**Score:** 55

**Logic:** Finds all active projects where `NextTaskId` is null. Cross-references with `ProjectStuck` signals by matching project title in signal description. Links contributing signal IDs.

### 5. ExperimentRecommendationGenerator

**Type:** `ExperimentRecommendation`
**Context:** Any
**Action:** `Create`
**Target:** UserProfile
**Score:** 45

**Logic:** If no experiment is currently active AND at least one signal has severity >= 60, suggests creating a behavioral experiment based on the highest-severity signal. Links the signal ID.

### 6. GoalScoreboardSuggestionGenerator

**Type:** `GoalScoreboardSuggestion`
**Context:** Any
**Action:** `Update`
**Target:** Goal
**Score:** 40

**Logic:** Finds all active goals with incomplete scoreboards (missing Lag, Lead, or Constraint metrics). Identifies which specific kinds are missing and generates a recommendation to add them. Cross-references with `GoalScoreboardIncomplete` signals. Links contributing signal IDs.

### 7. HabitFromLeadMetricSuggestionGenerator

**Type:** `HabitFromLeadMetricSuggestion`
**Context:** Any
**Action:** `Create` (create habit)
**Target:** Metric
**Score:** 45

**Logic:** For each `NoActuatorForLeadMetric` signal, finds the corresponding orphaned metric and suggests creating a habit to drive it. Links the signal ID.

### 8. CheckInConsistencyNudgeGenerator

**Type:** `CheckInConsistencyNudge`
**Context:** Any
**Action:** `ReflectPrompt`
**Target:** UserProfile
**Score:** 55

**Logic:** For each `CheckInConsistencyDrop` signal, generates a nudge to re-establish the check-in routine. Links the signal ID.

### 9. MetricObservationReminderGenerator

**Type:** `MetricObservationReminder`
**Context:** Any
**Action:** `Create` (record observation)
**Target:** Metric
**Score:** 35

**Logic:** Finds all manual metrics where:
- `LastObservationDate` is null (never recorded), OR
- `LastObservationDate` is older than 7 days (stale)

Generates context-aware rationale: "hasn't been updated since [date]" or "has never been recorded".

---

## Pipeline Services

### UserStateAssembler

Builds a comprehensive `UserStateSnapshot` by fetching from 8 repositories in parallel.

**Data sources fetched (via `Task.WhenAll`):**
1. Goals (with metrics)
2. Habits (with occurrences)
3. Tasks
4. Projects
5. Experiments
6. Recent check-ins (last 14 days)
7. Metric definitions
8. Metric observations (last 7 days)

**Derived calculations:**
- Habit adherence rates (7-day window)
- Habit streaks (consecutive completions)
- Metric source types (derived from goal metric `SourceHint` bindings)
- Aggregated metric current values
- Check-in streak count

### DefaultRecommendationRanker

Simple but effective ranking strategy:

1. **Deduplicate** by `(TargetKind, EntityId)` — keeps the highest-scoring candidate per target entity
2. **Sort** by score descending
3. **Take top N** (default: 5)

### DeterministicLlmOrchestrator

Phase 1 implementation: passes ranked candidates through unchanged.

- `SelectionMethod` = `"Deterministic"`
- No prompt version, model version, or raw response

**Future phases** can swap this for:
- Claude API orchestrator (semantic selection + natural language rationale)
- Reinforcement learning orchestrator (learn from acceptance rates)

### RecommendationPipeline

Master orchestrator executing all pipeline steps:

1. **Assemble state** — `UserStateAssembler.AssembleAsync()`
2. **Detect signals** — Loop all `IDiagnosticSignalDetector` instances (continue on error)
3. **Persist signals** — Save new `DiagnosticSignal` entities
4. **Generate candidates** — Loop all `IRecommendationCandidateGenerator` instances (continue on error)
5. **Rank** — `DefaultRecommendationRanker.Rank()`
6. **Orchestrate** — `ILlmRecommendationOrchestrator.OrchestrateAsync()`
7. **Persist** — For each selected candidate:
   - Create `Recommendation` entity with 24h expiry
   - Create `RecommendationTrace` with full pipeline snapshot
8. **Expire stale** — `ExpirePendingBeforeAsync()` for recommendations older than 24h

**Error handling:** Each detector and generator runs independently. If one throws, the error is logged and the pipeline continues with the remaining components.

---

## Implementation Architecture

### Layer Responsibilities

```
┌─────────────────────────────────────────────────────────────────┐
│                         API Layer                               │
│  Controllers/RecommendationsController.cs                       │
│  Contracts/Recommendations/Requests.cs                          │
│  - HTTP endpoints (7 routes)                                    │
│  - Request/response mapping                                     │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                     Application Layer                            │
│  Features/Recommendations/Commands/ (4 commands)                │
│  Features/Recommendations/Queries/ (3 queries)                  │
│  Features/Recommendations/Services/ (4 services)                │
│  Features/Recommendations/SignalDetectors/ (11 detectors)       │
│  Features/Recommendations/CandidateGenerators/ (9 generators)   │
│  Features/Recommendations/Models/ (DTOs + mappings)             │
│  Common/Interfaces/ (6 pipeline interfaces)                     │
│  Common/Models/ (3 pipeline models)                             │
│  - CQRS via MediatR                                             │
│  - Pipeline orchestration                                       │
│  - Signal detection + candidate generation                      │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                       Domain Layer                               │
│  Entities/Recommendation/ (3 entities)                          │
│  ValueObjects/ (2 value objects)                                │
│  Enums/ (6 enums)                                               │
│  Events/RecommendationEvents.cs                                 │
│  Interfaces/ (2 repository interfaces)                          │
│  - Business rules & invariants                                  │
│  - Status transitions                                           │
│  - Domain events                                                │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                   Infrastructure Layer                           │
│  Data/Configurations/ (3 EF configurations)                     │
│  Repositories/ (2 repository implementations)                   │
│  - EF Core mapping (JSON columns, owned entities)              │
│  - Database operations                                          │
│  - Query optimization (indexes)                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Key Files Reference

| Layer | File | Purpose |
|-------|------|---------|
| Domain | `Entities/Recommendation/Recommendation.cs` | Aggregate root with lifecycle methods |
| Domain | `Entities/Recommendation/RecommendationTrace.cs` | Audit trail entity |
| Domain | `Entities/Recommendation/DiagnosticSignal.cs` | Signal aggregate root |
| Domain | `ValueObjects/RecommendationTarget.cs` | Target entity reference |
| Domain | `ValueObjects/SignalEvidence.cs` | Quantitative signal evidence |
| Domain | `Enums/RecommendationType.cs` | 12 recommendation types |
| Domain | `Enums/SignalType.cs` | 11 signal types |
| Domain | `Events/RecommendationEvents.cs` | Domain events |
| Domain | `Interfaces/IRecommendationRepository.cs` | Repository contract |
| Domain | `Interfaces/IDiagnosticSignalRepository.cs` | Signal repository contract |
| Application | `Common/Interfaces/IRecommendationPipeline.cs` | Pipeline contract |
| Application | `Common/Interfaces/IDiagnosticSignalDetector.cs` | Detector contract |
| Application | `Common/Interfaces/IRecommendationCandidateGenerator.cs` | Generator contract |
| Application | `Common/Interfaces/IRecommendationRanker.cs` | Ranker contract |
| Application | `Common/Interfaces/ILlmRecommendationOrchestrator.cs` | LLM orchestrator contract |
| Application | `Common/Interfaces/IUserStateAssembler.cs` | State assembler contract |
| Application | `Common/Models/UserStateSnapshot.cs` | Snapshot model + nested records |
| Application | `Common/Models/RecommendationCandidate.cs` | Pre-ranked candidate |
| Application | `Common/Models/RankedRecommendation.cs` | LLM orchestration result |
| Application | `Features/Recommendations/Services/RecommendationPipeline.cs` | Pipeline implementation |
| Application | `Features/Recommendations/Services/UserStateAssembler.cs` | State assembly |
| Application | `Features/Recommendations/Services/RecommendationRanker.cs` | Ranking implementation |
| Application | `Features/Recommendations/Services/DeterministicLlmOrchestrator.cs` | Phase 1 LLM stub |
| Application | `Features/Recommendations/SignalDetectors/` | 11 detector implementations |
| Application | `Features/Recommendations/CandidateGenerators/` | 9 generator implementations |
| Application | `Features/Recommendations/Models/RecommendationDto.cs` | DTOs + mappings |
| Application | `Features/Recommendations/Commands/GenerateRecommendations/` | Pipeline trigger |
| Application | `Features/Recommendations/Commands/AcceptRecommendation/` | Accept action |
| Application | `Features/Recommendations/Commands/DismissRecommendation/` | Dismiss action |
| Application | `Features/Recommendations/Commands/SnoozeRecommendation/` | Snooze action |
| Application | `Features/Recommendations/Queries/GetActiveRecommendations/` | Active listing |
| Application | `Features/Recommendations/Queries/GetRecommendationById/` | Detail with trace |
| Application | `Features/Recommendations/Queries/GetRecommendationHistory/` | History listing |
| Infrastructure | `Data/Configurations/RecommendationConfiguration.cs` | EF mapping |
| Infrastructure | `Data/Configurations/RecommendationTraceConfiguration.cs` | Trace EF mapping |
| Infrastructure | `Data/Configurations/DiagnosticSignalConfiguration.cs` | Signal EF mapping |
| Infrastructure | `Repositories/RecommendationRepository.cs` | Data access |
| Infrastructure | `Repositories/DiagnosticSignalRepository.cs` | Signal data access |
| API | `Controllers/RecommendationsController.cs` | HTTP endpoints |
| API | `Contracts/Recommendations/Requests.cs` | Request DTOs |

### Database Schema

```sql
-- Recommendations table
CREATE TABLE Recommendations (
    Id uniqueidentifier PRIMARY KEY,
    UserId nvarchar(256) NOT NULL,
    Type nvarchar(50) NOT NULL,
    Status nvarchar(20) NOT NULL,
    Context nvarchar(30) NOT NULL,
    TargetKind nvarchar(30) NOT NULL,
    TargetEntityId uniqueidentifier NULL,
    TargetEntityTitle nvarchar(200) NULL,
    ActionKind nvarchar(20) NOT NULL,
    Title nvarchar(500) NOT NULL,
    Rationale nvarchar(2000) NOT NULL,
    ActionPayload nvarchar(max) NULL,
    Score decimal(5,2) NOT NULL,
    ExpiresAt datetime2 NULL,
    RespondedAt datetime2 NULL,
    DismissReason nvarchar(2000) NULL,
    SignalIds nvarchar(max) NOT NULL,         -- JSON array of Guids
    CreatedAt datetime2 NOT NULL,
    CreatedBy nvarchar(256) NULL,
    ModifiedAt datetime2 NULL,
    ModifiedBy nvarchar(256) NULL
);

-- RecommendationTraces table (1:1 with Recommendations)
CREATE TABLE RecommendationTraces (
    Id uniqueidentifier PRIMARY KEY,
    RecommendationId uniqueidentifier NOT NULL UNIQUE,
    StateSnapshotJson nvarchar(max) NOT NULL,
    SignalsSummaryJson nvarchar(max) NOT NULL,
    CandidateListJson nvarchar(max) NOT NULL,
    PromptVersion nvarchar(50) NULL,
    ModelVersion nvarchar(50) NULL,
    RawLlmResponse nvarchar(max) NULL,
    SelectionMethod nvarchar(50) NOT NULL,
    CreatedAt datetime2 NOT NULL,

    CONSTRAINT FK_RecommendationTraces_Recommendations
        FOREIGN KEY (RecommendationId) REFERENCES Recommendations(Id)
        ON DELETE CASCADE
);

-- DiagnosticSignals table
CREATE TABLE DiagnosticSignals (
    Id uniqueidentifier PRIMARY KEY,
    UserId nvarchar(256) NOT NULL,
    Type nvarchar(50) NOT NULL,
    Title nvarchar(500) NOT NULL,
    Description nvarchar(2000) NOT NULL,
    Severity int NOT NULL,
    EvidenceMetric nvarchar(200) NOT NULL,
    EvidenceCurrentValue decimal(18,4) NOT NULL,
    EvidenceThresholdValue decimal(18,4) NULL,
    EvidenceDetail nvarchar(500) NULL,
    DetectedOn date NOT NULL,
    IsActive bit NOT NULL DEFAULT 1,
    ResolvedByRecommendationId uniqueidentifier NULL,
    CreatedAt datetime2 NOT NULL,
    CreatedBy nvarchar(256) NULL,
    ModifiedAt datetime2 NULL,
    ModifiedBy nvarchar(256) NULL
);

-- Indexes
CREATE INDEX IX_Recommendations_UserId ON Recommendations(UserId);
CREATE INDEX IX_Recommendations_UserId_Status ON Recommendations(UserId, Status);
CREATE INDEX IX_Recommendations_UserId_Context_Status ON Recommendations(UserId, Context, Status);
CREATE INDEX IX_DiagnosticSignals_UserId ON DiagnosticSignals(UserId);
CREATE INDEX IX_DiagnosticSignals_UserId_Type ON DiagnosticSignals(UserId, Type);
CREATE INDEX IX_DiagnosticSignals_UserId_IsActive ON DiagnosticSignals(UserId, IsActive);
```

---

## API Endpoints

### Recommendation Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/recommendations/generate` | Trigger pipeline for a context |
| `GET` | `/api/recommendations` | Get active recommendations (optional context filter) |
| `GET` | `/api/recommendations/{id}` | Get recommendation with full trace |
| `POST` | `/api/recommendations/{id}/accept` | Accept recommendation |
| `POST` | `/api/recommendations/{id}/dismiss` | Dismiss with optional reason |
| `POST` | `/api/recommendations/{id}/snooze` | Snooze recommendation |
| `GET` | `/api/recommendations/history` | Get history with date range filter |

### Request/Response Examples

**Generate Recommendations (POST /api/recommendations/generate)**
```json
{
  "context": "MorningCheckIn"
}
```

**Active Recommendations Response (GET /api/recommendations)**
```json
[
  {
    "id": "a1b2c3d4-...",
    "userId": "auth0|123456",
    "type": "NextBestAction",
    "status": "Pending",
    "context": "MorningCheckIn",
    "targetKind": "Task",
    "targetEntityId": "e5f6g7h8-...",
    "targetEntityTitle": "Review API Design Doc",
    "actionKind": "ExecuteToday",
    "title": "Complete: Review API Design Doc",
    "rationale": "This is your highest-priority task due tomorrow. It aligns with your Product Launch goal.",
    "actionPayload": null,
    "score": 55.0,
    "expiresAt": "2026-01-28T10:30:00Z",
    "signalIds": [],
    "createdAt": "2026-01-27T10:30:00Z"
  }
]
```

**Recommendation Detail Response (GET /api/recommendations/{id})**
```json
{
  "id": "a1b2c3d4-...",
  "type": "ProjectStuckFix",
  "status": "Pending",
  "context": "MorningCheckIn",
  "targetKind": "Project",
  "targetEntityId": "p1p2p3p4-...",
  "targetEntityTitle": "Website Redesign",
  "actionKind": "Create",
  "title": "Define next action for Website Redesign",
  "rationale": "This project has no next action defined and appears stuck.",
  "score": 55.0,
  "signalIds": ["s1s2s3s4-..."],
  "trace": {
    "stateSnapshotJson": "{\"userId\":\"auth0|123456\",\"goalsCount\":3,\"habitsCount\":5,\"tasksCount\":12}",
    "signalsSummaryJson": "[{\"id\":\"s1s2s3s4-...\",\"type\":\"ProjectStuck\",\"severity\":70}]",
    "candidateListJson": "[{\"type\":\"ProjectStuckFix\",\"title\":\"Define next action...\",\"score\":55}]",
    "selectionMethod": "Deterministic",
    "promptVersion": null,
    "modelVersion": null,
    "rawLlmResponse": null
  },
  "createdAt": "2026-01-27T10:30:00Z"
}
```

**Dismiss Recommendation (POST /api/recommendations/{id}/dismiss)**
```json
{
  "reason": "Already working on this project through a different approach"
}
```

---

## UI Implementation

### Pages & Routes

| Route | Component | Purpose |
|-------|-----------|---------|
| `/recommendations` | `RecommendationsPage` | Active + history tabs with context filter |

### Component Structure

```
features/recommendations/
├── api/
│   └── recommendations-api.ts   # 7 API functions
├── hooks/
│   └── use-recommendations.ts   # 3 query hooks + 4 mutation hooks
├── schemas/
│   └── recommendation-schema.ts # Zod validation (generate, dismiss)
├── components/
│   ├── recommendation-card.tsx  # Card with type badge, score, actions
│   ├── recommendations-list.tsx # Grid with skeletons and empty state
│   ├── recommendation-detail-sheet.tsx # Side sheet with full trace
│   ├── recommendation-history-list.tsx # Date-grouped history
│   ├── signal-badge.tsx         # Signal type badge with severity
│   ├── generate-button.tsx      # Pipeline trigger with context select
│   └── index.ts                 # Barrel export
└── pages/
    └── recommendations-page.tsx # Tabbed page (Active / History)
```

### Key UI Patterns

**1. Active Recommendations View**
- Context filter buttons: All / Morning / Midday / Evening / Weekly
- Generate button with context selector
- Responsive grid of recommendation cards (2 columns on desktop)
- Click card to open detail sheet

**2. Recommendation Card**
- Type badge (color-coded) + status badge (if not Pending)
- Title + rationale (2-line clamp)
- Target entity with icon
- Score progress bar (0-100)
- Accept / Snooze / Dismiss buttons (only if Pending or Snoozed)
- Expiry countdown (if < 4 hours remaining)
- Left border colored by recommendation type

**3. Detail Sheet**
- Header: type badge + status + context badge
- Full rationale (no line clamp)
- Target info section
- Score display
- Action payload (if present)
- Collapsible trace section (signals summary, candidate list, model info)
- Timestamps (created, responded, expires)
- Action buttons with loading states
- Dismiss reason textarea (shown when dismissing)

**4. History View**
- Date range filter (Last 7 days / 30 days / All time)
- Timeline-style list sorted by creation date descending
- Status indicators with colors
- No action buttons (read-only)

### State Management

```typescript
// Query Keys (TanStack Query)
export const recommendationKeys = {
  all: ['recommendations'] as const,
  lists: () => [...recommendationKeys.all, 'list'] as const,
  active: (context?: string) => [...recommendationKeys.lists(), 'active', { context }] as const,
  history: (from?: string, to?: string) => [...recommendationKeys.lists(), 'history', { from, to }] as const,
  details: () => [...recommendationKeys.all, 'detail'] as const,
  detail: (id: string) => [...recommendationKeys.details(), id] as const,
}
```

All mutations invalidate the full `recommendationKeys.all` query tree on success, ensuring consistent cache state.

### UI Helpers

```typescript
// Recommendation type styling
export const recommendationTypeInfo: Record<RecommendationType, { label, description, color, bgColor }> = {
  NextBestAction:              { label: 'Next Action', ... },
  Top1Suggestion:              { label: 'Top Priority', ... },
  HabitModeSuggestion:         { label: 'Habit Scale', ... },
  ProjectStuckFix:             { label: 'Unblock Project', ... },
  ExperimentRecommendation:    { label: 'Experiment', ... },
  GoalScoreboardSuggestion:    { label: 'Scoreboard', ... },
  HabitFromLeadMetricSuggestion: { label: 'New Habit', ... },
  CheckInConsistencyNudge:     { label: 'Check-in', ... },
  MetricObservationReminder:   { label: 'Record Metric', ... },
  // ...
}

// Recommendation status styling
export const recommendationStatusInfo: Record<RecommendationStatus, { label, color, bgColor }> = {
  Pending:   { label: 'Pending',   color: 'text-yellow-400', bgColor: 'bg-yellow-500/10' },
  Accepted:  { label: 'Accepted',  color: 'text-green-400',  bgColor: 'bg-green-500/10' },
  Dismissed: { label: 'Dismissed', color: 'text-red-400',    bgColor: 'bg-red-500/10' },
  Snoozed:   { label: 'Snoozed',   color: 'text-blue-400',   bgColor: 'bg-blue-500/10' },
  Expired:   { label: 'Expired',   color: 'text-zinc-500',   bgColor: 'bg-zinc-500/10' },
  Executed:  { label: 'Executed',  color: 'text-emerald-400', bgColor: 'bg-emerald-500/10' },
}

// Signal type styling
export const signalTypeInfo: Record<SignalType, { label, icon, color, bgColor }> = {
  PlanRealismRisk:          { label: 'Plan Overload', ... },
  Top1FollowThroughLow:    { label: 'Top-1 Follow-Through', ... },
  CheckInConsistencyDrop:   { label: 'Check-in Drop', ... },
  HabitAdherenceDrop:       { label: 'Habit Adherence', ... },
  FrictionHigh:             { label: 'High Friction', ... },
  ProjectStuck:             { label: 'Stuck Project', ... },
  LeadMetricDrift:          { label: 'Metric Drift', ... },
  GoalScoreboardIncomplete: { label: 'Incomplete Scoreboard', ... },
  NoActuatorForLeadMetric:  { label: 'Orphaned Metric', ... },
  CapacityOverload:         { label: 'Capacity Overload', ... },
  EnergyTrendLow:           { label: 'Low Energy', ... },
}
```

---

## Extension Guide

### Adding a New Signal Detector

1. **Domain**: Add to `SignalType` enum in `Enums/SignalType.cs`

2. **Application**: Create detector class in `Features/Recommendations/SignalDetectors/`
   ```csharp
   public sealed class NewSignalDetector : IDiagnosticSignalDetector
   {
       public IReadOnlyList<SignalType> SupportedSignals => [SignalType.NewSignal];

       public Task<IReadOnlyList<DiagnosticSignal>> DetectAsync(
           UserStateSnapshot state, CancellationToken ct)
       {
           // Detection logic
       }
   }
   ```

3. **Application**: Register in `DependencyInjection.cs`
   ```csharp
   services.AddTransient<IDiagnosticSignalDetector, NewSignalDetector>();
   ```

4. **Frontend**: Add to `signalTypeInfo` in `types/recommendation.ts`

### Adding a New Candidate Generator

1. **Domain**: Add to `RecommendationType` enum

2. **Application**: Create generator class in `Features/Recommendations/CandidateGenerators/`
   ```csharp
   public sealed class NewGenerator : IRecommendationCandidateGenerator
   {
       public IReadOnlyList<RecommendationType> SupportedTypes => [RecommendationType.NewType];

       public Task<IReadOnlyList<RecommendationCandidate>> GenerateAsync(
           UserStateSnapshot state,
           IReadOnlyList<DiagnosticSignal> signals,
           RecommendationContext context,
           CancellationToken ct)
       {
           // Generation logic
       }
   }
   ```

3. **Application**: Register in `DependencyInjection.cs`
   ```csharp
   services.AddTransient<IRecommendationCandidateGenerator, NewGenerator>();
   ```

4. **Frontend**: Add to `recommendationTypeInfo` in `types/recommendation.ts`

### Replacing the LLM Orchestrator

1. **Application**: Create new implementation of `ILlmRecommendationOrchestrator`
   ```csharp
   public sealed class ClaudeLlmOrchestrator : ILlmRecommendationOrchestrator
   {
       public async Task<LlmOrchestrationResult> OrchestrateAsync(...)
       {
           // Call Claude API with ranked candidates
           // Return selected subset with rationale
       }
   }
   ```

2. **Application**: Swap registration in `DependencyInjection.cs`
   ```csharp
   services.AddTransient<ILlmRecommendationOrchestrator, ClaudeLlmOrchestrator>();
   ```

### Adding a New State Source

1. **Application**: Add new snapshot record in `UserStateSnapshot.cs`

2. **Application**: Update `UserStateAssembler.AssembleAsync()` to fetch the new data

3. **Application**: Update detectors/generators that need the new data

---

## Testing Considerations

### Unit Tests (Domain)

- `Recommendation.Create()` validates required fields
- `Recommendation.Accept()` rejects non-Pending/Snoozed
- `Recommendation.Dismiss()` stores reason
- `Recommendation.MarkExpired()` only works on Pending/Snoozed
- `RecommendationTarget.Create()` validates required kind
- `SignalEvidence.Create()` validates metric name
- `DiagnosticSignal.Resolve()` sets resolution reference

### Unit Tests (Signal Detectors)

- Each detector returns empty list when condition is not met
- Each detector returns correct signal type and severity
- Evidence values match expected thresholds
- Edge cases: empty state, single data point, boundary values

### Unit Tests (Candidate Generators)

- Each generator returns empty list when preconditions fail
- Correct recommendation type and action kind
- Score calculations are deterministic
- Signal ID linking is correct
- Context-specific generators (e.g., Top1) only fire in correct context

### Integration Tests (Application)

- `GenerateRecommendationsCommand` runs full pipeline
- `AcceptRecommendationCommand` transitions status correctly
- `DismissRecommendationCommand` stores reason
- `GetActiveRecommendationsQuery` filters by status
- `GetRecommendationHistoryQuery` filters by date range
- Pipeline continues when individual detectors/generators fail

### API Tests

- POST `/api/recommendations/generate` with valid context returns 200 + list
- POST `/api/recommendations/generate` with invalid context returns 400
- GET `/api/recommendations` returns only Pending/Snoozed
- GET `/api/recommendations/{id}` includes trace
- POST `/api/recommendations/{id}/accept` returns 204
- POST `/api/recommendations/{id}/dismiss` with reason returns 204

---

## Future Considerations

### Potential Enhancements

1. **LLM Orchestration** (Phase 2): Replace deterministic orchestrator with Claude API for semantic selection and natural language rationale generation
2. **Learning Engine**: Use acceptance/dismissal data to adjust candidate scores per user (reinforcement learning)
3. **Recommendation Chains**: Link recommendations sequentially (e.g., "create habit" → "record first observation")
4. **Push Notifications**: Trigger notifications when high-severity signals detected
5. **Recommendation Templates**: Pre-built intervention library for common patterns
6. **A/B Testing**: Test different ranking strategies or rationale styles
7. **Dismissal Analysis**: Aggregate dismissal reasons to improve candidate quality
8. **Context Auto-Detection**: Automatically determine context from time of day and user state

### Performance Considerations

- UserStateAssembler uses parallel I/O (8 concurrent repo queries)
- Signal detectors are stateless and can be parallelized
- Recommendation queries indexed by `(UserId, Status)` and `(UserId, Context, Status)`
- Trace JSON is stored as `nvarchar(max)` — not indexed, query by recommendation ID only
- Consider caching `UserStateSnapshot` within a pipeline run (already done)
- Pipeline auto-expires stale recommendations to keep active set small

### Data Migration Path

If changing recommendation structure:
1. Add new columns with defaults
2. Backfill transformation
3. Update application to use new format
4. Remove old columns in subsequent migration

---

## Glossary

| Term | Definition |
|------|------------|
| **Recommendation** | A typed, executable suggestion backed by diagnostic signals |
| **Diagnostic Signal** | A detected condition that warrants intervention |
| **Signal Evidence** | Quantitative data backing a diagnostic signal |
| **Recommendation Target** | The entity a recommendation acts upon |
| **Recommendation Trace** | Full audit trail of pipeline inputs and outputs |
| **Candidate** | A pre-ranked recommendation before final selection |
| **Pipeline** | The 7-step process: state → signals → candidates → rank → select → persist |
| **Detector** | Component that inspects user state to detect signals |
| **Generator** | Component that transforms signals into actionable candidates |
| **Ranker** | Component that deduplicates and sorts candidates by score |
| **Orchestrator** | Component that makes final selection (deterministic or LLM) |
| **UserStateSnapshot** | Read-only snapshot of all user data for pipeline processing |
| **Scoreboard** | A goal's set of Lag, Lead, and Constraint metrics |
| **Actuator** | A habit or task that drives a metric (the "action" in the control loop) |
