# Mastery Design System

Reference documentation for UI development.

## Brand Identity

**Colors**: Orange (#EC8B5E) + Navy (#141A46)

**Philosophy**: Calm coach aesthetic - trustworthy, warm but serious, effortless.

**Target**: <2 minutes/day user input. Every interaction must be frictionless.

---

## Color Tokens

Use semantic tokens, not raw colors. This ensures consistency and enables theming.

### Primary Palette

| Token | Dark Mode | Use Case |
|-------|-----------|----------|
| `bg-background` | Deep navy | Page backgrounds |
| `bg-card` | Lighter navy | Cards, elevated surfaces |
| `bg-primary` | Orange | CTAs, primary buttons |
| `bg-secondary` | Muted navy | Secondary buttons |
| `bg-muted` | Subtle navy | Disabled states, backgrounds |

### Text Colors

| Token | Use Case |
|-------|----------|
| `text-foreground` | Primary text |
| `text-muted-foreground` | Secondary text, labels, timestamps |
| `text-primary` | Links, active states, emphasis |
| `text-primary-foreground` | Text on orange backgrounds |

### Semantic Colors

| Token | Use Case |
|-------|----------|
| `bg-success` / `text-success` | Completions, achievements, habit done |
| `bg-warning` / `text-warning` | Capacity alerts, near-limit states |
| `bg-destructive` / `text-destructive` | Errors, delete actions |
| `bg-info` / `text-info` | Informational messages |

### Brand Scales

For gradients and fine-tuned styling:

```
Orange: orange-50 through orange-900 (orange-500 = brand orange)
Navy:   navy-50 through navy-950 (navy-900 = brand navy)
```

---

## Component Usage

### shadcn/ui Components

Located in `src/components/ui/`. Import and use directly:

```tsx
import { Button } from '@/components/ui/button'
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Badge } from '@/components/ui/badge'
import { Separator } from '@/components/ui/separator'
```

### Adding New Components

```bash
cd src/web
npx shadcn@latest add [component-name]
```

Common components to add as needed:
- Forms: `form`, `textarea`, `slider`, `switch`, `checkbox`, `select`, `calendar`
- Layout: `tabs`, `sheet`, `dialog`, `dropdown-menu`
- Feedback: `progress`, `skeleton`, `sonner`, `alert`, `tooltip`

### Custom Components

Place in appropriate directory:
- `src/components/common/` - Shared composed components (page-header, loading-state)
- `src/components/layout/` - Layout components (nav-header, mobile-nav)
- `src/components/mastery/` - Domain-specific components (habit-card, metric-card)

---

## Styling Patterns

### Class Merging

Always use `cn()` for conditional classes:

```tsx
import { cn } from '@/lib/utils'

<div className={cn(
  'base-classes',
  isActive && 'active-classes',
  variant === 'highlight' && 'highlight-classes'
)} />
```

### Card Pattern

```tsx
<Card>
  <CardHeader>
    <CardTitle>Title</CardTitle>
  </CardHeader>
  <CardContent>
    Content here
  </CardContent>
</Card>
```

### Highlighted Card (Next Best Action)

```tsx
<Card className="border-2 border-orange-500/40 bg-gradient-to-br from-orange-500/15 to-orange-500/5 shadow-lg shadow-orange-500/10">
  {/* High-priority content that needs to stand out */}
</Card>
```

### Button Variants

```tsx
<Button>Primary Action</Button>
<Button variant="secondary">Secondary</Button>
<Button variant="outline">Outline</Button>
<Button variant="ghost">Ghost</Button>
<Button variant="destructive">Delete</Button>
```

### Form Pattern

```tsx
<div className="space-y-2">
  <Label htmlFor="field">Field Label</Label>
  <Input id="field" placeholder="Enter value..." />
</div>
```

---

## Spacing & Layout

### Container

```tsx
<main className="mx-auto max-w-7xl px-4 py-8">
```

### Vertical Spacing

- `space-y-2` - Tight (form fields)
- `space-y-4` - Normal (sections)
- `space-y-6` - Loose (major sections)
- `space-y-8` - Page sections

### Card Grid

```tsx
<div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
  <Card>...</Card>
  <Card>...</Card>
  <Card>...</Card>
</div>
```

---

## Typography

### Font

Inter (with system fallbacks). Loaded via Google Fonts in `index.html`.

### Scale

| Class | Use |
|-------|-----|
| `text-4xl font-bold` | Page titles |
| `text-3xl font-semibold` | Section headers |
| `text-2xl font-semibold` | Subsection headers |
| `text-xl font-medium` | Card titles |
| `text-base` | Body text |
| `text-sm` | Secondary text, labels |
| `text-xs` | Captions, timestamps |

### Text Colors by Context

| Context | Classes |
|---------|---------|
| Primary heading | `text-foreground` |
| Secondary text | `text-muted-foreground` |
| Link/active | `text-primary` |
| Success message | `text-success` |
| Warning | `text-warning` |
| Error | `text-destructive` |

---

## Icons

Using Lucide React. Import icons as needed:

```tsx
import { Check, Flame, Clock, Sparkles, ChevronRight } from 'lucide-react'

<Check className="size-4" />
<Flame className="size-5 text-orange-400" />
```

Common sizing:
- `size-3` - Inline with small text
- `size-4` - Inline with body text, buttons
- `size-5` - Standalone icons
- `size-6` - Large icons, headers

---

## Animation

Using `tw-animate-css`. Common patterns:

```tsx
// Fade in on mount
<div className="animate-in fade-in duration-300">

// Slide in from bottom
<div className="animate-in slide-in-from-bottom-4 duration-300">

// Fade out
<div className="animate-out fade-out duration-200">
```

---

## Responsive Design

### Breakpoints

- `sm:` - 640px+
- `md:` - 768px+ (tablet)
- `lg:` - 1024px+ (desktop)
- `xl:` - 1280px+

### Mobile Detection Hook

```tsx
import { useIsMobile } from '@/hooks/use-mobile'

function Component() {
  const isMobile = useIsMobile()
  return isMobile ? <MobileView /> : <DesktopView />
}
```

### Common Responsive Patterns

```tsx
// Stack on mobile, row on desktop
<div className="flex flex-col gap-4 md:flex-row">

// Hide on mobile
<div className="hidden md:block">

// Full width on mobile, constrained on desktop
<div className="w-full md:w-auto">
```

---

## Dark Mode

Dark mode is the primary theme. The `dark` class is applied to `<html>` in `index.html`.

All color tokens automatically adjust. No manual dark mode variants needed when using semantic tokens.

---

## Accessibility

### Focus States

Focus ring uses `--ring` (orange). Automatically applied to interactive elements.

### Touch Targets

Minimum 44x44px for touch targets. Use `min-h-11 min-w-11` if needed.

### Contrast

All text colors meet WCAG 4.5:1 contrast ratio against their intended backgrounds.

---

## File Reference

| File | Purpose |
|------|---------|
| `src/index.css` | All CSS variables and theme definition |
| `src/lib/utils.ts` | `cn()` utility for class merging |
| `src/hooks/use-mobile.ts` | Mobile detection hook |
| `components.json` | shadcn/ui configuration |
| `src/components/ui/` | shadcn/ui components |
