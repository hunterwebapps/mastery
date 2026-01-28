import { z } from 'zod'

export const targetSchema = z.object({
  type: z.enum(['AtLeast', 'AtMost', 'Between', 'Exactly']),
  value: z.number().min(0),
  maxValue: z.number().min(0).optional(),
})

export const evaluationWindowSchema = z.object({
  windowType: z.enum(['Daily', 'Weekly', 'Monthly', 'Rolling']),
  rollingDays: z.number().min(1).max(365).optional(),
  startDay: z.number().min(0).max(6).optional(),
})

export const goalMetricSchema = z.object({
  metricDefinitionId: z.string().uuid(),
  kind: z.enum(['Lag', 'Lead', 'Constraint']),
  target: targetSchema,
  evaluationWindow: evaluationWindowSchema,
  aggregation: z.enum(['Sum', 'Average', 'Max', 'Min', 'Count', 'Latest']),
  sourceHint: z.enum(['Manual', 'Habit', 'Task', 'CheckIn', 'Integration', 'Computed']),
  weight: z.number().min(0).max(1).optional(),
  displayOrder: z.number().min(0).optional(),
  baseline: z.number().optional(),
  minimumThreshold: z.number().optional(),
})

export const createGoalSchema = z.object({
  title: z.string().min(1, 'Title is required').max(200, 'Title is too long'),
  description: z.string().max(2000).optional(),
  why: z.string().max(1000).optional(),
  priority: z.number().min(1).max(5).optional(),
  deadline: z.string().optional(),
  seasonId: z.string().uuid().optional(),
  roleIds: z.array(z.string().uuid()).optional(),
  valueIds: z.array(z.string().uuid()).optional(),
  dependencyIds: z.array(z.string().uuid()).optional(),
  metrics: z.array(goalMetricSchema).optional(),
})

export const updateGoalSchema = z.object({
  title: z.string().min(1, 'Title is required').max(200, 'Title is too long'),
  description: z.string().max(2000).optional(),
  why: z.string().max(1000).optional(),
  priority: z.number().min(1).max(5).optional(),
  deadline: z.string().optional(),
  seasonId: z.string().uuid().optional(),
  roleIds: z.array(z.string().uuid()).optional(),
  valueIds: z.array(z.string().uuid()).optional(),
  dependencyIds: z.array(z.string().uuid()).optional(),
})

export const updateGoalStatusSchema = z.object({
  newStatus: z.enum(['Draft', 'Active', 'Paused', 'Completed', 'Archived']),
  completionNotes: z.string().max(2000).optional(),
})

export type CreateGoalFormData = z.infer<typeof createGoalSchema>
export type UpdateGoalFormData = z.infer<typeof updateGoalSchema>
export type UpdateGoalStatusFormData = z.infer<typeof updateGoalStatusSchema>
export type GoalMetricFormData = z.infer<typeof goalMetricSchema>
