// Action Payload Types for Client-Side Recommendation Handlers
// These interfaces define the structure of actionPayload JSON for each recommendation type

import type { MetricKind } from './goal'

/**
 * Payload for GoalScoreboardSuggestion recommendations
 * Suggests adding missing metric kinds to a goal's scoreboard
 */
export interface GoalScoreboardPayload {
  goalId: string
  missingMetricKinds: MetricKind[]
  _summary: string
}

/**
 * Payload for ExperimentRecommendation
 * Suggests creating a new experiment
 */
export interface CreateExperimentPayload {
  title: string
  description?: string
  category: 'Behavioral' | 'Environmental' | 'Cognitive' | 'Social' | 'Physical'
  hypothesis: {
    change: string
    expectedOutcome: string
    rationale?: string
  }
  measurementPlan: {
    runWindowDays: number
  }
  _summary: string
}

/**
 * Payload for CheckInConsistencyNudge recommendations
 * Prompts user to complete their check-in
 */
export interface CheckInNudgePayload {
  prompt: string
  _summary: string
}

/**
 * Payload for MetricObservationReminder recommendations
 * Reminds user to record a metric observation
 */
export interface MetricObservationPayload {
  metricDefinitionId: string
  metricName: string
  _summary: string
}

/**
 * Payload for HabitFromLeadMetricSuggestion recommendations
 * Suggests creating a habit to drive a lead metric
 */
export interface HabitFromLeadMetricPayload {
  metricDefinitionId: string
  metricName: string
  suggestedHabitName?: string
  _summary: string
}

/**
 * Payload for ProjectStuckFix recommendations
 * Suggests an action to unblock a stuck project
 */
export interface ProjectStuckFixPayload {
  projectId: string
  projectName: string
  suggestedAction: string
  _summary: string
}

/**
 * Payload for ProjectGoalLinkSuggestion recommendations
 * Suggests linking an unattached project to a relevant goal
 */
export interface ProjectGoalLinkPayload {
  projectId: string
  goalId: string
  goalTitle: string
  _summary: string
}
