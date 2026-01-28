# Projects & Tasks Feature Documentation

## Overview

**Projects & Tasks** are the primary "actuators" in the Mastery control loop—they convert user intentions into discrete execution steps. While Goals define *what* to achieve and Habits define *recurring behaviors*, Tasks represent *one-time actions* and Projects group related tasks into cohesive execution containers.

**Design Philosophy**: Tasks are not just todo items—they're capacity-aware work units with energy cost, context requirements, and optional metric contributions. Projects provide structure without over-planning, using the "Stuck" indicator to surface when action is needed.

---

## Business Context

### Why Projects & Tasks Exist

Most productivity apps fail because they:
1. Don't account for **energy** (not all tasks are equal)
2. Ignore **context** (some tasks require specific environments)
3. Lack **friction tracking** (why do tasks keep getting rescheduled?)
4. Don't connect to **outcomes** (tasks feel disconnected from goals)

Mastery's Projects & Tasks feature addresses this by:
1. **Energy-aware planning** - Each task has an energy cost (1-5) for capacity matching
2. **Context tagging** - Filter tasks by where/when they can be done
3. **Reschedule tracking** - Capture *why* tasks slip for diagnostic insights
4. **Goal & Metric integration** - Tasks can contribute to metrics, connecting action to outcomes

### The Control System Analogy

In control theory terms:

| Control Concept | Projects & Tasks Component |
|-----------------|---------------------------|
| **Actuators** | Tasks (the actions that change reality) |
| **Execution Container** | Projects (group related actuators) |
| **Capacity Signal** | Energy cost + estimated minutes |
| **Context Constraints** | Context tags (Computer, Home, etc.) |
| **Feedback Signal** | Reschedule reasons, completion data |
| **Stuck Detection** | Project with no actionable tasks |

### The Inbox → Ready → Done Workflow

Tasks follow a deliberate triage workflow:

```
Capture → Triage → Execute → Complete
   │         │         │         │
 Inbox    Ready    Scheduled   Done
           ↓
      (or Schedule)
```

**Inbox**: Quick capture without friction. Title only, defaults applied.
**Ready**: Triaged and actionable. Has all needed information.
**Scheduled**: Committed to a specific date.
**Completed**: Done, with optional actual time and notes.

---

## Domain Model

### Entity Hierarchy

```
Task (Aggregate Root)
├── Id (Guid)
├── UserId (string) ────────────── Owner
├── Title (string, max 200) ─────── What needs to be done
├── Description? (string) ──────── Additional context
├── Status (enum) ──────────────── Lifecycle state
├── Priority (1-5) ─────────────── Urgency ranking (1=highest)
├── EstimatedMinutes (int) ──────── Time estimate (default: 30)
├── EnergyCost (1-5) ───────────── Mental/physical effort (3=medium)
├── DisplayOrder (int) ──────────── Sort order within lists
│
├── Due? (Value Object) ─────────── Optional deadline
│   ├── DueOn (DateOnly) ─────────── Target date
│   ├── DueAt? (TimeOnly) ────────── Optional specific time
│   └── DueType (Soft|Hard) ──────── Guidance vs. commitment
│
├── Scheduling? (Value Object) ──── When to work on it
│   ├── ScheduledOn (DateOnly) ──── Planned date
│   └── PreferredTimeWindow? ────── Optional time range
│
├── Completion? (Value Object) ──── Completion data
│   ├── CompletedAtUtc (DateTime)
│   ├── CompletedOn (DateOnly) ──── In user timezone
│   ├── ActualMinutes? (int)
│   ├── CompletionNote? (string)
│   └── EnteredValue? (decimal) ─── For metric bindings
│
├── ContextTags[] (JSON) ────────── Where/when executable
├── DependencyTaskIds[] (JSON) ──── Blocked-by tasks
├── RoleIds[] (JSON) ────────────── Associated roles
├── ValueIds[] (JSON) ───────────── Aligned values
│
├── ProjectId? (FK) ─────────────── Parent project
├── GoalId? (FK) ────────────────── Contributing goal
│
├── MetricBindings[] (Child) ────── Metrics to update on complete
│   └── TaskMetricBinding
│       ├── Id, TaskId
│       ├── MetricDefinitionId (FK)
│       ├── ContributionType (enum)
│       ├── FixedValue? (decimal)
│       └── Notes?
│
├── LastRescheduleReason? (enum) ── Why it was rescheduled
├── RescheduleCount (int) ───────── Friction tracking
│
└── Computed Properties
    ├── IsBlocked ────────────────── Has unresolved dependencies
    ├── IsOverdue ────────────────── Past due date
    └── IsEligibleForNBA ─────────── Ready for "Next Best Action"


Project (Aggregate Root)
├── Id (Guid)
├── UserId (string) ────────────── Owner
├── Title (string, max 200) ─────── Project name
├── Description? (string) ──────── Project scope/context
├── Status (enum) ──────────────── Lifecycle state
├── Priority (1-5) ─────────────── Importance ranking
│
├── GoalId? (FK) ───────────────── Contributing goal
├── SeasonId? (FK) ─────────────── Associated season
├── RoleIds[] (JSON) ───────────── Associated roles
├── ValueIds[] (JSON) ──────────── Aligned values
│
├── TargetEndDate? (DateOnly) ──── Optional deadline
├── NextTaskId? (FK) ───────────── Highlighted next action
│
├── Milestones[] (Child) ────────── Progress markers
│   └── Milestone
│       ├── Id, ProjectId
│       ├── Title (string)
│       ├── TargetDate? (DateOnly)
│       ├── Status (enum)
│       ├── Notes?
│       ├── DisplayOrder (int)
│       └── CompletedAtUtc?
│
├── OutcomeNotes? (string) ──────── Retrospective notes
├── CompletedAtUtc? (DateTime)
│
└── Computed Properties
    ├── IsStuck ──────────────────── Needs attention (see rules)
    ├── HasMilestones
    └── CompletedMilestonesCount
```

