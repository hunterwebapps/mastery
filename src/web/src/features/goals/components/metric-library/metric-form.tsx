import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Loader2, Archive, ArchiveRestore } from 'lucide-react'
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
  createMetricDefinitionSchema,
  type CreateMetricDefinitionFormData,
} from '../../schemas'
import { metricDataTypeInfo, metricDirectionInfo } from '@/types'

interface MetricFormProps {
  onSubmit: (data: CreateMetricDefinitionFormData) => Promise<void>
  onCancel: () => void
  isSubmitting?: boolean
  defaultValues?: Partial<CreateMetricDefinitionFormData>
  submitLabel?: string
  cancelLabel?: string
  onArchive?: () => void
  onRestore?: () => void
  isArchived?: boolean
}

export function MetricForm({ onSubmit, onCancel, isSubmitting, defaultValues, submitLabel = 'Create Metric', cancelLabel = 'Cancel', onArchive, onRestore, isArchived }: MetricFormProps) {
  const {
    register,
    handleSubmit,
    setValue,
    watch,
    formState: { errors },
  } = useForm<CreateMetricDefinitionFormData>({
    resolver: zodResolver(createMetricDefinitionSchema),
    defaultValues: {
      name: '',
      description: '',
      dataType: 'Number',
      direction: 'Increase',
      defaultCadence: 'Weekly',
      defaultAggregation: 'Sum',
      tags: [],
      ...defaultValues,
    },
  })

  const dataType = watch('dataType')
  const direction = watch('direction')
  const defaultCadence = watch('defaultCadence')
  const defaultAggregation = watch('defaultAggregation')

  const handleFormSubmit = (e: React.FormEvent) => {
    e.stopPropagation()
    handleSubmit(onSubmit)(e)
  }

  return (
    <form onSubmit={handleFormSubmit} className="space-y-4">
      <div className="space-y-2">
        <Label htmlFor="name">
          Metric Name <span className="text-destructive">*</span>
        </Label>
        <Input
          id="name"
          placeholder="e.g., Deep Work Hours, Body Weight, Revenue"
          {...register('name')}
          className={errors.name ? 'border-destructive' : ''}
        />
        {errors.name && (
          <p className="text-sm text-destructive">{errors.name.message}</p>
        )}
      </div>

      <div className="space-y-2">
        <Label htmlFor="description">Description</Label>
        <Textarea
          id="description"
          placeholder="What does this metric measure?"
          rows={2}
          {...register('description')}
        />
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <div className="space-y-2">
          <Label>Data Type</Label>
          <Select
            value={dataType}
            onValueChange={(value) => setValue('dataType', value as any)}
          >
            <SelectTrigger>
              <SelectValue placeholder="Select type" />
            </SelectTrigger>
            <SelectContent>
              {Object.entries(metricDataTypeInfo).map(([key, info]) => (
                <SelectItem key={key} value={key}>
                  <span className="flex items-center gap-2">
                    <span>{info.icon}</span>
                    <span>{info.label}</span>
                  </span>
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <p className="text-xs text-muted-foreground">
            {dataType && metricDataTypeInfo[dataType]?.description}
          </p>
        </div>

        <div className="space-y-2">
          <Label>Direction</Label>
          <Select
            value={direction}
            onValueChange={(value) => setValue('direction', value as any)}
          >
            <SelectTrigger>
              <SelectValue placeholder="Select direction" />
            </SelectTrigger>
            <SelectContent>
              {Object.entries(metricDirectionInfo).map(([key, info]) => (
                <SelectItem key={key} value={key}>
                  <span className="flex items-center gap-2">
                    <span>{info.icon}</span>
                    <span>{info.label}</span>
                  </span>
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <p className="text-xs text-muted-foreground">
            {direction && metricDirectionInfo[direction]?.description}
          </p>
        </div>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <div className="space-y-2">
          <Label>Default Cadence</Label>
          <Select
            value={defaultCadence}
            onValueChange={(value) => setValue('defaultCadence', value as any)}
          >
            <SelectTrigger>
              <SelectValue placeholder="Select cadence" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="Daily">Daily</SelectItem>
              <SelectItem value="Weekly">Weekly</SelectItem>
              <SelectItem value="Monthly">Monthly</SelectItem>
              <SelectItem value="Rolling">Rolling</SelectItem>
            </SelectContent>
          </Select>
        </div>

        <div className="space-y-2">
          <Label>Default Aggregation</Label>
          <Select
            value={defaultAggregation}
            onValueChange={(value) => setValue('defaultAggregation', value as any)}
          >
            <SelectTrigger>
              <SelectValue placeholder="Select aggregation" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="Sum">Sum</SelectItem>
              <SelectItem value="Average">Average</SelectItem>
              <SelectItem value="Max">Max</SelectItem>
              <SelectItem value="Min">Min</SelectItem>
              <SelectItem value="Count">Count</SelectItem>
              <SelectItem value="Latest">Latest</SelectItem>
            </SelectContent>
          </Select>
        </div>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <div className="space-y-2">
          <Label htmlFor="unit-type">Unit Type</Label>
          <Input
            id="unit-type"
            placeholder="e.g., kg, hours, USD"
            {...register('unit.type')}
          />
        </div>

        <div className="space-y-2">
          <Label htmlFor="unit-label">Unit Label</Label>
          <Input
            id="unit-label"
            placeholder="e.g., kg, h, $"
            {...register('unit.label')}
          />
        </div>
      </div>

      <div className="flex justify-between gap-2 pt-4">
        <div>
          {onArchive && !isArchived && (
            <Button type="button" variant="ghost" onClick={onArchive} className="text-muted-foreground hover:text-destructive">
              <Archive className="size-4 mr-2" />
              Archive
            </Button>
          )}
          {onRestore && isArchived && (
            <Button type="button" variant="ghost" onClick={onRestore} className="text-muted-foreground">
              <ArchiveRestore className="size-4 mr-2" />
              Restore
            </Button>
          )}
        </div>
        <div className="flex gap-2">
          <Button type="button" variant="outline" onClick={onCancel}>
            {cancelLabel}
          </Button>
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting && <Loader2 className="size-4 mr-2 animate-spin" />}
            {submitLabel}
          </Button>
        </div>
      </div>
    </form>
  )
}
