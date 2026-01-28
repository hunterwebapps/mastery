import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { goalsApi } from '../api'
import type {
  CreateGoalRequest,
  UpdateGoalRequest,
  UpdateGoalStatusRequest,
  UpdateGoalScoreboardRequest,
  GoalStatus,
} from '@/types'

export const goalKeys = {
  all: ['goals'] as const,
  lists: () => [...goalKeys.all, 'list'] as const,
  list: (status?: GoalStatus) => [...goalKeys.lists(), { status }] as const,
  details: () => [...goalKeys.all, 'detail'] as const,
  detail: (id: string) => [...goalKeys.details(), id] as const,
}

export function useGoals(status?: GoalStatus) {
  return useQuery({
    queryKey: goalKeys.list(status),
    queryFn: () => goalsApi.getGoals(status),
  })
}

export function useGoal(id: string) {
  return useQuery({
    queryKey: goalKeys.detail(id),
    queryFn: () => goalsApi.getGoalById(id),
    enabled: !!id,
  })
}

export function useCreateGoal() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: CreateGoalRequest) => goalsApi.createGoal(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: goalKeys.lists() })
    },
  })
}

export function useUpdateGoal() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateGoalRequest }) =>
      goalsApi.updateGoal(id, request),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: goalKeys.lists() })
      queryClient.invalidateQueries({ queryKey: goalKeys.detail(id) })
    },
  })
}

export function useUpdateGoalStatus() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateGoalStatusRequest }) =>
      goalsApi.updateGoalStatus(id, request),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: goalKeys.lists() })
      queryClient.invalidateQueries({ queryKey: goalKeys.detail(id) })
    },
  })
}

export function useUpdateGoalScoreboard() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateGoalScoreboardRequest }) =>
      goalsApi.updateGoalScoreboard(id, request),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: goalKeys.detail(id) })
    },
  })
}

export function useDeleteGoal() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => goalsApi.deleteGoal(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: goalKeys.lists() })
    },
  })
}
