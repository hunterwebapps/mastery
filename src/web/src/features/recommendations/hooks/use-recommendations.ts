import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { recommendationsApi } from '../api'
import { executeClientAction } from '../lib/action-handlers'
import { storePayload } from '../lib/payload-storage'
import { buildRedirectUrl } from '../lib/url-builder'
import { goalKeys } from '@/features/goals/hooks/use-goals'
import { metricKeys } from '@/features/goals/hooks/use-metrics'
import { habitKeys } from '@/features/habits/hooks/use-habits'
import { taskKeys } from '@/features/tasks/hooks/use-tasks'
import { projectKeys } from '@/features/projects/hooks/use-projects'
import { experimentKeys } from '@/features/experiments/hooks/use-experiments'
import type {
  RecommendationContext,
  ExecutionResult,
  RecommendationDto,
  RecommendationSummaryDto,
  RecommendationActionKind,
  RecommendationTargetKind,
} from '@/types'

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

/**
 * Map of target kinds to their query keys for cache invalidation.
 */
const targetKindToQueryKeys: Partial<Record<RecommendationTargetKind, readonly unknown[]>> = {
  Goal: goalKeys.all,
  Metric: metricKeys.all,
  Habit: habitKeys.all,
  Task: taskKeys.all,
  Project: projectKeys.all,
  Experiment: experimentKeys.all,
}

/**
 * Map of target kinds to their URL path segments for navigation.
 */
const targetKindToPath: Record<string, string> = {
  Goal: 'goals',
  Task: 'tasks',
  Habit: 'habits',
  Project: 'projects',
  Experiment: 'experiments',
  Metric: 'metrics',
}

export function useAcceptRecommendation() {
  const queryClient = useQueryClient()
  const navigate = useNavigate()

  return useMutation({
    mutationFn: (recommendation: RecommendationDto | RecommendationSummaryDto) =>
      recommendationsApi.acceptRecommendation(recommendation.id),
    onSuccess: (result: ExecutionResult, recommendation: RecommendationDto | RecommendationSummaryDto) => {
      queryClient.invalidateQueries({ queryKey: recommendationKeys.all })

      if (!result.success) return

      // Server-side executed (ExecuteToday, Defer) - entity was already modified
      if (!result.requiresClientAction) {
        // Invalidate the specific entity cache
        if (result.entityKind) {
          const keys = targetKindToQueryKeys[result.entityKind as RecommendationTargetKind]
          if (keys) {
            queryClient.invalidateQueries({ queryKey: keys })
          }
        }

        // Navigate to entity detail page if we have an ID
        if (result.entityId && result.entityKind) {
          const path = targetKindToPath[result.entityKind]
          if (path) {
            setTimeout(() => navigate(`/${path}/${result.entityId}`), 300)
          }
        }
        return
      }

      // Client-side form pre-population (Create, Update, Remove)
      if (result.actionPayload) {
        try {
          const payload = JSON.parse(result.actionPayload)
          storePayload({
            recommendationId: recommendation.id,
            actionKind: result.actionKind as RecommendationActionKind,
            targetKind: result.targetKind as RecommendationTargetKind,
            payload,
            storedAt: Date.now(),
          })

          // Build redirect URL on client
          const redirectUrl = buildRedirectUrl(
            recommendation.id,
            result.actionKind as RecommendationActionKind,
            result.targetKind as RecommendationTargetKind,
            result.targetEntityId
          )
          setTimeout(() => navigate(redirectUrl), 300)
        } catch (error) {
          console.error('Failed to parse action payload:', error)
          // Fall back to legacy handler
          executeClientAction({ recommendation, result, navigate })
        }
        return
      }

      // Fallback: Use legacy client-side handler for recommendation type
      executeClientAction({ recommendation, result, navigate })
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
