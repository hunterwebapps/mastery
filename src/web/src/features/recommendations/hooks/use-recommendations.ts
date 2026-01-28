import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { recommendationsApi } from '../api'
import type { RecommendationContext } from '@/types'

export const recommendationKeys = {
  all: ['recommendations'] as const,
  active: (context?: RecommendationContext) => [...recommendationKeys.all, 'active', { context }] as const,
  details: () => [...recommendationKeys.all, 'detail'] as const,
  detail: (id: string) => [...recommendationKeys.details(), id] as const,
  history: (fromDate?: string, toDate?: string) => [...recommendationKeys.all, 'history', { fromDate, toDate }] as const,
}

export function useActiveRecommendations(context?: RecommendationContext) {
  return useQuery({
    queryKey: recommendationKeys.active(context),
    queryFn: () => recommendationsApi.getActiveRecommendations(context),
  })
}

export function useRecommendation(id: string) {
  return useQuery({
    queryKey: recommendationKeys.detail(id),
    queryFn: () => recommendationsApi.getRecommendationById(id),
    enabled: !!id,
  })
}

export function useRecommendationHistory(fromDate?: string, toDate?: string) {
  return useQuery({
    queryKey: recommendationKeys.history(fromDate, toDate),
    queryFn: () => recommendationsApi.getRecommendationHistory(fromDate, toDate),
  })
}

export function useGenerateRecommendations() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (context: string) => recommendationsApi.generateRecommendations(context),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: recommendationKeys.all })
    },
  })
}

export function useAcceptRecommendation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => recommendationsApi.acceptRecommendation(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: recommendationKeys.all })
    },
  })
}

export function useDismissRecommendation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason?: string }) =>
      recommendationsApi.dismissRecommendation(id, reason),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: recommendationKeys.all })
    },
  })
}

export function useSnoozeRecommendation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => recommendationsApi.snoozeRecommendation(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: recommendationKeys.all })
    },
  })
}
