// Goal Types

import type { MetricUnitDto } from './metric'

export type GoalStatus = 'Draft' | 'Active' | 'Paused' | 'Completed' | 'Archived'
export type MetricKind = 'Lag' | 'Lead' | 'Constraint'
export type TargetType = 'AtLeast' | 'AtMost' | 'Between' | 'Exactly'
export type WindowType = 'Daily' | 'Weekly' | 'Monthly' | 'Rolling'
export type MetricAggregation = 'Sum' | 'Average' | 'Max' | 'Min' | 'Count' | 'Latest'
export type MetricSourceType = 'Manual' | 'Habit' | 'Task' | 'CheckIn' | 'Integration' | 'Computed'

export interface TargetDto {
  type: TargetType
  value: number
  maxValue?: number
}

export interface EvaluationWindowDto {
  windowType: WindowType
  rollingDays?: number
  startDay?: number
}

export interface GoalMetricDto {
  id: string
  metricDefinitionId: string
  metricName: string
  kind: MetricKind
  target: TargetDto
  evaluationWindow: EvaluationWindowDto
  aggregation: MetricAggregation
  weight: number
  sourceHint: MetricSourceType
  displayOrder: number
  baseline?: number
  minimumThreshold?: number
  unit?: MetricUnitDto
}

export interface GoalDto {
  id: string
  userId: string
  title: string
  description?: string
  why?: string
  status: GoalStatus
  priority: number
  deadline?: string
  seasonId?: string
  roleIds: string[]
  valueIds: string[]
  dependencyIds: string[]
  metrics: GoalMetricDto[]
  completionNotes?: string
  completedAt?: string
  createdAt: string
  modifiedAt?: string
}

export interface GoalSummaryDto {
  id: string
  title: string
  status: GoalStatus
  priority: number
  deadline?: string
  seasonId?: string
  metricCount: number
  lagMetricCount: number
  leadMetricCount: number
  constraintMetricCount: number
  createdAt: string
}

// Request types
export interface CreateTargetRequest {
  type: TargetType
  value: number
  maxValue?: number
}

export interface CreateEvaluationWindowRequest {
  windowType: WindowType
  rollingDays?: number
  startDay?: number
}

export interface CreateGoalMetricRequest {
  metricDefinitionId: string
  kind: MetricKind
  target: CreateTargetRequest
  evaluationWindow: CreateEvaluationWindowRequest
  aggregation: MetricAggregation
  sourceHint: MetricSourceType
  weight?: number
  displayOrder?: number
  baseline?: number
  minimumThreshold?: number
}

export interface CreateGoalRequest {
  title: string
  description?: string
  why?: string
  priority?: number
  deadline?: string
  seasonId?: string
  roleIds?: string[]
  valueIds?: string[]
  dependencyIds?: string[]
  metrics?: CreateGoalMetricRequest[]
}

export interface UpdateGoalRequest {
  title: string
  description?: string
  why?: string
  priority?: number
  deadline?: string
  seasonId?: string
  roleIds?: string[]
  valueIds?: string[]
  dependencyIds?: string[]
}

export interface UpdateGoalStatusRequest {
  newStatus: GoalStatus
  completionNotes?: string
}

export interface UpdateGoalMetricRequest {
  id?: string
  metricDefinitionId: string
  kind: MetricKind
  target: CreateTargetRequest
  evaluationWindow: CreateEvaluationWindowRequest
  aggregation: MetricAggregation
  sourceHint: MetricSourceType
  weight?: number
  displayOrder?: number
  baseline?: number
  minimumThreshold?: number
}

export interface UpdateGoalScoreboardRequest {
  metrics: UpdateGoalMetricRequest[]
}

// UI helpers
export const goalStatusInfo: Record<
  GoalStatus,
  { label: string; description: string; color: string; bgColor: string }
> = {
  Draft: {
    label: 'Draft',
    description: 'Goal is being defined',
    color: 'text-gray-400',
    bgColor: 'bg-gray-500/10',
  },
  Active: {
    label: 'Active',
    description: 'Actively working toward this goal',
    color: 'text-green-400',
    bgColor: 'bg-green-500/10',
  },
  Paused: {
    label: 'Paused',
    description: 'Temporarily on hold',
    color: 'text-yellow-400',
    bgColor: 'bg-yellow-500/10',
  },
  Completed: {
    label: 'Completed',
    description: 'Goal has been achieved',
    color: 'text-blue-400',
    bgColor: 'bg-blue-500/10',
  },
  Archived: {
    label: 'Archived',
    description: 'No longer tracking this goal',
    color: 'text-zinc-500',
    bgColor: 'bg-zinc-500/10',
  },
}

export const metricKindInfo: Record<
  MetricKind,
  { label: string; description: string; color: string }
> = {
  Lag: {
    label: 'Outcome',
    description: 'The result you want to achieve',
    color: 'text-purple-400',
  },
  Lead: {
    label: 'Leading',
    description: 'Predictive behaviors that drive outcomes',
    color: 'text-blue-400',
  },
  Constraint: {
    label: 'Constraint',
    description: 'Guardrail metric - what not to sacrifice',
    color: 'text-orange-400',
  },
}
