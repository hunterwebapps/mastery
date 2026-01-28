import { useState } from 'react'
import { Check, Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import type { MetricDefinitionDto } from '@/types'
import { metricDataTypeInfo } from '@/types'

interface ObservationInputProps {
  metric: MetricDefinitionDto
  onSubmit: (value: number, note?: string) => Promise<void>
  isSubmitting?: boolean
}

export function ObservationInput({ metric, onSubmit, isSubmitting }: ObservationInputProps) {
  const [value, setValue] = useState<string>('')
  const [note, setNote] = useState('')
  const [showNote, setShowNote] = useState(false)

  const dataTypeInfo = metricDataTypeInfo[metric.dataType]

  const handleSubmit = async () => {
    const numValue = parseFloat(value)
    if (isNaN(numValue)) return
    await onSubmit(numValue, note || undefined)
    setValue('')
    setNote('')
    setShowNote(false)
  }

  const handleBooleanClick = async (boolValue: boolean) => {
    await onSubmit(boolValue ? 1 : 0)
  }

  // Boolean metrics have a special input
  if (metric.dataType === 'Boolean') {
    return (
      <div className="space-y-2">
        <div className="flex items-center gap-2">
          <div className="p-2 rounded-lg bg-primary/10">
            <span className="text-lg">{dataTypeInfo.icon}</span>
          </div>
          <div className="flex-1 min-w-0">
            <p className="font-medium text-sm truncate">{metric.name}</p>
            {metric.description && (
              <p className="text-xs text-muted-foreground truncate">{metric.description}</p>
            )}
          </div>
        </div>
        <div className="flex gap-2">
          <Button
            variant="outline"
            className="flex-1"
            onClick={() => handleBooleanClick(false)}
            disabled={isSubmitting}
          >
            {isSubmitting ? (
              <Loader2 className="size-4 animate-spin" />
            ) : (
              'No'
            )}
          </Button>
          <Button
            className="flex-1"
            onClick={() => handleBooleanClick(true)}
            disabled={isSubmitting}
          >
            {isSubmitting ? (
              <Loader2 className="size-4 animate-spin" />
            ) : (
              <>
                <Check className="size-4 mr-1" />
                Yes
              </>
            )}
          </Button>
        </div>
      </div>
    )
  }

  // Rating metrics have a 1-5 scale
  if (metric.dataType === 'Rating') {
    return (
      <div className="space-y-2">
        <div className="flex items-center gap-2">
          <div className="p-2 rounded-lg bg-primary/10">
            <span className="text-lg">{dataTypeInfo.icon}</span>
          </div>
          <div className="flex-1 min-w-0">
            <p className="font-medium text-sm truncate">{metric.name}</p>
            {metric.description && (
              <p className="text-xs text-muted-foreground truncate">{metric.description}</p>
            )}
          </div>
        </div>
        <div className="flex gap-1">
          {[1, 2, 3, 4, 5].map((rating) => (
            <Button
              key={rating}
              variant="outline"
              className="flex-1"
              onClick={async () => {
                await onSubmit(rating)
              }}
              disabled={isSubmitting}
            >
              {isSubmitting ? (
                <Loader2 className="size-4 animate-spin" />
              ) : (
                rating
              )}
            </Button>
          ))}
        </div>
      </div>
    )
  }

  // Default numeric input
  return (
    <div className="space-y-2">
      <div className="flex items-center gap-2">
        <div className="p-2 rounded-lg bg-primary/10">
          <span className="text-lg">{dataTypeInfo.icon}</span>
        </div>
        <div className="flex-1 min-w-0">
          <p className="font-medium text-sm truncate">{metric.name}</p>
          {metric.description && (
            <p className="text-xs text-muted-foreground truncate">{metric.description}</p>
          )}
        </div>
      </div>
      <div className="flex gap-2">
        <Input
          type="number"
          placeholder={`Enter ${metric.dataType.toLowerCase()}`}
          value={value}
          onChange={(e) => setValue(e.target.value)}
          className="flex-1"
        />
        {metric.unit && (
          <span className="flex items-center px-3 text-sm text-muted-foreground bg-muted rounded-md">
            {metric.unit.label}
          </span>
        )}
        <Button
          onClick={handleSubmit}
          disabled={isSubmitting || !value}
        >
          {isSubmitting ? (
            <Loader2 className="size-4 animate-spin" />
          ) : (
            <Check className="size-4" />
          )}
        </Button>
      </div>
      {showNote ? (
        <Textarea
          placeholder="Add a note (optional)"
          value={note}
          onChange={(e) => setNote(e.target.value)}
          rows={2}
        />
      ) : (
        <button
          type="button"
          className="text-xs text-muted-foreground hover:text-foreground transition-colors"
          onClick={() => setShowNote(true)}
        >
          + Add note
        </button>
      )}
    </div>
  )
}
