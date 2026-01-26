# UserProfile Feature Documentation

## Overview

The **UserProfile** is the foundational aggregate root in the Mastery system. It represents the stable "setpoint + guardrails" that the control loop uses to interpret signals, rank actions, and personalize coaching. Think of it as the user's identity configuration that changes infrequently (onboarding, occasional edits, quarterly season resets) but influences every decision the system makes.

---

## Business Context

### Why UserProfile Exists

Traditional productivity apps fail because they treat all users the same. Mastery is different—it needs to understand:

1. **Who you are** (Values, Roles) - Your identity shapes what matters
2. **What season you're in** (Season) - Context for current priorities
3. **How you want to be coached** (Preferences) - Personalized interaction style
4. **What limits apply** (Constraints) - Hard boundaries the system must respect

### The Control System Analogy

In control theory terms:

| Control Concept | UserProfile Component |
|-----------------|----------------------|
| **Setpoint** | Values + Roles + Season priorities |
| **Controller Tuning** | Preferences (coaching style, verbosity) |
| **Saturation Limits** | Constraints (max minutes, blocked windows) |
| **Operating Mode** | Season type (Sprint, Recover, etc.) |

The Planning Engine, Diagnostic Engine, and Coaching Engine all read from UserProfile to make capacity-aware, personalized decisions.

---

## Domain Model

### Entity Hierarchy

```
UserProfile (Aggregate Root)
├── UserId (string) ─────────────── External auth system ID
├── Timezone (Value Object) ─────── IANA timezone for scheduling
├── Locale (Value Object) ──────────  Formatting preferences (en-US)
├── OnboardingVersion (int) ──────── Progressive profiling support
│
├── Values[] (JSON) ─────────────── Core values, ranked
│   └── UserValue { Id, Label, Description?, Rank }
│
├── Roles[] (JSON) ──────────────── Life roles with priorities
│   └── UserRole { Id, Label, Description?, Status, Rank, SeasonPriority, IdealHoursPerWeek? }
│
├── CurrentSeasonId? (FK) ──────── Active season reference
├── CurrentSeason (Navigation) ─── Season entity
│
├── Preferences (Owned Entity) ─── Coaching configuration
│   ├── CoachingStyle (enum)
│   ├── ExplanationVerbosity (enum)
│   ├── NudgeLevel (enum)
│   ├── NotificationChannels[] (JSON)
│   ├── CheckInSchedule (Owned)
│   │   ├── MorningTime (TimeOnly)
│   │   └── EveningTime (TimeOnly)
│   ├── PlanningDefaults (Owned)
│   │   ├── DefaultTaskDurationMinutes
│   │   ├── AutoScheduleHabits
│   │   └── BufferBetweenTasksMinutes
│   └── Privacy (Owned)
│       ├── ShareProgressWithCoach
│       └── AllowAnonymousAnalytics
│
└── Constraints (Owned Entity) ─── Hard limits
    ├── MaxPlannedMinutesWeekday (default: 480 = 8 hours)
    ├── MaxPlannedMinutesWeekend (default: 240 = 4 hours)
    ├── BlockedTimeWindows[] (JSON)
    │   └── BlockedWindow { DayOfWeek, Window: TimeWindow, Reason? }
    ├── NoNotificationsWindows[] (JSON)
    │   └── TimeWindow { Start, End }
    ├── HealthNotes (string?)
    └── ContentBoundaries[] (JSON) ─── Topics to avoid
```

### Season Entity (Separate Table)

Seasons are time-bounded priority contexts. They're stored separately because:
- Seasons accumulate over time (history is valuable)
- A user may have many seasons but only one is "current"
- Season history enables retrospectives and pattern analysis

```
Season (Aggregate Root)
├── Id (Guid)
├── UserId (string) ────────────── Owner
├── Label (string) ─────────────── User-facing name ("Q1 2026 Sprint")
├── Type (enum) ────────────────── Sprint | Build | Maintain | Recover | Transition | Explore
├── StartDate (DateOnly)
├── ExpectedEndDate? (DateOnly)
├── ActualEndDate? (DateOnly) ──── Set when season ends
├── FocusRoleIds[] (JSON) ──────── Roles to prioritize this season
├── FocusGoalIds[] (JSON) ──────── Goals to prioritize this season
├── SuccessStatement? (string) ── "I'll know this season succeeded when..."
├── NonNegotiables[] (JSON) ────── Things that must happen regardless
├── Intensity (1-5) ────────────── How hard to push (affects planning)
└── Outcome? (string) ──────────── Retrospective notes when season ends
```

