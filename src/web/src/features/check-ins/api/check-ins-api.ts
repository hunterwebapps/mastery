import { apiClient } from '@/lib/api-client'
import type {
  CheckInDto,
  CheckInSummaryDto,
  TodayCheckInStateDto,
  SubmitMorningCheckInRequest,
  SubmitEveningCheckInRequest,
  UpdateCheckInRequest,
  SkipCheckInRequest,
} from '@/types/check-in'

export const checkInsApi = {
  /**
   * Submits a morning check-in.
   */
  submitMorning: async (request: SubmitMorningCheckInRequest): Promise<string> => {
    const { data } = await apiClient.post<string>('/check-ins/morning', request)
    return data
  },

  /**
   * Submits an evening check-in.
   */
  submitEvening: async (request: SubmitEveningCheckInRequest): Promise<string> => {
    const { data } = await apiClient.post<string>('/check-ins/evening', request)
    return data
  },

  /**
   * Gets today's check-in state for the daily loop.
   */
  getTodayState: async (): Promise<TodayCheckInStateDto> => {
    const { data } = await apiClient.get<TodayCheckInStateDto>('/check-ins/today')
    return data
  },

  /**
   * Gets check-in history for a date range.
   */
  getCheckIns: async (fromDate?: string, toDate?: string): Promise<CheckInSummaryDto[]> => {
    const params: Record<string, string> = {}
    if (fromDate) params.fromDate = fromDate
    if (toDate) params.toDate = toDate
    const { data } = await apiClient.get<CheckInSummaryDto[]>('/check-ins', { params })
    return data
  },

  /**
   * Gets a single check-in by ID.
   */
  getCheckInById: async (id: string): Promise<CheckInDto> => {
    const { data } = await apiClient.get<CheckInDto>(`/check-ins/${id}`)
    return data
  },

  /**
   * Updates an existing check-in.
   */
  updateCheckIn: async (id: string, request: UpdateCheckInRequest): Promise<void> => {
    await apiClient.put(`/check-ins/${id}`, request)
  },

  /**
   * Skips a check-in.
   */
  skipCheckIn: async (request: SkipCheckInRequest): Promise<string> => {
    const { data } = await apiClient.post<string>('/check-ins/skip', request)
    return data
  },
}
