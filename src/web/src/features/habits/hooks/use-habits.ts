import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { habitsApi } from '../api/habits-api'
import type {
  HabitStatus,
  TodayHabitDto,
  CreateHabitRequest,
  UpdateHabitRequest,
  CompleteOccurrenceRequest,
  SkipOccurrenceRequest,
} from '@/types/habit'

export const habitKeys = {
  all: ['habits'] as const,
  lists: () => [...habitKeys.all, 'list'] as const,
  list: (status?: HabitStatus) => [...habitKeys.lists(), { status }] as const,
  details: () => [...habitKeys.all, 'detail'] as const,
  detail: (id: string) => [...habitKeys.details(), id] as const,
  today: () => [...habitKeys.all, 'today'] as const,
  history: (id: string) => [...habitKeys.all, 'history', id] as const,
  stats: (id: string) => [...habitKeys.all, 'stats', id] as const,
}

export function useHabits(status?: HabitStatus) {
  return useQuery({
    queryKey: habitKeys.list(status),
    queryFn: () => habitsApi.getHabits(status),
  })
}

export function useHabit(id: string) {
  return useQuery({
    queryKey: habitKeys.detail(id),
    queryFn: () => habitsApi.getHabitById(id),
    enabled: !!id,
  })
}

export function useTodayHabits() {
  return useQuery({
    queryKey: habitKeys.today(),
    queryFn: () => habitsApi.getTodayHabits(),
    // Refetch on window focus for fresh data
    refetchOnWindowFocus: true,
  })
}

export function useHabitHistory(id: string, fromDate: string, toDate: string) {
  return useQuery({
    queryKey: [...habitKeys.history(id), { fromDate, toDate }],
    queryFn: () => habitsApi.getHabitHistory(id, fromDate, toDate),
    enabled: !!id && !!fromDate && !!toDate,
  })
}

export function useHabitStats(id: string) {
  return useQuery({
    queryKey: habitKeys.stats(id),
    queryFn: () => habitsApi.getHabitStats(id),
    enabled: !!id,
  })
}

export function useCreateHabit() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: CreateHabitRequest) => habitsApi.createHabit(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: habitKeys.lists() })
      queryClient.invalidateQueries({ queryKey: habitKeys.today() })
    },
  })
}

export function useUpdateHabit() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateHabitRequest }) =>
      habitsApi.updateHabit(id, request),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: habitKeys.lists() })
      queryClient.invalidateQueries({ queryKey: habitKeys.detail(id) })
      queryClient.invalidateQueries({ queryKey: habitKeys.today() })
    },
  })
}

export function useUpdateHabitStatus() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, status }: { id: string; status: string }) =>
      habitsApi.updateHabitStatus(id, status),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: habitKeys.lists() })
      queryClient.invalidateQueries({ queryKey: habitKeys.detail(id) })
      queryClient.invalidateQueries({ queryKey: habitKeys.today() })
    },
  })
}

export function useDeleteHabit() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => habitsApi.deleteHabit(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: habitKeys.lists() })
      queryClient.invalidateQueries({ queryKey: habitKeys.today() })
    },
  })
}

/**
 * Complete a habit occurrence with optimistic update for instant feedback.
 */
export function useCompleteOccurrence() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({
      habitId,
      date,
      request,
    }: {
      habitId: string
      date: string
      request?: CompleteOccurrenceRequest
    }) => habitsApi.completeOccurrence(habitId, date, request),

    // Optimistic update for instant feedback
    onMutate: async ({ habitId, date, request }) => {
      // Cancel any outgoing refetches
      await queryClient.cancelQueries({ queryKey: habitKeys.today() })

      // Snapshot the previous value
      const previousHabits = queryClient.getQueryData<TodayHabitDto[]>(habitKeys.today())

      // Optimistically update
      queryClient.setQueryData<TodayHabitDto[]>(habitKeys.today(), (old) =>
        old?.map((h) =>
          h.id === habitId
            ? {
                ...h,
                currentStreak: h.currentStreak + 1,
                todayOccurrence: {
                  id: crypto.randomUUID(),
                  habitId,
                  scheduledOn: date,
                  status: 'Completed' as const,
                  completedAt: new Date().toISOString(),
                  completedOn: date,
                  modeUsed: request?.mode ?? h.defaultMode,
                  enteredValue: request?.value,
                  note: request?.note,
                },
              }
            : h
        )
      )

      return { previousHabits }
    },

    onError: (_err, _variables, context) => {
      // Rollback on error
      if (context?.previousHabits) {
        queryClient.setQueryData(habitKeys.today(), context.previousHabits)
      }
    },

    onSettled: (_, __, { habitId }) => {
      // Refetch after success or error
      queryClient.invalidateQueries({ queryKey: habitKeys.today() })
      queryClient.invalidateQueries({ queryKey: habitKeys.detail(habitId) })
      queryClient.invalidateQueries({ queryKey: habitKeys.stats(habitId) })
    },
  })
}

/**
 * Undo a completed occurrence with optimistic update.
 */
export function useUndoOccurrence() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ habitId, date }: { habitId: string; date: string }) =>
      habitsApi.undoOccurrence(habitId, date),

    onMutate: async ({ habitId }) => {
      await queryClient.cancelQueries({ queryKey: habitKeys.today() })
      const previousHabits = queryClient.getQueryData<TodayHabitDto[]>(habitKeys.today())

      queryClient.setQueryData<TodayHabitDto[]>(habitKeys.today(), (old) =>
        old?.map((h) =>
          h.id === habitId
            ? {
                ...h,
                currentStreak: Math.max(0, h.currentStreak - 1),
                todayOccurrence: h.todayOccurrence
                  ? { ...h.todayOccurrence, status: 'Pending' as const, completedAt: undefined }
                  : undefined,
              }
            : h
        )
      )

      return { previousHabits }
    },

    onError: (_err, _variables, context) => {
      if (context?.previousHabits) {
        queryClient.setQueryData(habitKeys.today(), context.previousHabits)
      }
    },

    onSettled: (_, __, { habitId }) => {
      queryClient.invalidateQueries({ queryKey: habitKeys.today() })
      queryClient.invalidateQueries({ queryKey: habitKeys.detail(habitId) })
      queryClient.invalidateQueries({ queryKey: habitKeys.stats(habitId) })
    },
  })
}

/**
 * Skip a habit occurrence with optimistic update.
 */
export function useSkipOccurrence() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({
      habitId,
      date,
      request,
    }: {
      habitId: string
      date: string
      request?: SkipOccurrenceRequest
    }) => habitsApi.skipOccurrence(habitId, date, request),

    onMutate: async ({ habitId, date, request }) => {
      await queryClient.cancelQueries({ queryKey: habitKeys.today() })
      const previousHabits = queryClient.getQueryData<TodayHabitDto[]>(habitKeys.today())

      queryClient.setQueryData<TodayHabitDto[]>(habitKeys.today(), (old) =>
        old?.map((h) =>
          h.id === habitId
            ? {
                ...h,
                todayOccurrence: {
                  id: crypto.randomUUID(),
                  habitId,
                  scheduledOn: date,
                  status: 'Skipped' as const,
                  note: request?.reason,
                },
              }
            : h
        )
      )

      return { previousHabits }
    },

    onError: (_err, _variables, context) => {
      if (context?.previousHabits) {
        queryClient.setQueryData(habitKeys.today(), context.previousHabits)
      }
    },

    onSettled: (_, __, { habitId }) => {
      queryClient.invalidateQueries({ queryKey: habitKeys.today() })
      queryClient.invalidateQueries({ queryKey: habitKeys.detail(habitId) })
    },
  })
}