### Season Types Explained

| Type | Description | System Behavior |
|------|-------------|-----------------|
| **Sprint** | Intense focus period | Aggressive planning, minimal buffer, high urgency |
| **Build** | Steady progress | Normal planning, balanced approach |
| **Maintain** | Hold ground | Conservative planning, protect habits |
| **Recover** | Rest and reset | Minimal commitments, self-care priority |
| **Transition** | Life change | Flexible planning, expect disruption |
| **Explore** | Discovery mode | Low commitment, experiment-friendly |

---

## Values and Roles

### Values (Soft Guideline: 5-10)

Values are the user's core principles that guide decision-making. Examples:
- "Family First" - Family time takes priority
- "Continuous Learning" - Growth is important
- "Health Foundation" - Physical health enables everything else

**How the system uses values:**
- Recommendation explanations reference relevant values
- Goal alignment scoring considers value match
- Weekly reviews highlight value-aligned achievements

### Roles (Soft Guideline: 3-8 Active)

Roles represent the different "hats" a user wears. Examples:
- "Parent" - 15 hours/week ideal
- "Engineer" - 40 hours/week ideal
- "Runner" - 5 hours/week ideal

**Role Status:**
- `Active` - Currently relevant, included in planning
- `Dormant` - Temporarily paused (e.g., on leave)
- `Archived` - No longer relevant

**SeasonPriority (1-5):**
During a season, some roles get elevated priority. A "Sprint" season for a product launch might set "Engineer" to priority 5 while "Hobbyist" drops to 1.

**How the system uses roles:**
- Capacity allocation considers ideal hours per role
- Tasks/habits are tagged to roles for time tracking
- Season focus narrows active role set

---

## Preferences Deep Dive

### Coaching Style

Controls the AI's communication tone:

| Style | Behavior |
|-------|----------|
| `Supportive` | Encouraging, gentle nudges, celebrates wins |
| `Direct` | Straightforward feedback, no sugar-coating |
| `Socratic` | Questions to promote self-reflection |
| `DataDriven` | Numbers-focused, trend analysis |

### Explanation Verbosity

Controls how much detail the AI provides:

| Level | Behavior |
|-------|----------|
| `Concise` | Bullet points, minimal explanation |
| `Moderate` | Brief rationale with key points |
| `Detailed` | Full explanation with reasoning chain |

### Nudge Level

Controls notification frequency and urgency:

| Level | Behavior |
|-------|----------|
| `Minimal` | Only critical reminders |
| `Moderate` | Daily check-ins + important nudges |
| `Proactive` | Frequent suggestions + context-aware prompts |

### Notification Channels

Array of enabled channels:
- `InApp` - In-application notifications
- `Push` - Mobile push notifications
- `Email` - Email digest/alerts
- `Sms` - SMS for critical items

### Check-In Schedule

