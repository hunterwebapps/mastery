import { useFormContext, useFieldArray } from 'react-hook-form'
import { Plus, Trash2, Info } from 'lucide-react'
import { Label } from '@/components/ui/label'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Slider } from '@/components/ui/slider'
import { Switch } from '@/components/ui/switch'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Alert, AlertDescription } from '@/components/ui/alert'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { cn } from '@/lib/utils'
import type { CreateHabitFormData } from '../../schemas/habit-schema'
import type { HabitMode } from '@/types/habit'
import { habitModeInfo } from '@/types/habit'

export function StepVariants() {
  const {
    watch,
    setValue,
    register,
    control,
  } = useFormContext<CreateHabitFormData>()

  const { fields, append, remove } = useFieldArray({
    control,
    name: 'variants',
  })

  const defaultMode = watch('defaultMode')
  const variants = watch('variants') || []

  const usedModes = variants.map(v => v.mode)
  const availableModes = (['Full', 'Maintenance', 'Minimum'] as HabitMode[])
    .filter(mode => !usedModes.includes(mode))

  const addVariant = () => {
    if (availableModes.length === 0) return
    append({
      mode: availableModes[0],
      label: habitModeInfo[availableModes[0]].label,
      defaultValue: 1,
      estimatedMinutes: availableModes[0] === 'Full' ? 30 : availableModes[0] === 'Maintenance' ? 15 : 5,
      energyCost: availableModes[0] === 'Full' ? 4 : availableModes[0] === 'Maintenance' ? 2 : 1,
      countsAsCompletion: true,
    })
  }

  return (
    <div className="space-y-6">
      <div className="text-center mb-8">
        <h2 className="text-xl font-semibold">Set up mode variants (optional)</h2>
        <p className="text-muted-foreground mt-1">
          Create scaled versions for low-energy days. Even a tiny action keeps the streak alive.
        </p>
      </div>

      <Alert>
        <Info className="size-4" />
        <AlertDescription>
          <strong>Mode scaling</strong> lets you adapt your habit to your energy level.
          On tough days, doing a "Minimum" version is better than skipping entirely.
        </AlertDescription>
      </Alert>

      {/* Default mode selector */}
      <div className="space-y-2">
        <Label>Default mode</Label>
        <Select
          value={defaultMode}
          onValueChange={(value) => setValue('defaultMode', value as HabitMode)}
        >
          <SelectTrigger>
            <SelectValue placeholder="Select default mode" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="Full">Full - Complete version</SelectItem>
            <SelectItem value="Maintenance">Maintenance - Reduced version</SelectItem>
            <SelectItem value="Minimum">Minimum - Bare minimum</SelectItem>
          </SelectContent>
        </Select>
        <p className="text-xs text-muted-foreground">
          The mode that will be pre-selected when completing this habit.
        </p>
      </div>

      {/* Variants list */}
      {fields.length > 0 && (
        <div className="space-y-4">
          <Label>Mode variants</Label>
          {fields.map((field, index) => (
            <Card key={field.id}>
              <CardHeader className="pb-3">
                <div className="flex items-center justify-between">
                  <CardTitle className="text-base flex items-center gap-2">
                    <span
                      className={cn(
                        'size-3 rounded-full',
                        variants[index]?.mode === 'Full' && 'bg-blue-500',
                        variants[index]?.mode === 'Maintenance' && 'bg-yellow-500',
                        variants[index]?.mode === 'Minimum' && 'bg-orange-500'
                      )}
                    />
                    {habitModeInfo[variants[index]?.mode || 'Full'].label} Mode
                  </CardTitle>
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon-xs"
                    onClick={() => remove(index)}
                    className="text-muted-foreground hover:text-destructive"
                  >
                    <Trash2 className="size-4" />
                  </Button>
                </div>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="grid gap-4 sm:grid-cols-2">
                  <div className="space-y-2">
                    <Label>Label</Label>
                    <Input
                      placeholder="e.g., Quick workout, 5-min meditation"
                      {...register(`variants.${index}.label`)}
                    />
                  </div>
                  <div className="space-y-2">
                    <Label>Mode</Label>
                    <Select
                      value={variants[index]?.mode}
                      onValueChange={(value) => setValue(`variants.${index}.mode`, value as HabitMode)}
                    >
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {(['Full', 'Maintenance', 'Minimum'] as HabitMode[]).map(mode => (
                          <SelectItem
                            key={mode}
                            value={mode}
                            disabled={usedModes.includes(mode) && variants[index]?.mode !== mode}
                          >
                            {habitModeInfo[mode].label}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                </div>

                <div className="grid gap-4 sm:grid-cols-2">
                  <div className="space-y-2">
                    <Label>Estimated time (minutes)</Label>
                    <div className="flex items-center gap-3">
                      <Slider
                        value={[variants[index]?.estimatedMinutes || 15]}
                        onValueChange={([value]) => setValue(`variants.${index}.estimatedMinutes`, value)}
                        min={1}
                        max={120}
                        step={5}
                        className="flex-1"
                      />
                      <span className="text-sm font-medium w-12 text-right">
                        {variants[index]?.estimatedMinutes || 15}m
                      </span>
                    </div>
                  </div>
                  <div className="space-y-2">
                    <Label>Energy cost (1-5)</Label>
                    <div className="flex items-center gap-3">
                      <Slider
                        value={[variants[index]?.energyCost || 3]}
                        onValueChange={([value]) => setValue(`variants.${index}.energyCost`, value)}
                        min={1}
                        max={5}
                        step={1}
                        className="flex-1"
                      />
                      <span className="text-sm font-medium w-8 text-right">
                        {variants[index]?.energyCost || 3}/5
                      </span>
                    </div>
                  </div>
                </div>

                <div className="flex items-center justify-between">
                  <div className="space-y-0.5">
                    <Label htmlFor={`countsAsCompletion-${index}`}>Counts as completion</Label>
                    <p className="text-xs text-muted-foreground">
                      When enabled, this mode maintains your streak.
                    </p>
                  </div>
                  <Switch
                    id={`countsAsCompletion-${index}`}
                    checked={variants[index]?.countsAsCompletion ?? true}
                    onCheckedChange={(checked) => setValue(`variants.${index}.countsAsCompletion`, checked)}
                  />
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      {/* Add variant button */}
      {availableModes.length > 0 && fields.length < 3 && (
        <Button
          type="button"
          variant="outline"
          className="w-full"
          onClick={addVariant}
        >
          <Plus className="size-4 mr-2" />
          Add {fields.length === 0 ? 'mode variant' : 'another variant'}
        </Button>
      )}

      {fields.length === 0 && (
        <p className="text-sm text-muted-foreground text-center py-4">
          No variants configured. You can skip this step and add them later.
        </p>
      )}
    </div>
  )
}
