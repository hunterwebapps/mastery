What your system is optimizing for

Across the docs, Mastery is already structured as a closed-loop control system:
	•	Setpoints + guardrails live in UserProfile (values, roles, season, preferences, constraints).  ￼
	•	Objectives + scoreboards live in Goals & Metrics (lag/lead/constraint metrics + observations).  ￼
	•	Actuators are Habits (recurring lead behaviors with mode variants) and Projects/Tasks (one-time actions with energy/context/time).
	•	Sensors are Check-Ins (AM intent + PM reality capture; energy/stress/blockers; Top-1 completion as “error signal”).  ￼
	•	Adaptive mechanism is Experiments (hypothesis → baseline/run windows → guardrails → outcome classification), with the single-active constraint to avoid confounds.  ￼
	•	The system design doc explicitly pushes “deterministic constraints first, LLM second,” plus event-ledger + recommendation traces.  ￼

So the app’s goal is not “generate advice.” It’s: continuously translate goals into feasible daily execution under capacity constraints, then learn what interventions actually work for this user.

That’s exactly where an agentic system fits—as the controller that runs multiple feedback loops (daily, weekly, experimental), while keeping your “<2 minutes/day input” constraint intact.

⸻

What “agentic” should mean in Mastery

In this domain, “agentic” is not “the LLM autonomously edits the user’s life.” The reliable version is:
	1.	Compute feasibility + candidates deterministically (plans, NBA candidates, overload, etc.).
	2.	Use an LLM to select, explain, and adapt language (bounded to candidates + structured intervention library).
	3.	Persist an auditable trace (what inputs/rules/candidates led to what recommendation).
	4.	Measure acceptance → completion → outcome and update a per-user playbook.

This aligns directly with the system-design principle you already wrote down.  ￼

⸻

Agentic assistance surfaces along the “path to Mastery”

Here are the highest leverage places the agent can help, mapped to your existing primitives (so it stays implementable):

1) Goal formation → scoreboard quality (high leverage, low frequency)
	•	Convert “I want X” into 1 lag + 2 lead + 1 constraint scoreboard defaults.
	•	Detect missing baselines, wrong aggregation/window, unrealistic targets.
	•	Suggest metric definitions and default cadences.

(Uses Goals & Metrics entities and target/window/aggregation rules.)  ￼

2) Goal → actuator mapping (habits/tasks/projects)
	•	For each lead metric, propose habits (recurring) and projects/tasks (one-time) that plausibly move it.
	•	Auto-link tasks/habits to goals and metric bindings where appropriate, so downstream “goal impact scoring” is real, not vibes.

(Uses Habits metric bindings + Task metric bindings + GoalId links.)

3) Daily plan generation under capacity constraints (daily)
	•	After AM check-in, compute a feasible plan given:
	•	energy + mode (Full/Maintenance/Minimum),
	•	constraints (max planned minutes, blocked windows),
	•	due dates, energy costs, context tags, dependencies,
	•	season/role priorities.

Then pick Top 1 + 2–3 support actions, and present alternatives.

(Check-ins provide energy/mode/top1; constraints in profile; tasks/habits provide energy/time/context; the doc calls this “Next Best Action” and “plan realism.”)

4) Friction diagnosis + interventions (daily/weekly)
	•	Cluster friction signals:
	•	habit miss reasons, task reschedule reasons, evening blocker categories, Top-1 misses.
	•	Convert those into one targeted intervention (not a laundry list).

(Miss reasons / blockers are explicitly designed as diagnostic signals.)

5) Weekly review → one experiment/week (weekly)
	•	Summarize the week (wins/misses, lead vs lag drift, constraint violations).
	•	Choose one experiment to run next week (single-active constraint respected).
	•	Define measurement plan: primary metric + guardrails + compliance threshold.

(Your experiment model already has the right scaffolding; the agent’s job is selection + packaging.)