---

## Enums Reference

### TaskStatus

Controls task lifecycle and what operations are permitted:

| Status | Description | Allowed Transitions |
|--------|-------------|---------------------|
| `Inbox` | Captured, not triaged | → Ready, → Scheduled, → Archived |
| `Ready` | Triaged, actionable | → Scheduled, → InProgress, → Completed, → Cancelled, → Archived |
| `Scheduled` | Assigned to a date | → Ready, → InProgress, → Completed, → Cancelled, → Archived |
| `InProgress` | Currently working | → Completed, → Cancelled |
| `Completed` | Done | → Ready (undo) |
| `Cancelled` | Not doing | → Archived |
| `Archived` | Soft-deleted | (terminal state) |

**Business Rules**:
- New tasks start in `Inbox` by default, or `Ready` if `startAsReady` is true
- Tasks with scheduling automatically go to `Scheduled` status
- Completing a task from any active status goes to `Completed`
- Undo completion returns task to `Ready` status
- Cannot modify an `Archived` task

### ProjectStatus

| Status | Description | Allowed Transitions |
|--------|-------------|---------------------|
| `Draft` | Being planned | → Active, → Archived |
| `Active` | Actively working | → Paused, → Completed, → Archived |
| `Paused` | Temporarily on hold | → Active, → Completed, → Archived |
| `Completed` | Project finished | → Archived |
| `Archived` | Soft-deleted | (terminal state) |

**Business Rules**:
- New projects default to `Active` status (not `Draft`)
- Save as Draft option available for planning-phase projects
- Cannot modify `Completed` or `Archived` projects
- Completing a project clears `NextTaskId`

### MilestoneStatus

| Status | Description |
|--------|-------------|
| `NotStarted` | Initial state |
| `InProgress` | Work has begun |
| `Completed` | Milestone achieved |

### DueType

Distinguishes guidance dates from hard commitments:

| Type | Description | System Behavior |
|------|-------------|-----------------|
| `Soft` | Guidance date | Yellow warning when approaching |
| `Hard` | Firm commitment | Red alert, affects overdue calculation |

### ContextTag

Where or when a task can be executed:

| Tag | Description | Use Case |
|-----|-------------|----------|
| `Computer` | Requires computer | Coding, email, documents |
| `Phone` | Can do on phone | Quick messages, calls |
| `Errands` | Outside the house | Shopping, appointments |
| `Home` | At home | Chores, personal tasks |
| `Office` | At workplace | In-person meetings |
| `DeepWork` | Needs focus time | Complex thinking tasks |
| `LowEnergy` | Can do when tired | Administrative, routine |
| `Anywhere` | No location requirement | Reading, thinking |

