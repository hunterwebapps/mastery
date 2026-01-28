import { apiClient } from '@/lib/api-client'
import type {
  GoalDto,
  GoalSummaryDto,
  CreateGoalRequest,
  UpdateGoalRequest,
  UpdateGoalStatusRequest,
  UpdateGoalScoreboardRequest,
} from '@/types'

export const goalsApi = {
  /**
   * Gets all goals for the current user.
   */
  getGoals: async (status?: string): Promise<GoalSummaryDto[]> => {
    const params = status ? { status } : undefined
    const { data } = await apiClient.get<GoalSummaryDto[]>('/goals', { params })
    return data
  },

  /**
   * Gets a single goal by ID with full details and metrics.
   */
  getGoalById: async (id: string): Promise<GoalDto> => {
    const { data } = await apiClient.get<GoalDto>(`/goals/${id}`)
    return data
  },

  /**
   * Creates a new goal.
   */
  createGoal: async (request: CreateGoalRequest): Promise<string> => {
    const { data } = await apiClient.post<string>('/goals', request)
    return data
  },

  /**
   * Updates goal basic information (title, description, etc.).
   */
  updateGoal: async (id: string, request: UpdateGoalRequest): Promise<void> => {
    await apiClient.put(`/goals/${id}`, request)
  },

  /**
   * Updates goal status (activate, pause, complete, archive).
   */
  updateGoalStatus: async (id: string, request: UpdateGoalStatusRequest): Promise<void> => {
    await apiClient.put(`/goals/${id}/status`, request)
  },

  /**
   * Updates goal scoreboard (metrics configuration).
   */
  updateGoalScoreboard: async (id: string, request: UpdateGoalScoreboardRequest): Promise<void> => {
    await apiClient.put(`/goals/${id}/scoreboard`, request)
  },

  /**
   * Archives a goal (soft delete).
   */
  deleteGoal: async (id: string): Promise<void> => {
    await apiClient.delete(`/goals/${id}`)
  },
}
