import { apiClient } from '@/lib/api-client'
import type {
  UserListDto,
  UserDetailDto,
  PaginatedList,
  UpdateUserRolesRequest,
  SetUserDisabledRequest,
} from '@/types'

export interface GetUsersParams {
  search?: string
  page?: number
  pageSize?: number
}

export const usersApi = {
  getUsers: async (params: GetUsersParams = {}): Promise<PaginatedList<UserListDto>> => {
    const { data } = await apiClient.get<PaginatedList<UserListDto>>('/users', { params })
    return data
  },

  getUserById: async (id: string): Promise<UserDetailDto> => {
    const { data } = await apiClient.get<UserDetailDto>(`/users/${id}`)
    return data
  },

  updateUserRoles: async (id: string, request: UpdateUserRolesRequest): Promise<void> => {
    await apiClient.put(`/users/${id}/roles`, request)
  },

  setUserDisabled: async (id: string, request: SetUserDisabledRequest): Promise<void> => {
    await apiClient.put(`/users/${id}/disable`, request)
  },
}
