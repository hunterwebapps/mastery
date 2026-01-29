import type { NavigateFunction } from 'react-router-dom'
import type {
  RecommendationType,
  RecommendationDto,
  RecommendationSummaryDto,
  ExecutionResult,
  GoalScoreboardPayload,
  MetricObservationPayload,
} from '@/types'

/**
 * Context passed to each action handler
 */
export interface ActionHandlerContext {
  recommendation: RecommendationDto | RecommendationSummaryDto
  result: ExecutionResult
  navigate: NavigateFunction
}

/**
 * Handler function signature for client-side recommendation actions
 */
export type ActionHandler = (ctx: ActionHandlerContext) => void

/**
 * Safely parse JSON action payload
 */
function parsePayload<T>(json: string | undefined): T | null {
  if (!json) return null
  try {
    return JSON.parse(json) as T
  } catch {
    return null
  }
}

/**
 * Get actionPayload from either a full RecommendationDto or summary
 */
function getActionPayload(rec: RecommendationDto | RecommendationSummaryDto): string | undefined {
  return 'actionPayload' in rec ? rec.actionPayload : undefined
}

/**
 * Handler for GoalScoreboardSuggestion
 * Navigates to goal detail with add-metric action and preselected kinds
 */
const goalScoreboardHandler: ActionHandler = ({ recommendation, navigate }) => {
  const payload = parsePayload<GoalScoreboardPayload>(getActionPayload(recommendation))
  const goalId = recommendation.targetEntityId ?? payload?.goalId

  if (!goalId) return

  const kinds = payload?.missingMetricKinds?.join(',') ?? ''
  navigate(`/goals/${goalId}?action=add-metric&kinds=${kinds}`)
}

/**
 * Handler for MetricObservationReminder
 * Navigates to metric detail with record-observation action
 */
const metricObservationHandler: ActionHandler = ({ recommendation, navigate }) => {
  const payload = parsePayload<MetricObservationPayload>(getActionPayload(recommendation))
  const metricId = recommendation.targetEntityId ?? payload?.metricDefinitionId

  if (!metricId) return

  navigate(`/metrics/${metricId}?action=record-observation`)
}

/**
 * Handler for CheckInConsistencyNudge
 * Navigates to check-in page
 */
const checkInNudgeHandler: ActionHandler = ({ navigate }) => {
  navigate('/check-in')
}

/**
 * Handler for HabitFromLeadMetricSuggestion
 * Navigates to habit creation with metric context
 */
const habitFromLeadMetricHandler: ActionHandler = ({ recommendation, navigate }) => {
  const metricId = recommendation.targetEntityId
  if (metricId) {
    navigate(`/habits/new?fromMetric=${metricId}`)
  } else {
    navigate('/habits/new')
  }
}

/**
 * Default handler for entity-targeted recommendations
 * Navigates to the entity detail page based on targetKind
 */
const defaultEntityHandler: ActionHandler = ({ recommendation, result, navigate }) => {
  const entityId = result.entityId ?? recommendation.targetEntityId
  const kind = result.entityKind ?? recommendation.targetKind

  if (!entityId || !kind) return

  const kindToPath: Record<string, string> = {
    Goal: 'goals',
    Task: 'tasks',
    Habit: 'habits',
    Project: 'projects',
    Experiment: 'experiments',
    Metric: 'metrics',
  }

  const path = kindToPath[kind]
  if (path) {
    navigate(`/${path}/${entityId}`)
  }
}

/**
 * Registry mapping recommendation types to their client-side handlers
 * Types not in this registry will fall back to defaultEntityHandler if entity info is available
 */
export const actionHandlers: Partial<Record<RecommendationType, ActionHandler>> = {
  GoalScoreboardSuggestion: goalScoreboardHandler,
  MetricObservationReminder: metricObservationHandler,
  CheckInConsistencyNudge: checkInNudgeHandler,
  HabitFromLeadMetricSuggestion: habitFromLeadMetricHandler,
}

/**
 * Execute client-side action for a recommendation
 *
 * Priority:
 * 1. Use registered handler for the recommendation type
 * 2. Fall back to default entity navigation if entity info is available
 *
 * @returns true if an action was taken, false otherwise
 */
export function executeClientAction(ctx: ActionHandlerContext): boolean {
  const handler = actionHandlers[ctx.recommendation.type]

  if (handler) {
    handler(ctx)
    return true
  }

  // Fallback to default entity navigation if we have entity info
  if (ctx.recommendation.targetEntityId || ctx.result.entityId) {
    defaultEntityHandler(ctx)
    return true
  }

  return false
}
