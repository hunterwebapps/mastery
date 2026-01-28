import { z } from 'zod'

export const contextTagSchema = z.enum([
  'Computer',
  'Phone',
  'Errands',
  'Home',
  'Office',
  'DeepWork',
  'LowEnergy',
  'Anywhere',
])

export const dueTypeSchema = z.enum(['Soft', 'Hard'])

export const contributionTypeSchema = z.enum([
  'BooleanAs1',
  'FixedValue',
  'UseActualMinutes',
  'UseEnteredValue',
])

export const rescheduleReasonSchema = z.enum([
  'NoTime',
  'TooTired',
  'Blocked',
  'Forgot',
  'ScopeTooBig',
  'WaitingOnSomeone',
  'Other',
])

export const taskDueSchema = z.object({
  dueOn: z.string().min(1, 'Due date is required'),
  dueAt: z.string().optional(),
  dueType: dueTypeSchema.optional(),
})

export const taskSchedulingSchema = z.object({
  scheduledOn: z.string().min(1, 'Scheduled date is required'),
  preferredTimeWindowStart: z.string().optional(),
  preferredTimeWindowEnd: z.string().optional(),
})

export const taskMetricBindingSchema = z.object({
  metricDefinitionId: z.string().uuid(),
  contributionType: contributionTypeSchema,
  fixedValue: z.number().optional(),
  notes: z.string().max(500).optional(),
}).superRefine((data, ctx) => {
  if (data.contributionType === 'FixedValue' && !data.fixedValue) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      message: 'Fixed value is required for this contribution type',
      path: ['fixedValue'],
    })
  }
})

export const createTaskSchema = z.object({
  title: z.string().min(1, 'Title is required').max(200),
  description: z.string().max(2000).optional(),
  estimatedMinutes: z.number().min(1).max(480).optional(),
  energyCost: z.number().min(1).max(5).optional(),
  priority: z.number().min(1).max(5).optional(),
  projectId: z.string().uuid().optional(),
  goalId: z.string().uuid().optional(),
  due: taskDueSchema.optional(),
  scheduling: taskSchedulingSchema.optional(),
  contextTags: z.array(contextTagSchema).optional(),
  dependencyTaskIds: z.array(z.string().uuid()).optional(),
  roleIds: z.array(z.string().uuid()).optional(),
  valueIds: z.array(z.string().uuid()).optional(),
  metricBindings: z.array(taskMetricBindingSchema).optional(),
})

export const updateTaskSchema = z.object({
  title: z.string().min(1, 'Title is required').max(200).optional(),
  description: z.string().max(2000).optional(),
  estimatedMinutes: z.number().min(1).max(480).optional(),
  energyCost: z.number().min(1).max(5).optional(),
  priority: z.number().min(1).max(5).optional(),
  projectId: z.string().uuid().optional().nullable(),
  goalId: z.string().uuid().optional().nullable(),
  due: taskDueSchema.optional().nullable(),
  contextTags: z.array(contextTagSchema).optional(),
  dependencyTaskIds: z.array(z.string().uuid()).optional(),
  roleIds: z.array(z.string().uuid()).optional(),
  valueIds: z.array(z.string().uuid()).optional(),
})

export const scheduleTaskSchema = z.object({
  scheduledOn: z.string().min(1, 'Scheduled date is required'),
  preferredTimeWindowStart: z.string().optional(),
  preferredTimeWindowEnd: z.string().optional(),
})

export const rescheduleTaskSchema = z.object({
  newDate: z.string().min(1, 'New date is required'),
  reason: rescheduleReasonSchema.optional(),
})

export const completeTaskSchema = z.object({
  completedOn: z.string().min(1, 'Completion date is required'),
  actualMinutes: z.number().min(0).max(480).optional(),
  note: z.string().max(500).optional(),
  enteredValue: z.number().optional(),
})

// Quick add schema for inbox - minimal fields
export const quickAddTaskSchema = z.object({
  title: z.string().min(1, 'Title is required').max(200),
  estimatedMinutes: z.number().min(1).max(480).optional(),
  energyCost: z.number().min(1).max(5).optional(),
  contextTags: z.array(contextTagSchema).optional(),
})

export type CreateTaskFormData = z.infer<typeof createTaskSchema>
export type UpdateTaskFormData = z.infer<typeof updateTaskSchema>
export type ScheduleTaskFormData = z.infer<typeof scheduleTaskSchema>
export type RescheduleTaskFormData = z.infer<typeof rescheduleTaskSchema>
export type CompleteTaskFormData = z.infer<typeof completeTaskSchema>
export type QuickAddTaskFormData = z.infer<typeof quickAddTaskSchema>
