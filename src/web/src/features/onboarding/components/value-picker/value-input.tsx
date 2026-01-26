import { useState } from 'react'
import { Plus } from 'lucide-react'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'

interface ValueInputProps {
  onAdd: (value: string) => void
  existingValues: string[]
}

export function ValueInput({ onAdd, existingValues }: ValueInputProps) {
  const [input, setInput] = useState('')
  const [error, setError] = useState<string | null>(null)

  const handleAdd = () => {
    const trimmed = input.trim()
    if (!trimmed) {
      setError('Please enter a value')
      return
    }
    if (existingValues.some((v) => v.toLowerCase() === trimmed.toLowerCase())) {
      setError('This value already exists')
      return
    }
    if (trimmed.length > 50) {
      setError('Value must be 50 characters or less')
      return
    }
    onAdd(trimmed)
    setInput('')
    setError(null)
  }

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      e.preventDefault()
      handleAdd()
    }
  }

  return (
    <div className="space-y-2">
      <div className="flex gap-2">
        <Input
          placeholder="Add a custom value..."
          value={input}
          onChange={(e) => {
            setInput(e.target.value)
            setError(null)
          }}
          onKeyDown={handleKeyDown}
          className="flex-1"
        />
        <Button onClick={handleAdd} disabled={!input.trim()}>
          <Plus className="size-4 mr-2" />
          Add
        </Button>
      </div>
      {error && <p className="text-xs text-destructive">{error}</p>}
    </div>
  )
}
