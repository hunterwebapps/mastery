import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { checkInsApi } from '../api/check-ins-api'
import type {
  SubmitMorningCheckInRequest,
  SubmitEveningCheckInRequest,
  UpdateCheckInRequest,
  SkipCheckInRequest,
} from '@/types/check-in'

export const checkInKeys = {
  all: ['check-ins'] as const,
  today: () => [...checkInKeys.all, 'today'] as const,
  lists: () => [...checkInKeys.all, 'list'] as const,
  list: (fromDate?: string, toDate?: string) => [...checkInKeys.lists(), { fromDate, toDate }] as const,
  details: () => [...checkInKeys.all, 'detail'] as const,
  detail: (id: string) => [...checkInKeys.details(), id] as const,
}

export function useTodayCheckInState() {
  return useQuery({
    queryKey: checkInKeys.today(),
    queryFn: () => checkInsApi.getTodayState(),
    refetchOnWindowFocus: true,
  })
}

export function useCheckIns(fromDate?: string, toDate?: string) {
  return useQuery({
    queryKey: checkInKeys.list(fromDate, toDate),
    queryFn: () => checkInsApi.getCheckIns(fromDate, toDate),
  })
}

export function useCheckIn(id: string) {
  return useQuery({
    queryKey: checkInKeys.detail(id),
    queryFn: () => checkInsApi.getCheckInById(id),
    enabled: !!id,
  })
}

export function useSubmitMorningCheckIn() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: SubmitMorningCheckInRequest) =>
      checkInsApi.submitMorning(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: checkInKeys.today() })
      queryClient.invalidateQueries({ queryKey: checkInKeys.lists() })
    },
  })
}

export function useSubmitEveningCheckIn() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: SubmitEveningCheckInRequest) =>
      checkInsApi.submitEvening(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: checkInKeys.today() })
      queryClient.invalidateQueries({ queryKey: checkInKeys.lists() })
    },
  })
}

export function useUpdateCheckIn() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateCheckInRequest }) =>
      checkInsApi.updateCheckIn(id, request),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: checkInKeys.today() })
      queryClient.invalidateQueries({ queryKey: checkInKeys.detail(id) })
      queryClient.invalidateQueries({ queryKey: checkInKeys.lists() })
    },
  })
}

export function useSkipCheckIn() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: SkipCheckInRequest) =>
      checkInsApi.skipCheckIn(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: checkInKeys.today() })
      queryClient.invalidateQueries({ queryKey: checkInKeys.lists() })
    },
  })
}
