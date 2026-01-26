import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, X } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import { Input } from '@/components/ui/input'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Slider } from '@/components/ui/slider'
import { Badge } from '@/components/ui/badge'
import { Textarea } from '@/components/ui/textarea'
import type { CreateSeasonRequest, SeasonType } from '@/types'
import { seasonTypeInfo } from '@/types'

interface NewSeasonDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onCreate: (request: CreateSeasonRequest) => Promise<void>
  isCreating: boolean
}

const seasonSchema = z.object({
  label: z.string().min(1, 'Season name is required').max(100),
  type: z.enum(['Sprint', 'Build', 'Maintain', 'Recover', 'Transition', 'Explore']),
  startDate: z.string().min(1, 'Start date is required'),
  expectedEndDate: z.string().optional(),
  successStatement: z.string().max(500).optional(),
  intensity: z.number().min(1).max(10),
})

type SeasonFormValues = z.infer<typeof seasonSchema>

const SEASON_TYPES: SeasonType[] = ['Sprint', 'Build', 'Maintain', 'Recover', 'Transition', 'Explore']

export function NewSeasonDialog({
  open,
  onOpenChange,
  onCreate,
  isCreating,
}: NewSeasonDialogProps) {
  const [nonNegotiables, setNonNegotiables] = useState<string[]>([])
  const [newNonNegotiable, setNewNonNegotiable] = useState('')

  const form = useForm<SeasonFormValues>({
    resolver: zodResolver(seasonSchema),
    defaultValues: {
      label: '',
      type: 'Build',
      startDate: new Date().toISOString().split('T')[0],
      expectedEndDate: '',
      successStatement: '',
      intensity: 5,
    },
  })

  const handleAddNonNegotiable = () => {
    const trimmed = newNonNegotiable.trim()
    if (trimmed && !nonNegotiables.includes(trimmed)) {
      setNonNegotiables([...nonNegotiables, trimmed])
      setNewNonNegotiable('')
    }
  }

  const handleRemoveNonNegotiable = (item: string) => {
    setNonNegotiables(nonNegotiables.filter((n) => n !== item))
  }

  const onSubmit = async (data: SeasonFormValues) => {
    const request: CreateSeasonRequest = {
      label: data.label,
      type: data.type,
      startDate: data.startDate,
      expectedEndDate: data.expectedEndDate || undefined,
      successStatement: data.successStatement || undefined,
      nonNegotiables: nonNegotiables.length > 0 ? nonNegotiables : undefined,
      intensity: data.intensity,
    }
    await onCreate(request)
    form.reset()
    setNonNegotiables([])
    onOpenChange(false)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[85vh] overflow-y-auto sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle>Start New Season</DialogTitle>
          <DialogDescription>
            A season defines a focused period with specific goals and intensity.
          </DialogDescription>
        </DialogHeader>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6 py-4">
            <FormField
              control={form.control}
              name="label"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Season Name</FormLabel>
                  <FormControl>
                    <Input placeholder="e.g., Q1 Product Launch" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="type"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Season Type</FormLabel>
                  <Select onValueChange={field.onChange} defaultValue={field.value}>
                    <FormControl>
                      <SelectTrigger>
                        <SelectValue placeholder="Select a type" />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      {SEASON_TYPES.map((type) => (
                        <SelectItem key={type} value={type}>
                          <div className="flex flex-col">
                            <span className={seasonTypeInfo[type].color}>
                              {seasonTypeInfo[type].label}
                            </span>
                            <span className="text-xs text-muted-foreground">
                              {seasonTypeInfo[type].description}
                            </span>
                          </div>
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />

            <div className="grid gap-4 sm:grid-cols-2">
              <FormField
                control={form.control}
                name="startDate"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Start Date</FormLabel>
                    <FormControl>
                      <Input type="date" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="expectedEndDate"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Expected End Date (optional)</FormLabel>
                    <FormControl>
                      <Input type="date" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </div>

            <FormField
              control={form.control}
              name="intensity"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Intensity Level</FormLabel>
                  <div className="flex items-center gap-4">
                    <FormControl>
                      <Slider
                        value={[field.value]}
                        onValueChange={([value]) => field.onChange(value)}
                        min={1}
                        max={10}
                        step={1}
                        className="flex-1"
                      />
                    </FormControl>
                    <span className="w-12 text-right text-sm font-medium">
                      {field.value}/10
                    </span>
                  </div>
                  <FormDescription>
                    Higher intensity means more aggressive planning and nudges
                  </FormDescription>
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="successStatement"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Success Statement (optional)</FormLabel>
                  <FormControl>
                    <Textarea
                      placeholder="What does success look like at the end of this season?"
                      className="resize-none"
                      rows={2}
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <div className="space-y-3">
              <FormLabel>Non-Negotiables (optional)</FormLabel>
              <FormDescription>
                Things you absolutely won't compromise on during this season
              </FormDescription>
              {nonNegotiables.length > 0 && (
                <div className="flex flex-wrap gap-2">
                  {nonNegotiables.map((item) => (
                    <Badge key={item} variant="secondary" className="gap-1 pr-1">
                      {item}
                      <button
                        type="button"
                        onClick={() => handleRemoveNonNegotiable(item)}
                        className="ml-1 rounded-full hover:bg-muted-foreground/20 p-0.5"
                      >
                        <X className="size-3" />
                      </button>
                    </Badge>
                  ))}
                </div>
              )}
              <div className="flex gap-2">
                <Input
                  placeholder="e.g., 8 hours sleep, family dinner"
                  value={newNonNegotiable}
                  onChange={(e) => setNewNonNegotiable(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter') {
                      e.preventDefault()
                      handleAddNonNegotiable()
                    }
                  }}
                />
                <Button
                  type="button"
                  variant="outline"
                  size="icon"
                  onClick={handleAddNonNegotiable}
                >
                  <Plus className="size-4" />
                </Button>
              </div>
            </div>

            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => onOpenChange(false)}
              >
                Cancel
              </Button>
              <Button type="submit" disabled={isCreating}>
                {isCreating ? 'Creating...' : 'Start Season'}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}
