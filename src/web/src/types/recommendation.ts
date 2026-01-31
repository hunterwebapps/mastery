// Recommendation Types

export type RecommendationType =
  | 'NextBestAction'
  | 'Top1Suggestion'
  | 'HabitModeSuggestion'
  | 'PlanRealismAdjustment'
  | 'TaskBreakdownSuggestion'
  | 'ScheduleAdjustmentSuggestion'
  | 'ProjectStuckFix'
  | 'ProjectSuggestion'
  | 'ProjectGoalLinkSuggestion'
  | 'ExperimentRecommendation'
  | 'GoalScoreboardSuggestion'
  | 'HabitFromLeadMetricSuggestion'
  | 'CheckInConsistencyNudge'
  | 'MetricObservationReminder'
  // Edit/Archive suggestions
  | 'TaskEditSuggestion'
  | 'TaskArchiveSuggestion'
  | 'HabitEditSuggestion'
  | 'HabitArchiveSuggestion'
  | 'GoalEditSuggestion'
  | 'GoalArchiveSuggestion'
  | 'ProjectEditSuggestion'
  | 'ProjectArchiveSuggestion'
  | 'MetricEditSuggestion'
  | 'ExperimentEditSuggestion'
  | 'ExperimentArchiveSuggestion'

export type RecommendationStatus = 'Pending' | 'Accepted' | 'Dismissed' | 'Snoozed' | 'Expired' | 'Executed'

export type RecommendationContext =
  | 'Onboarding'
  | 'MorningCheckIn'
  | 'Midday'
  | 'EveningCheckIn'
  | 'WeeklyReview'
  | 'DriftAlert'

export type RecommendationTargetKind =
  | 'Goal'
  | 'Metric'
  | 'Habit'
  | 'HabitOccurrence'
  | 'Task'
  | 'Project'
  | 'Experiment'
  | 'UserProfile'

export type RecommendationActionKind =
  | 'Create'
  | 'Update'
  | 'ExecuteToday'
  | 'Defer'
  | 'Remove'
  | 'ReflectPrompt'
  | 'LearnPrompt'

export type SignalType =
  | 'PlanRealismRisk'
  | 'Top1FollowThroughLow'
  | 'CheckInConsistencyDrop'
  | 'HabitAdherenceDrop'
  | 'FrictionHigh'
  | 'ProjectStuck'
  | 'LeadMetricDrift'
  | 'GoalScoreboardIncomplete'
  | 'NoActuatorForLeadMetric'
  | 'CapacityOverload'
  | 'EnergyTrendLow'

export interface RecommendationDto {
  id: string
  userId: string
  type: RecommendationType
  status: RecommendationStatus
  context: RecommendationContext
  targetKind: RecommendationTargetKind
  targetEntityId?: string
  targetEntityTitle?: string
  actionKind: RecommendationActionKind
  title: string
  rationale: string
  actionPayload?: string
  actionSummary?: string
  score: number
  expiresAt?: string
  respondedAt?: string
  dismissReason?: string
  signalIds: string[]
  trace?: RecommendationTraceDto
  createdAt: string
  modifiedAt?: string
}

export interface RecommendationSummaryDto {
  id: string
  type: RecommendationType
  status: RecommendationStatus
  context: RecommendationContext
  targetKind: RecommendationTargetKind
  targetEntityId?: string
  targetEntityTitle?: string
  actionKind: RecommendationActionKind
  title: string
  rationale: string
  actionSummary?: string
  score: number
  expiresAt?: string
  createdAt: string
}

export interface ExecutionResult {
  entityId?: string
  entityKind?: string
  success: boolean
  errorMessage?: string
  /** JSON payload for client-side form pre-population */
  actionPayload?: string
  /** The action kind (Create, Update, Remove, etc.) */
  actionKind?: string
  /** The target entity kind (Task, Habit, Goal, etc.) */
  targetKind?: string
  /** The target entity ID for Update/Remove actions */
  targetEntityId?: string
  /** True for Create/Update/Remove actions that require client-side form navigation */
  requiresClientAction?: boolean
}

export interface RecommendationTraceDto {
  id: string
  stateSnapshotJson: string
  signalsSummaryJson: string
  candidateListJson: string
  promptVersion?: string
  modelVersion?: string
  rawLlmResponse?: string
  selectionMethod: string
  createdAt: string
}

