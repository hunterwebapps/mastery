export interface PreferencesDto {
  coachingStyle: CoachingStyle
  explanationVerbosity: VerbosityLevel
  nudgeLevel: NudgeLevel
  notificationChannels: NotificationChannel[]
  morningCheckInTime: string
  eveningCheckInTime: string
  planningDefaults: PlanningDefaultsDto
  privacy: PrivacySettingsDto
}

export interface PlanningDefaultsDto {
  defaultTaskDurationMinutes: number
  autoScheduleHabits: boolean
  bufferBetweenTasksMinutes: number
}

export interface PrivacySettingsDto {
  shareProgressWithCoach: boolean
  allowAnonymousAnalytics: boolean
}

export type CoachingStyle = 'Direct' | 'Encouraging' | 'Analytical'

export type VerbosityLevel = 'Minimal' | 'Medium' | 'Detailed'

export type NudgeLevel = 'Off' | 'Low' | 'Medium' | 'High'

export type NotificationChannel = 'Push' | 'Email' | 'SMS'

// Default values for new profiles
export const defaultPreferences: PreferencesDto = {
  coachingStyle: 'Encouraging',
  explanationVerbosity: 'Medium',
  nudgeLevel: 'Medium',
  notificationChannels: ['Push'],
  morningCheckInTime: '07:00',
  eveningCheckInTime: '21:00',
  planningDefaults: {
    defaultTaskDurationMinutes: 30,
    autoScheduleHabits: true,
    bufferBetweenTasksMinutes: 5,
  },
  privacy: {
    shareProgressWithCoach: false,
    allowAnonymousAnalytics: true,
  },
}
