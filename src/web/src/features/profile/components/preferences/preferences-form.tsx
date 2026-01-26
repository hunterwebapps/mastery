import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Input } from '@/components/ui/input'
import { Checkbox } from '@/components/ui/checkbox'
import { Slider } from '@/components/ui/slider'
import { Label } from '@/components/ui/label'
import { Switch } from '@/components/ui/switch'
import type { PreferencesDto, NotificationChannel } from '@/types'

const preferencesSchema = z.object({
  coachingStyle: z.enum(['Direct', 'Encouraging', 'Analytical']),
  explanationVerbosity: z.enum(['Minimal', 'Medium', 'Detailed']),
  nudgeLevel: z.enum(['Off', 'Low', 'Medium', 'High']),
  notificationChannels: z.array(z.enum(['Push', 'Email', 'SMS'])),
  morningCheckInTime: z.string(),
  eveningCheckInTime: z.string(),
  planningDefaults: z.object({
    defaultTaskDurationMinutes: z.number().min(1).max(480),
    autoScheduleHabits: z.boolean(),
    bufferBetweenTasksMinutes: z.number().min(0).max(60),
  }),
  privacy: z.object({
    shareProgressWithCoach: z.boolean(),
    allowAnonymousAnalytics: z.boolean(),
  }),
})

type PreferencesFormValues = z.infer<typeof preferencesSchema>

interface PreferencesFormProps {
  preferences: PreferencesDto
  onSave: (preferences: PreferencesDto) => Promise<void>
  isSaving: boolean
  onCancel: () => void
}

const notificationChannelOptions: { value: NotificationChannel; label: string }[] = [
  { value: 'Push', label: 'Push Notifications' },
  { value: 'Email', label: 'Email' },
  { value: 'SMS', label: 'SMS' },
]

