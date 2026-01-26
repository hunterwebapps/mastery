export interface SeasonDto {
  id: string
  userId: string
  label: string
  type: SeasonType
  startDate: string
  expectedEndDate: string | null
  actualEndDate: string | null
  successStatement: string | null
  nonNegotiables: string[]
  focusRoleIds: string[]
  focusGoalIds: string[]
  intensity: number
  outcome: string | null
  isEnded: boolean
  createdAt: string
  modifiedAt: string | null
}

export type SeasonType =
  | 'Sprint'
  | 'Build'
  | 'Maintain'
  | 'Recover'
  | 'Transition'
  | 'Explore'

export interface CreateSeasonRequest {
  label: string
  type: SeasonType
  startDate: string
  expectedEndDate?: string
  focusRoleIds?: string[]
  focusGoalIds?: string[]
  successStatement?: string
  nonNegotiables?: string[]
  intensity?: number
}

export interface EndSeasonRequest {
  outcome?: string
}

// Season type metadata for UI
export const seasonTypeInfo: Record<
  SeasonType,
  { label: string; description: string; color: string }
> = {
  Sprint: {
    label: 'Sprint',
    description: 'Intense focus period with aggressive planning',
    color: 'text-orange-400',
  },
  Build: {
    label: 'Build',
    description: 'Steady progress with balanced approach',
    color: 'text-blue-400',
  },
  Maintain: {
    label: 'Maintain',
    description: 'Conservative planning to protect habits',
    color: 'text-green-400',
  },
  Recover: {
    label: 'Recover',
    description: 'Rest and reset with minimal commitments',
    color: 'text-purple-400',
  },
  Transition: {
    label: 'Transition',
    description: 'Flexible planning during life changes',
    color: 'text-yellow-400',
  },
  Explore: {
    label: 'Explore',
    description: 'Low commitment discovery mode',
    color: 'text-cyan-400',
  },
}
