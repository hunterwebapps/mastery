import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { experimentsApi } from '../api'
import type {
  ExperimentStatus,
  CreateExperimentRequest,
  UpdateExperimentRequest,
  CompleteExperimentRequest,
  AbandonExperimentRequest,
  AddExperimentNoteRequest,
} from '@/types'

export const experimentKeys = {
  all: ['experiments'] as const,
  lists: () => [...experimentKeys.all, 'list'] as const,
  list: (status?: ExperimentStatus) => [...experimentKeys.lists(), { status }] as const,
  details: () => [...experimentKeys.all, 'detail'] as const,
  detail: (id: string) => [...experimentKeys.details(), id] as const,
  active: () => [...experimentKeys.all, 'active'] as const,
}

export function useExperiments(status?: ExperimentStatus) {
  return useQuery({
    queryKey: experimentKeys.list(status),
    queryFn: () => experimentsApi.getExperiments(status),
  })
}

export function useExperiment(id: string) {
  return useQuery({
    queryKey: experimentKeys.detail(id),
    queryFn: () => experimentsApi.getExperimentById(id),
    enabled: !!id,
  })
}

export function useActiveExperiment() {
  return useQuery({
    queryKey: experimentKeys.active(),
    queryFn: () => experimentsApi.getActiveExperiment(),
  })
}

export function useCreateExperiment() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (request: CreateExperimentRequest) => experimentsApi.createExperiment(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: experimentKeys.lists() })
    },
  })
}

export function useUpdateExperiment() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateExperimentRequest }) =>
      experimentsApi.updateExperiment(id, request),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: experimentKeys.lists() })
      queryClient.invalidateQueries({ queryKey: experimentKeys.detail(id) })
    },
  })
}

export function useStartExperiment() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => experimentsApi.startExperiment(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: experimentKeys.all })
    },
  })
}

export function usePauseExperiment() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => experimentsApi.pauseExperiment(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: experimentKeys.all })
    },
  })
}

export function useResumeExperiment() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => experimentsApi.resumeExperiment(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: experimentKeys.all })
    },
  })
}

export function useCompleteExperiment() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: CompleteExperimentRequest }) =>
      experimentsApi.completeExperiment(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: experimentKeys.all })
    },
  })
}

export function useAbandonExperiment() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: AbandonExperimentRequest }) =>
      experimentsApi.abandonExperiment(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: experimentKeys.all })
    },
  })
}

export function useAddExperimentNote() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: AddExperimentNoteRequest }) =>
      experimentsApi.addNote(id, request),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: experimentKeys.detail(id) })
    },
  })
}