Morning and evening check-in times (in user's timezone). The system uses these to:
- Schedule morning intention prompts
- Schedule evening reflection prompts
- Avoid notifications outside these windows

### Planning Defaults

- **DefaultTaskDurationMinutes** (default: 30) - When a task has no estimate
- **AutoScheduleHabits** (default: true) - Automatically place habits in calendar
- **BufferBetweenTasksMinutes** (default: 5) - Breathing room between items

### Privacy Settings

- **ShareProgressWithCoach** - If using human coach integration
- **AllowAnonymousAnalytics** - Aggregated data for system improvement

---

## Constraints Deep Dive

### Capacity Limits

- **MaxPlannedMinutesWeekday** (default: 480 = 8 hours)
- **MaxPlannedMinutesWeekend** (default: 240 = 4 hours)

These are HARD limits. The Planning Engine will not schedule more than this. The system treats these as "saturation limits" in control theory terms—exceeding them causes plan rejection.

### Blocked Time Windows

Recurring time blocks where no tasks should be scheduled:

```json
[
  {
    "dayOfWeek": "Monday",
    "window": { "start": "12:00", "end": "13:00" },
    "reason": "Lunch with team"
  },
  {
    "dayOfWeek": "Saturday",
    "window": { "start": "09:00", "end": "12:00" },
    "reason": "Family time"
  }
]
```

### No-Notifications Windows

Times when the system should not send any notifications:

```json
[
  { "start": "22:00", "end": "07:00" },  // Sleep
  { "start": "12:00", "end": "13:00" }   // Focus time
]
```

### Health Notes

Free-text field for health considerations the AI should be aware of:
- "ADHD - need shorter tasks with clear starts"
- "Chronic fatigue - avoid back-to-back meetings"
- "Morning person - schedule important work before noon"

### Content Boundaries

Topics the AI should avoid discussing:
- "religion"
- "politics"
- "weight loss" (for ED recovery)

---

## Domain Events

### UserProfile Events

| Event | Trigger | Typical Handler Action |
|-------|---------|----------------------|
| `UserProfileCreatedEvent` | Onboarding complete | Initialize default goals, send welcome |
| `UserProfileUpdatedEvent` | Any section updated | Audit log, sync to analytics |
| `PreferencesUpdatedEvent` | Coaching prefs changed | Update AI system prompt cache |
| `ConstraintsUpdatedEvent` | Capacity limits changed | Revalidate current week's plan |

### Season Events

| Event | Trigger | Typical Handler Action |
|-------|---------|----------------------|
| `SeasonCreatedEvent` | New season started | Archive old season, reset metrics |
| `SeasonActivatedEvent` | Season set as current | Update planning priorities |
| `SeasonEndedEvent` | Season completed | Generate retrospective, prompt review |

---

## Implementation Architecture

### Layer Responsibilities

```
┌─────────────────────────────────────────────────────────────────┐
│                         API Layer                                │
│  Controllers/UserProfilesController.cs                          │
│  - HTTP endpoints                                                │
│  - Request/response mapping                                      │
│  - Input validation (structural)                                 │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                     Application Layer                            │
│  Features/UserProfiles/Commands/                                │
│  Features/UserProfiles/Queries/                                 │
│  - Use case orchestration                                       │
│  - Business validation (FluentValidation)                       │
│  - DTO mapping                                                  │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                       Domain Layer                               │
│  Entities/UserProfile/                                          │
│  ValueObjects/                                                  │
│  Events/                                                        │
│  - Business rules                                               │
│  - Invariant enforcement                                        │
│  - Domain events                                                │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                   Infrastructure Layer                           │
│  Data/Configurations/UserProfileConfiguration.cs                │
│  Repositories/UserProfileRepository.cs                          │
│  - EF Core mapping                                              │
│  - Database operations                                          │
│  - JSON serialization                                           │
└─────────────────────────────────────────────────────────────────┘
```

### Key Files Reference

| Layer | File | Purpose |
|-------|------|---------|
| Domain | `Entities/UserProfile/UserProfile.cs` | Aggregate root with business logic |
| Domain | `Entities/UserProfile/Preferences.cs` | Coaching preferences with enums |
| Domain | `Entities/UserProfile/Constraints.cs` | Capacity limits and blocked windows |
| Domain | `Entities/UserProfile/UserValue.cs` | Value record for JSON storage |
| Domain | `Entities/UserProfile/UserRole.cs` | Role record with status enum |
| Domain | `Entities/Season.cs` | Season aggregate with type enum |
| Domain | `ValueObjects/Timezone.cs` | IANA timezone validation |
| Domain | `ValueObjects/Locale.cs` | Locale format validation |
| Domain | `ValueObjects/TimeWindow.cs` | Start/end time pair |
| Domain | `ValueObjects/CheckInSchedule.cs` | Morning/evening times |
| Domain | `Events/UserProfileEvents.cs` | Profile domain events |
| Domain | `Events/SeasonEvents.cs` | Season domain events |
| Domain | `Interfaces/IUserProfileRepository.cs` | Repository contract |
| Domain | `Interfaces/ISeasonRepository.cs` | Season repository contract |
| Application | `Features/UserProfiles/Models/UserProfileDto.cs` | All DTOs |
| Application | `Features/UserProfiles/Commands/CreateUserProfile/` | Onboarding command |
| Application | `Features/UserProfiles/Queries/GetCurrentUserProfile/` | Profile retrieval |
| Application | `Features/Seasons/Commands/CreateSeason/` | Season creation |
| Application | `Features/Seasons/Queries/GetUserSeasons/` | Season history |
| Infrastructure | `Data/Configurations/UserProfileConfiguration.cs` | EF Core mapping |
| Infrastructure | `Data/Configurations/SeasonConfiguration.cs` | Season EF mapping |
| Infrastructure | `Repositories/UserProfileRepository.cs` | Profile data access |
| Infrastructure | `Repositories/SeasonRepository.cs` | Season data access |
| API | `Controllers/UserProfilesController.cs` | HTTP endpoints |
| API | `Contracts/UserProfiles/Requests.cs` | Request DTOs |

### Database Schema

```sql
-- UserProfiles table
CREATE TABLE UserProfiles (
    Id uniqueidentifier PRIMARY KEY,
    UserId nvarchar(256) NOT NULL UNIQUE,
    Timezone nvarchar(50) NOT NULL,
    Locale nvarchar(10) NOT NULL,
    OnboardingVersion int NOT NULL DEFAULT 1,
    CurrentSeasonId uniqueidentifier NULL,

    -- Preferences (owned entity, flattened)
    CoachingStyle nvarchar(20) NOT NULL,
    ExplanationVerbosity nvarchar(20) NOT NULL,
    NudgeLevel nvarchar(20) NOT NULL,
    NotificationChannels nvarchar(max) NOT NULL,  -- JSON
    CheckInMorningTime time NOT NULL,
    CheckInEveningTime time NOT NULL,
    DefaultTaskDurationMinutes int NOT NULL DEFAULT 30,
    AutoScheduleHabits bit NOT NULL DEFAULT 1,
    BufferBetweenTasksMinutes int NOT NULL DEFAULT 5,
    ShareProgressWithCoach bit NOT NULL DEFAULT 0,
    AllowAnonymousAnalytics bit NOT NULL DEFAULT 1,

    -- Constraints (owned entity, flattened)
    MaxPlannedMinutesWeekday int NOT NULL DEFAULT 480,
    MaxPlannedMinutesWeekend int NOT NULL DEFAULT 240,
    BlockedTimeWindows nvarchar(max) NOT NULL,     -- JSON
    NoNotificationsWindows nvarchar(max) NOT NULL, -- JSON
    HealthNotes nvarchar(1000) NULL,
    ContentBoundaries nvarchar(max) NOT NULL,      -- JSON

    -- JSON columns for variable-length collections
    Values nvarchar(max) NOT NULL,  -- JSON array of UserValue
    Roles nvarchar(max) NOT NULL,   -- JSON array of UserRole

    -- Audit
    CreatedAt datetime2 NOT NULL,
    CreatedBy nvarchar(256) NULL,
    ModifiedAt datetime2 NULL,
    ModifiedBy nvarchar(256) NULL,

    CONSTRAINT FK_UserProfiles_Seasons FOREIGN KEY (CurrentSeasonId)
        REFERENCES Seasons(Id) ON DELETE SET NULL
);

-- Seasons table
CREATE TABLE Seasons (
    Id uniqueidentifier PRIMARY KEY,
    UserId nvarchar(256) NOT NULL,
    Label nvarchar(100) NOT NULL,
    Type nvarchar(20) NOT NULL,
    StartDate date NOT NULL,
    ExpectedEndDate date NULL,
    ActualEndDate date NULL,
    SuccessStatement nvarchar(500) NULL,
    Intensity int NOT NULL DEFAULT 3,
    Outcome nvarchar(2000) NULL,
    FocusRoleIds nvarchar(max) NOT NULL,    -- JSON
    FocusGoalIds nvarchar(max) NOT NULL,    -- JSON
    NonNegotiables nvarchar(max) NOT NULL,  -- JSON

    -- Audit
    CreatedAt datetime2 NOT NULL,
    CreatedBy nvarchar(256) NULL,
    ModifiedAt datetime2 NULL,
    ModifiedBy nvarchar(256) NULL
);

-- Indexes
CREATE UNIQUE INDEX IX_UserProfiles_UserId ON UserProfiles(UserId);
CREATE INDEX IX_UserProfiles_CurrentSeasonId ON UserProfiles(CurrentSeasonId);
CREATE INDEX IX_Seasons_UserId ON Seasons(UserId);
CREATE INDEX IX_Seasons_UserId_StartDate ON Seasons(UserId, StartDate);
```

---

## API Endpoints

### Profile Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/user-profile` | Get current user's profile |
| `POST` | `/api/user-profile` | Create profile (onboarding) |
| `PUT` | `/api/user-profile/values` | Update values |
| `PUT` | `/api/user-profile/roles` | Update roles |
| `PUT` | `/api/user-profile/preferences` | Update preferences |
| `PUT` | `/api/user-profile/constraints` | Update constraints |

### Season Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/user-profile/seasons` | Get all seasons (history) |
| `POST` | `/api/user-profile/seasons` | Create new season (becomes current) |
| `PUT` | `/api/user-profile/seasons/{id}/end` | End a season with outcome |

### Request/Response Examples

**Create Profile (POST /api/user-profile)**
```json
{
  "timezone": "America/New_York",
  "locale": "en-US",
  "values": [
    { "label": "Family First", "description": "Family time is sacred", "rank": 1 },
    { "label": "Health", "description": "Physical health enables everything", "rank": 2 }
  ],
  "roles": [
    { "label": "Parent", "status": "Active", "rank": 1, "seasonPriority": 5, "idealHoursPerWeek": 20 },
    { "label": "Engineer", "status": "Active", "rank": 2, "seasonPriority": 4, "idealHoursPerWeek": 40 }
  ],
  "preferences": {
    "coachingStyle": "Supportive",
    "explanationVerbosity": "Moderate",
    "nudgeLevel": "Moderate",
    "notificationChannels": ["InApp", "Push"],
    "checkInSchedule": {
      "morningTime": "07:00",
      "eveningTime": "21:00"
    }
  },
  "constraints": {
    "maxPlannedMinutesWeekday": 480,
    "maxPlannedMinutesWeekend": 240,
    "healthNotes": "ADHD - need shorter tasks"
  }
}
```

**Get Profile Response**
```json
{
  "id": "a1b2c3d4-...",
  "userId": "auth0|123456",
  "timezone": "America/New_York",
  "locale": "en-US",
  "onboardingVersion": 1,
  "values": [...],
  "roles": [...],
  "currentSeason": {
    "id": "e5f6g7h8-...",
    "label": "Q1 2026 Sprint",
    "type": "Sprint",
    "startDate": "2026-01-01",
    "intensity": 4
  },
  "preferences": {...},
  "constraints": {...},
  "createdAt": "2026-01-25T...",
  "modifiedAt": null
}
```

**Create Season (POST /api/user-profile/seasons)**
```json
{
  "label": "Q1 2026 Product Launch",
  "type": "Sprint",
  "startDate": "2026-01-01",
  "expectedEndDate": "2026-03-31",
  "focusRoleIds": ["role-guid-1", "role-guid-2"],
  "successStatement": "Ship v2.0 with positive user feedback",
  "nonNegotiables": ["Daily exercise", "Family dinner"],
  "intensity": 4
}
```

---

## Extension Guide

### Adding a New Preference

1. **Domain**: Add property to `Preferences.cs`
   ```csharp
   public SomeType NewPreference { get; private set; } = defaultValue;
   ```

2. **Domain**: Add update method if needed
   ```csharp
   public void UpdateNewPreference(SomeType value) { ... }
   ```

3. **Infrastructure**: Update `UserProfileConfiguration.cs`
   ```csharp
   prefs.Property(p => p.NewPreference)
       .HasColumnName("NewPreference")
       .HasDefaultValue(defaultValue);
   ```

4. **Application**: Update `PreferencesDto.cs`

5. **Migration**: Generate and apply
   ```bash
   dotnet ef migrations add AddNewPreference --project Mastery.Infrastructure --startup-project Mastery.Api
   ```

### Adding a New Constraint

Same pattern as preferences, but in `Constraints.cs` and the constraints section of configuration.

### Adding a New Season Type

1. **Domain**: Add to `SeasonType` enum in `Season.cs`
   ```csharp
   public enum SeasonType { ..., NewType }
   ```

2. **Application**: Update validation if type has special rules

3. **UI**: Add type to season creation form with description

### Implementing Update Commands

The following commands are stubbed in the controller and need implementation:

- `UpdateValuesCommand` - Similar to `CreateUserProfile` but only updates values
- `UpdateRolesCommand` - Similar pattern for roles
- `UpdatePreferencesCommand` - Update preferences section
- `UpdateConstraintsCommand` - Update constraints section
- `EndSeasonCommand` - Set `ActualEndDate` and `Outcome` on a season

Pattern to follow (from `CreateUserProfileCommandHandler`):
1. Get current user ID from `ICurrentUserService`
2. Load existing profile via repository
3. Call domain method to update
4. Save via `IUnitOfWork`
5. Return success/updated DTO

---

## UI Implementation Guide

### Onboarding Flow

Recommended multi-step wizard:

1. **Basics** - Timezone, locale
2. **Values** - Guided value selection/entry (5-10)
3. **Roles** - Role definition with ideal hours (3-8)
4. **Preferences** - Coaching style, notification preferences
5. **Constraints** - Capacity limits, blocked times
6. **Season** - Optional first season setup

### Profile Settings Page

Sections that can be edited independently:
- Values (reorderable list)
- Roles (cards with status toggle)
- Preferences (form with dropdowns)
- Constraints (form with time pickers)
- Current Season (read-only with "End Season" action)

### Season Management

- Season history timeline
- Active season card with key info
- "Start New Season" wizard
- "End Season" dialog with outcome prompt

### Key UX Considerations

1. **Soft Validation Feedback**: Show warnings (not errors) when values/roles counts are outside guidelines
2. **Timezone Detection**: Auto-detect from browser, allow override
3. **Preview Impact**: When changing constraints, show how it affects current week's plan
4. **Season Transitions**: Guide user through ending old season before starting new

---

## Testing Considerations

### Unit Tests (Domain)

- `UserProfile.Create()` validates required fields
- `UserProfile.UpdateValues()` replaces value list, raises event
- `UserProfile.SetCurrentSeason()` rejects season with different UserId
- `Timezone.Create()` rejects invalid IANA IDs
- `Season.Create()` validates date logic

### Integration Tests (Application)

- `CreateUserProfileCommand` creates profile and raises event
- `GetCurrentUserProfileQuery` returns null for missing profile
- `CreateSeasonCommand` creates season and sets as current
- Duplicate `UserId` is rejected

### API Tests

- POST `/api/user-profile` with valid data returns 201
- POST `/api/user-profile` with invalid timezone returns 400
- GET `/api/user-profile` returns 404 when no profile exists
- Authenticated user can only access own profile

---

## Future Considerations

### Potential Enhancements

1. **Profile Templates**: Pre-built value/role sets for common archetypes
2. **Role Inheritance**: Child roles that inherit from parent (e.g., "Work" -> "Engineer", "Manager")
3. **Constraint Suggestions**: AI-suggested capacity limits based on calendar analysis
4. **Season Templates**: Pre-built season configurations for common scenarios
5. **Profile Sharing**: Export/import profile configurations
6. **A/B Testing Preferences**: Allow system to test different coaching approaches

### Data Migration Path

If schema changes significantly:
1. Add new columns with defaults
2. Backfill data transformation
3. Remove old columns in subsequent migration

### Performance Considerations

- Profile is read frequently (every request via `ICurrentUserService`)
- Consider caching profile in request scope
- JSON columns are not indexed—don't filter on them in queries
- Season history queries should use `UserId + StartDate` index