6) Learning engine → personalization (ongoing)
	•	Track “what worked when”:
	•	intervention type, context features (energy, overload, day-of-week, season intensity),
	•	acceptance and completion outcomes,
	•	experiment classifications.

Then weight future recommendations using a simple bandit model.

(This is explicitly on your roadmap as “Learning Engine / playbook.”)  ￼

⸻

Proposed architecture: “Mastery Intelligence” as a controller pipeline

Key idea

You don’t need a swarm of chatty agents. You need one orchestrator that runs a deterministic + LLM controller pipeline, plus a few specialized sub-components.

Logical components

Client (mobile/web)
   │
   ▼
BFF / API (TodayView, WeeklyReviewView, CoachChat)
   │
   ├── Domain modules (existing):
   │     UserProfile, Goals/Metrics, Habits, Tasks/Projects, CheckIns, Experiments
   │
   └── Mastery Intelligence (new)
         ├── State Builder (projection-backed)
         ├── Planning Engine (deterministic)
         ├── Diagnostic Engine (rules-first)
         ├── Intervention Library (curated)
         ├── LLM Orchestrator (bounded choice + explanation)
         ├── Recommendation Store + Trace Store
         └── Learning Engine (bandit/playbook)

The controller pipeline (the core “agent run”)

This is the concrete, implementable pipeline I’d build around your existing domain events:

Step 0 — Trigger

Triggered by one of:
	•	MorningCheckInSubmittedEvent (daily plan)
	•	EveningCheckInSubmittedEvent (drift update + next day precompute)
	•	weekly scheduled job (weekly review)
	•	explicit “Ask Coach” request

(Check-ins already emit these domain events.)  ￼

Step 1 — Build a structured UserStateSnapshot

This should be strictly structured, assembled from projections for speed:
	•	UserProfile (values/roles/season + constraints + preferences)  ￼
	•	Active goals + scoreboard metrics + recent aggregated values/trends  ￼
	•	Today habits due + streak/adherence + today occurrence status  ￼
	•	Today tasks (scheduled/due/overdue) + energy cost + context + dependencies + reschedule friction  ￼
	•	Latest check-ins (energy/mode/top1/blockers) + streak  ￼
	•	Active experiment (if any) + measurement plan status  ￼

Step 2 — Deterministic computation (“code decides what’s feasible”)

Produce:
	•	Capacity budget for today (minutes + energy) from constraints + energy/mode.
	•	Feasibility gating: filter out tasks/habits that can’t fit (time, context, blocked windows, dependencies).
	•	Candidate actions with scores:
	•	Next Best Action candidates (tasks)
	•	Due habits with mode recommendation (Full/Maintenance/Minimum)
	•	“Triage” candidates (Inbox→Ready tasks, “scope too big” breakdown)
	•	“Unstick project” candidates (create next action)
	•	Diagnostic flags: overcommitment, repeated friction patterns, check-in consistency drop, Top1 follow-through drop.

This is where you operationalize your “deterministic constraints first” rule.  ￼

Step 3 — LLM selection + explanation (bounded)

Give the LLM:
	•	the snapshot summary (not the full DB),
	•	diagnostic flags,
	•	top N candidates (with structured fields + scores),
	•	the user’s coaching preferences (style/verbosity/nudge level).

Ask it to output only:
	•	selected recommendation(s) from the candidate set,
	•	a short explanation aligned to values/roles,
	•	optionally a single “micro-intervention” from a curated library,
	•	optionally one question only if necessary to unblock a decision.

Step 4 — Policy + validation layer (non-LLM)

Before persisting:
	•	Enforce hard constraints:
	•	max planned minutes, blocked windows, no-notification windows, content boundaries.
	•	Enforce business invariants:
	•	single active experiment,
	•	draft-only edits for experiments,
	•	etc. (already in your aggregates).
	•	Ensure the LLM’s choice is still feasible; if not, auto-fallback to next best candidate and log.

Step 5 — Persist outputs as first-class artifacts

