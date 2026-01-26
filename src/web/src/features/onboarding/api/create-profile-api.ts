import { apiClient } from '@/lib/api-client'
import type {
  UserValueDto,
  UserRoleDto,
  PreferencesDto,
  ConstraintsDto,
  CreateSeasonRequest,
} from '@/types'

export interface CreateProfileRequest {
  timezone: string
  locale: string
  values: UserValueDto[]
  roles: UserRoleDto[]
  preferences: PreferencesDto
  constraints: ConstraintsDto
  initialSeason?: CreateSeasonRequest
}

export const createProfileApi = {
  /**
   * Creates a new user profile during onboarding.
   */
  createProfile: async (request: CreateProfileRequest): Promise<string> => {
    const { data } = await apiClient.post<string>('/user-profile', request)
    return data
  },
}
