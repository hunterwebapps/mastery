import { useQuery } from '@tanstack/react-query'
import { recommendationsDebugApi } from '../api'
import type { AdminTraceFilterParams } from '@/types'

export const recommendationTraceKeys = {
  all: ['admin', 'recommendation-traces'] as const,
  lists: () => [...recommendationTraceKeys.all, 'list'] as const,
  list: (params: AdminTraceFilterParams) => [...recommendationTraceKeys.lists(), params] as const,
  details: () => [...recommendationTraceKeys.all, 'detail'] as const,
  detail: (id: string) => [...recommendationTraceKeys.details(), id] as const,
}

export function useRecommendationTraces(params: AdminTraceFilterParams = {}) {
  return useQuery({
    queryKey: recommendationTraceKeys.list(params),
    queryFn: () => recommendationsDebugApi.getTraces(params),
  })
}

export function useRecommendationTrace(id: string) {
  return useQuery({
    queryKey: recommendationTraceKeys.detail(id),
    queryFn: () => recommendationsDebugApi.getTraceById(id),
    enabled: !!id,
  })
}
