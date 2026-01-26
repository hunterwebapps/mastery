import type { SeasonDto } from './season'
import type { PreferencesDto } from './preferences'
import type { ConstraintsDto } from './constraints'

export interface UserProfileDto {
  id: string
  userId: string
  timezone: string
  locale: string
  onboardingVersion: number
  values: UserValueDto[]
  roles: UserRoleDto[]
  currentSeason: SeasonDto | null
  preferences: PreferencesDto
  constraints: ConstraintsDto
  createdAt: string
  modifiedAt: string | null
}

export interface UserValueDto {
  id: string
  key: string | null
  label: string
  rank: number
  weight: number | null
  notes: string | null
  source: string | null
}

export interface UserRoleDto {
  id: string
  key: string | null
  label: string
  rank: number
  seasonPriority: number
  minWeeklyMinutes: number
  targetWeeklyMinutes: number
  tags: string[]
  status: RoleStatus
}

export type RoleStatus = 'Active' | 'Inactive'

// Re-export from other type files for convenience
export type { SeasonDto, SeasonType } from './season'
export type { PreferencesDto, PlanningDefaultsDto, PrivacySettingsDto } from './preferences'
export type { ConstraintsDto, BlockedWindowDto, TimeWindowDto } from './constraints'
