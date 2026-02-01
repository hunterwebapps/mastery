import type { RecommendationType, RecommendationStatus, RecommendationContext } from './recommendation'

// Admin trace list item (summary)
export interface AdminTraceListDto {
  id: string
  recommendationId: string
  userId: string
  userEmail: string
  recommendationType: RecommendationType
  recommendationStatus: RecommendationStatus
  context: RecommendationContext
  selectionMethod: string
  finalTier: number
  processingWindowType: string
  totalDurationMs: number
  totalTokens: number
  agentRunCount: number
  createdAt: string
}

// Admin trace detail (full)
export interface AdminTraceDetailDto {
  id: string
  recommendationId: string
  userId: string
  userEmail: string
  // Recommendation details
  recommendationType: RecommendationType
  recommendationStatus: RecommendationStatus
  recommendationTitle: string
  recommendationRationale: string
  context: RecommendationContext
  recommendationScore: number
  // Trace metadata
  selectionMethod: string
  promptVersion?: string
  modelVersion?: string
  finalTier: number
  processingWindowType: string
  totalDurationMs: number
  // Decompressed JSON objects
  stateSnapshot?: unknown
  signalsSummary?: unknown
  candidateList?: unknown
  tier0TriggeredRules?: unknown
  tier1Scores?: unknown
  tier1EscalationReason?: string
  policyResult?: unknown
  rawLlmResponse?: string
  // Agent runs
  agentRuns: AgentRunDto[]
  // Timestamps
  createdAt: string
  modifiedAt?: string
}

// LLM call details
export interface AgentRunDto {
  id: string
  stage: string
  model: string
  provider?: string
  inputTokens: number
  outputTokens: number
  cachedInputTokens?: number
  reasoningTokens?: number
  totalTokens: number
  latencyMs: number
  isSuccess: boolean
  errorType?: string
  errorMessage?: string
  retryCount: number
  systemFingerprint?: string
  requestId?: string
  startedAt: string
  completedAt: string
}

// Filter parameters
export interface AdminTraceFilterParams {
  dateFrom?: string
  dateTo?: string
  context?: string
  status?: string
  userId?: string
  selectionMethod?: string
  finalTier?: number
  page?: number
  pageSize?: number
}

// Tier badge info helper
export const tierInfo: Record<number, { label: string; color: string; bgColor: string }> = {
  0: { label: 'Tier 0 (Rules)', color: 'text-green-400', bgColor: 'bg-green-500/10' },
  1: { label: 'Tier 1 (Quick)', color: 'text-yellow-400', bgColor: 'bg-yellow-500/10' },
  2: { label: 'Tier 2 (LLM)', color: 'text-purple-400', bgColor: 'bg-purple-500/10' },
}

// Processing window type info helper
export const windowTypeInfo: Record<string, { label: string; color: string; bgColor: string }> = {
  Morning: { label: 'Morning', color: 'text-sky-400', bgColor: 'bg-sky-500/10' },
  Evening: { label: 'Evening', color: 'text-orange-400', bgColor: 'bg-orange-500/10' },
  Weekly: { label: 'Weekly', color: 'text-indigo-400', bgColor: 'bg-indigo-500/10' },
  ProactiveCheck: { label: 'Proactive', color: 'text-gray-400', bgColor: 'bg-gray-500/10' },
  Unknown: { label: 'Unknown', color: 'text-zinc-400', bgColor: 'bg-zinc-500/10' },
}
