import { useEffect } from 'react'
import { Globe, Languages } from 'lucide-react'
import { Label } from '@/components/ui/label'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { TimezoneSelect, detectTimezone } from '../time-inputs'
import { useOnboardingStore, type BasicsData } from '@/stores/onboarding-store'
import { StepNavigation } from '../step-navigation'

const LOCALES = [
  { value: 'en-US', label: 'English (US)' },
  { value: 'en-GB', label: 'English (UK)' },
  { value: 'en-AU', label: 'English (Australia)' },
  { value: 'es-ES', label: 'Spanish (Spain)' },
  { value: 'es-MX', label: 'Spanish (Mexico)' },
  { value: 'fr-FR', label: 'French (France)' },
  { value: 'de-DE', label: 'German (Germany)' },
  { value: 'pt-BR', label: 'Portuguese (Brazil)' },
  { value: 'ja-JP', label: 'Japanese' },
  { value: 'zh-CN', label: 'Chinese (Simplified)' },
]

export function StepBasics() {
  const { data, setBasics, nextStep, currentStep, totalSteps } = useOnboardingStore()
  const basics = data.basics

  // Auto-detect timezone on mount
  useEffect(() => {
    if (!basics) {
      const detectedTimezone = detectTimezone()
      const detectedLocale = navigator.language || 'en-US'
      setBasics({
        timezone: detectedTimezone,
        locale: detectedLocale,
      })
    }
  }, [basics, setBasics])

  const handleChange = (updates: Partial<BasicsData>) => {
    setBasics({
      timezone: basics?.timezone || detectTimezone(),
      locale: basics?.locale || 'en-US',
      ...updates,
    })
  }

  const isValid = basics?.timezone && basics?.locale

  return (
    <div className="space-y-8">
      <div>
        <h2 className="text-xl font-semibold mb-2">Let's start with the basics</h2>
        <p className="text-muted-foreground">
          We've detected your settings automatically. Feel free to adjust if needed.
        </p>
      </div>

      <div className="space-y-6">
        <div className="space-y-2">
          <Label className="flex items-center gap-2">
            <Globe className="size-4" />
            Timezone
          </Label>
          <TimezoneSelect
            value={basics?.timezone || ''}
            onChange={(value) => handleChange({ timezone: value })}
          />
          <p className="text-xs text-muted-foreground">
            Used for scheduling reminders and displaying times correctly
          </p>
        </div>

        <div className="space-y-2">
          <Label className="flex items-center gap-2">
            <Languages className="size-4" />
            Language & Format
          </Label>
          <Select
            value={basics?.locale || ''}
            onValueChange={(value) => handleChange({ locale: value })}
          >
            <SelectTrigger>
              <SelectValue placeholder="Select language" />
            </SelectTrigger>
            <SelectContent>
              {LOCALES.map((locale) => (
                <SelectItem key={locale.value} value={locale.value}>
                  {locale.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <p className="text-xs text-muted-foreground">
            Affects date formats, number formatting, and interface language
          </p>
        </div>
      </div>

      <StepNavigation
        currentStep={currentStep}
        totalSteps={totalSteps}
        onNext={nextStep}
        onPrev={() => {}}
        isNextDisabled={!isValid}
      />
    </div>
  )
}