export interface DiagnosticSignalDto {
  id: string
  type: SignalType
  title: string
  description: string
  severity: number
  evidenceMetric: string
  evidenceCurrentValue: number
  evidenceThresholdValue?: number
  evidenceDetail?: string
  isActive: boolean
  detectedOn: string
  createdAt: string
}

// Request types
export interface GenerateRecommendationsRequest {
  context: string
}

export interface DismissRecommendationRequest {
  reason?: string
}

// UI helpers
export const recommendationStatusInfo: Record<RecommendationStatus, { label: string; color: string; bgColor: string }> = {
  Pending: { label: 'Pending', color: 'text-yellow-400', bgColor: 'bg-yellow-500/10' },
  Accepted: { label: 'Accepted', color: 'text-green-400', bgColor: 'bg-green-500/10' },
  Dismissed: { label: 'Dismissed', color: 'text-gray-400', bgColor: 'bg-gray-500/10' },
  Snoozed: { label: 'Snoozed', color: 'text-orange-400', bgColor: 'bg-orange-500/10' },
  Expired: { label: 'Expired', color: 'text-zinc-500', bgColor: 'bg-zinc-500/10' },
  Executed: { label: 'Executed', color: 'text-blue-400', bgColor: 'bg-blue-500/10' },
}

export const recommendationTypeInfo: Record<RecommendationType, { label: string; description: string; color: string; bgColor: string }> = {
  NextBestAction: { label: 'Next Best Action', description: 'The most impactful action to take right now', color: 'text-green-400', bgColor: 'bg-green-500/10' },
  Top1Suggestion: { label: 'Top-1 Suggestion', description: 'Suggested priority focus for the day', color: 'text-purple-400', bgColor: 'bg-purple-500/10' },
  HabitModeSuggestion: { label: 'Habit Mode', description: 'Suggested habit scaling adjustment', color: 'text-sky-400', bgColor: 'bg-sky-500/10' },
  PlanRealismAdjustment: { label: 'Plan Realism', description: 'Adjustment to improve plan feasibility', color: 'text-indigo-400', bgColor: 'bg-indigo-500/10' },
  TaskBreakdownSuggestion: { label: 'Task Breakdown', description: 'Break a large task into smaller steps', color: 'text-blue-400', bgColor: 'bg-blue-500/10' },
  ScheduleAdjustmentSuggestion: { label: 'Schedule Adjustment', description: 'Suggested schedule change', color: 'text-teal-400', bgColor: 'bg-teal-500/10' },
  ProjectStuckFix: { label: 'Project Unstick', description: 'Action to unblock a stuck project', color: 'text-rose-400', bgColor: 'bg-rose-500/10' },
  ProjectSuggestion: { label: 'New Project', description: 'Suggested project to organize work', color: 'text-pink-400', bgColor: 'bg-pink-500/10' },
  ProjectGoalLinkSuggestion: { label: 'Link to Goal', description: 'Link a project to a related goal', color: 'text-fuchsia-400', bgColor: 'bg-fuchsia-500/10' },
  ExperimentRecommendation: { label: 'Experiment', description: 'Suggested experiment to run', color: 'text-violet-400', bgColor: 'bg-violet-500/10' },
  GoalScoreboardSuggestion: { label: 'Goal Scoreboard', description: 'Improve goal measurement setup', color: 'text-amber-400', bgColor: 'bg-amber-500/10' },
  HabitFromLeadMetricSuggestion: { label: 'Habit from Metric', description: 'Create a habit to drive a lead metric', color: 'text-lime-400', bgColor: 'bg-lime-500/10' },
  CheckInConsistencyNudge: { label: 'Check-in Nudge', description: 'Encouragement to maintain check-in streak', color: 'text-cyan-400', bgColor: 'bg-cyan-500/10' },
  MetricObservationReminder: { label: 'Metric Reminder', description: 'Reminder to record a metric observation', color: 'text-orange-400', bgColor: 'bg-orange-500/10' },
  // Edit suggestions
  TaskEditSuggestion: { label: 'Edit Task', description: 'Suggested task modification', color: 'text-blue-400', bgColor: 'bg-blue-500/10' },
  HabitEditSuggestion: { label: 'Edit Habit', description: 'Suggested habit modification', color: 'text-sky-400', bgColor: 'bg-sky-500/10' },
  GoalEditSuggestion: { label: 'Edit Goal', description: 'Suggested goal modification', color: 'text-amber-400', bgColor: 'bg-amber-500/10' },
  ProjectEditSuggestion: { label: 'Edit Project', description: 'Suggested project modification', color: 'text-rose-400', bgColor: 'bg-rose-500/10' },
  MetricEditSuggestion: { label: 'Edit Metric', description: 'Suggested metric modification', color: 'text-orange-400', bgColor: 'bg-orange-500/10' },
  ExperimentEditSuggestion: { label: 'Edit Experiment', description: 'Suggested experiment modification', color: 'text-violet-400', bgColor: 'bg-violet-500/10' },
  // Archive suggestions
  TaskArchiveSuggestion: { label: 'Archive Task', description: 'Suggested task to archive', color: 'text-gray-400', bgColor: 'bg-gray-500/10' },
  HabitArchiveSuggestion: { label: 'Archive Habit', description: 'Suggested habit to archive', color: 'text-gray-400', bgColor: 'bg-gray-500/10' },
  GoalArchiveSuggestion: { label: 'Archive Goal', description: 'Suggested goal to archive', color: 'text-gray-400', bgColor: 'bg-gray-500/10' },
  ProjectArchiveSuggestion: { label: 'Archive Project', description: 'Suggested project to archive', color: 'text-gray-400', bgColor: 'bg-gray-500/10' },
  ExperimentArchiveSuggestion: { label: 'Archive Experiment', description: 'Suggested experiment to archive', color: 'text-gray-400', bgColor: 'bg-gray-500/10' },
}