### RescheduleReason

Captures *why* tasks slip (diagnostic signal):

| Reason | Description |
|--------|-------------|
| `NoTime` | Day was too full |
| `TooTired` | Insufficient energy |
| `Blocked` | Waiting on something |
| `Forgot` | Slipped through cracks |
| `ScopeTooBig` | Task needs breakdown |
| `WaitingOnSomeone` | External dependency |
| `Other` | Custom reason |

### TaskContributionType

How task completion contributes to metrics:

| Type | Behavior |
|------|----------|
| `BooleanAs1` | Each completion adds 1 |
| `FixedValue` | Adds a predetermined amount |
| `UseActualMinutes` | Uses actual time spent |
| `UseEnteredValue` | User enters value at completion |

---

## Business Rules

### Task Lifecycle Rules

**Creation**:
- Title is required, max 200 characters
- Default values: EstimatedMinutes=30, EnergyCost=3, Priority=3
- Status is `Inbox` unless `startAsReady=true` or scheduling is provided
- If scheduling is provided, status is `Scheduled`

**Inbox → Ready Transition**:
- Manual: User clicks "Move to Ready" or uses edit form
- Implicit: User schedules the task (→ Scheduled)
- Edit Form: "Save & Move to Ready" button in edit mode

**Completion**:
- Requires `CompletedOn` date (typically today)
- Optional: `ActualMinutes`, `CompletionNote`, `EnteredValue`
- If task has metric bindings, observations are created
- Completion can be undone (returns to Ready)

**Rescheduling**:
- Requires new date and optional reason
- Increments `RescheduleCount` for friction tracking
- Stores `LastRescheduleReason` for diagnostics

**Dependencies**:
- Task is `IsBlocked` if any `DependencyTaskIds` are not Completed
- Blocked tasks can still be edited but shouldn't be worked on

### Project Lifecycle Rules

**Creation**:
- Title is required, max 200 characters
- Default status is `Active` (not `Draft`)
- Use "Save as Draft" for planning-phase projects

**Stuck Indicator**:
A project shows as "Stuck" when ALL of these conditions are true:
1. Status is `Active`
2. Has NO actionable tasks (none in Ready, InProgress, or Scheduled status)
3. Is NOT ready to complete

A project is "ready to complete" when:
- ALL tasks are completed (totalTasks > 0 AND incompleteTasks == 0)
- AND ALL milestones are completed (or no milestones exist)

**Examples**:
| Scenario | IsStuck |
|----------|---------|
| No tasks, no milestones | Yes |
| All tasks completed, milestones incomplete | Yes |
| Tasks only in Inbox | Yes |
| Has Ready tasks | No |
| Has Scheduled tasks | No |
| All tasks + milestones completed | No (ready to complete) |

**Completion**:
- Records `OutcomeNotes` for retrospective
- Sets `CompletedAtUtc` timestamp
- Clears `NextTaskId`

### Milestone Rules

- Milestones are owned by their parent project
- Can only add/modify milestones on non-completed, non-archived projects
- Completing a milestone sets `CompletedAtUtc`
- Milestones affect the "ready to complete" calculation

### Task-Project Association

- Tasks can optionally belong to a project via `ProjectId`
- Creating a task from project detail auto-sets `ProjectId`
- Task and Project queries include task counts by status
- Project detail page shows all associated tasks with completion buttons

### Task-Goal Association

- Tasks can optionally link to a goal via `GoalId`
- Used for alignment tracking and filtering
- Goal title is denormalized in task DTOs for display

### Metric Bindings

When a task with metric bindings is completed:
1. For each binding, create a `MetricObservation`
2. Source is `Task`, CorrelationId is the task ID
3. Value depends on `ContributionType`:
   - `BooleanAs1`: value = 1
   - `FixedValue`: value = binding.FixedValue
   - `UseActualMinutes`: value = completion.ActualMinutes
   - `UseEnteredValue`: value = completion.EnteredValue

---

## Value Objects

### TaskDue

Encapsulates due date configuration:

