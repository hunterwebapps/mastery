import { apiClient } from '@/lib/api-client'
import type {
  MetricDefinitionDto,
  MetricTimeSeriesDto,
  CreateMetricDefinitionRequest,
  UpdateMetricDefinitionRequest,
  RecordObservationRequest,
} from '@/types'

export const metricsApi = {
  /**
   * Gets all metric definitions for the current user.
   */
  getMetricDefinitions: async (includeArchived = false): Promise<MetricDefinitionDto[]> => {
    const { data } = await apiClient.get<MetricDefinitionDto[]>('/metrics', {
      params: { includeArchived },
    })
    return data
  },

  /**
   * Gets a single metric definition by ID.
   */
  getMetricDefinitionById: async (id: string): Promise<MetricDefinitionDto> => {
    const { data } = await apiClient.get<MetricDefinitionDto>(`/metrics/${id}`)
    return data
  },

  /**
   * Creates a new metric definition.
   */
  createMetricDefinition: async (request: CreateMetricDefinitionRequest): Promise<string> => {
    const { data } = await apiClient.post<string>('/metrics', request)
    return data
  },

  /**
   * Updates a metric definition.
   */
  updateMetricDefinition: async (
    id: string,
    request: UpdateMetricDefinitionRequest
  ): Promise<void> => {
    await apiClient.put(`/metrics/${id}`, request)
  },

  /**
   * Records an observation for a metric.
   */
  recordObservation: async (
    metricId: string,
    request: RecordObservationRequest
  ): Promise<string> => {
    const { data } = await apiClient.post<string>(`/metrics/${metricId}/observations`, request)
    return data
  },

  /**
   * Gets observations for a metric within a date range.
   */
  getObservations: async (
    metricId: string,
    startDate: string,
    endDate: string
  ): Promise<MetricTimeSeriesDto> => {
    const { data } = await apiClient.get<MetricTimeSeriesDto>(
      `/metrics/${metricId}/observations`,
      {
        params: { startDate, endDate },
      }
    )
    return data
  },
}
