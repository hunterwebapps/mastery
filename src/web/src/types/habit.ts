// Enums
export type HabitStatus = 'Active' | 'Paused' | 'Archived'
export type HabitOccurrenceStatus = 'Pending' | 'Completed' | 'Missed' | 'Skipped' | 'Rescheduled'
export type ScheduleType = 'Daily' | 'DaysOfWeek' | 'WeeklyFrequency' | 'Interval'
export type HabitContributionType = 'BooleanAs1' | 'FixedValue' | 'UseEnteredValue'
export type HabitMode = 'Full' | 'Maintenance' | 'Minimum'
export type MissReason = 'TooTired' | 'NoTime' | 'Forgot' | 'Environment' | 'Conflict' | 'Sickness' | 'Other'

// Schedule DTO
export interface HabitScheduleDto {
  type: ScheduleType
  daysOfWeek?: number[]
  preferredTimes?: string[]
  frequencyPerWeek?: number
  intervalDays?: number
  startDate: string
  endDate?: string
}

// Policy DTO
export interface HabitPolicyDto {
  allowLateCompletion: boolean
  lateCutoffTime?: string
  allowSkip: boolean
  requireMissReason: boolean
  allowBackfill: boolean
  maxBackfillDays: number
}

// Metric Binding DTO
export interface HabitMetricBindingDto {
  id: string
  metricDefinitionId: string
  metricName?: string
  contributionType: HabitContributionType
  fixedValue?: number
  notes?: string
}

// Variant DTO
export interface HabitVariantDto {
  id: string
  mode: HabitMode
  label: string
  defaultValue: number
  estimatedMinutes: number
  energyCost: number
  countsAsCompletion: boolean
}

// Occurrence DTO
export interface HabitOccurrenceDto {
  id: string
  habitId: string
  scheduledOn: string
  status: HabitOccurrenceStatus
  completedAt?: string
  completedOn?: string
  modeUsed?: HabitMode
  enteredValue?: number
  missReason?: MissReason
  note?: string
  rescheduledTo?: string
}

// Full Habit DTO
export interface HabitDto {
  id: string
  userId: string
  title: string
  description?: string
  why?: string
  status: HabitStatus
  displayOrder: number
  schedule: HabitScheduleDto
  policy: HabitPolicyDto
  defaultMode: HabitMode
  roleIds: string[]
  valueIds: string[]
  goalIds: string[]
  metricBindings: HabitMetricBindingDto[]
  variants: HabitVariantDto[]
  currentStreak: number
  adherenceRate7Day: number
  createdAt: string
  modifiedAt?: string
}

// Summary DTO for list views
export interface HabitSummaryDto {
  id: string
  title: string
  description?: string
  status: HabitStatus
  defaultMode: HabitMode
  displayOrder: number
  scheduleType: ScheduleType
  scheduleDescription: string
  metricBindingCount: number
  variantCount: number
  currentStreak: number
  adherenceRate7Day: number
  createdAt: string
}

// Today projection - optimized for daily loop
export interface TodayHabitDto {
  id: string
  title: string
  description?: string
  isDue: boolean
  defaultMode: HabitMode
  todayOccurrence?: HabitOccurrenceDto
  variants: HabitVariantDto[]
  currentStreak: number
  adherenceRate7Day: number
  goalImpactTags: string[]
  requiresValueEntry: boolean
  displayOrder: number
}

// Stats DTO
export interface HabitStatsDto {
  habitId: string
  currentStreak: number
  longestStreak: number
  adherenceRate7Day: number
  adherenceRate30Day: number
  totalCompletions: number
  totalMissed: number
  totalSkipped: number
  completionsByDayOfWeek: Record<string, number>
  missReasonDistribution: Record<string, number>
}

// History DTO
export interface HabitHistoryDto {
  habitId: string
  fromDate: string
  toDate: string
  occurrences: HabitOccurrenceDto[]
  totalDue: number
  totalCompleted: number
  totalMissed: number
  totalSkipped: number
}

// Request types
export interface CreateHabitScheduleRequest {
  type: ScheduleType
  daysOfWeek?: number[]
  preferredTimes?: string[]
  frequencyPerWeek?: number
  intervalDays?: number
  startDate?: string
  endDate?: string
}

