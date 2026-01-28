import { apiClient } from '@/lib/api-client'
import type {
  ExperimentDto,
  ExperimentSummaryDto,
  CreateExperimentRequest,
  UpdateExperimentRequest,
  CompleteExperimentRequest,
  AbandonExperimentRequest,
  AddExperimentNoteRequest,
} from '@/types'

export const experimentsApi = {
  getExperiments: async (status?: string): Promise<ExperimentSummaryDto[]> => {
    const params = status ? { status } : undefined
    const { data } = await apiClient.get<ExperimentSummaryDto[]>('/experiments', { params })
    return data
  },

  getExperimentById: async (id: string): Promise<ExperimentDto> => {
    const { data } = await apiClient.get<ExperimentDto>(`/experiments/${id}`)
    return data
  },

  getActiveExperiment: async (): Promise<ExperimentDto | null> => {
    const response = await apiClient.get<ExperimentDto>('/experiments/active')
    return response.status === 204 ? null : response.data
  },

  createExperiment: async (request: CreateExperimentRequest): Promise<string> => {
    const { data } = await apiClient.post<string>('/experiments', request)
    return data
  },

  updateExperiment: async (id: string, request: UpdateExperimentRequest): Promise<void> => {
    await apiClient.put(`/experiments/${id}`, request)
  },

  startExperiment: async (id: string): Promise<void> => {
    await apiClient.put(`/experiments/${id}/start`)
  },

  pauseExperiment: async (id: string): Promise<void> => {
    await apiClient.put(`/experiments/${id}/pause`)
  },

  resumeExperiment: async (id: string): Promise<void> => {
    await apiClient.put(`/experiments/${id}/resume`)
  },

  completeExperiment: async (id: string, request: CompleteExperimentRequest): Promise<void> => {
    await apiClient.put(`/experiments/${id}/complete`, request)
  },

  abandonExperiment: async (id: string, request: AbandonExperimentRequest): Promise<void> => {
    await apiClient.put(`/experiments/${id}/abandon`, request)
  },

  addNote: async (id: string, request: AddExperimentNoteRequest): Promise<string> => {
    const { data } = await apiClient.post<string>(`/experiments/${id}/notes`, request)
    return data
  },
}
