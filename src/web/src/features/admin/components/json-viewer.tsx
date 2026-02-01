import { useState } from 'react'
import { ChevronDown, ChevronRight, Copy, Check } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

interface JsonViewerProps {
  data: unknown
  title: string
  defaultExpanded?: boolean
  maxHeight?: string
}

export function JsonViewer({ data, title, defaultExpanded = false, maxHeight = '400px' }: JsonViewerProps) {
  const [isExpanded, setIsExpanded] = useState(defaultExpanded)
  const [copied, setCopied] = useState(false)

  const jsonString = data !== undefined && data !== null
    ? JSON.stringify(data, null, 2)
    : 'null'

  const handleCopy = async () => {
    await navigator.clipboard.writeText(jsonString)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  if (data === undefined || data === null) {
    return (
      <div className="border rounded-lg">
        <div className="flex items-center justify-between p-3 bg-muted/30">
          <span className="text-sm font-medium text-muted-foreground">{title}</span>
          <span className="text-xs text-muted-foreground">No data</span>
        </div>
      </div>
    )
  }

  return (
    <div className="border rounded-lg overflow-hidden">
      <button
        type="button"
        className="flex items-center justify-between w-full p-3 bg-muted/30 hover:bg-muted/50 transition-colors"
        onClick={() => setIsExpanded(!isExpanded)}
      >
        <div className="flex items-center gap-2">
          {isExpanded ? (
            <ChevronDown className="size-4 text-muted-foreground" />
          ) : (
            <ChevronRight className="size-4 text-muted-foreground" />
          )}
          <span className="text-sm font-medium">{title}</span>
        </div>
        <span className="text-xs text-muted-foreground">
          {Array.isArray(data) ? `${data.length} items` : typeof data === 'object' ? `${Object.keys(data as object).length} keys` : typeof data}
        </span>
      </button>

      {isExpanded && (
        <div className="relative">
          <Button
            variant="ghost"
            size="sm"
            className="absolute top-2 right-2 z-10 h-7 px-2 text-xs"
            onClick={handleCopy}
          >
            {copied ? (
              <>
                <Check className="size-3 mr-1" />
                Copied
              </>
            ) : (
              <>
                <Copy className="size-3 mr-1" />
                Copy
              </>
            )}
          </Button>
          <pre
            className={cn(
              'p-4 text-xs overflow-auto bg-zinc-950/50 text-zinc-300',
              'font-mono whitespace-pre-wrap break-words'
            )}
            style={{ maxHeight }}
          >
            {jsonString}
          </pre>
        </div>
      )}
    </div>
  )
}
