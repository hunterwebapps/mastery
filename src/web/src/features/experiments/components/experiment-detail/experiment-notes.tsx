import { useState } from 'react'
import { StickyNote, Send, MessageSquare, Loader2 } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Textarea } from '@/components/ui/textarea'
import type { ExperimentNoteDto } from '@/types'

interface ExperimentNotesProps {
  notes: ExperimentNoteDto[]
  onAddNote: (content: string) => void
  isAdding?: boolean
  canAddNote?: boolean
}

function formatRelativeTime(dateString: string): string {
  const date = new Date(dateString)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffSeconds = Math.floor(diffMs / 1000)
  const diffMinutes = Math.floor(diffSeconds / 60)
  const diffHours = Math.floor(diffMinutes / 60)
  const diffDays = Math.floor(diffHours / 24)
  const diffWeeks = Math.floor(diffDays / 7)

  if (diffSeconds < 60) return 'just now'
  if (diffMinutes < 60) return `${diffMinutes} minute${diffMinutes === 1 ? '' : 's'} ago`
  if (diffHours < 24) return `${diffHours} hour${diffHours === 1 ? '' : 's'} ago`
  if (diffDays < 7) return `${diffDays} day${diffDays === 1 ? '' : 's'} ago`
  if (diffWeeks < 4) return `${diffWeeks} week${diffWeeks === 1 ? '' : 's'} ago`

  return date.toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: date.getFullYear() !== now.getFullYear() ? 'numeric' : undefined,
  })
}

export function ExperimentNotes({
  notes,
  onAddNote,
  isAdding = false,
  canAddNote = true,
}: ExperimentNotesProps) {
  const [newNote, setNewNote] = useState('')

  const handleSubmit = () => {
    const trimmed = newNote.trim()
    if (!trimmed) return
    onAddNote(trimmed)
    setNewNote('')
  }

  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && (e.metaKey || e.ctrlKey)) {
      e.preventDefault()
      handleSubmit()
    }
  }

  return (
    <Card>
      <CardHeader className="pb-4">
        <div className="flex items-center justify-between">
          <CardTitle className="text-base font-semibold flex items-center gap-2">
            <StickyNote className="size-4 text-amber-400" />
            Notes
          </CardTitle>
          {notes.length > 0 && (
            <Badge variant="outline" className="text-[11px] font-normal tabular-nums">
              {notes.length}
            </Badge>
          )}
        </div>
      </CardHeader>
      <CardContent className="space-y-5">
        {/* Add note input */}
        {canAddNote && (
          <div className="space-y-2.5">
            <Textarea
              placeholder="Add a note about this experiment..."
              value={newNote}
              onChange={(e) => setNewNote(e.target.value)}
              onKeyDown={handleKeyDown}
              rows={3}
              disabled={isAdding}
              className="resize-none text-sm"
            />
            <div className="flex items-center justify-between">
              <span className="text-[11px] text-muted-foreground/60">
                Press {navigator.platform?.includes('Mac') ? 'Cmd' : 'Ctrl'}+Enter to submit
              </span>
              <Button
                size="sm"
                onClick={handleSubmit}
                disabled={!newNote.trim() || isAdding}
              >
                {isAdding ? (
                  <Loader2 className="size-3.5 mr-1.5 animate-spin" />
                ) : (
                  <Send className="size-3.5 mr-1.5" />
                )}
                Add Note
              </Button>
            </div>
          </div>
        )}

        {/* Notes timeline */}
        {notes.length > 0 ? (
          <div className="relative space-y-0">
            {/* Timeline line */}
            {notes.length > 1 && (
              <div className="absolute left-[9px] top-3 bottom-3 w-px bg-border/60" />
            )}

            {notes.map((note) => (
              <div key={note.id} className="relative flex gap-3.5 pb-5 last:pb-0">
                {/* Timeline dot */}
                <div className="relative z-10 mt-1.5 shrink-0">
                  <div className="size-[19px] rounded-full border-2 border-amber-500/40 bg-background flex items-center justify-center">
                    <div className="size-2 rounded-full bg-amber-400" />
                  </div>
                </div>

                {/* Note content */}
                <div className="min-w-0 flex-1 space-y-1.5">
                  <p className="text-sm text-foreground leading-relaxed whitespace-pre-wrap">
                    {note.content}
                  </p>
                  <p className="text-[11px] text-muted-foreground/70">
                    {formatRelativeTime(note.createdAt)}
                  </p>
                </div>
              </div>
            ))}
          </div>
        ) : (
          <div className="flex flex-col items-center justify-center py-8 text-center">
            <div className="rounded-full bg-muted/50 p-3 mb-3">
              <MessageSquare className="size-5 text-muted-foreground/50" />
            </div>
            <p className="text-sm font-medium text-muted-foreground">No notes yet</p>
            <p className="text-xs text-muted-foreground/60 mt-1">
              {canAddNote
                ? 'Add notes to track observations during your experiment.'
                : 'No observations were recorded for this experiment.'}
            </p>
          </div>
        )}
      </CardContent>
    </Card>
  )
}
