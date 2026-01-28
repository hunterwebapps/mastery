// Enums
export type CheckInType = 'Morning' | 'Evening'
export type CheckInStatus = 'Draft' | 'Completed' | 'Skipped'
export type Top1Type = 'Task' | 'Habit' | 'Project' | 'FreeText'
export type BlockerCategory = 'TooTired' | 'NoTime' | 'Forgot' | 'Environment' | 'Conflict' | 'Sickness' | 'Other'

// Full Check-in DTO
export interface CheckInDto {
  id: string
  userId: string
  checkInDate: string
  type: CheckInType
  status: CheckInStatus
  completedAt?: string
  // Morning fields
  energyLevel?: number
  selectedMode?: string
  top1Type?: Top1Type
  top1EntityId?: string
  top1FreeText?: string
  intention?: string
  // Evening fields
  energyLevelPm?: number
  stressLevel?: number
  reflection?: string
  blockerCategory?: BlockerCategory
  blockerNote?: string
  top1Completed?: boolean
  // Audit
  createdAt: string
  modifiedAt?: string
}

// Lightweight summary for list views
export interface CheckInSummaryDto {
  id: string
  checkInDate: string
  type: CheckInType
  status: CheckInStatus
  energyLevel?: number
  energyLevelPm?: number
  selectedMode?: string
  top1Completed?: boolean
  completedAt?: string
}

// Today's state for the daily loop
export interface TodayCheckInStateDto {
  morningCheckIn?: CheckInDto
  eveningCheckIn?: CheckInDto
  morningStatus: string
  eveningStatus: string
  checkInStreakDays: number
}

// Request types
export interface SubmitMorningCheckInRequest {
  energyLevel: number
  selectedMode: string
  top1Type?: Top1Type
  top1EntityId?: string
  top1FreeText?: string
  intention?: string
  checkInDate?: string
}

export interface SubmitEveningCheckInRequest {
  top1Completed?: boolean
  energyLevelPm?: number
  stressLevel?: number
  reflection?: string
  blockerCategory?: BlockerCategory
  blockerNote?: string
  checkInDate?: string
}

export interface UpdateCheckInRequest {
  // Morning fields
  energyLevel?: number
  selectedMode?: string
  top1Type?: Top1Type
  top1EntityId?: string
  top1FreeText?: string
  intention?: string
  // Evening fields
  top1Completed?: boolean
  energyLevelPm?: number
  stressLevel?: number
  reflection?: string
  blockerCategory?: BlockerCategory
  blockerNote?: string
}

export interface SkipCheckInRequest {
  type: CheckInType
  checkInDate?: string
}

// UI Helpers
export const energyLevelInfo: Record<number, {
  label: string
  color: string
  bgColor: string
  emoji: string
}> = {
  1: {
    label: 'Exhausted',
    color: 'text-red-400',
    bgColor: 'bg-red-500/10',
    emoji: '🔴',
  },
  2: {
    label: 'Low',
    color: 'text-orange-400',
    bgColor: 'bg-orange-500/10',
    emoji: '🟠',
  },
  3: {
    label: 'Moderate',
    color: 'text-yellow-400',
    bgColor: 'bg-yellow-500/10',
    emoji: '🟡',
  },
  4: {
    label: 'Good',
    color: 'text-green-400',
    bgColor: 'bg-green-500/10',
    emoji: '🟢',
  },
  5: {
    label: 'Peak',
    color: 'text-emerald-400',
    bgColor: 'bg-emerald-500/10',
    emoji: '⚡',
  },
}

export const blockerCategoryInfo: Record<BlockerCategory, {
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

export const top1TypeInfo: Record<Top1Type, {
  label: string
  description: string
}> = {
  Task: { label: 'Task', description: 'A specific task to complete' },
  Habit: { label: 'Habit', description: 'A habit to focus on' },
  Project: { label: 'Project', description: 'A project to advance' },
  FreeText: { label: 'Custom', description: 'Write your own priority' },
}
