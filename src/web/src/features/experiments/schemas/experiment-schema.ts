import { z } from 'zod'

export const hypothesisSchema = z.object({
  change: z.string().min(1, 'Describe what you will change').max(500),
  expectedOutcome: z.string().min(1, 'Describe the expected outcome').max(500),
  rationale: z.string().max(1000).optional(),
})

export const measurementPlanSchema = z.object({
  primaryMetricDefinitionId: z.string().uuid('Select a primary metric'),
  primaryAggregation: z.enum(['Sum', 'Average', 'Max', 'Min', 'Count', 'Latest']),
  baselineWindowDays: z.number().min(1).max(90),
  runWindowDays: z.number().min(1).max(90),
  guardrailMetricDefinitionIds: z.array(z.string().uuid()).optional(),
  minComplianceThreshold: z.number().min(0).max(1),
})

export const createExperimentSchema = z.object({
  title: z.string().min(1, 'Title is required').max(200, 'Title is too long'),
  description: z.string().max(2000).optional(),
  category: z.enum(['Habit', 'Routine', 'Environment', 'Mindset', 'Productivity', 'Health', 'Social', 'PlanRealism', 'FrictionReduction', 'CheckInConsistency', 'Top1FollowThrough', 'Other']),
  createdFrom: z.enum(['Manual', 'WeeklyReview', 'Diagnostic', 'Coaching']),
  hypothesis: hypothesisSchema,
  measurementPlan: measurementPlanSchema,
  linkedGoalIds: z.array(z.string().uuid()).optional(),
  startDate: z.string().optional(),
  endDatePlanned: z.string().optional(),
})

export const completeExperimentSchema = z.object({
  outcomeClassification: z.enum(['Success', 'PartialSuccess', 'NoEffect', 'Negative', 'Inconclusive']),
  baselineValue: z.number().optional(),
  runValue: z.number().optional(),
  complianceRate: z.number().min(0).max(1).optional(),
  narrativeSummary: z.string().max(4000).optional(),
})

export const abandonExperimentSchema = z.object({
  reason: z.string().max(2000).optional(),
})

export const addNoteSchema = z.object({
  content: z.string().min(1, 'Note content is required').max(2000),
})

export type CreateExperimentFormData = z.infer<typeof createExperimentSchema>
export type CompleteExperimentFormData = z.infer<typeof completeExperimentSchema>
export type AbandonExperimentFormData = z.infer<typeof abandonExperimentSchema>
export type AddNoteFormData = z.infer<typeof addNoteSchema>
export type HypothesisFormData = z.infer<typeof hypothesisSchema>
export type MeasurementPlanFormData = z.infer<typeof measurementPlanSchema>
