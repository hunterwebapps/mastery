import { z } from 'zod'

export const metricUnitSchema = z.object({
  type: z.string().min(1).max(50),
  label: z.string().min(1).max(20),
})

export const createMetricDefinitionSchema = z.object({
  name: z.string().min(1, 'Name is required').max(100, 'Name is too long'),
  description: z.string().max(500).optional(),
  dataType: z.enum(['Number', 'Boolean', 'Duration', 'Percentage', 'Count', 'Rating']).optional(),
  unit: metricUnitSchema.optional(),
  direction: z.enum(['Increase', 'Decrease', 'Maintain']).optional(),
  defaultCadence: z.enum(['Daily', 'Weekly', 'Monthly', 'Rolling']).optional(),
  defaultAggregation: z.enum(['Sum', 'Average', 'Max', 'Min', 'Count', 'Latest']).optional(),
  tags: z.array(z.string()).optional(),
})

export const updateMetricDefinitionSchema = z.object({
  name: z.string().min(1, 'Name is required').max(100, 'Name is too long'),
  description: z.string().max(500).optional(),
  dataType: z.enum(['Number', 'Boolean', 'Duration', 'Percentage', 'Count', 'Rating']).optional(),
  unit: metricUnitSchema.optional(),
  direction: z.enum(['Increase', 'Decrease', 'Maintain']).optional(),
  defaultCadence: z.enum(['Daily', 'Weekly', 'Monthly', 'Rolling']).optional(),
  defaultAggregation: z.enum(['Sum', 'Average', 'Max', 'Min', 'Count', 'Latest']).optional(),
  isArchived: z.boolean().optional(),
  tags: z.array(z.string()).optional(),
})

export const recordObservationSchema = z.object({
  value: z.number(),
  observedOn: z.string().optional(),
  source: z.string().max(50).optional(),
  correlationId: z.string().uuid().optional(),
  note: z.string().max(500).optional(),
})

export type CreateMetricDefinitionFormData = z.infer<typeof createMetricDefinitionSchema>
export type UpdateMetricDefinitionFormData = z.infer<typeof updateMetricDefinitionSchema>
export type RecordObservationFormData = z.infer<typeof recordObservationSchema>
