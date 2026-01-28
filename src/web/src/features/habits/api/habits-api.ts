import { apiClient } from '@/lib/api-client'
import type {
  HabitDto,
  HabitSummaryDto,
  TodayHabitDto,
  HabitHistoryDto,
  HabitStatsDto,
  CreateHabitRequest,
  UpdateHabitRequest,
  CompleteOccurrenceRequest,
  SkipOccurrenceRequest,
} from '@/types/habit'

export const habitsApi = {
  /**
   * Gets all habits for the current user.
   */
  getHabits: async (status?: string): Promise<HabitSummaryDto[]> => {
    const params = status ? { status } : undefined
    const { data } = await apiClient.get<HabitSummaryDto[]>('/habits', { params })
    return data
  },

  /**
   * Gets habits for today's daily loop.
   */
  getTodayHabits: async (): Promise<TodayHabitDto[]> => {
    const { data } = await apiClient.get<TodayHabitDto[]>('/habits/today')
    return data
  },

  /**
   * Gets a single habit by ID with full details.
   */
  getHabitById: async (id: string): Promise<HabitDto> => {
    const { data } = await apiClient.get<HabitDto>(`/habits/${id}`)
    return data
  },

  /**
   * Gets habit history (occurrences) for a date range.
   */
  getHabitHistory: async (
    id: string,
    fromDate: string,
    toDate: string
  ): Promise<HabitHistoryDto> => {
    const params = { fromDate, toDate }
    const { data } = await apiClient.get<HabitHistoryDto>(`/habits/${id}/history`, { params })
    return data
  },

  /**
   * Gets habit statistics.
   */
  getHabitStats: async (id: string): Promise<HabitStatsDto> => {
    const { data } = await apiClient.get<HabitStatsDto>(`/habits/${id}/stats`)
    return data
  },

  /**
   * Creates a new habit.
   */
  createHabit: async (request: CreateHabitRequest): Promise<string> => {
    const { data } = await apiClient.post<string>('/habits', request)
    return data
  },

  /**
   * Updates habit information.
   */
  updateHabit: async (id: string, request: UpdateHabitRequest): Promise<void> => {
    await apiClient.put(`/habits/${id}`, request)
  },

  /**
   * Updates habit status (activate, pause, archive).
   */
  updateHabitStatus: async (id: string, status: string): Promise<void> => {
    await apiClient.put(`/habits/${id}/status`, { newStatus: status })
  },

  /**
   * Archives a habit (soft delete).
   */
  deleteHabit: async (id: string): Promise<void> => {
    await apiClient.delete(`/habits/${id}`)
  },

  /**
   * Completes a habit occurrence.
   */
  completeOccurrence: async (
    habitId: string,
    date: string,
    request?: CompleteOccurrenceRequest
  ): Promise<void> => {
    await apiClient.post(`/habits/${habitId}/occurrences/${date}/complete`, request ?? {})
  },

  /**
   * Undoes a completed occurrence.
   */
  undoOccurrence: async (habitId: string, date: string): Promise<void> => {
    await apiClient.post(`/habits/${habitId}/occurrences/${date}/undo`)
  },

  /**
   * Skips a habit occurrence.
   */
  skipOccurrence: async (
    habitId: string,
    date: string,
    request?: SkipOccurrenceRequest
  ): Promise<void> => {
    await apiClient.post(`/habits/${habitId}/occurrences/${date}/skip`, request ?? {})
  },
}
