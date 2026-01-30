# Recommendations Architecture

This document describes the signal-driven, three-tier assessment pipeline that powers Mastery's recommendation engine. The system processes domain events through deterministic rules with optional LLM escalation, following the principle: **deterministic constraints first, LLM second**.

## Table of Contents

1. [System Overview](#system-overview)
2. [Data Flow](#data-flow)
3. [Domain Events & Signal Classification](#domain-events--signal-classification)
4. [Outbox Pattern & Message Flow](#outbox-pattern--message-flow)
5. [Signal Priority & Routing](#signal-priority--routing)
6. [Tiered Assessment Pipeline](#tiered-assessment-pipeline)
7. [Tier 0: Deterministic Rules](#tier-0-deterministic-rules)
8. [Tier 1: Quick Assessment](#tier-1-quick-assessment)
9. [Tier 2: LLM Pipeline](#tier-2-llm-pipeline)
10. [Recommendation Entity](#recommendation-entity)
11. [Audit & Observability](#audit--observability)
12. [Key File Locations](#key-file-locations)

---

## System Overview

The recommendations architecture implements a closed-loop control system where:

- **Domain events** from aggregate roots are classified into **signals**
- Signals are routed to **priority-based queues** via Azure Service Bus
- A **three-tier assessment pipeline** evaluates signals and generates recommendations
- Full **audit trails** enable explainability and debugging

### Design Principles

1. **Deterministic First**: All feasibility, severity, and candidate actions computed via code
2. **Explainability as First-Class**: Every recommendation includes a trace of inputs, rules triggered, and selection method
3. **Cost-Efficient**: LLM calls only when deterministic rules escalate
4. **Event-Sourced**: Everything the user does becomes a domain event; recommendations are projections

---

## Data Flow

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                              DOMAIN LAYER                                            │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│   Aggregate Roots (Goal, Habit, Task, Project, CheckIn, Experiment, etc.)           │
│         │                                                                            │
│         ▼                                                                            │
│   Domain Events (annotated with [SignalClassification] or [NoSignal])               │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
                                        │
                                        ▼
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                           PERSISTENCE LAYER                                          │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│   MasteryDbContext.SaveChangesAsync()                                               │
│         │                                                                            │
│         ├──► Dispatch domain events via MediatR (for event handlers)                │
│         │                                                                            │
│         └──► Create OutboxEntry records (for embedding + signal processing)         │
│                    │                                                                 │
│                    ▼                                                                 │
│              Publish EntityChangedBatchEvent to Service Bus                         │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
                                        │
                                        ▼
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                           EMBEDDING PIPELINE                                         │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│   EmbeddingConsumer (CAP subscriber: "embeddings-pending")                          │
│         │                                                                            │
│         ├──► Resolve entities from database                                         │
│         ├──► Generate embedding text via strategy pattern                           │
│         ├──► Generate embeddings via IEmbeddingService                              │
│         ├──► Store in vector store (IVectorStore)                                   │
│         │                                                                            │
│         └──► Classify signals via ISignalClassifier                                 │
│                    │                                                                 │
│                    ▼                                                                 │
│              Route signals to priority-based queues                                 │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
                                        │
            ┌───────────────────────────┼───────────────────────────┐
            │                           │                           │
            ▼                           ▼                           ▼
   signals-urgent              signals-window              signals-batch
   (P0 - Immediate)        (P1 - Window-Aligned)        (P2/P3 - Standard/Low)
            │                           │                           │
            ▼                           ▼                           ▼
   UrgentSignalConsumer      WindowSignalConsumer       BatchSignalConsumer
            │                           │                           │
            └───────────────────────────┼───────────────────────────┘
                                        │
                                        ▼
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                      TIERED ASSESSMENT PIPELINE                                      │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│   TieredAssessmentEngine.AssessAsync()                                              │
│         │                                                                            │
│         ├──► TIER 0: Deterministic Rules (IDeterministicRulesEngine)                │
│         │         │                                                                  │
│         │         ├── Evaluate all 16 rules in parallel                             │
│         │         ├── Generate direct recommendations                               │
│         │         └── Determine escalation to Tier 1                                │
│         │                                                                            │
│         ├──► TIER 1: Quick Assessment (IQuickAssessmentService) [if escalated]      │
│         │         │                                                                  │
│         │         ├── Calculate state delta since last assessment                   │
│         │         ├── Vector search for relevant context                            │
│         │         ├── Compute combined score (relevance + delta + urgency)          │
│         │         └── Determine escalation to Tier 2 (threshold: 0.5)               │
│         │                                                                            │
│         └──► TIER 2: LLM Pipeline (IRecommendationOrchestrator) [if escalated]      │
│                   │                                                                  │
│                   ├── Assemble full user state                                      │
│                   ├── Run LLM recommendation orchestration                          │
│                   └── Record baseline for future delta calculations                 │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
                                        │
                                        ▼
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                           RECOMMENDATION OUTPUT                                      │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                      │
│   Recommendation entities persisted with:                                           │
│         ├── Type, Context, Target, ActionKind                                       │
│         ├── Title, Rationale, Score                                                 │
│         ├── ActionPayload, ActionSummary                                            │
│         └── RecommendationTrace (full audit trail)                                  │
│                                                                                      │
│   SignalEntry records marked as processed with AssessmentTier                       │
│   SignalProcessingHistory recorded for observability                                │
│                                                                                      │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

---

## Domain Events & Signal Classification

Domain events are annotated with attributes that determine their signal classification:

### Attributes

| Attribute | Purpose |
|-----------|---------|
| `[SignalClassification(priority, windowType, rationale)]` | Classifies event for signal processing |
| `[NoSignal(reason)]` | Marks event as not requiring signal processing |

**File**: `src/api/Mastery.Domain/Common/SignalClassificationAttribute.cs`

### Signal Priority Enum

```csharp
public enum SignalPriority
{
    Urgent = 0,         // Process within 5 minutes (P0)
    WindowAligned = 1,  // Process at user's natural window (P1)
    Standard = 2,       // Process within 4-6 hours in batch (P2)
    Low = 3             // Process within 24 hours in background (P3)
}
```

### Processing Window Type Enum

```csharp
public enum ProcessingWindowType
{
    Immediate,      // Process immediately (urgent signals)
    MorningWindow,  // Typically 6-9 AM local time
    EveningWindow,  // Typically 8-10 PM local time
    WeeklyReview,   // Sunday evening
    BatchWindow     // Next batch run (standard/low priority)
}
```

### Complete Domain Events Reference

#### CheckIn Events

| Event | Priority | Window | Rationale |
|-------|----------|--------|-----------|
| `MorningCheckInSubmittedEvent` | WindowAligned | MorningWindow | User active - ideal for morning recommendations |
| `EveningCheckInSubmittedEvent` | WindowAligned | EveningWindow | User reflecting - ideal for evening coaching |
| `CheckInUpdatedEvent` | Low | BatchWindow | Metadata update - triggers re-indexing |
| `CheckInSkippedEvent` | WindowAligned | BatchWindow | May indicate disengagement |

#### Habit Events

| Event | Priority | Window | Rationale |
|-------|----------|--------|-----------|
| `HabitCreatedEvent` | Low | BatchWindow | Setup event - triggers metadata indexing |
| `HabitUpdatedEvent` | Low | BatchWindow | Metadata update - triggers re-indexing |
| `HabitStatusChangedEvent` | Low | BatchWindow | Lifecycle change - may affect planning |
| `HabitArchivedEvent` | Low | BatchWindow | Lifecycle change - removes from active planning |
| `HabitCompletedEvent` | Standard | BatchWindow | Behavioral signal - affects adherence and recommendations |
| `HabitUndoneEvent` | **NoSignal** | - | Internal correction - no signal needed |
| `HabitSkippedEvent` | Standard | BatchWindow | Behavioral signal - may indicate friction |
| `HabitMissedEvent` | Standard | BatchWindow | May trigger P0 via adherence detection |
| `HabitOccurrenceRescheduledEvent` | Standard | BatchWindow | Rescheduling indicates friction |
| `HabitStreakMilestoneEvent` | Standard | BatchWindow | Positive reinforcement opportunity |
| `HabitModeSuggestedEvent` | **NoSignal** | - | Internal suggestion event |

#### Task Events

| Event | Priority | Window | Rationale |
|-------|----------|--------|-----------|
| `TaskCreatedEvent` | Low | BatchWindow | Setup event - triggers metadata indexing |
| `TaskUpdatedEvent` | Low | BatchWindow | Metadata update - triggers re-indexing |
| `TaskStatusChangedEvent` | **NoSignal** | - | Internal state transition |
| `TaskScheduledEvent` | **NoSignal** | - | Internal scheduling event |
| `TaskRescheduledEvent` | Standard | BatchWindow | Behavioral signal - may trigger P0 via reschedule pattern |
| `TaskCompletedEvent` | Standard | BatchWindow | Behavioral signal - affects capacity and recommendations |
| `TaskCompletionUndoneEvent` | **NoSignal** | - | Internal correction - no signal needed |
| `TaskCancelledEvent` | **NoSignal** | - | Internal lifecycle event |
| `TaskArchivedEvent` | Low | BatchWindow | Lifecycle change - removes from active planning |
| `TaskDependencyAddedEvent` | **NoSignal** | - | Internal graph update |
| `TaskDependencyRemovedEvent` | **NoSignal** | - | Internal graph update |

#### Goal Events

| Event | Priority | Window | Rationale |
|-------|----------|--------|-----------|
| `GoalCreatedEvent` | Low | BatchWindow | Setup event - triggers metadata indexing |
| `GoalUpdatedEvent` | Low | BatchWindow | Metadata update - triggers re-indexing |
| `GoalStatusChangedEvent` | Standard | BatchWindow | Behavioral signal - affects goal progress tracking |
| `GoalCompletedEvent` | Standard | BatchWindow | Major achievement - triggers celebration and review |
| `GoalScoreboardUpdatedEvent` | **NoSignal** | - | Internal scoreboard update |

#### Project Events

| Event | Priority | Window | Rationale |
|-------|----------|--------|-----------|
| `ProjectCreatedEvent` | Low | BatchWindow | Setup event - triggers metadata indexing |
| `ProjectUpdatedEvent` | Low | BatchWindow | Metadata update - triggers re-indexing |
| `ProjectStatusChangedEvent` | Standard | BatchWindow | Behavioral signal - affects project progress tracking |
| `ProjectNextActionSetEvent` | **NoSignal** | - | Internal planning update |
| `ProjectCompletedEvent` | Standard | BatchWindow | Major achievement - triggers celebration and review |
| `MilestoneAddedEvent` | **NoSignal** | - | Internal planning update |
| `MilestoneCompletedEvent` | Standard | BatchWindow | Progress milestone - triggers celebration |

#### Experiment Events

| Event | Priority | Window | Rationale |
|-------|----------|--------|-----------|
| `ExperimentCreatedEvent` | Low | BatchWindow | Setup event - triggers metadata indexing |
| `ExperimentUpdatedEvent` | Low | BatchWindow | Metadata update - triggers re-indexing |
| `ExperimentStartedEvent` | Standard | BatchWindow | Behavioral signal - experiment tracking begins |
| `ExperimentPausedEvent` | **NoSignal** | - | Internal state transition |
| `ExperimentResumedEvent` | **NoSignal** | - | Internal state transition |
| `ExperimentCompletedEvent` | Standard | BatchWindow | Experiment outcome - learning engine input |
| `ExperimentAbandonedEvent` | Standard | BatchWindow | Experiment abandoned - may indicate friction |
| `ExperimentArchivedEvent` | **NoSignal** | - | Internal lifecycle event |
| `ExperimentNoteAddedEvent` | Low | BatchWindow | Note content may be useful for analysis |

#### Metric Events

| Event | Priority | Window | Rationale |
|-------|----------|--------|-----------|
| `MetricDefinitionCreatedEvent` | Low | BatchWindow | Setup event - triggers metadata indexing |
| `MetricDefinitionUpdatedEvent` | Low | BatchWindow | Metadata update - triggers re-indexing |
| `MetricDefinitionArchivedEvent` | Low | BatchWindow | Lifecycle change - removes from active tracking |
| `MetricObservationRecordedEvent` | Standard | BatchWindow | Behavioral signal - affects goal progress |
| `MetricObservationCorrectedEvent` | **NoSignal** | - | Internal correction - no signal needed |

#### UserProfile Events

| Event | Priority | Window | Rationale |
|-------|----------|--------|-----------|
| `UserProfileCreatedEvent` | Low | BatchWindow | Setup event - triggers metadata indexing |
| `UserProfileUpdatedEvent` | Low | BatchWindow | Profile update - may affect capacity and preferences |
| `PreferencesUpdatedEvent` | **NoSignal** | - | Internal preference update |
| `ConstraintsUpdatedEvent` | **NoSignal** | - | Internal constraint update |

#### Season Events

| Event | Priority | Window | Rationale |
|-------|----------|--------|-----------|
| `SeasonCreatedEvent` | Low | BatchWindow | Setup event - triggers metadata indexing |
| `SeasonActivatedEvent` | Standard | BatchWindow | Season change affects planning |
| `SeasonEndedEvent` | Low | BatchWindow | Lifecycle change - may trigger review |
| `SeasonClearedEvent` | **NoSignal** | - | Internal state transition |

#### Recommendation Events

| Event | Classification | Rationale |
|-------|----------------|-----------|
| `RecommendationsGeneratedEvent` | **NoSignal** | System output event - no signal needed |
| `RecommendationAcceptedEvent` | **NoSignal** | Feedback event - processed separately |
| `RecommendationDismissedEvent` | **NoSignal** | Feedback event - processed separately |
| `RecommendationSnoozedEvent` | **NoSignal** | Feedback event - processed separately |
| `DiagnosticSignalDetectedEvent` | **NoSignal** | Internal diagnostic event |

### Summary Statistics

- **Total Domain Events**: ~60
- **With SignalClassification**: ~39 events
- **With NoSignal**: ~21 events
- **Priority Distribution**: Urgent (0), WindowAligned (4), Standard (18), Low (17)

---

## Outbox Pattern & Message Flow

### OutboxEntry Entity

Entity changes are captured in the `OutboxEntry` table during `SaveChangesAsync`:

```csharp
public sealed class OutboxEntry
{
    public long Id { get; private set; }
    public string EntityType { get; private set; }       // "Goal", "Habit", etc.
    public Guid EntityId { get; private set; }
    public string Operation { get; private set; }        // "Created", "Updated", "Deleted"
    public string? UserId { get; private set; }
    public string DomainEventType { get; private set; }  // "HabitCompletedEvent", etc.
    public DateTime CreatedAt { get; private set; }
    public OutboxEntryStatus Status { get; private set; }
    // Lease-based processing fields...
}
```

**File**: `src/api/Mastery.Infrastructure/Outbox/OutboxEntry.cs`

### Message Flow

1. **SaveChangesAsync** captures domain events from tracked entities
2. One `OutboxEntry` created per domain event (not per entity change)
3. `EntityChangedBatchEvent` published to `embeddings-pending` queue
4. Both mechanisms ensure reliability:
   - Service Bus for fast processing when available
   - Outbox for SQL-based retry when Service Bus fails

### EntityChangedBatchEvent

```csharp
public sealed record EntityChangedBatchEvent
{
    public Guid BatchId { get; init; }
    public required IReadOnlyList<EntityChangedEvent> Events { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? CorrelationId { get; init; }
}
```

**File**: `src/api/Mastery.Infrastructure/Messaging/Events/EntityChangedBatchEvent.cs`

---

## Signal Priority & Routing

### Service Bus Queues

| Queue Name | Priority | Processing |
|------------|----------|------------|
| `embeddings-pending` | - | Entity embedding generation |
| `signals-urgent` | P0 (Urgent) | Immediate processing (<5 min) |
| `signals-window` | P1 (WindowAligned) | Scheduled for user's natural window |
| `signals-batch` | P2/P3 (Standard/Low) | Background batch processing |

**Configuration**: `src/api/Mastery.Infrastructure/Messaging/ServiceBusOptions.cs`

### Signal Routing Service

The `SignalRoutingService` routes classified signals to appropriate queues:

```csharp
private string GetQueueForPriority(SignalPriority priority) => priority switch
{
    SignalPriority.Urgent => _options.Value.UrgentQueueName,
    SignalPriority.WindowAligned => _options.Value.WindowQueueName,
    SignalPriority.Standard or SignalPriority.Low => _options.Value.BatchQueueName,
    _ => _options.Value.BatchQueueName
};
```

For **window-aligned signals**, the service calculates the user's next window start time and schedules the message for delayed delivery.

**File**: `src/api/Mastery.Infrastructure/Messaging/Services/SignalRoutingService.cs`

### Signal Consumers

Each queue has a dedicated CAP consumer:

| Consumer | Queue | Processing Window Type |
|----------|-------|------------------------|
| `UrgentSignalConsumer` | signals-urgent | Immediate |
| `WindowSignalConsumer` | signals-window | MorningWindow/EveningWindow (from signal) |
| `BatchSignalConsumer` | signals-batch | BatchWindow |

All consumers inherit from `BaseSignalConsumer` which implements the common processing pipeline.

**Files**:
- `src/api/Mastery.Infrastructure/Messaging/Consumers/UrgentSignalConsumer.cs`
- `src/api/Mastery.Infrastructure/Messaging/Consumers/WindowSignalConsumer.cs`
- `src/api/Mastery.Infrastructure/Messaging/Consumers/BatchSignalConsumer.cs`

### Escalation to Urgent

The `SignalClassifier` can escalate signals to urgent based on patterns:

```csharp
public bool ShouldEscalateToUrgent(IReadOnlyList<SignalClassification> pendingSignals, object? state)
{
    var missedHabits = pendingSignals.Count(s => s.EventType == nameof(HabitMissedEvent));
    var rescheduledTasks = pendingSignals.Count(s => s.EventType == nameof(TaskRescheduledEvent));
    var skippedCheckIns = pendingSignals.Count(s => s.EventType == nameof(CheckInSkippedEvent));

    if (missedHabits >= 3) return true;
    if (rescheduledTasks >= 3) return true;
    if (skippedCheckIns >= 2 && missedHabits >= 1) return true;

    return false;
}
```

**File**: `src/api/Mastery.Infrastructure/Services/SignalClassifier.cs`

---

## Tiered Assessment Pipeline

### Overview

The `TieredAssessmentEngine` orchestrates a three-tier pipeline:

```
Signals → Tier 0 (Always) → [Escalate?] → Tier 1 → [Escalate?] → Tier 2
              │                              │                      │
              ▼                              ▼                      ▼
    Direct Recommendations          State Delta Score        LLM Recommendations
```

### Assessment Tiers

```csharp
public enum AssessmentTier
{
    Tier0_Deterministic,      // Rules only, no LLM
    Tier1_QuickAssessment,    // Lightweight embeddings/delta
    Tier2_FullPipeline,       // Full 3-stage LLM pipeline
    Skipped
}
```

### TieredAssessmentOutcome

```csharp
public sealed record TieredAssessmentOutcome(
    string UserId,
    IReadOnlyList<SignalEntry> ProcessedSignals,
    RuleEvaluationResult Tier0Result,
    QuickAssessmentResult? Tier1Result,
    bool Tier2Executed,
    IReadOnlyList<Recommendation> GeneratedRecommendations,
    TieredAssessmentStatistics Statistics,
    DateTime StartedAt,
    DateTime CompletedAt);
```

**File**: `src/api/Mastery.Infrastructure/Services/TieredAssessmentEngine.cs`

---

## Tier 0: Deterministic Rules

Tier 0 evaluates all enabled deterministic rules in parallel and generates direct recommendations without LLM involvement.

### Interface

```csharp
public interface IDeterministicRulesEngine
{
    Task<RuleEvaluationResult> EvaluateAsync(
        UserStateSnapshot state,
        IReadOnlyList<SignalEntry> signals,
        CancellationToken cancellationToken = default);
}
```

### Rule Severity

```csharp
public enum RuleSeverity
{
    Low,
    Medium,
    High,
    Critical
}
```

### Rule Result

```csharp
public sealed record RuleResult(
    string RuleId,
    string RuleName,
    bool Triggered,
    RuleSeverity Severity,
    IReadOnlyDictionary<string, object> Evidence,
    DirectRecommendationCandidate? DirectRecommendation = null,
    bool RequiresEscalation = false);
```

### Implemented Rules (16 Total)

#### Check-In Rules

| Rule | Description | Severity Factors |
|------|-------------|------------------|
| `CheckInMissingRule` | Detects missing morning/evening check-ins | Streak length (30+ = Critical) |
| `CheckInNoTop1SelectedRule` | Ensures user selects a priority | - |

#### Task Rules

| Rule | Description | Severity Factors |
|------|-------------|------------------|
| `TaskCapacityOverloadRule` | Planned work > capacity × 1.2 | Overload %: Critical (50%+), High (35-50%), Medium (20-35%) |
| `TaskOverdueRule` | Detects overdue tasks | Reschedule count, overdue duration |
| `TaskEnergyMismatchRule` | Task energy > current energy state | - |
| `RecurringTaskStalenessRule` | Recurring tasks not updated recently | - |

#### Habit Rules

| Rule | Description | Severity Factors |
|------|-------------|------------------|
| `HabitAdherenceThresholdRule` | 7-day adherence < 50% | Adherence + streak + mode + goal linkage |
| `HabitStreakBreakDetectionRule` | Early warning before streak breaks | - |

#### Goal Rules

| Rule | Description | Severity Factors |
|------|-------------|------------------|
| `GoalProgressAtRiskRule` | Goal at risk of missing deadline | Rate ratio: Critical (<25%), High (<50%), Medium (<75%) |
| `DeadlineProximityRule` | Imminent deadlines (24-48h window) | Days remaining, progress % |
| `GoalScoreboardIncompleteRule` | Goal metrics not tracked | - |

#### Project Rules

| Rule | Description | Severity Factors |
|------|-------------|------------------|
| `ProjectStuckRule` | Active project with no progress | - |

#### Experiment Rules

| Rule | Description | Severity Factors |
|------|-------------|------------------|
| `ExperimentStaleRule` | Experiment not updated recently | - |

#### Metric Rules

| Rule | Description | Severity Factors |
|------|-------------|------------------|
| `MetricObservationOverdueRule` | Metrics missing recent observations | - |

### Escalation to Tier 1

Tier 0 recommends escalation when:

- 2+ high/critical severity rules triggered
- Rules explicitly request escalation (`RequiresEscalation = true`)
- Conflicting recommendations detected
- 4+ rules triggered (complex situation)
- Any urgent signal present

**Files**:
- `src/api/Mastery.Application/Common/Interfaces/IDeterministicRulesEngine.cs`
- `src/api/Mastery.Infrastructure/Services/Rules/DeterministicRulesEngine.cs`
- `src/api/Mastery.Infrastructure/Services/Rules/DeterministicRuleBase.cs`
- `src/api/Mastery.Infrastructure/Services/Rules/*.cs` (individual rules)

---

## Tier 1: Quick Assessment

Tier 1 determines whether to escalate to the full LLM pipeline by computing a combined score from state delta, vector search relevance, and signal urgency.

### Interface

```csharp
public interface IQuickAssessmentService
{
    Task<QuickAssessmentResult> AssessAsync(
        UserStateSnapshot state,
        IReadOnlyList<SignalEntry> signals,
        RuleEvaluationResult tier0Result,
        CancellationToken cancellationToken = default);
}
```

### Score Calculation

```
Combined Score = (Relevance × 0.3) + (Delta × 0.4) + (Urgency × 0.3)
```

| Component | Weight | Source |
|-----------|--------|--------|
| Relevance | 0.3 | Vector search similarity scores |
| Delta | 0.4 | State changes since last assessment |
| Urgency | 0.3 | Signal priorities + Tier 0 severity |

### Escalation Threshold: 0.5

Tier 1 recommends escalation when:

- Combined score ≥ 0.5
- Tier 0 already requested escalation
- Critical severity detected
- High urgency (>0.7) + moderate delta (>0.3)
- ≥ 3 missed items detected

### State Delta Calculator

Tracks changes since last assessment with weighted scoring:

| Change Type | Weight |
|-------------|--------|
| New Entity | 0.15 |
| Modified Entity | 0.10 |
| Completed Item | 0.05 |
| Missed Item | 0.20 |
| New Signal | 0.08 |

**Files**:
- `src/api/Mastery.Application/Common/Interfaces/IQuickAssessmentService.cs`
- `src/api/Mastery.Infrastructure/Services/QuickAssessmentService.cs`
- `src/api/Mastery.Application/Common/Interfaces/IStateDeltaCalculator.cs`
- `src/api/Mastery.Infrastructure/Services/StateDeltaCalculator.cs`

---

## Tier 2: LLM Pipeline

Tier 2 runs the full LLM recommendation orchestration when escalated from Tier 1.

### Interface

```csharp
public interface IRecommendationOrchestrator
{
    Task<RecommendationOrchestrationResult> OrchestrateAsync(
        UserStateSnapshot state,
        RecommendationContext context,
        CancellationToken cancellationToken = default);
}
```

### Recommendation Context

```csharp
public enum RecommendationContext
{
    MorningCheckIn,
    EveningCheckIn,
    WeeklyReview,
    DriftAlert,
    ProactiveCheck
}
```

Context is determined from signal types:

- Check-in signals → `MorningCheckIn` or `EveningCheckIn`
- Weekly window signals → `WeeklyReview`
- Urgent signals → `DriftAlert`
- Other → `ProactiveCheck`

### User State Assembly

The `UserStateAssembler` creates a comprehensive snapshot:

```csharp
public sealed record UserStateSnapshot(
    string UserId,
    UserProfileSnapshot? Profile,
    IReadOnlyList<GoalSnapshot> Goals,
    IReadOnlyList<HabitSnapshot> Habits,
    IReadOnlyList<TaskSnapshot> Tasks,
    IReadOnlyList<ProjectSnapshot> Projects,
    IReadOnlyList<ExperimentSnapshot> Experiments,
    IReadOnlyList<CheckInSnapshot> RecentCheckIns,
    IReadOnlyList<MetricDefinitionSnapshot> MetricDefinitions,
    int CheckInStreak,
    DateOnly Today);
```

### Baseline Recording

After Tier 2 execution, a baseline is recorded for future delta calculations:

```csharp
await _deltaCalculator.RecordBaselineAsync(state.UserId, state, ct);
```

**Files**:
- `src/api/Mastery.Application/Common/Interfaces/IRecommendationOrchestrator.cs`
- `src/api/Mastery.Application/Common/Interfaces/IUserStateAssembler.cs`
- `src/api/Mastery.Application/Features/Recommendations/Services/UserStateAssembler.cs`
- `src/api/Mastery.Application/Common/Models/UserStateSnapshot.cs`

---

## Recommendation Entity

### Recommendation

```csharp
public sealed class Recommendation : OwnedEntity, IAggregateRoot
{
    public RecommendationType Type { get; }
    public RecommendationStatus Status { get; }
    public RecommendationContext Context { get; }
    public RecommendationTarget Target { get; }
    public RecommendationActionKind ActionKind { get; }
    public string Title { get; }
    public string Rationale { get; }
    public string? ActionPayload { get; }
    public string? ActionSummary { get; }
    public decimal Score { get; }
    public DateTime? ExpiresAt { get; }
    public IReadOnlyList<Guid> SignalIds { get; }
    public RecommendationTrace? Trace { get; }
}
```

### Recommendation Types

```csharp
public enum RecommendationType
{
    // Action-based
    NextBestAction,
    Top1Suggestion,
    HabitModeSuggestion,
    PlanRealismAdjustment,
    TaskBreakdownSuggestion,
    ScheduleAdjustmentSuggestion,
    ProjectStuckFix,
    ExperimentRecommendation,
    GoalScoreboardSuggestion,
    HabitFromLeadMetricSuggestion,
    CheckInConsistencyNudge,
    MetricObservationReminder,

    // Task suggestions
    TaskEditSuggestion,
    TaskArchiveSuggestion,
    TaskTriageSuggestion,

    // Habit suggestions
    HabitEditSuggestion,
    HabitArchiveSuggestion,

    // Goal suggestions
    GoalEditSuggestion,
    GoalArchiveSuggestion,

    // Project suggestions
    ProjectSuggestion,
    ProjectEditSuggestion,
    ProjectArchiveSuggestion,

    // Other
    MetricEditSuggestion,
    ExperimentEditSuggestion,
    ExperimentArchiveSuggestion
}
```

### Recommendation Status Lifecycle

```
Pending → Accepted → Executed
       → Dismissed
       → Snoozed → Accepted/Dismissed
       → Expired
```

**File**: `src/api/Mastery.Domain/Entities/Recommendation/Recommendation.cs`

---

## Audit & Observability

### RecommendationTrace

Every recommendation includes a full audit trail:

```csharp
public sealed class RecommendationTrace : AuditableEntity
{
    public Guid RecommendationId { get; }
    public string StateSnapshotJson { get; }      // Full user state at generation time
    public string SignalsSummaryJson { get; }     // Signals that triggered generation
    public string CandidateListJson { get; }      // All candidates considered
    public string? PromptVersion { get; }         // LLM prompt version (if Tier 2)
    public string? ModelVersion { get; }          // LLM model version (if Tier 2)
    public string? RawLlmResponse { get; }        // Raw LLM output (if Tier 2)
    public string SelectionMethod { get; }        // "Tier0_Direct", "Tier2_LLM", etc.
}
```

**File**: `src/api/Mastery.Domain/Entities/Recommendation/RecommendationTrace.cs`

### SignalEntry

Signals are persisted for audit with processing metadata:

```csharp
public sealed class SignalEntry
{
    public string UserId { get; }
    public string EventType { get; }
    public SignalPriority Priority { get; }
    public ProcessingWindowType WindowType { get; }
    public SignalStatus Status { get; }           // Pending, Processing, Processed, Skipped, Failed, Expired
    public AssessmentTier? ProcessingTier { get; }
    public string? SkipReason { get; }
    public DateTime CreatedAt { get; }
    public DateTime? ProcessedAt { get; }
}
```

**File**: `src/api/Mastery.Domain/Entities/Signal/SignalEntry.cs`

### SignalProcessingHistory

Each processing cycle is recorded:

```csharp
public sealed class SignalProcessingHistory
{
    public string UserId { get; }
    public ProcessingWindowType WindowType { get; }
    public int SignalsReceived { get; }
    public int SignalsProcessed { get; }
    public int SignalsSkipped { get; }
    public int Tier0RulesTriggered { get; }
    public decimal? Tier1CombinedScore { get; }
    public string? Tier1DeltaSummaryJson { get; }
    public bool Tier2Executed { get; }
    public int RecommendationsGenerated { get; }
    public string? RecommendationIdsJson { get; }
    public string? ErrorMessage { get; }
    public DateTime StartedAt { get; }
    public DateTime? CompletedAt { get; }
}
```

**File**: `src/api/Mastery.Domain/Entities/Signal/SignalProcessingHistory.cs`

---

## Key File Locations

### Domain Layer

| Component | Path |
|-----------|------|
| Signal Classification Attributes | `src/api/Mastery.Domain/Common/SignalClassificationAttribute.cs` |
| Signal Enums | `src/api/Mastery.Domain/Enums/SignalPriority.cs`, `ProcessingWindowType.cs`, `AssessmentTier.cs` |
| Domain Events | `src/api/Mastery.Domain/Entities/*/Events.cs` |
| SignalEntry | `src/api/Mastery.Domain/Entities/Signal/SignalEntry.cs` |
| SignalProcessingHistory | `src/api/Mastery.Domain/Entities/Signal/SignalProcessingHistory.cs` |
| Recommendation | `src/api/Mastery.Domain/Entities/Recommendation/Recommendation.cs` |
| RecommendationTrace | `src/api/Mastery.Domain/Entities/Recommendation/RecommendationTrace.cs` |
| RecommendationType | `src/api/Mastery.Domain/Enums/RecommendationType.cs` |

### Application Layer

| Component | Path |
|-----------|------|
| IDeterministicRulesEngine | `src/api/Mastery.Application/Common/Interfaces/IDeterministicRulesEngine.cs` |
| ITieredAssessmentEngine | `src/api/Mastery.Application/Common/Interfaces/ITieredAssessmentEngine.cs` |
| IQuickAssessmentService | `src/api/Mastery.Application/Common/Interfaces/IQuickAssessmentService.cs` |
| IRecommendationOrchestrator | `src/api/Mastery.Application/Common/Interfaces/IRecommendationOrchestrator.cs` |
| IStateDeltaCalculator | `src/api/Mastery.Application/Common/Interfaces/IStateDeltaCalculator.cs` |
| IUserStateAssembler | `src/api/Mastery.Application/Common/Interfaces/IUserStateAssembler.cs` |
| UserStateSnapshot | `src/api/Mastery.Application/Common/Models/UserStateSnapshot.cs` |
| RuleResult | `src/api/Mastery.Application/Common/Models/RuleResult.cs` |
| QuickAssessmentResult | `src/api/Mastery.Application/Common/Models/QuickAssessmentResult.cs` |
| UserStateAssembler | `src/api/Mastery.Application/Features/Recommendations/Services/UserStateAssembler.cs` |

### Infrastructure Layer

| Component | Path |
|-----------|------|
| MasteryDbContext | `src/api/Mastery.Infrastructure/Data/MasteryDbContext.cs` |
| OutboxEntry | `src/api/Mastery.Infrastructure/Outbox/OutboxEntry.cs` |
| ServiceBusOptions | `src/api/Mastery.Infrastructure/Messaging/ServiceBusOptions.cs` |
| SignalClassifier | `src/api/Mastery.Infrastructure/Services/SignalClassifier.cs` |
| SignalRoutingService | `src/api/Mastery.Infrastructure/Messaging/Services/SignalRoutingService.cs` |
| EmbeddingConsumer | `src/api/Mastery.Infrastructure/Messaging/Consumers/EmbeddingConsumer.cs` |
| BaseSignalConsumer | `src/api/Mastery.Infrastructure/Messaging/Consumers/BaseSignalConsumer.cs` |
| UrgentSignalConsumer | `src/api/Mastery.Infrastructure/Messaging/Consumers/UrgentSignalConsumer.cs` |
| WindowSignalConsumer | `src/api/Mastery.Infrastructure/Messaging/Consumers/WindowSignalConsumer.cs` |
| BatchSignalConsumer | `src/api/Mastery.Infrastructure/Messaging/Consumers/BatchSignalConsumer.cs` |
| TieredAssessmentEngine | `src/api/Mastery.Infrastructure/Services/TieredAssessmentEngine.cs` |
| DeterministicRulesEngine | `src/api/Mastery.Infrastructure/Services/Rules/DeterministicRulesEngine.cs` |
| DeterministicRuleBase | `src/api/Mastery.Infrastructure/Services/Rules/DeterministicRuleBase.cs` |
| Individual Rules | `src/api/Mastery.Infrastructure/Services/Rules/*.cs` |
| QuickAssessmentService | `src/api/Mastery.Infrastructure/Services/QuickAssessmentService.cs` |
| StateDeltaCalculator | `src/api/Mastery.Infrastructure/Services/StateDeltaCalculator.cs` |
| Message Events | `src/api/Mastery.Infrastructure/Messaging/Events/*.cs` |
