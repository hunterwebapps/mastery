import { useForm, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Award, Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/components/ui/dialog'
import { completeExperimentSchema, type CompleteExperimentFormData } from '../schemas'
import { experimentOutcomeInfo } from '@/types'
import type { CompleteExperimentRequest, ExperimentOutcome } from '@/types'

interface CompleteExperimentDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onComplete: (request: CompleteExperimentRequest) => void
  isPending?: boolean
}

export function CompleteExperimentDialog({
  open,
  onOpenChange,
  onComplete,
  isPending,
}: CompleteExperimentDialogProps) {
  const {
    register,
    handleSubmit,
    control,
    reset,
    formState: { errors },
  } = useForm<CompleteExperimentFormData>({
    resolver: zodResolver(completeExperimentSchema),
    defaultValues: {
      outcomeClassification: 'Inconclusive',
      baselineValue: undefined,
      runValue: undefined,
      complianceRate: undefined,
      narrativeSummary: '',
    },
  })

  const onSubmit = (data: CompleteExperimentFormData) => {
    onComplete({
      outcomeClassification: data.outcomeClassification,
      baselineValue: data.baselineValue,
      runValue: data.runValue,
      complianceRate: data.complianceRate,
      narrativeSummary: data.narrativeSummary || undefined,
    })
  }

  const handleOpenChange = (value: boolean) => {
    if (!value) {
      reset()
    }
    onOpenChange(value)
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <div className="flex items-center gap-2">
            <Award className="size-5 text-primary" />
            <DialogTitle>Complete Experiment</DialogTitle>
          </div>
          <DialogDescription>
            Record the outcome and results of your experiment.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="space-y-2">
            <Label>
              Outcome Classification <span className="text-destructive">*</span>
            </Label>
            <Controller
              name="outcomeClassification"
              control={control}
              render={({ field }) => (
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger
                    className={errors.outcomeClassification ? 'border-destructive' : ''}
                  >
                    <SelectValue placeholder="How did it go?" />
                  </SelectTrigger>
                  <SelectContent>
                    {(Object.entries(experimentOutcomeInfo) as [ExperimentOutcome, typeof experimentOutcomeInfo[ExperimentOutcome]][]).map(
                      ([key, info]) => (
                        <SelectItem key={key} value={key}>
                          <span className={info.color}>{info.label}</span>
                        </SelectItem>
                      )
                    )}
                  </SelectContent>
                </Select>
              )}
            />
            {errors.outcomeClassification && (
              <p className="text-sm text-destructive">{errors.outcomeClassification.message}</p>
            )}
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="baselineValue">Baseline Value</Label>
              <Input
                id="baselineValue"
                type="number"
                step="any"
                placeholder="e.g. 3.5"
                {...register('baselineValue', { valueAsNumber: true })}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="runValue">Run Value</Label>
              <Input
                id="runValue"
                type="number"
                step="any"
                placeholder="e.g. 5.2"
                {...register('runValue', { valueAsNumber: true })}
              />
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="complianceRate">Compliance Rate (%)</Label>
            <Controller
              name="complianceRate"
              control={control}
              render={({ field }) => (
                <Input
                  id="complianceRate"
                  type="number"
                  min={0}
                  max={100}
                  step={1}
                  placeholder="e.g. 85"
                  value={field.value !== undefined ? Math.round(field.value * 100) : ''}
                  onChange={(e) => {
                    const val = e.target.value
                    field.onChange(val === '' ? undefined : Number(val) / 100)
                  }}
                />
              )}
            />
            <p className="text-xs text-muted-foreground">
              Percentage of days you followed the experiment protocol (0-100)
            </p>
            {errors.complianceRate && (
              <p className="text-sm text-destructive">{errors.complianceRate.message}</p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="narrativeSummary">Summary</Label>
            <Textarea
              id="narrativeSummary"
              placeholder="What did you learn? What would you do differently?"
              rows={4}
              {...register('narrativeSummary')}
            />
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => handleOpenChange(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={isPending}>
              {isPending && <Loader2 className="size-4 mr-2 animate-spin" />}
              Complete Experiment
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
