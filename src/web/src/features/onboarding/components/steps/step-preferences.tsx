import { useOnboardingStore } from '@/stores/onboarding-store'
import { StepNavigation } from '../step-navigation'
import { Label } from '@/components/ui/label'
import { Input } from '@/components/ui/input'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Checkbox } from '@/components/ui/checkbox'
import type { PreferencesDto, NotificationChannel } from '@/types'

const COACHING_STYLES = [
  { value: 'Encouraging', label: 'Encouraging', description: 'Supportive and celebratory' },
  { value: 'Direct', label: 'Direct', description: 'Straightforward feedback' },
  { value: 'Analytical', label: 'Analytical', description: 'Data-driven insights' },
]

const VERBOSITY_LEVELS = [
  { value: 'Minimal', label: 'Minimal', description: 'Quick bullet points' },
  { value: 'Medium', label: 'Medium', description: 'Brief rationale' },
  { value: 'Detailed', label: 'Detailed', description: 'Full explanations' },
]

const NUDGE_LEVELS = [
  { value: 'Off', label: 'Off', description: 'No nudges' },
  { value: 'Low', label: 'Low', description: 'Critical reminders only' },
  { value: 'Medium', label: 'Medium', description: 'Daily check-ins' },
  { value: 'High', label: 'High', description: 'Proactive suggestions' },
]

const NOTIFICATION_CHANNELS: { value: NotificationChannel; label: string }[] = [
  { value: 'Push', label: 'Push notifications' },
  { value: 'Email', label: 'Email' },
  { value: 'SMS', label: 'SMS' },
]

export function StepPreferences() {
  const { data, setPreferences, nextStep, prevStep, currentStep, totalSteps } =
    useOnboardingStore()

  const preferences = data.preferences

  const handleUpdate = (updates: Partial<PreferencesDto>) => {
    setPreferences({ ...preferences, ...updates })
  }

  const toggleNotificationChannel = (channel: NotificationChannel) => {
    const channels = preferences.notificationChannels
    const newChannels = channels.includes(channel)
      ? channels.filter((c) => c !== channel)
      : [...channels, channel]
    handleUpdate({ notificationChannels: newChannels })
  }

  return (
    <div className="space-y-8">
      <div>
        <h2 className="text-xl font-semibold mb-2">How should I coach you?</h2>
        <p className="text-muted-foreground">
          Customize how Mastery communicates with you. You can always change these later.
        </p>
      </div>

      <div className="space-y-6">
        {/* Coaching Style */}
        <div className="space-y-2">
          <Label>Coaching Style</Label>
          <Select
            value={preferences.coachingStyle}
            onValueChange={(value) =>
              handleUpdate({ coachingStyle: value as PreferencesDto['coachingStyle'] })
            }
          >
            <SelectTrigger>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {COACHING_STYLES.map((style) => (
                <SelectItem key={style.value} value={style.value}>
                  <div>
                    <span className="font-medium">{style.label}</span>
                    <span className="text-muted-foreground ml-2">- {style.description}</span>
                  </div>
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        {/* Verbosity */}
        <div className="space-y-2">
          <Label>Explanation Detail</Label>
          <Select
            value={preferences.explanationVerbosity}
            onValueChange={(value) =>
              handleUpdate({ explanationVerbosity: value as PreferencesDto['explanationVerbosity'] })
            }
          >
            <SelectTrigger>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {VERBOSITY_LEVELS.map((level) => (
                <SelectItem key={level.value} value={level.value}>
                  <div>
                    <span className="font-medium">{level.label}</span>
                    <span className="text-muted-foreground ml-2">- {level.description}</span>
                  </div>
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        {/* Nudge Level */}
        <div className="space-y-2">
          <Label>Nudge Level</Label>
          <Select
            value={preferences.nudgeLevel}
            onValueChange={(value) =>
              handleUpdate({ nudgeLevel: value as PreferencesDto['nudgeLevel'] })
            }
          >
            <SelectTrigger>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {NUDGE_LEVELS.map((level) => (
                <SelectItem key={level.value} value={level.value}>
                  <div>
                    <span className="font-medium">{level.label}</span>
                    <span className="text-muted-foreground ml-2">- {level.description}</span>
                  </div>
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        {/* Check-in Times */}
        <div className="grid gap-4 sm:grid-cols-2">
          <div className="space-y-2">
            <Label>Morning Check-in</Label>
            <Input
              type="time"
              value={preferences.morningCheckInTime}
              onChange={(e) => handleUpdate({ morningCheckInTime: e.target.value })}
            />
          </div>
          <div className="space-y-2">
            <Label>Evening Check-in</Label>
            <Input
              type="time"
              value={preferences.eveningCheckInTime}
              onChange={(e) => handleUpdate({ eveningCheckInTime: e.target.value })}
            />
          </div>
        </div>

        {/* Notification Channels */}
        <div className="space-y-3">
          <Label>Notification Channels</Label>
          <div className="space-y-2">
            {NOTIFICATION_CHANNELS.map((channel) => (
              <label
                key={channel.value}
                className="flex items-center gap-2 cursor-pointer"
              >
                <Checkbox
                  checked={preferences.notificationChannels.includes(channel.value)}
                  onCheckedChange={() => toggleNotificationChannel(channel.value)}
                />
                <span className="text-sm">{channel.label}</span>
              </label>
            ))}
          </div>
        </div>
      </div>

      <StepNavigation
        currentStep={currentStep}
        totalSteps={totalSteps}
        onNext={nextStep}
        onPrev={prevStep}
      />
    </div>
  )
}
