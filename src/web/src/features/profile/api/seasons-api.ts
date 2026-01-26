import { apiClient } from '@/lib/api-client'
import type { SeasonDto, CreateSeasonRequest, EndSeasonRequest } from '@/types'

export const seasonsApi = {
  /**
   * Gets all seasons for the current user.
   */
  getSeasons: async (): Promise<SeasonDto[]> => {
    const { data } = await apiClient.get<SeasonDto[]>('/user-profile/seasons')
    return data
  },

  /**
   * Creates a new season (becomes the current season).
   */
  createSeason: async (request: CreateSeasonRequest): Promise<string> => {
    const { data } = await apiClient.post<string>('/user-profile/seasons', request)
    return data
  },

  /**
   * Ends a season with an optional outcome.
   */
  endSeason: async (seasonId: string, request: EndSeasonRequest): Promise<void> => {
    await apiClient.put(`/user-profile/seasons/${seasonId}/end`, request)
  },
}
