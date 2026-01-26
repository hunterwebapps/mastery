export interface ConstraintsDto {
  maxPlannedMinutesWeekday: number
  maxPlannedMinutesWeekend: number
  blockedTimeWindows: BlockedWindowDto[]
  noNotificationsWindows: TimeWindowDto[]
  healthNotes: string | null
  contentBoundaries: string[]
}

export interface BlockedWindowDto {
  label: string | null
  timeWindow: TimeWindowDto
  applicableDays: DayOfWeek[]
}

export interface TimeWindowDto {
  start: string
  end: string
}

export type DayOfWeek =
  | 'Sunday'
  | 'Monday'
  | 'Tuesday'
  | 'Wednesday'
  | 'Thursday'
  | 'Friday'
  | 'Saturday'

// Default values for new profiles
export const defaultConstraints: ConstraintsDto = {
  maxPlannedMinutesWeekday: 480, // 8 hours
  maxPlannedMinutesWeekend: 240, // 4 hours
  blockedTimeWindows: [],
  noNotificationsWindows: [],
  healthNotes: null,
  contentBoundaries: [],
}
