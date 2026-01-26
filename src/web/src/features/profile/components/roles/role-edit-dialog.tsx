import { useState, useEffect } from 'react'
import { Plus, Trash2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Slider } from '@/components/ui/slider'
import { Switch } from '@/components/ui/switch'
import type { UserRoleDto } from '@/types'

interface RoleEditDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  roles: UserRoleDto[]
  onSave: (roles: UserRoleDto[]) => Promise<void>
  isSaving: boolean
}

export function RoleEditDialog({
  open,
  onOpenChange,
  roles,
  onSave,
  isSaving,
}: RoleEditDialogProps) {
  const [editedRoles, setEditedRoles] = useState<UserRoleDto[]>([])

  useEffect(() => {
    if (open) {
      setEditedRoles([...roles].sort((a, b) => a.rank - b.rank))
    }
  }, [open, roles])

  const handleAdd = () => {
    const newRole: UserRoleDto = {
      id: crypto.randomUUID(),
      key: null,
      label: '',
      rank: editedRoles.length + 1,
      seasonPriority: 3,
      minWeeklyMinutes: 0,
      targetWeeklyMinutes: 300, // 5 hours default
      tags: [],
      status: 'Active',
    }
    setEditedRoles([...editedRoles, newRole])
  }

  const handleRemove = (id: string) => {
    const filtered = editedRoles.filter((r) => r.id !== id)
    const reranked = filtered.map((r, index) => ({ ...r, rank: index + 1 }))
    setEditedRoles(reranked)
  }

  const handleUpdate = (id: string, updates: Partial<UserRoleDto>) => {
    setEditedRoles(
      editedRoles.map((r) => (r.id === id ? { ...r, ...updates } : r))
    )
  }

  const handleSave = async () => {
    const validRoles = editedRoles.filter((r) => r.label.trim() !== '')
    await onSave(validRoles)
    onOpenChange(false)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[85vh] overflow-y-auto sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle>Edit Your Roles</DialogTitle>
          <DialogDescription>
            Define the different "hats" you wear in life. Set weekly time targets for each.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-6 py-4">
          {editedRoles.map((role) => (
            <div
              key={role.id}
              className="rounded-lg border border-border p-4 space-y-4"
            >
              <div className="flex items-start justify-between gap-4">
                <div className="flex-1 space-y-3">
                  <Input
                    placeholder="Role name (e.g., Engineer, Parent)"
                    value={role.label}
                    onChange={(e) =>
                      handleUpdate(role.id, { label: e.target.value })
                    }
                  />

                  <div className="grid gap-4 sm:grid-cols-2">
                    <div className="space-y-2">
                      <Label className="text-xs">Weekly hours</Label>
                      <div className="flex items-center gap-2">
                        <Slider
                          value={[role.targetWeeklyMinutes / 60]}
                          onValueChange={([value]) =>
                            handleUpdate(role.id, {
                              targetWeeklyMinutes: value * 60,
                            })
                          }
                          max={60}
                          step={1}
                          className="flex-1"
                        />
                        <span className="w-12 text-right text-sm font-medium">
                          {Math.round(role.targetWeeklyMinutes / 60)}h
                        </span>
                      </div>
                    </div>

                    <div className="space-y-2">
                      <Label className="text-xs">Season priority (1-5)</Label>
                      <div className="flex items-center gap-2">
                        <Slider
                          value={[role.seasonPriority]}
                          onValueChange={([value]) =>
                            handleUpdate(role.id, { seasonPriority: value })
                          }
                          min={1}
                          max={5}
                          step={1}
                          className="flex-1"
                        />
                        <span className="w-8 text-right text-sm font-medium">
                          P{role.seasonPriority}
                        </span>
                      </div>
                    </div>
                  </div>

                  <div className="flex items-center gap-2">
                    <Switch
                      checked={role.status === 'Active'}
                      onCheckedChange={(checked) =>
                        handleUpdate(role.id, {
                          status: checked ? 'Active' : 'Inactive',
                        })
                      }
                    />
                    <Label className="text-sm">Active</Label>
                  </div>
                </div>

                <Button
                  variant="ghost"
                  size="icon"
                  className="text-destructive hover:bg-destructive/10"
                  onClick={() => handleRemove(role.id)}
                >
                  <Trash2 className="size-4" />
                </Button>
              </div>
            </div>
          ))}

          <Button variant="outline" onClick={handleAdd} className="w-full">
            <Plus className="mr-2 size-4" />
            Add Role
          </Button>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button onClick={handleSave} disabled={isSaving}>
            {isSaving ? 'Saving...' : 'Save Changes'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
