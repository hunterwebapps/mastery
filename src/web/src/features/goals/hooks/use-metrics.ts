import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { metricsApi } from '../api'
import type {
  CreateMetricDefinitionRequest,
  UpdateMetricDefinitionRequest,
  RecordObservationRequest,
} from '@/types'

export const metricKeys = {
  all: ['metrics'] as const,
  lists: () => [...metricKeys.all, 'list'] as const,
  list: (includeArchived?: boolean) => [...metricKeys.lists(), { includeArchived }] as const,
  details: () => [...metricKeys.all, 'detail'] as const,
  detail: (id: string) => [...metricKeys.details(), id] as const,
  observations: (id: string) => [...metricKeys.all, 'observations', id] as const,
  observationRange: (id: string, startDate: string, endDate: string) =>
    [...metricKeys.observations(id), { startDate, endDate }] as const,
}

export function useMetrics(includeArchived = false) {
  return useQuery({
    queryKey: metricKeys.list(includeArchived),
    queryFn: () => metricsApi.getMetricDefinitions(includeArchived),
  })
}

export function useMetric(id: string) {
  return useQuery({
    queryKey: metricKeys.detail(id),
    queryFn: () => metricsApi.getMetricDefinitionById(id),
    enabled: !!id,
  })
}

export function useMetricObservations(
  metricId: string,
  startDate: string,
  endDate: string,
  enabled = true
) {
  return useQuery({
    queryKey: metricKeys.observationRange(metricId, startDate, endDate),
    queryFn: () => metricsApi.getObservations(metricId, startDate, endDate),
    enabled: enabled && !!metricId && !!startDate && !!endDate,
  })
}

export function useCreateMetricDefinition() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: CreateMetricDefinitionRequest) =>
      metricsApi.createMetricDefinition(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: metricKeys.lists() })
    },
  })
}

export function useUpdateMetricDefinition() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateMetricDefinitionRequest }) =>
      metricsApi.updateMetricDefinition(id, request),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: metricKeys.lists() })
      queryClient.invalidateQueries({ queryKey: metricKeys.detail(id) })
    },
  })
}

export function useRecordObservation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ metricId, request }: { metricId: string; request: RecordObservationRequest }) =>
      metricsApi.recordObservation(metricId, request),
    onSuccess: (_, { metricId }) => {
      queryClient.invalidateQueries({ queryKey: metricKeys.observations(metricId) })
    },
  })
}
