import { z } from 'zod'

export const scheduleSchema = z.object({
  type: z.enum(['Daily', 'DaysOfWeek', 'WeeklyFrequency', 'Interval']),
  daysOfWeek: z.array(z.number().min(0).max(6)).optional(),
  preferredTimes: z.array(z.string()).optional(),
  frequencyPerWeek: z.number().min(1).max(7).optional(),
  intervalDays: z.number().min(2).max(30).optional(),
  startDate: z.string().optional(),
  endDate: z.string().optional(),
}).superRefine((data, ctx) => {
  // Validate schedule type-specific fields
  if (data.type === 'DaysOfWeek' && (!data.daysOfWeek || data.daysOfWeek.length === 0)) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      message: 'Please select at least one day',
      path: ['daysOfWeek'],
    })
  }
  if (data.type === 'WeeklyFrequency' && !data.frequencyPerWeek) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      message: 'Please set how many times per week',
      path: ['frequencyPerWeek'],
    })
  }
  if (data.type === 'Interval' && !data.intervalDays) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      message: 'Please set the interval in days',
      path: ['intervalDays'],
    })
  }
})

export const policySchema = z.object({
  allowLateCompletion: z.boolean().optional(),
  lateCutoffTime: z.string().optional(),
  allowSkip: z.boolean().optional(),
  requireMissReason: z.boolean().optional(),
  allowBackfill: z.boolean().optional(),
  maxBackfillDays: z.number().min(0).max(30).optional(),
})

export const variantSchema = z.object({
  mode: z.enum(['Full', 'Maintenance', 'Minimum']),
  label: z.string().min(1, 'Label is required'),
  defaultValue: z.number().min(0),
  estimatedMinutes: z.number().min(1).max(480),
  energyCost: z.number().min(1).max(5),
  countsAsCompletion: z.boolean(),
})

export const metricBindingSchema = z.object({
  metricDefinitionId: z.string().uuid(),
  contributionType: z.enum(['BooleanAs1', 'FixedValue', 'UseEnteredValue']),
  fixedValue: z.number().optional(),
  notes: z.string().optional(),
})

export const createHabitSchema = z.object({
  title: z.string().min(1, 'Title is required').max(200),
  description: z.string().max(1000).optional(),
  why: z.string().max(500).optional(),
  schedule: scheduleSchema,
  policy: policySchema.optional(),
  defaultMode: z.enum(['Full', 'Maintenance', 'Minimum']),
  variants: z.array(variantSchema).optional(),
  metricBindings: z.array(metricBindingSchema).optional(),
  roleIds: z.array(z.string().uuid()).optional(),
  valueIds: z.array(z.string().uuid()).optional(),
  goalIds: z.array(z.string().uuid()).optional(),
})

export type CreateHabitFormData = z.infer<typeof createHabitSchema>
export type ScheduleFormData = z.infer<typeof scheduleSchema>
export type VariantFormData = z.infer<typeof variantSchema>
