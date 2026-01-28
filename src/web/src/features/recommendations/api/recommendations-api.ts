import { apiClient } from '@/lib/api-client'
import type {
  RecommendationDto,
  RecommendationSummaryDto,
} from '@/types'

export const recommendationsApi = {
  generateRecommendations: async (context: string): Promise<void> => {
    await apiClient.post('/recommendations/generate', { context })
  },

  getActiveRecommendations: async (context?: string): Promise<RecommendationSummaryDto[]> => {
    const params = context ? { context } : undefined
    const { data } = await apiClient.get<RecommendationSummaryDto[]>('/recommendations', { params })
    return data
  },

  getRecommendationById: async (id: string): Promise<RecommendationDto> => {
    const { data } = await apiClient.get<RecommendationDto>(`/recommendations/${id}`)
    return data
  },

  acceptRecommendation: async (id: string): Promise<void> => {
    await apiClient.post(`/recommendations/${id}/accept`)
  },

  dismissRecommendation: async (id: string, reason?: string): Promise<void> => {
    await apiClient.post(`/recommendations/${id}/dismiss`, { reason })
  },

  snoozeRecommendation: async (id: string): Promise<void> => {
    await apiClient.post(`/recommendations/${id}/snooze`)
  },

  getRecommendationHistory: async (fromDate?: string, toDate?: string): Promise<RecommendationSummaryDto[]> => {
    const params: Record<string, string> = {}
    if (fromDate) params.fromDate = fromDate
    if (toDate) params.toDate = toDate
    const { data } = await apiClient.get<RecommendationSummaryDto[]>('/recommendations/history', { params })
    return data
  },
}
