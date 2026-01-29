import type { RecommendationTargetKind } from '@/types'
import type { ContextTag } from '@/types/task'

/**
 * Valid context tag values that the form accepts.
 */
const VALID_CONTEXT_TAGS: ContextTag[] = [
  'Computer',
  'Phone',
  'Errands',
  'Home',
  'Office',
  'DeepWork',
  'LowEnergy',
  'Anywhere',
]

/**
 * Maps various LLM context tag formats to valid form values.
 */
const CONTEXT_TAG_MAP: Record<string, ContextTag> = {
  // Exact matches
  computer: 'Computer',
  phone: 'Phone',
  errands: 'Errands',
  home: 'Home',
  office: 'Office',
  deepwork: 'DeepWork',
  deep_work: 'DeepWork',
  'deep-work': 'DeepWork',
  lowenergy: 'LowEnergy',
  low_energy: 'LowEnergy',
  'low-energy': 'LowEnergy',
  anywhere: 'Anywhere',
  // Common variations
  laptop: 'Computer',
  desktop: 'Computer',
  pc: 'Computer',
  mobile: 'Phone',
  smartphone: 'Phone',
  outside: 'Errands',
  out: 'Errands',
  work: 'Office',
  remote: 'Anywhere',
  flexible: 'Anywhere',
  focus: 'DeepWork',
  focused: 'DeepWork',
  concentration: 'DeepWork',
  easy: 'LowEnergy',
  simple: 'LowEnergy',
  light: 'LowEnergy',
}

/**
 * Normalizes a context tag from LLM output to a valid form value.
 */
function normalizeContextTag(tag: unknown): ContextTag | null {
  if (typeof tag !== 'string') return null

  // Check if it's already a valid tag (case-sensitive)
  if (VALID_CONTEXT_TAGS.includes(tag as ContextTag)) {
    return tag as ContextTag
  }

  // Try case-insensitive lookup
  const normalized = tag.toLowerCase().replace(/[\s-_]/g, '')
  const mapped = CONTEXT_TAG_MAP[normalized] || CONTEXT_TAG_MAP[tag.toLowerCase()]

  return mapped || null
}

/**
 * Transforms LLM-generated payload field names to form-compatible field names.
 * This bridges the gap between LLM output schemas and frontend form schemas.
 */
export function transformPayloadForForm(
  payload: Record<string, unknown>,
  targetKind: RecommendationTargetKind
): Record<string, unknown> {
  switch (targetKind) {
    case 'Task':
      return transformTaskPayload(payload)
    case 'Habit':
      return transformHabitPayload(payload)
    case 'Experiment':
      return transformExperimentPayload(payload)
    case 'Goal':
      return transformGoalPayload(payload)
    case 'Project':
      return transformProjectPayload(payload)
    default:
      return payload
  }
}

/**
 * Transform Task payload from LLM format to form format.
 * LLM uses: estMinutes, newTitle, newEstMinutes, newEnergyCost, etc.
 * Form uses: estimatedMinutes, title, energyCost, etc.
 */
function transformTaskPayload(payload: Record<string, unknown>): Record<string, unknown> {
  const result: Record<string, unknown> = {}

  // Direct mappings for Create action
  if (payload.title !== undefined) result.title = payload.title
  if (payload.description !== undefined) result.description = payload.description
  if (payload.estMinutes !== undefined) result.estimatedMinutes = payload.estMinutes
  if (payload.energyCost !== undefined) result.energyCost = payload.energyCost
  if (payload.priority !== undefined) result.priority = payload.priority
  if (payload.projectId !== undefined) result.projectId = payload.projectId
  if (payload.goalId !== undefined) result.goalId = payload.goalId

  // Normalize context tags
  if (payload.contextTags !== undefined && Array.isArray(payload.contextTags)) {
    const normalizedTags = payload.contextTags
      .map(normalizeContextTag)
      .filter((tag): tag is ContextTag => tag !== null)
    // Only include if we have valid tags, otherwise leave undefined
    if (normalizedTags.length > 0) {
      result.contextTags = normalizedTags
    }
  }

  // Mappings for Update action (newX → X)
  if (payload.newTitle !== undefined) result.title = payload.newTitle
  if (payload.newDescription !== undefined) result.description = payload.newDescription
  if (payload.newEstMinutes !== undefined) result.estimatedMinutes = payload.newEstMinutes
  if (payload.newEnergyCost !== undefined) result.energyCost = payload.newEnergyCost
  if (payload.newPriority !== undefined) result.priority = payload.newPriority

  return result
}