```csharp
TaskDue.Create(dueOn: DateOnly, dueAt?: TimeOnly, dueType: DueType)

// Examples
TaskDue.Create(new DateOnly(2026, 2, 1), null, DueType.Soft)
TaskDue.Create(new DateOnly(2026, 2, 1), new TimeOnly(17, 0), DueType.Hard)
```

**Validation**:
- DueOn is required
- DueAt is optional (for time-sensitive tasks)
- DueType defaults to Soft

### TaskScheduling

Encapsulates scheduling configuration:

```csharp
TaskScheduling.Create(scheduledOn: DateOnly, preferredTimeWindow?: TimeWindow)

// Examples
TaskScheduling.Create(new DateOnly(2026, 1, 27))
TaskScheduling.Create(new DateOnly(2026, 1, 27), TimeWindow.Create(new TimeOnly(9, 0), new TimeOnly(12, 0)))
```

### TaskCompletion

Encapsulates completion data:

```csharp
TaskCompletion.Create(
    completedAtUtc: DateTime,
    completedOn: DateOnly,
    actualMinutes?: int,
    completionNote?: string,
    enteredValue?: decimal
)
```

---

## Domain Events

### Task Events

| Event | Trigger | Typical Handler Action |
|-------|---------|------------------------|
| `TaskCreatedEvent` | New task created | Update project task counts |
| `TaskUpdatedEvent` | Task details changed | Audit log |
| `TaskStatusChangedEvent` | Status transition | Update project stuck indicator |
| `TaskScheduledEvent` | Task scheduled | Calendar integration |
| `TaskRescheduledEvent` | Task rescheduled | Friction analytics, reason tracking |
| `TaskCompletedEvent` | Task completed | Create metric observations |
| `TaskCompletionUndoneEvent` | Undo completion | Remove metric observations |
| `TaskCancelledEvent` | Task cancelled | Diagnostic signal |
| `TaskArchivedEvent` | Task archived | Update counts |

### Project Events

| Event | Trigger | Typical Handler Action |
|-------|---------|------------------------|
| `ProjectCreatedEvent` | New project | Dashboard update |
| `ProjectUpdatedEvent` | Details changed | Audit log |
| `ProjectStatusChangedEvent` | Status transition | Update goal progress |
| `ProjectNextActionSetEvent` | Next action changed | Highlight in UI |
| `ProjectCompletedEvent` | Project completed | Celebration notification |
| `MilestoneAddedEvent` | Milestone created | Update project progress |
| `MilestoneCompletedEvent` | Milestone done | Progress notification |

---

## API Endpoints

### Task Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/tasks` | List tasks (filterable) |
| `GET` | `/api/tasks/today` | Today's tasks (optimized) |
| `GET` | `/api/tasks/inbox` | Inbox tasks only |
| `GET` | `/api/tasks/{id}` | Single task with details |
| `POST` | `/api/tasks` | Create task |
| `PUT` | `/api/tasks/{id}` | Update task |
| `POST` | `/api/tasks/{id}/schedule` | Schedule task |
| `POST` | `/api/tasks/{id}/reschedule` | Reschedule with reason |
| `POST` | `/api/tasks/{id}/complete` | Complete task |
| `POST` | `/api/tasks/{id}/undo` | Undo completion |
| `POST` | `/api/tasks/{id}/cancel` | Cancel task |
| `POST` | `/api/tasks/{id}/ready` | Move to Ready |
| `DELETE` | `/api/tasks/{id}` | Archive task |

### Project Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/projects` | List projects (filterable) |
| `GET` | `/api/projects/{id}` | Single project with milestones |
| `POST` | `/api/projects` | Create project |
| `PUT` | `/api/projects/{id}` | Update project |
| `PUT` | `/api/projects/{id}/status` | Change status |
| `POST` | `/api/projects/{id}/complete` | Complete project |
| `DELETE` | `/api/projects/{id}` | Archive project |
| `POST` | `/api/projects/{id}/milestones` | Add milestone |
| `PUT` | `/api/projects/{id}/milestones/{mid}` | Update milestone |
| `POST` | `/api/projects/{id}/milestones/{mid}/complete` | Complete milestone |
| `DELETE` | `/api/projects/{id}/milestones/{mid}` | Remove milestone |

