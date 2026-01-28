import { z } from 'zod'

export const generateRecommendationsSchema = z.object({
  context: z.enum(['Onboarding', 'MorningCheckIn', 'Midday', 'EveningCheckIn', 'WeeklyReview', 'DriftAlert']),
})

export const dismissRecommendationSchema = z.object({
  reason: z.string().max(2000).optional(),
})

export type GenerateRecommendationsFormData = z.infer<typeof generateRecommendationsSchema>
export type DismissRecommendationFormData = z.infer<typeof dismissRecommendationSchema>