export interface CreateHabitPolicyRequest {
  allowLateCompletion?: boolean
  lateCutoffTime?: string
  allowSkip?: boolean
  requireMissReason?: boolean
  allowBackfill?: boolean
  maxBackfillDays?: number
}

export interface CreateHabitMetricBindingRequest {
  metricDefinitionId: string
  contributionType: HabitContributionType
  fixedValue?: number
  notes?: string
}

export interface CreateHabitVariantRequest {
  mode: HabitMode
  label: string
  defaultValue: number
  estimatedMinutes: number
  energyCost: number
  countsAsCompletion?: boolean
}

export interface CreateHabitRequest {
  title: string
  schedule: CreateHabitScheduleRequest
  description?: string
  why?: string
  policy?: CreateHabitPolicyRequest
  defaultMode?: HabitMode
  metricBindings?: CreateHabitMetricBindingRequest[]
  variants?: CreateHabitVariantRequest[]
  roleIds?: string[]
  valueIds?: string[]
  goalIds?: string[]
}

export interface UpdateHabitRequest {
  title?: string
  description?: string
  why?: string
  defaultMode?: HabitMode
  schedule?: CreateHabitScheduleRequest
  policy?: CreateHabitPolicyRequest
  roleIds?: string[]
  valueIds?: string[]
  goalIds?: string[]
}

export interface CompleteOccurrenceRequest {
  mode?: HabitMode
  value?: number
  note?: string
}

export interface SkipOccurrenceRequest {
  reason?: string
}

// UI Helpers
export const habitStatusInfo: Record<HabitStatus, {
  label: string
  description: string
  color: string
  bgColor: string
}> = {
  Active: {
    label: 'Active',
    description: 'Currently tracking',
    color: 'text-green-400',
    bgColor: 'bg-green-500/10',
  },
  Paused: {
    label: 'Paused',
    description: 'Temporarily paused',
    color: 'text-yellow-400',
    bgColor: 'bg-yellow-500/10',
  },
  Archived: {
    label: 'Archived',
    description: 'No longer tracking',
    color: 'text-muted-foreground',
    bgColor: 'bg-muted/50',
  },
}

export const habitModeInfo: Record<HabitMode, {
  label: string
  description: string
  color: string
  bgColor: string
}> = {
  Full: {
    label: 'Full',
    description: 'Complete version',
    color: 'text-blue-400',
    bgColor: 'bg-blue-500/10',
  },
  Maintenance: {
    label: 'Maintenance',
    description: 'Reduced version',
    color: 'text-yellow-400',
    bgColor: 'bg-yellow-500/10',
  },
  Minimum: {
    label: 'Minimum',
    description: 'Bare minimum',
    color: 'text-orange-400',
    bgColor: 'bg-orange-500/10',
  },
}

export const missReasonInfo: Record<MissReason, {
  label: string
  emoji: string
}> = {
  TooTired: { label: 'Too tired', emoji: '😴' },
  NoTime: { label: 'No time', emoji: '⏰' },
  Forgot: { label: 'Forgot', emoji: '🤔' },
  Environment: { label: 'Environment', emoji: '🏠' },
  Conflict: { label: 'Conflict', emoji: '⚡' },
  Sickness: { label: 'Sick', emoji: '🤒' },
  Other: { label: 'Other', emoji: '❓' },
}

export const scheduleTypeInfo: Record<ScheduleType, {
  label: string
  description: string
}> = {
  Daily: { label: 'Daily', description: 'Every day' },
  DaysOfWeek: { label: 'Specific Days', description: 'On selected days of the week' },
  WeeklyFrequency: { label: 'X Times/Week', description: 'Flexible days, set frequency' },
  Interval: { label: 'Every N Days', description: 'Repeat at regular intervals' },
}

export const contributionTypeInfo: Record<HabitContributionType, {
  label: string
  description: string
}> = {
  BooleanAs1: { label: 'Boolean (1)', description: 'Each completion adds 1' },
  FixedValue: { label: 'Fixed Value', description: 'Each completion adds a fixed amount' },
  UseEnteredValue: { label: 'Entered Value', description: 'User enters value at completion' },
}
