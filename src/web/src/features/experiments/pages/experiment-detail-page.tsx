import { useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { Skeleton } from '@/components/ui/skeleton'
import {
  useExperiment,
  useStartExperiment,
  usePauseExperiment,
  useResumeExperiment,
  useCompleteExperiment,
  useAbandonExperiment,
  useAddExperimentNote,
} from '../hooks'
import {
  ExperimentHeader,
  HypothesisCard,
  MeasurementPlanCard,
  ExperimentResultsCard,
  ExperimentNotes,
  ExperimentTimeline,
} from '../components/experiment-detail'
import { CompleteExperimentDialog } from '../components/complete-experiment-dialog'
import type { CompleteExperimentRequest } from '@/types'

function ExperimentDetailSkeleton() {
  return (
    <div className="min-h-screen bg-background">
      <div className="container max-w-5xl py-8 px-4 sm:px-6 lg:px-8">
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <Skeleton className="h-8 w-32" />
            <div className="flex gap-2">
              <Skeleton className="h-9 w-24" />
              <Skeleton className="h-9 w-32" />
            </div>
          </div>
          <div className="space-y-3">
            <div className="flex gap-2">
              <Skeleton className="h-6 w-16" />
              <Skeleton className="h-6 w-24" />
            </div>
            <Skeleton className="h-8 w-64" />
            <Skeleton className="h-5 w-96" />
          </div>
        </div>
        <div className="mt-8 grid gap-6 lg:grid-cols-3">
          <div className="lg:col-span-2 space-y-6">
            <Skeleton className="h-48 w-full" />
            <Skeleton className="h-40 w-full" />
          </div>
          <div className="space-y-6">
            <Skeleton className="h-64 w-full" />
            <Skeleton className="h-48 w-full" />
          </div>
        </div>
      </div>
    </div>
  )
}

export function Component() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { data: experiment, isLoading } = useExperiment(id!)

  const startExperiment = useStartExperiment()
  const pauseExperiment = usePauseExperiment()
  const resumeExperiment = useResumeExperiment()
  const completeExperiment = useCompleteExperiment()
  const abandonExperiment = useAbandonExperiment()
  const addNote = useAddExperimentNote()

  const [showCompleteDialog, setShowCompleteDialog] = useState(false)

  const isActionPending =
    startExperiment.isPending ||
    pauseExperiment.isPending ||
    resumeExperiment.isPending ||
    completeExperiment.isPending ||
    abandonExperiment.isPending

  if (isLoading || !experiment) {
    return <ExperimentDetailSkeleton />
  }

  const handleStart = () => {
    startExperiment.mutate(experiment.id)
  }

  const handlePause = () => {
    pauseExperiment.mutate(experiment.id)
  }

  const handleResume = () => {
    resumeExperiment.mutate(experiment.id)
  }

  const handleComplete = (request: CompleteExperimentRequest) => {
    completeExperiment.mutate(
      { id: experiment.id, request },
      { onSuccess: () => setShowCompleteDialog(false) }
    )
  }

  const handleAbandon = (reason?: string) => {
    abandonExperiment.mutate(
      { id: experiment.id, request: { reason } },
      { onSuccess: () => navigate('/experiments') }
    )
  }

  const handleAddNote = (content: string) => {
    addNote.mutate({ id: experiment.id, request: { content } })
  }

  return (
    <div className="min-h-screen bg-background">
      <div className="container max-w-5xl py-8 px-4 sm:px-6 lg:px-8">
        <ExperimentHeader
          experiment={experiment}
          onStart={handleStart}
          onPause={handlePause}
          onResume={handleResume}
          onComplete={() => setShowCompleteDialog(true)}
          onAbandon={handleAbandon}
          isActionPending={isActionPending}
        />

        <div className="mt-8 grid gap-6 lg:grid-cols-3">
          <div className="lg:col-span-2 space-y-6">
            <HypothesisCard hypothesis={experiment.hypothesis} />
            <MeasurementPlanCard plan={experiment.measurementPlan} />
            {experiment.result && (
              <ExperimentResultsCard result={experiment.result} />
            )}
          </div>

          <div className="space-y-6">
            <ExperimentTimeline experiment={experiment} />
            <ExperimentNotes
              notes={experiment.notes}
              onAddNote={handleAddNote}
              isAdding={addNote.isPending}
            />
          </div>
        </div>
      </div>

      <CompleteExperimentDialog
        open={showCompleteDialog}
        onOpenChange={setShowCompleteDialog}
        onComplete={handleComplete}
        isPending={completeExperiment.isPending}
      />
    </div>
  )
}