export function PreferencesForm({
  preferences,
  onSave,
  isSaving,
  onCancel,
}: PreferencesFormProps) {
  const form = useForm<PreferencesFormValues>({
    resolver: zodResolver(preferencesSchema),
    defaultValues: preferences,
  })

  const onSubmit = async (data: PreferencesFormValues) => {
    await onSave(data as PreferencesDto)
  }

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
        {/* Coaching Style */}
        <FormField
          control={form.control}
          name="coachingStyle"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Coaching Style</FormLabel>
              <Select onValueChange={field.onChange} defaultValue={field.value}>
                <FormControl>
                  <SelectTrigger>
                    <SelectValue placeholder="Select style" />
                  </SelectTrigger>
                </FormControl>
                <SelectContent>
                  <SelectItem value="Encouraging">Encouraging - Supportive and celebratory</SelectItem>
                  <SelectItem value="Direct">Direct - Straightforward feedback</SelectItem>
                  <SelectItem value="Analytical">Analytical - Data-driven insights</SelectItem>
                </SelectContent>
              </Select>
              <FormMessage />
            </FormItem>
          )}
        />

        {/* Verbosity */}
        <FormField
          control={form.control}
          name="explanationVerbosity"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Explanation Detail</FormLabel>
              <Select onValueChange={field.onChange} defaultValue={field.value}>
                <FormControl>
                  <SelectTrigger>
                    <SelectValue placeholder="Select level" />
                  </SelectTrigger>
                </FormControl>
                <SelectContent>
                  <SelectItem value="Minimal">Minimal - Quick bullet points</SelectItem>
                  <SelectItem value="Medium">Medium - Brief rationale</SelectItem>
                  <SelectItem value="Detailed">Detailed - Full explanations</SelectItem>
                </SelectContent>
              </Select>
              <FormMessage />
            </FormItem>
          )}
        />

        {/* Nudge Level */}
        <FormField
          control={form.control}
          name="nudgeLevel"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Nudge Level</FormLabel>
              <Select onValueChange={field.onChange} defaultValue={field.value}>
                <FormControl>
                  <SelectTrigger>
                    <SelectValue placeholder="Select level" />
                  </SelectTrigger>
                </FormControl>
                <SelectContent>
                  <SelectItem value="Off">Off - No nudges</SelectItem>
                  <SelectItem value="Low">Low - Critical reminders only</SelectItem>
                  <SelectItem value="Medium">Medium - Daily check-ins</SelectItem>
                  <SelectItem value="High">High - Proactive suggestions</SelectItem>
                </SelectContent>
              </Select>
              <FormMessage />
            </FormItem>
          )}
        />

        {/* Check-in Times */}
        <div className="grid gap-4 sm:grid-cols-2">
          <FormField
            control={form.control}
            name="morningCheckInTime"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Morning Check-in</FormLabel>
                <FormControl>
                  <Input type="time" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
          <FormField
            control={form.control}
            name="eveningCheckInTime"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Evening Check-in</FormLabel>
                <FormControl>
                  <Input type="time" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        {/* Notification Channels */}
        <FormField
          control={form.control}
          name="notificationChannels"
          render={() => (
            <FormItem>
              <FormLabel>Notification Channels</FormLabel>
              <div className="space-y-2">
                {notificationChannelOptions.map((option) => (
                  <FormField
                    key={option.value}
                    control={form.control}
                    name="notificationChannels"
                    render={({ field }) => (
                      <FormItem className="flex items-center gap-2">
                        <FormControl>
                          <Checkbox
                            checked={field.value?.includes(option.value)}
                            onCheckedChange={(checked) => {
                              const newValue = checked
                                ? [...field.value, option.value]
                                : field.value?.filter((v) => v !== option.value)
                              field.onChange(newValue)
                            }}
                          />
                        </FormControl>
                        <Label className="text-sm font-normal">{option.label}</Label>
                      </FormItem>
                    )}
                  />
                ))}
              </div>
              <FormMessage />
            </FormItem>
          )}
        />

        {/* Planning Defaults */}
        <div className="space-y-4 rounded-lg border border-border p-4">
          <h4 className="font-medium">Planning Defaults</h4>

          <FormField
            control={form.control}
            name="planningDefaults.defaultTaskDurationMinutes"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Default Task Duration</FormLabel>
                <div className="flex items-center gap-4">
                  <FormControl>
                    <Slider
                      value={[field.value]}
                      onValueChange={([value]) => field.onChange(value)}
                      min={5}
                      max={120}
                      step={5}
                      className="flex-1"
                    />
                  </FormControl>
                  <span className="w-16 text-right text-sm">{field.value} min</span>
                </div>
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="planningDefaults.bufferBetweenTasksMinutes"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Buffer Between Tasks</FormLabel>
                <div className="flex items-center gap-4">
                  <FormControl>
                    <Slider
                      value={[field.value]}
                      onValueChange={([value]) => field.onChange(value)}
                      min={0}
                      max={30}
                      step={5}
                      className="flex-1"
                    />
                  </FormControl>
                  <span className="w-16 text-right text-sm">{field.value} min</span>
                </div>
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="planningDefaults.autoScheduleHabits"
            render={({ field }) => (
              <FormItem className="flex items-center gap-2">
                <FormControl>
                  <Switch checked={field.value} onCheckedChange={field.onChange} />
                </FormControl>
                <FormLabel className="font-normal">Auto-schedule habits</FormLabel>
              </FormItem>
            )}
          />
        </div>

        {/* Privacy */}
        <div className="space-y-4 rounded-lg border border-border p-4">
          <h4 className="font-medium">Privacy</h4>

          <FormField
            control={form.control}
            name="privacy.shareProgressWithCoach"
            render={({ field }) => (
              <FormItem className="flex items-center gap-2">
                <FormControl>
                  <Switch checked={field.value} onCheckedChange={field.onChange} />
                </FormControl>
                <FormLabel className="font-normal">Share progress with coach</FormLabel>
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="privacy.allowAnonymousAnalytics"
            render={({ field }) => (
              <FormItem className="flex items-center gap-2">
                <FormControl>
                  <Switch checked={field.value} onCheckedChange={field.onChange} />
                </FormControl>
                <FormLabel className="font-normal">Allow anonymous analytics</FormLabel>
              </FormItem>
            )}
          />
        </div>

        <div className="flex justify-end gap-2">
          <Button type="button" variant="outline" onClick={onCancel}>
            Cancel
          </Button>
          <Button type="submit" disabled={isSaving}>
            {isSaving ? 'Saving...' : 'Save Changes'}
          </Button>
        </div>
      </form>
    </Form>
  )
}