/**
 * Transform Habit payload from LLM format to form format.
 */
function transformHabitPayload(payload: Record<string, unknown>): Record<string, unknown> {
  const result: Record<string, unknown> = {}

  // Direct mappings
  if (payload.title !== undefined) result.title = payload.title
  if (payload.description !== undefined) result.description = payload.description
  if (payload.why !== undefined) result.why = payload.why

  // Schedule mappings - LLM might use different structure
  if (payload.schedule !== undefined) {
    const schedule = payload.schedule as Record<string, unknown>
    result.schedule = {
      type: schedule.type,
      daysOfWeek: schedule.daysOfWeek,
      frequencyPerWeek: schedule.frequencyPerWeek,
      intervalDays: schedule.intervalDays,
      preferredTimes: schedule.preferredTimes,
      startDate: schedule.startDate,
      endDate: schedule.endDate,
    }
  }

  // Variant mappings
  if (payload.defaultMode !== undefined) result.defaultMode = payload.defaultMode
  if (payload.variants !== undefined) result.variants = payload.variants

  // Update action mappings
  if (payload.newTitle !== undefined) result.title = payload.newTitle
  if (payload.newDescription !== undefined) result.description = payload.newDescription

  return result
}

/**
 * Transform Experiment payload from LLM format to form format.
 */
function transformExperimentPayload(payload: Record<string, unknown>): Record<string, unknown> {
  const result: Record<string, unknown> = {}

  // Direct mappings
  if (payload.title !== undefined) result.title = payload.title
  if (payload.description !== undefined) result.description = payload.description
  if (payload.category !== undefined) result.category = payload.category

  // Hypothesis mapping
  if (payload.hypothesis !== undefined) {
    result.hypothesis = payload.hypothesis
  }

  // Measurement plan mapping
  if (payload.measurementPlan !== undefined) {
    const mp = payload.measurementPlan as Record<string, unknown>
    result.measurementPlan = {
      primaryMetricDefinitionId: mp.primaryMetricDefinitionId,
      primaryAggregation: mp.primaryAggregation ?? 'Average',
      baselineWindowDays: mp.baselineWindowDays ?? 7,
      runWindowDays: mp.runWindowDays ?? 14,
      guardrailMetricDefinitionIds: mp.guardrailMetricDefinitionIds ?? [],
      minComplianceThreshold: mp.minComplianceThreshold ?? 0.7,
    }
  }

  // Update action mappings
  if (payload.newTitle !== undefined) result.title = payload.newTitle
  if (payload.newDescription !== undefined) result.description = payload.newDescription

  return result
}

/**
 * Transform Goal payload from LLM format to form format.
 */
function transformGoalPayload(payload: Record<string, unknown>): Record<string, unknown> {
  const result: Record<string, unknown> = {}

  // Direct mappings
  if (payload.title !== undefined) result.title = payload.title
  if (payload.description !== undefined) result.description = payload.description
  if (payload.why !== undefined) result.why = payload.why
  if (payload.priority !== undefined) result.priority = payload.priority
  if (payload.deadline !== undefined) result.deadline = payload.deadline
  if (payload.metrics !== undefined) result.metrics = payload.metrics

  // Update action mappings
  if (payload.newTitle !== undefined) result.title = payload.newTitle
  if (payload.newPriority !== undefined) result.priority = payload.newPriority
  if (payload.newDeadline !== undefined) result.deadline = payload.newDeadline

  return result
}

/**
 * Transform Project payload from LLM format to form format.
 */
function transformProjectPayload(payload: Record<string, unknown>): Record<string, unknown> {
  const result: Record<string, unknown> = {}

  // Direct mappings
  if (payload.title !== undefined) result.title = payload.title
  if (payload.description !== undefined) result.description = payload.description
  if (payload.priority !== undefined) result.priority = payload.priority
  if (payload.targetEndDate !== undefined) result.targetEndDate = payload.targetEndDate
  if (payload.milestones !== undefined) result.milestones = payload.milestones

  // Update action mappings
  if (payload.newTitle !== undefined) result.title = payload.newTitle
  if (payload.newPriority !== undefined) result.priority = payload.newPriority

  return result
}
