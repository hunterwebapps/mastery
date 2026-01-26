import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'

// Common timezones organized by region
const TIMEZONES = [
  { group: 'Americas', zones: [
    { value: 'America/New_York', label: 'Eastern Time (New York)' },
    { value: 'America/Chicago', label: 'Central Time (Chicago)' },
    { value: 'America/Denver', label: 'Mountain Time (Denver)' },
    { value: 'America/Los_Angeles', label: 'Pacific Time (Los Angeles)' },
    { value: 'America/Anchorage', label: 'Alaska Time' },
    { value: 'Pacific/Honolulu', label: 'Hawaii Time' },
    { value: 'America/Toronto', label: 'Eastern Time (Toronto)' },
    { value: 'America/Vancouver', label: 'Pacific Time (Vancouver)' },
    { value: 'America/Mexico_City', label: 'Mexico City' },
    { value: 'America/Sao_Paulo', label: 'São Paulo' },
    { value: 'America/Buenos_Aires', label: 'Buenos Aires' },
  ]},
  { group: 'Europe', zones: [
    { value: 'Europe/London', label: 'London (GMT/BST)' },
    { value: 'Europe/Paris', label: 'Paris (CET)' },
    { value: 'Europe/Berlin', label: 'Berlin (CET)' },
    { value: 'Europe/Amsterdam', label: 'Amsterdam (CET)' },
    { value: 'Europe/Madrid', label: 'Madrid (CET)' },
    { value: 'Europe/Rome', label: 'Rome (CET)' },
    { value: 'Europe/Stockholm', label: 'Stockholm (CET)' },
    { value: 'Europe/Moscow', label: 'Moscow' },
    { value: 'Europe/Istanbul', label: 'Istanbul' },
  ]},
  { group: 'Asia & Pacific', zones: [
    { value: 'Asia/Dubai', label: 'Dubai' },
    { value: 'Asia/Kolkata', label: 'India (Kolkata)' },
    { value: 'Asia/Singapore', label: 'Singapore' },
    { value: 'Asia/Hong_Kong', label: 'Hong Kong' },
    { value: 'Asia/Shanghai', label: 'Shanghai' },
    { value: 'Asia/Tokyo', label: 'Tokyo' },
    { value: 'Asia/Seoul', label: 'Seoul' },
    { value: 'Australia/Sydney', label: 'Sydney' },
    { value: 'Australia/Melbourne', label: 'Melbourne' },
    { value: 'Pacific/Auckland', label: 'Auckland' },
  ]},
  { group: 'Africa & Middle East', zones: [
    { value: 'Africa/Cairo', label: 'Cairo' },
    { value: 'Africa/Johannesburg', label: 'Johannesburg' },
    { value: 'Africa/Lagos', label: 'Lagos' },
    { value: 'Asia/Jerusalem', label: 'Jerusalem' },
    { value: 'Asia/Riyadh', label: 'Riyadh' },
  ]},
]

interface TimezoneSelectProps {
  value: string
  onChange: (value: string) => void
  disabled?: boolean
}

export function TimezoneSelect({ value, onChange, disabled }: TimezoneSelectProps) {
  return (
    <Select value={value} onValueChange={onChange} disabled={disabled}>
      <SelectTrigger>
        <SelectValue placeholder="Select timezone" />
      </SelectTrigger>
      <SelectContent className="max-h-80">
        {TIMEZONES.map((group) => (
          <div key={group.group}>
            <div className="px-2 py-1.5 text-xs font-semibold text-muted-foreground">
              {group.group}
            </div>
            {group.zones.map((zone) => (
              <SelectItem key={zone.value} value={zone.value}>
                {zone.label}
              </SelectItem>
            ))}
          </div>
        ))}
      </SelectContent>
    </Select>
  )
}

// Helper to detect user's timezone
export function detectTimezone(): string {
  try {
    return Intl.DateTimeFormat().resolvedOptions().timeZone
  } catch {
    return 'America/New_York'
  }
}