export const signalTypeInfo: Record<SignalType, { label: string; icon: string; color: string; bgColor: string }> = {
  PlanRealismRisk: { label: 'Plan Realism Risk', icon: 'AlertTriangle', color: 'text-indigo-400', bgColor: 'bg-indigo-500/10' },
  Top1FollowThroughLow: { label: 'Low Top-1 Follow-through', icon: 'Target', color: 'text-purple-400', bgColor: 'bg-purple-500/10' },
  CheckInConsistencyDrop: { label: 'Check-in Drop', icon: 'Activity', color: 'text-cyan-400', bgColor: 'bg-cyan-500/10' },
  HabitAdherenceDrop: { label: 'Habit Adherence Drop', icon: 'Activity', color: 'text-green-400', bgColor: 'bg-green-500/10' },
  FrictionHigh: { label: 'High Friction', icon: 'AlertTriangle', color: 'text-orange-400', bgColor: 'bg-orange-500/10' },
  ProjectStuck: { label: 'Project Stuck', icon: 'AlertTriangle', color: 'text-rose-400', bgColor: 'bg-rose-500/10' },
  LeadMetricDrift: { label: 'Lead Metric Drift', icon: 'Activity', color: 'text-amber-400', bgColor: 'bg-amber-500/10' },
  GoalScoreboardIncomplete: { label: 'Incomplete Scoreboard', icon: 'Target', color: 'text-yellow-400', bgColor: 'bg-yellow-500/10' },
  NoActuatorForLeadMetric: { label: 'No Actuator', icon: 'AlertTriangle', color: 'text-red-400', bgColor: 'bg-red-500/10' },
  CapacityOverload: { label: 'Capacity Overload', icon: 'AlertTriangle', color: 'text-red-400', bgColor: 'bg-red-500/10' },
  EnergyTrendLow: { label: 'Low Energy Trend', icon: 'Activity', color: 'text-sky-400', bgColor: 'bg-sky-500/10' },
}

export const recommendationContextInfo: Record<RecommendationContext, { label: string; description: string }> = {
  Onboarding: { label: 'Onboarding', description: 'Initial setup and configuration' },
  MorningCheckIn: { label: 'Morning Check-in', description: 'Start of day planning' },
  Midday: { label: 'Midday', description: 'Mid-day course correction' },
  EveningCheckIn: { label: 'Evening Check-in', description: 'End of day reflection' },
  WeeklyReview: { label: 'Weekly Review', description: 'Weekly planning and review' },
  DriftAlert: { label: 'Drift Alert', description: 'Triggered by detected drift from goals' },
}
