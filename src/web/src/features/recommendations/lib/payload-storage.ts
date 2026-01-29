import type { RecommendationActionKind, RecommendationTargetKind } from '@/types'

const STORAGE_KEY = 'mastery:rec_payload'
const EXPIRY_MS = 30 * 60 * 1000 // 30 minutes

/**
 * Data structure stored in sessionStorage for recommendation payloads.
 */
export interface StoredPayload {
  recommendationId: string
  actionKind: RecommendationActionKind
  targetKind: RecommendationTargetKind
  payload: Record<string, unknown>
  storedAt: number
}

/**
 * Stores a recommendation payload in sessionStorage for form pre-population.
 */
export function storePayload(data: StoredPayload): void {
  try {
    sessionStorage.setItem(
      `${STORAGE_KEY}:${data.recommendationId}`,
      JSON.stringify(data)
    )
  } catch (error) {
    console.error('Failed to store recommendation payload:', error)
  }
}

/**
 * Retrieves a stored recommendation payload by ID.
 * Returns null if not found or expired.
 */
export function getPayload(id: string): StoredPayload | null {
  try {
    const raw = sessionStorage.getItem(`${STORAGE_KEY}:${id}`)
    if (!raw) return null

    const data = JSON.parse(raw) as StoredPayload

    // Check expiry
    if (Date.now() - data.storedAt > EXPIRY_MS) {
      clearPayload(id)
      return null
    }

    return data
  } catch (error) {
    console.error('Failed to retrieve recommendation payload:', error)
    return null
  }
}

/**
 * Clears a stored recommendation payload by ID.
 */
export function clearPayload(id: string): void {
  try {
    sessionStorage.removeItem(`${STORAGE_KEY}:${id}`)
  } catch (error) {
    console.error('Failed to clear recommendation payload:', error)
  }
}

/**
 * Clears all stored recommendation payloads.
 */
export function clearAllPayloads(): void {
  try {
    const keys = Object.keys(sessionStorage)
    for (const key of keys) {
      if (key.startsWith(STORAGE_KEY)) {
        sessionStorage.removeItem(key)
      }
    }
  } catch (error) {
    console.error('Failed to clear all recommendation payloads:', error)
  }
}