Store:
	•	Recommendation (what the user sees)
	•	RecommendationTrace (why)
	•	AgentRun (inputs, versions, latency, model metadata)

Your system design doc already calls out “recommendation trace” as core.  ￼

Step 6 — Outcome capture

When the user interacts:
	•	Accepted / dismissed / swapped to alternative
	•	Completion correlation (did they complete the task/habit?)
	•	Downstream metric movement (over time)

Then the learning engine updates weights.

⸻

What to store: minimal new domain objects

1) Recommendation (user-facing)

Fields (conceptual):
	•	Id, UserId, CreatedAt
	•	Type (NBA, PlanAdjustment, HabitModeSuggestion, ExperimentProposal, TriagePrompt, ReviewPrompt)
	•	Payload (structured: entity IDs, time estimates, alternatives)
	•	RationaleText
	•	Status (Proposed, Accepted, Dismissed, Applied, Expired)
	•	ExpiresAt (e.g., end of day)

2) RecommendationTrace (debuggable + explainable)
	•	SnapshotFeatures (energy, mode, overload score, streaks, etc.)
	•	RulesTriggered[] (e.g., Overcommitment, TooTiredCluster, ForgotCluster)
	•	CandidateList[] (top 10, with scores and reasons)
	•	ChosenCandidate
	•	PromptVersion, ModelVersion
	•	SafetyFlags[]

3) InterventionLibrary (curated, parameterized)

Interventions are not free-form text. They’re structured templates + parameters, e.g.:
	•	PlanRealism: “Drop 2 low-impact tasks” (parameter: task IDs)
	•	FrictionReduction: “Break down scope-too-big task into 3 subtasks”
	•	Top1FollowThrough: “Midday Top1 rescue ping at 2pm”
	•	CheckInConsistency: “Reduce evening check-in to one-tap for 7 days”

This maps directly to the diagnostic categories you already use in experiments and system design.

4) UserPlaybook (learning)

Store tuples:
	•	InterventionType + ContextFeatures → SuccessWeight
Where success can be:
	•	acceptance rate,
	•	accepted→completed conversion,
	•	effect on lead metric adherence,
	•	effect on constraint metric stability.

⸻

Deterministic scoring: make “Next Best Action” actually defensible

You already have the raw inputs to compute NBA reliably:
	•	Task importance / urgency: due dates (soft/hard), overdue flag, priority.  ￼
	•	Energy match: morning energy + selected mode vs task energy cost.
	•	Time fit: estimated minutes vs remaining capacity and buffers.
	•	Context fit: tags like Computer, DeepWork, Home, etc.  ￼
	•	Goal impact:
	•	direct goal link (GoalId)
	•	metric bindings (task completion updates metric observations)
	•	Friction penalty: high reschedule count / repeated reschedule reasons.  ￼
	•	Blocked penalty: dependencies unresolved.  ￼

A practical pattern:
	1.	Eligibility filter: only “doable now.”
	2.	Score: weighted sum.
	3.	Explain: LLM explains top factors + tradeoffs.

This matches your “LLM selects among candidates” architecture.  ￼

⸻

How the agent drives the user via multi-timescale loops

Daily loop

Morning (after check-in):
	•	Produce:
	•	Top 1 (or propose 3 options if none selected)
	•	2–3 support actions
	•	habit mode recommendations (Full/Maintenance/Minimum) based on energy and recent adherence
	•	one micro-coaching intervention (optional)

(Check-in constraints + mode logic already exist.)

During day (optional, based on nudge level):
	•	If Top 1 not started by a threshold time, send “Top1 rescue” with:
	•	1 tiny next step
	•	offer to downgrade plan if overload detected

(Use preferences NudgeLevel and no-notification windows.)  ￼

Evening (after check-in):
	•	Summarize:
	•	Top1 outcome, blockers, energy delta, stress
	•	Update:
	•	friction signals, streaks
	•	draft next-day plan (optional)