### Query Parameters

**GET /api/tasks**:
- `status` - Filter by TaskStatus
- `projectId` - Filter by project
- `goalId` - Filter by goal
- `contextTag` - Filter by context
- `isOverdue` - Filter overdue tasks

**GET /api/projects**:
- `status` - Filter by ProjectStatus
- `goalId` - Filter by goal

### Request Examples

**Create Task (POST /api/tasks)**
```json
{
  "title": "Review Q1 budget proposal",
  "description": "Check numbers and provide feedback",
  "estimatedMinutes": 45,
  "energyCost": 4,
  "priority": 2,
  "projectId": "...",
  "goalId": "...",
  "due": {
    "dueOn": "2026-01-30",
    "dueType": "Soft"
  },
  "contextTags": ["Computer", "DeepWork"],
  "startAsReady": true
}
```

**Complete Task (POST /api/tasks/{id}/complete)**
```json
{
  "completedOn": "2026-01-27",
  "actualMinutes": 35,
  "note": "Sent feedback via email"
}
```

**Create Project (POST /api/projects)**
```json
{
  "title": "Website Redesign",
  "description": "Complete overhaul of company website",
  "priority": 1,
  "goalId": "...",
  "targetEndDate": "2026-03-31",
  "saveAsDraft": false
}
```

---

## UI Implementation

### Pages & Routes

| Route | Component | Purpose |
|-------|-----------|---------|
| `/tasks` | `TasksPage` | Main tasks hub with status tabs |
| `/tasks/new` | `CreateTaskPage` | Task creation form |
| `/tasks/:id` | `TaskDetailPage` | Full task details |
| `/tasks/:id/edit` | `EditTaskPage` | Task edit form |
| `/projects` | `ProjectsPage` | Project list with status tabs |
| `/projects/new` | `CreateProjectPage` | Project creation form |
| `/projects/:id` | `ProjectDetailPage` | Project with milestones & tasks |
| `/projects/:id/edit` | `EditProjectPage` | Project edit form |

### Component Structure

```
features/tasks/
├── api/tasks-api.ts          # API client
├── hooks/use-tasks.ts        # React Query hooks
├── schemas/task-schema.ts    # Zod validation
├── components/
│   ├── task-form/
│   │   └── task-form.tsx     # Create/edit form
│   ├── task-list/
│   │   ├── task-card.tsx     # Summary card
│   │   └── tasks-list.tsx    # List with filters
│   └── task-detail/
│       └── (detail components)
└── pages/
    ├── tasks-page.tsx
    ├── create-task-page.tsx
    ├── task-detail-page.tsx
    └── edit-task-page.tsx

features/projects/
├── api/projects-api.ts
├── hooks/use-projects.ts
├── schemas/project-schema.ts
├── components/
│   ├── project-form/
│   ├── project-list/
│   └── project-detail/
└── pages/
    ├── projects-page.tsx
    ├── create-project-page.tsx
    ├── project-detail-page.tsx
    └── edit-project-page.tsx
```

### Key UI Patterns

**1. Task Form Save Options**

Create mode shows two buttons:
- "Save to Inbox" - Creates task in Inbox status
- "Save as Ready" - Creates task in Ready status

Edit mode (when task is in Inbox):
- "Save & Keep in Inbox" - Updates but keeps in Inbox
- "Save & Move to Ready" - Updates and moves to Ready

**2. Project Detail Task List**

- Shows all tasks associated with the project
- Completion button (circle) on each non-completed task
- Click task title to navigate to task detail
- "X to triage" badge when tasks are in Inbox

**3. Stuck Indicator**

Shown as amber "Stuck" badge with tooltip:
- "No actionable tasks - add tasks or move them to Ready"

**4. Optimistic Updates**

Task completion uses optimistic updates:
- UI updates immediately on click
- Server confirms in background
- Rollback on error

### State Management

