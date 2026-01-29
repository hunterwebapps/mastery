import { useState } from 'react'
import { Sparkles } from 'lucide-react'
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs'
import {
  useActiveRecommendations,
  useRecommendationHistory,
  useAcceptRecommendation,
  useDismissRecommendation,
  useSnoozeRecommendation,
} from '../hooks'
import {
  RecommendationsList,
  RecommendationHistoryList,
  RecommendationDetailSheet,
  GenerateButton,
} from '../components'
import type { RecommendationContext, RecommendationSummaryDto } from '@/types'

type ContextFilter = RecommendationContext | 'all'

const contextOptions: { value: ContextFilter; label: string }[] = [
  { value: 'all', label: 'All' },
  { value: 'MorningCheckIn', label: 'Morning' },
  { value: 'Midday', label: 'Midday' },
  { value: 'EveningCheckIn', label: 'Evening' },
  { value: 'WeeklyReview', label: 'Weekly' },
]

const historyRangeOptions: { value: number | undefined; label: string }[] = [
  { value: 7, label: 'Last 7 days' },
  { value: 30, label: 'Last 30 days' },
  { value: undefined, label: 'All time' },
]

function getDateRange(days?: number): { fromDate?: string; toDate?: string } {
  if (!days) return {}
  const toDate = new Date().toISOString().split('T')[0]
  const fromDate = new Date(Date.now() - days * 86400000).toISOString().split('T')[0]
  return { fromDate, toDate }
}

export function Component() {
  const [activeTab, setActiveTab] = useState<'active' | 'history'>('active')
  const [contextFilter, setContextFilter] = useState<ContextFilter>('all')
  const [historyDays, setHistoryDays] = useState<number | undefined>(7)
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [sheetOpen, setSheetOpen] = useState(false)

  // Queries
  const activeContext = contextFilter === 'all' ? undefined : contextFilter
  const { data: activeRecommendations, isLoading: isLoadingActive } =
    useActiveRecommendations(activeContext)

  const { fromDate, toDate } = getDateRange(historyDays)
  const { data: historyRecommendations, isLoading: isLoadingHistory } =
    useRecommendationHistory(fromDate, toDate)

  // Mutations
  const acceptMutation = useAcceptRecommendation()
  const dismissMutation = useDismissRecommendation()
  const snoozeMutation = useSnoozeRecommendation()

  // Handlers
  const handleAccept = (recommendation: RecommendationSummaryDto) => {
    acceptMutation.mutate(recommendation)
  }

  const handleDismiss = (id: string) => {
    dismissMutation.mutate({ id })
  }

  const handleSnooze = (id: string) => {
    snoozeMutation.mutate(id)
  }

  // Determine which context to use for the generate button
  const generateContext: RecommendationContext =
    contextFilter === 'all' ? 'MorningCheckIn' : contextFilter

  return (
    <div className="min-h-screen bg-background">
      <div className="container max-w-6xl py-8 px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <div className="flex items-center justify-between mb-8">
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-lg bg-primary/10">
              <Sparkles className="size-6 text-primary" />
            </div>
            <div>
              <h1 className="text-2xl font-bold text-foreground">Recommendations</h1>
              <p className="text-sm text-muted-foreground">
                Personalized, actionable guidance based on your goals and signals
              </p>
            </div>
          </div>
        </div>

        {/* Main tabs: Active / History */}
        <Tabs
          value={activeTab}
          onValueChange={(value) => setActiveTab(value as 'active' | 'history')}
        >
          <TabsList className="mb-6">
            <TabsTrigger value="active">Active</TabsTrigger>
            <TabsTrigger value="history">History</TabsTrigger>
          </TabsList>

          {/* Active tab content */}
          <TabsContent value="active" className="space-y-6">
            {/* Context filter row */}
            <div className="flex items-center justify-between gap-4 flex-wrap">
              <div className="flex items-center gap-1 rounded-lg bg-muted p-1">
                {contextOptions.map((option) => (
                  <button
                    key={option.value}
                    onClick={() => setContextFilter(option.value)}
                    className={`px-3 py-1.5 rounded-md text-sm font-medium transition-colors ${
                      contextFilter === option.value
                        ? 'bg-background text-foreground shadow-sm'
                        : 'text-muted-foreground hover:text-foreground'
                    }`}
                  >
                    {option.label}
                  </button>
                ))}
              </div>

              <GenerateButton context={generateContext} />
            </div>

            {/* Active recommendations list */}
            <RecommendationsList
              recommendations={activeRecommendations ?? []}
              isLoading={isLoadingActive}
              onAccept={handleAccept}
              onDismiss={handleDismiss}
              onSnooze={handleSnooze}
            />
          </TabsContent>

          {/* History tab content */}
          <TabsContent value="history" className="space-y-6">
            {/* Date range filter */}
            <div className="flex items-center gap-1 rounded-lg bg-muted p-1 w-fit">
              {historyRangeOptions.map((option) => (
                <button
                  key={option.label}
                  onClick={() => setHistoryDays(option.value)}
                  className={`px-3 py-1.5 rounded-md text-sm font-medium transition-colors ${
                    historyDays === option.value
                      ? 'bg-background text-foreground shadow-sm'
                      : 'text-muted-foreground hover:text-foreground'
                  }`}
                >
                  {option.label}
                </button>
              ))}
            </div>

            {/* History list */}
            <RecommendationHistoryList
              recommendations={historyRecommendations ?? []}
              isLoading={isLoadingHistory}
            />
          </TabsContent>
        </Tabs>

        {/* Detail sheet */}
        <RecommendationDetailSheet
          recommendationId={selectedId}
          open={sheetOpen}
          onOpenChange={(open) => {
            setSheetOpen(open)
            if (!open) setSelectedId(null)
          }}
        />
      </div>
    </div>
  )
}
