import { apiClient } from '@/lib/api-client'
import type { UserRoleDto } from '@/types'

export interface UpdateRolesRequest {
  roles: UserRoleDto[]
}

export const updateRolesApi = {
  /**
   * Updates the current user's roles.
   */
  updateRoles: async (request: UpdateRolesRequest): Promise<void> => {
    await apiClient.put('/user-profile/roles', request)
  },
}
