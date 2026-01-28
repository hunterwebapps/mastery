import { z } from 'zod'

export const morningCheckInSchema = z.object({
  energyLevel: z.number().min(1).max(5),
  selectedMode: z.enum(['Full', 'Maintenance', 'Minimum']),
  top1Type: z.enum(['Task', 'Habit', 'Project', 'FreeText']).optional(),
  top1EntityId: z.string().uuid().optional(),
  top1FreeText: z.string().max(200).optional(),
  intention: z.string().max(500).optional(),
}).superRefine((data, ctx) => {
  if (data.top1Type === 'FreeText' && !data.top1FreeText?.trim()) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      message: 'Please describe your Top 1 priority',
      path: ['top1FreeText'],
    })
  }
  if (
    data.top1Type &&
    data.top1Type !== 'FreeText' &&
    !data.top1EntityId
  ) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      message: 'Please select an item for your Top 1',
      path: ['top1EntityId'],
    })
  }
})

export const eveningCheckInSchema = z.object({
  top1Completed: z.boolean().optional(),
  energyLevelPm: z.number().min(1).max(5).optional(),
  stressLevel: z.number().min(1).max(5).optional(),
  reflection: z.string().max(1000).optional(),
  blockerCategory: z.enum([
    'TooTired', 'NoTime', 'Forgot', 'Environment', 'Conflict', 'Sickness', 'Other',
  ]).optional(),
  blockerNote: z.string().max(500).optional(),
})

export type MorningCheckInFormData = z.infer<typeof morningCheckInSchema>
export type EveningCheckInFormData = z.infer<typeof eveningCheckInSchema>
