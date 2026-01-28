import { z } from 'zod'

export const projectStatusSchema = z.enum(['Draft', 'Active', 'Paused', 'Completed', 'Archived'])

export const milestoneSchema = z.object({
  title: z.string().min(1, 'Title is required').max(200),
  targetDate: z.string().optional(),
  notes: z.string().max(1000).optional(),
})

export const createProjectSchema = z.object({
  title: z.string().min(1, 'Title is required').max(200),
  description: z.string().max(2000).optional(),
  priority: z.number().min(1).max(5).optional(),
  goalId: z.string().uuid().optional(),
  seasonId: z.string().uuid().optional(),
  targetEndDate: z.string().optional(),
  roleIds: z.array(z.string().uuid()).optional(),
  valueIds: z.array(z.string().uuid()).optional(),
  milestones: z.array(milestoneSchema).optional(),
})

export const updateProjectSchema = z.object({
  title: z.string().min(1, 'Title is required').max(200).optional(),
  description: z.string().max(2000).optional(),
  priority: z.number().min(1).max(5).optional(),
  goalId: z.string().uuid().optional().nullable(),
  seasonId: z.string().uuid().optional().nullable(),
  targetEndDate: z.string().optional().nullable(),
  roleIds: z.array(z.string().uuid()).optional(),
  valueIds: z.array(z.string().uuid()).optional(),
})

export const completeProjectSchema = z.object({
  outcomeNotes: z.string().max(2000).optional(),
})

export type CreateProjectFormData = z.infer<typeof createProjectSchema>
export type UpdateProjectFormData = z.infer<typeof updateProjectSchema>
export type CompleteProjectFormData = z.infer<typeof completeProjectSchema>
export type MilestoneFormData = z.infer<typeof milestoneSchema>
