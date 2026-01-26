import { apiClient } from '@/lib/api-client'
import type { UserProfileDto } from '@/types'
import { AxiosError } from 'axios'

export const profileApi = {
  /**
   * Gets the current user's profile.
   * Returns null if no profile exists (404).
   */
  getCurrentProfile: async (): Promise<UserProfileDto | null> => {
    try {
      const { data } = await apiClient.get<UserProfileDto>('/user-profile')
      return data
    } catch (error) {
      if (error instanceof AxiosError && error.response?.status === 404) {
        return null
      }
      throw error
    }
  },

  /**
   * Checks if the current user has a profile.
   */
  hasProfile: async (): Promise<boolean> => {
    const profile = await profileApi.getCurrentProfile()
    return profile !== null
  },
}
