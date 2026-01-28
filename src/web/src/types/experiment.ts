// Experiment Types

export type ExperimentStatus = 'Draft' | 'Active' | 'Paused' | 'Completed' | 'Abandoned' | 'Archived'
export type ExperimentCategory =
  | 'Habit' | 'Routine' | 'Environment' | 'Mindset' | 'Productivity' | 'Health' | 'Social'
  | 'PlanRealism' | 'FrictionReduction' | 'CheckInConsistency' | 'Top1FollowThrough'
  | 'Other'
export type ExperimentOutcome = 'Success' | 'PartialSuccess' | 'NoEffect' | 'Negative' | 'Inconclusive'
export type ExperimentCreatedFrom = 'Manual' | 'WeeklyReview' | 'Diagnostic' | 'Coaching'
import type { MetricAggregation } from './goal'

export interface HypothesisDto {
  change: string
  expectedOutcome: string
  rationale?: string
  summary: string
}

export interface MeasurementPlanDto {
  primaryMetricDefinitionId: string
  primaryAggregation: MetricAggregation
  baselineWindowDays: number
  runWindowDays: number
  guardrailMetricDefinitionIds: string[]
  minComplianceThreshold: number
}

export interface ExperimentNoteDto {
  id: string
  content: string
  createdAt: string
}

export interface ExperimentResultDto {
  id: string
  baselineValue?: number
  runValue?: number
  delta?: number
  deltaPercent?: number
  outcomeClassification: ExperimentOutcome
  complianceRate?: number
  narrativeSummary?: string
  computedAt: string
}

export interface ExperimentDto {
  id: string
  userId: string
  title: string
  description?: string
  category: ExperimentCategory
  status: ExperimentStatus
  createdFrom: ExperimentCreatedFrom
  hypothesis: HypothesisDto
  measurementPlan: MeasurementPlanDto
  startDate?: string
  endDatePlanned?: string
  endDateActual?: string
  linkedGoalIds: string[]
  notes: ExperimentNoteDto[]
  result?: ExperimentResultDto
  daysRemaining?: number
  daysElapsed?: number
  createdAt: string
  modifiedAt?: string
}

export interface ExperimentSummaryDto {
  id: string
  title: string
  category: ExperimentCategory
  status: ExperimentStatus
  createdFrom: ExperimentCreatedFrom
  hypothesisSummary: string
  startDate?: string
  endDatePlanned?: string
  daysRemaining?: number
  daysElapsed?: number
  outcomeClassification?: ExperimentOutcome
  noteCount: number
  hasResult: boolean
  createdAt: string
}

// Request types
export interface CreateHypothesisRequest {
  change: string
  expectedOutcome: string
  rationale?: string
}

export interface CreateMeasurementPlanRequest {
  primaryMetricDefinitionId: string
  primaryAggregation: string
  baselineWindowDays?: number
  runWindowDays?: number
  guardrailMetricDefinitionIds?: string[]
  minComplianceThreshold?: number
}

export interface CreateExperimentRequest {
  title: string
  category: ExperimentCategory
  createdFrom: ExperimentCreatedFrom
  hypothesis: CreateHypothesisRequest
  measurementPlan: CreateMeasurementPlanRequest
  description?: string
  linkedGoalIds?: string[]
  startDate?: string
  endDatePlanned?: string
}

export interface UpdateExperimentRequest {
  title?: string
  description?: string
  category?: ExperimentCategory
  hypothesis?: CreateHypothesisRequest
  measurementPlan?: CreateMeasurementPlanRequest
  linkedGoalIds?: string[]
  startDate?: string
  endDatePlanned?: string
}

export interface CompleteExperimentRequest {
  outcomeClassification: ExperimentOutcome
  baselineValue?: number
  runValue?: number
  complianceRate?: number
  narrativeSummary?: string
}

export interface AbandonExperimentRequest {
  reason?: string
}

export interface AddExperimentNoteRequest {
  content: string
}