```typescript
// Task Query Keys
export const taskKeys = {
  all: ['tasks'] as const,
  lists: () => [...taskKeys.all, 'list'] as const,
  list: (params?: GetTasksParams) => [...taskKeys.lists(), params] as const,
  details: () => [...taskKeys.all, 'detail'] as const,
  detail: (id: string) => [...taskKeys.details(), id] as const,
  today: () => [...taskKeys.all, 'today'] as const,
  inbox: () => [...taskKeys.all, 'inbox'] as const,
  byProject: (projectId: string) => [...taskKeys.all, 'project', projectId] as const,
}

// Project Query Keys
export const projectKeys = {
  all: ['projects'] as const,
  lists: () => [...projectKeys.all, 'list'] as const,
  list: (params?: GetProjectsParams) => [...projectKeys.lists(), params] as const,
  details: () => [...projectKeys.all, 'detail'] as const,
  detail: (id: string) => [...projectKeys.details(), id] as const,
}
```

### UI Helpers

```typescript
// Task status styling
export const taskStatusInfo: Record<TaskStatus, { label, color, bgColor }> = {
  Inbox: { label: 'Inbox', color: 'text-gray-400', bgColor: 'bg-gray-500/10' },
  Ready: { label: 'Ready', color: 'text-blue-400', bgColor: 'bg-blue-500/10' },
  Scheduled: { label: 'Scheduled', color: 'text-purple-400', bgColor: 'bg-purple-500/10' },
  InProgress: { label: 'In Progress', color: 'text-yellow-400', bgColor: 'bg-yellow-500/10' },
  Completed: { label: 'Completed', color: 'text-green-400', bgColor: 'bg-green-500/10' },
  Cancelled: { label: 'Cancelled', color: 'text-red-400', bgColor: 'bg-red-500/10' },
  Archived: { label: 'Archived', color: 'text-muted-foreground', bgColor: 'bg-muted/50' },
}

// Energy cost indicator
export const energyCostInfo: Record<number, { label, color }> = {
  1: { label: 'Very Low', color: 'text-green-400' },
  2: { label: 'Low', color: 'text-green-300' },
  3: { label: 'Medium', color: 'text-yellow-400' },
  4: { label: 'High', color: 'text-orange-400' },
  5: { label: 'Very High', color: 'text-red-400' },
}

// Context tag display
export const contextTagInfo: Record<ContextTag, { label, emoji, color }> = {
  Computer: { label: 'Computer', emoji: '💻', color: 'text-blue-400' },
  Phone: { label: 'Phone', emoji: '📱', color: 'text-green-400' },
  // ... etc
}
```

---

## Implementation Architecture

### Layer Responsibilities

```
┌─────────────────────────────────────────────────────────────────┐
│                         API Layer                                │
│  Controllers/TasksController.cs, ProjectsController.cs          │
│  Contracts/Tasks/, Contracts/Projects/                          │
│  - HTTP endpoints & routing                                      │
│  - Request/response mapping                                      │
│  - Input validation (structural)                                 │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                     Application Layer                            │
│  Features/Tasks/Commands/, Features/Tasks/Queries/              │
│  Features/Projects/Commands/, Features/Projects/Queries/        │
│  - Use case orchestration (CQRS via MediatR)                    │
│  - Business validation                                           │
│  - DTO mapping                                                   │
│  - Event handlers (metric observations)                          │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                       Domain Layer                               │
│  Entities/Task/, Entities/Project/                              │
│  ValueObjects/, Enums/, Events/                                 │
│  - Business rules & invariants                                   │
│  - Status transitions                                            │
│  - Domain events                                                 │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                   Infrastructure Layer                           │
│  Data/Configurations/, Repositories/                            │
│  - EF Core mapping (incl. JSON columns)                         │
│  - Database operations                                           │
│  - Optimized queries (today view, task counts)                  │
└─────────────────────────────────────────────────────────────────┘
```

### Key Files Reference

