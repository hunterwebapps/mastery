import { useState } from 'react'
import { Plus, X } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Badge } from '@/components/ui/badge'

interface ContentBoundariesInputProps {
  values: string[]
  onChange: (values: string[]) => void
  disabled?: boolean
}

export function ContentBoundariesInput({
  values,
  onChange,
  disabled = false,
}: ContentBoundariesInputProps) {
  const [input, setInput] = useState('')

  const handleAdd = () => {
    const trimmed = input.trim()
    if (trimmed && !values.includes(trimmed)) {
      onChange([...values, trimmed])
      setInput('')
    }
  }

  const handleRemove = (value: string) => {
    onChange(values.filter((v) => v !== value))
  }

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      e.preventDefault()
      handleAdd()
    }
  }

  return (
    <div className="space-y-3">
      <div className="space-y-1">
        <Label className="text-sm font-medium">Content Boundaries</Label>
        <p className="text-xs text-muted-foreground">
          Topics the AI coach should avoid discussing
        </p>
      </div>

      {values.length > 0 && (
        <div className="flex flex-wrap gap-2">
          {values.map((value) => (
            <Badge
              key={value}
              variant="secondary"
              className="gap-1 pr-1"
            >
              {value}
              <button
                type="button"
                onClick={() => handleRemove(value)}
                disabled={disabled}
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
          placeholder="Add a boundary topic..."
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyDown={handleKeyDown}
          disabled={disabled}
          className="flex-1"
        />
        <Button
          type="button"
          variant="outline"
          size="icon"
          onClick={handleAdd}
          disabled={disabled || !input.trim()}
        >
          <Plus className="size-4" />
        </Button>
      </div>
    </div>
  )
}