// UI helpers
export const experimentStatusInfo: Record<ExperimentStatus, { label: string; description: string; color: string; bgColor: string; icon: string }> = {
  Draft: { label: 'Draft', description: 'Experiment is being designed', color: 'text-gray-400', bgColor: 'bg-gray-500/10', icon: 'Pencil' },
  Active: { label: 'Active', description: 'Experiment is currently running', color: 'text-green-400', bgColor: 'bg-green-500/10', icon: 'Play' },
  Paused: { label: 'Paused', description: 'Experiment is temporarily on hold', color: 'text-yellow-400', bgColor: 'bg-yellow-500/10', icon: 'Pause' },
  Completed: { label: 'Completed', description: 'Experiment has concluded with results', color: 'text-blue-400', bgColor: 'bg-blue-500/10', icon: 'CheckCircle' },
  Abandoned: { label: 'Abandoned', description: 'Experiment was stopped early', color: 'text-red-400', bgColor: 'bg-red-500/10', icon: 'XCircle' },
  Archived: { label: 'Archived', description: 'Experiment has been archived', color: 'text-zinc-500', bgColor: 'bg-zinc-500/10', icon: 'Archive' },
}

export const experimentCategoryInfo: Record<ExperimentCategory, { label: string; description: string; color: string; bgColor: string }> = {
  // Life-area categories
  Habit: { label: 'Habit', description: 'Habit formation, scaling, or modification', color: 'text-green-400', bgColor: 'bg-green-500/10' },
  Routine: { label: 'Routine', description: 'Routine or schedule changes', color: 'text-sky-400', bgColor: 'bg-sky-500/10' },
  Environment: { label: 'Environment', description: 'Context or environment changes', color: 'text-teal-400', bgColor: 'bg-teal-500/10' },
  Mindset: { label: 'Mindset', description: 'Cognitive or motivation strategies', color: 'text-violet-400', bgColor: 'bg-violet-500/10' },
  Productivity: { label: 'Productivity', description: 'Workflow or planning changes', color: 'text-blue-400', bgColor: 'bg-blue-500/10' },
  Health: { label: 'Health', description: 'Health, energy, or sleep changes', color: 'text-rose-400', bgColor: 'bg-rose-500/10' },
  Social: { label: 'Social', description: 'Social or accountability strategies', color: 'text-amber-400', bgColor: 'bg-amber-500/10' },
  // System-diagnostic categories
  PlanRealism: { label: 'Plan Realism', description: 'Testing planning accuracy', color: 'text-indigo-400', bgColor: 'bg-indigo-500/10' },
  FrictionReduction: { label: 'Friction Reduction', description: 'Reducing barriers to action', color: 'text-orange-400', bgColor: 'bg-orange-500/10' },
  CheckInConsistency: { label: 'Check-in Consistency', description: 'Improving check-in habits', color: 'text-cyan-400', bgColor: 'bg-cyan-500/10' },
  Top1FollowThrough: { label: 'Top-1 Follow Through', description: 'Improving priority execution', color: 'text-purple-400', bgColor: 'bg-purple-500/10' },
  Other: { label: 'Other', description: 'Custom experiment category', color: 'text-gray-400', bgColor: 'bg-gray-500/10' },
}

export const experimentOutcomeInfo: Record<ExperimentOutcome, { label: string; description: string; color: string; bgColor: string }> = {
  Success: { label: 'Success', description: 'Hypothesis confirmed', color: 'text-green-400', bgColor: 'bg-green-500/10' },
  PartialSuccess: { label: 'Partial Success', description: 'Some improvement observed', color: 'text-lime-400', bgColor: 'bg-lime-500/10' },
  NoEffect: { label: 'No Effect', description: 'No measurable change', color: 'text-gray-400', bgColor: 'bg-gray-500/10' },
  Negative: { label: 'Negative', description: 'Metrics worsened', color: 'text-red-400', bgColor: 'bg-red-500/10' },
  Inconclusive: { label: 'Inconclusive', description: 'Insufficient data to determine', color: 'text-yellow-400', bgColor: 'bg-yellow-500/10' },
}