(Check-in has blocker taxonomy and Top1 completion signal.)  ￼

Weekly loop
	•	Auto-generate weekly summary (lead vs lag trends, constraint violations).
	•	Pick one experiment for next week.
	•	Adjust targets (plan realism) if capacity mismatch persists.

(System design doc calls this out explicitly.)

Experiment loop (adaptive control)
	•	Ensure single-active experiment.
	•	During experiment: prompt for short notes if none.
	•	At completion: compute baseline vs run (when automated later), classify outcome, update playbook.

(Your experiment entity/measurement plan/results are already structured for this.)  ￼

⸻

Implementation blueprint: minimal steps that unlock “agentic” quickly

This is the shortest path to something that will feel meaningfully agentic while staying reliable.

Step 1 — Add “Recommendation + Trace” primitives
	•	New tables: Recommendations, RecommendationTraces, AgentRuns
	•	API endpoints:
	•	GET /today-view (already BFF-friendly)
	•	GET /recommendations?date=...
	•	POST /recommendations/{id}/accept
	•	POST /recommendations/{id}/dismiss
	•	POST /recommendations/{id}/choose-alternative

This implements the auditable “why” loop you described.  ￼

Step 2 — Build TodayState + WeeklyState projections
	•	Projection: TodayState (habits due, tasks due, energy/mode, capacity, active experiment)
	•	Projection: WeeklyState (aggregated adherence, friction histograms, metric deltas)

This keeps LLM prompts small and makes the system fast enough for daily usage.

Step 3 — Deterministic NBA candidate generator
	•	Build the eligibility + scoring algorithm in code.
	•	Surface top 10 candidates + score breakdown.

Step 4 — LLM orchestrator (bounded selection + explanation)
	•	Feed it top candidates + diagnostic flags + profile preferences.
	•	Output: selected NBA + explanation + optional intervention from library.

Step 5 — Hook into domain events (outbox)
	•	On morning check-in event: compute plan + recommendations.
	•	On evening check-in: update diagnostics + next-day precompute.
	•	On weekly job: weekly review + experiment proposal.

(Check-ins and experiments already have domain event patterns.)

Step 6 — Learning engine v1 (bandit)
	•	Start with just:
	•	per-intervention success weight,
	•	per-context bucket (e.g., low energy vs high energy),
	•	update weights on acceptance→completion.

Then expand.

⸻

A concrete “agent portfolio” inside the orchestrator

You can model these as internal modules (not separate services unless needed):
	1.	State Builder: produces UserStateSnapshot
	2.	Planner: computes capacity + candidate actions
	3.	Diagnostician: emits ranked hypotheses (“why you’re slipping”)
	4.	Coach (LLM): selects + explains + communicates
	5.	Experiment Designer: proposes experiment specs aligned to your experiment model
	6.	Learner: updates playbook weights

This matches your “Planning / Diagnostic / Coaching / Learning engines” decomposition without overcomplicating it.  ￼

⸻

How this architecture leverages your existing feature design (no rewrites)
	•	Check-ins already give you the minimal viable sensors and event triggers.  ￼
	•	Tasks/habits already have the planning primitives (energy cost, context tags, schedules, variants, friction reasons).
	•	Goals/metrics already give you a scoreboard and a place to anchor “impact.”  ￼
	•	Experiments already provide a formal adaptation loop and single-active constraint (critical for learning signal quality).  ￼
	•	UserProfile already provides the tuning knobs (coaching style/verbosity/nudges) and hard limits (capacity + content boundaries).  ￼
	•	Your system design doc already defines the right “controller pipeline” philosophy and traceability requirement.  ￼

⸻

If you want one “north star” design rule

The agent must always produce one of these outcomes:
	1.	a feasible next action,
	2.	a plan simplification (drop/defer), or
	3.	a clear experiment to learn what to do next.

Never “more information” as the default. That’s what drives users to goals while respecting low-input constraints.