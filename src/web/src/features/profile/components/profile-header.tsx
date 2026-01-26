import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import type { UserProfileDto } from '@/types'
import { seasonTypeInfo } from '@/types/season'

interface ProfileHeaderProps {
  profile: UserProfileDto | undefined
  isLoading: boolean
}

export function ProfileHeader({ profile, isLoading }: ProfileHeaderProps) {
  if (isLoading) {
    return (
      <div className="flex items-center gap-4">
        <Skeleton className="size-16 rounded-full" />
        <div className="space-y-2">
          <Skeleton className="h-6 w-32" />
          <Skeleton className="h-4 w-48" />
        </div>
      </div>
    )
  }

  if (!profile) return null

  const initials = profile.userId.slice(0, 2).toUpperCase()
  const seasonInfo = profile.currentSeason
    ? seasonTypeInfo[profile.currentSeason.type]
    : null

  return (
    <div className="flex items-center gap-4">
      <Avatar className="size-16 border-2 border-primary/20">
        <AvatarFallback className="bg-primary/10 text-xl font-semibold text-primary">
          {initials}
        </AvatarFallback>
      </Avatar>
      <div>
        <h1 className="text-2xl font-bold text-foreground">Your Profile</h1>
        <div className="flex items-center gap-2 text-muted-foreground">
          <span>{profile.timezone}</span>
          <span>•</span>
          <span>{profile.locale}</span>
          {profile.currentSeason && seasonInfo && (
            <>
              <span>•</span>
              <Badge variant="outline" className={seasonInfo.color}>
                {profile.currentSeason.label}
              </Badge>
            </>
          )}
        </div>
      </div>
    </div>
  )
}