| Layer | File | Purpose |
|-------|------|---------|
| Domain | `Entities/Task/Task.cs` | Task aggregate with business logic |
| Domain | `Entities/Task/TaskMetricBinding.cs` | Metric binding child entity |
| Domain | `Entities/Project/Project.cs` | Project aggregate |
| Domain | `Entities/Project/Milestone.cs` | Milestone child entity |
| Domain | `ValueObjects/TaskDue.cs` | Due date configuration |
| Domain | `ValueObjects/TaskScheduling.cs` | Scheduling configuration |
| Domain | `ValueObjects/TaskCompletion.cs` | Completion data |
| Domain | `Enums/TaskStatus.cs` | Task lifecycle states |
| Domain | `Enums/ProjectStatus.cs` | Project lifecycle states |
| Domain | `Events/TaskEvents.cs` | Task domain events |
| Domain | `Events/ProjectEvents.cs` | Project domain events |
| Application | `Features/Tasks/Commands/CreateTask/` | Task creation |
| Application | `Features/Tasks/Commands/CompleteTask/` | Task completion |
| Application | `Features/Tasks/EventHandlers/` | Metric observation creation |
| Application | `Features/Projects/Commands/` | Project commands |
| Application | `Features/Projects/Queries/` | Project queries with IsStuck calculation |
| Infrastructure | `Data/Configurations/TaskConfiguration.cs` | EF mapping |
| Infrastructure | `Repositories/TaskRepository.cs` | Task data access |
| API | `Controllers/TasksController.cs` | Task HTTP endpoints |
| API | `Controllers/ProjectsController.cs` | Project HTTP endpoints |

---

## Extension Guide

### Adding a New Task Status

1. **Domain**: Add to `TaskStatus` enum
2. **Domain**: Update transition methods in `Task.cs`
3. **Application**: Update validation and query logic
4. **Frontend**: Add to `taskStatusInfo` helper
5. **Frontend**: Update any status-dependent UI logic

### Adding a New Context Tag

1. **Domain**: Add to `ContextTag` enum
2. **Frontend**: Add to `contextTagInfo` with emoji and color
3. **Frontend**: Add to context tag selector in task form

### Adding a New Reschedule Reason

1. **Domain**: Add to `RescheduleReason` enum
2. **Frontend**: Add to `rescheduleReasonInfo` with emoji
3. **Frontend**: Add to reschedule dialog options

### Implementing Batch Operations

The API contracts support batch operations:
- `BatchCompleteTasksRequest`
- `BatchRescheduleTasksRequest`
- `BatchCancelTasksRequest`

Frontend hooks exist but batch UI is not yet implemented.

---

## Testing Considerations

### Unit Tests (Domain)

- `Task.Create()` validates required fields
- `Task.MoveToReady()` only from Inbox
- `Task.Complete()` sets completion data
- `Task.UndoCompletion()` clears completion, returns to Ready
- `Project.IsStuck` calculation scenarios
- `Project.Complete()` sets OutcomeNotes and timestamp

### Integration Tests (Application)

- `CreateTaskCommand` creates task with correct status
- `CompleteTaskCommand` creates metric observations
- `GetProjectByIdQuery` calculates IsStuck correctly
- Reschedule increments count and stores reason

### API Tests

- POST `/api/tasks` with `startAsReady=true` creates Ready task
- POST `/api/tasks/{id}/complete` returns 204
- GET `/api/projects/{id}` includes IsStuck based on task states

---

## Future Considerations

### Potential Enhancements

1. **Recurring Tasks**: Tasks that regenerate on schedule
2. **Task Templates**: Pre-built task configurations
3. **Smart Scheduling**: AI-suggested scheduling based on energy/context
4. **Dependency Visualization**: Graph view of blocked tasks
5. **Time Tracking**: Actual time vs. estimated analysis
6. **Batch UI**: Multi-select with floating action bar
7. **Inbox Quick-Add**: Ultra-low-friction capture widget

### Performance Considerations

- Today view is optimized query (scheduled + due + overdue)
- Task counts by status are computed in project queries
- Consider caching project IsStuck calculation
- Index on (UserId, Status) for common queries

---

## Glossary

| Term | Definition |
|------|------------|
| **Task** | A one-time action with energy cost and context |
| **Project** | Container grouping related tasks toward an outcome |
| **Milestone** | Progress marker within a project |
| **Inbox** | Captured tasks not yet triaged |
| **Ready** | Tasks triaged and ready to work on |
| **Scheduled** | Tasks committed to a specific date |
| **Stuck** | Project with no actionable tasks |
| **Energy Cost** | Mental/physical effort required (1-5) |
| **Context Tag** | Where/when a task can be executed |
| **Reschedule Reason** | Why a task was moved to a new date |
| **Metric Binding** | Connection between task and metric for auto-tracking |
