import type { HabitDto } from '@/types/habit'
import type { CreateHabitFormData } from '../schemas/habit-schema'

/**
 * Converts a HabitDto from the API to form data for the habit form.
 * Used when editing an existing habit.
 */
export function habitToFormData(habit: HabitDto): CreateHabitFormData {
  return {
    title: habit.title,
    description: habit.description ?? '',
    why: habit.why ?? '',
    schedule: {
      type: habit.schedule.type,
      daysOfWeek: habit.schedule.daysOfWeek,
      preferredTimes: habit.schedule.preferredTimes,
      frequencyPerWeek: habit.schedule.frequencyPerWeek,
      intervalDays: habit.schedule.intervalDays,
      startDate: habit.schedule.startDate,
      endDate: habit.schedule.endDate,
    },
    defaultMode: habit.defaultMode,
    policy: {
      allowLateCompletion: habit.policy.allowLateCompletion,
      lateCutoffTime: habit.policy.lateCutoffTime,
      allowSkip: habit.policy.allowSkip,
      requireMissReason: habit.policy.requireMissReason,
      allowBackfill: habit.policy.allowBackfill,
      maxBackfillDays: habit.policy.maxBackfillDays,
    },
    variants: habit.variants.map((v) => ({
      mode: v.mode,
      label: v.label,
      defaultValue: v.defaultValue,
      estimatedMinutes: v.estimatedMinutes,
      energyCost: v.energyCost,
      countsAsCompletion: v.countsAsCompletion,
    })),
    metricBindings: habit.metricBindings.map((b) => ({
      metricDefinitionId: b.metricDefinitionId,
      contributionType: b.contributionType,
      fixedValue: b.fixedValue,
      notes: b.notes,
    })),
    roleIds: habit.roleIds,
    valueIds: habit.valueIds,
    goalIds: habit.goalIds,
  }
}

/**
 * Gets the default form values for creating a new habit.
 */
export function getDefaultHabitFormData(): CreateHabitFormData {
  return {
    title: '',
    description: '',
    why: '',
    schedule: {
      type: 'Daily',
    },
    defaultMode: 'Full',
    variants: [],
  }
}
