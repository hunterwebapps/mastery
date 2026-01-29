import type { AppRole } from './auth'

export interface UserListDto {
  id: string
  email: string
  displayName: string | null
  authProvider: string
  roles: AppRole[]
  isDisabled: boolean
  createdAt: string
  lastLoginAt: string | null
}

export interface UserDetailDto {
  id: string
  email: string
  displayName: string | null
  authProvider: string
  roles: AppRole[]
  isDisabled: boolean
  createdAt: string
  lastLoginAt: string | null
  emailConfirmed: boolean
  hasProfile: boolean
}

export interface PaginatedList<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export interface UpdateUserRolesRequest {
  roles: AppRole[]
}

export interface SetUserDisabledRequest {
  disabled: boolean
}
