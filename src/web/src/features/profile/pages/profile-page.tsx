import { useProfile } from '../hooks/use-profile'
import { useUpdateValues } from '../hooks/use-update-values'
import { useUpdateRoles } from '../hooks/use-update-roles'
import { useUpdatePreferences } from '../hooks/use-update-preferences'
import { useUpdateConstraints } from '../hooks/use-update-constraints'
import { useSeasons, useCreateSeason, useEndSeason } from '../hooks/use-seasons'
import { ProfileHeader } from '../components/profile-header'
import { ValuesSection } from '../components/values'
import { RolesSection } from '../components/roles'
import { PreferencesSection } from '../components/preferences'
import { ConstraintsSection } from '../components/constraints'
import { SeasonSection } from '../components/season'

export function Component() {
  // Profile existence check is handled by RootLayout
  const { data: profile, isLoading: isLoadingProfile } = useProfile()
  const { data: seasons, isLoading: isLoadingSeasons } = useSeasons()

  const updateValues = useUpdateValues()
  const updateRoles = useUpdateRoles()
  const updatePreferences = useUpdatePreferences()
  const updateConstraints = useUpdateConstraints()
  const createSeason = useCreateSeason()
  const endSeason = useEndSeason()

  return (
    <div className="min-h-screen bg-background">
      <div className="container max-w-4xl py-8 px-4 sm:px-6 lg:px-8">
        <ProfileHeader
          profile={profile ?? undefined}
          isLoading={isLoadingProfile}
        />

        <div className="mt-8 space-y-6">
          <ValuesSection
            values={profile?.values || []}
            isLoading={isLoadingProfile}
            onSave={async (values) => {
              await updateValues.mutateAsync({ values })
            }}
            isSaving={updateValues.isPending}
          />

          <RolesSection
            roles={profile?.roles || []}
            isLoading={isLoadingProfile}
            onSave={async (roles) => {
              await updateRoles.mutateAsync({ roles })
            }}
            isSaving={updateRoles.isPending}
          />

          <PreferencesSection
            preferences={profile?.preferences}
            isLoading={isLoadingProfile}
            onSave={async (preferences) => {
              await updatePreferences.mutateAsync(preferences)
            }}
            isSaving={updatePreferences.isPending}
          />

          <ConstraintsSection
            constraints={profile?.constraints}
            isLoading={isLoadingProfile}
            onSave={async (constraints) => {
              await updateConstraints.mutateAsync(constraints)
            }}
            isSaving={updateConstraints.isPending}
          />

          <SeasonSection
            seasons={seasons}
            isLoading={isLoadingSeasons}
            onCreateSeason={async (request) => {
              await createSeason.mutateAsync(request)
            }}
            onEndSeason={async (seasonId, outcome) => {
              await endSeason.mutateAsync({ seasonId, request: { outcome } })
            }}
            isCreating={createSeason.isPending}
            isEnding={endSeason.isPending}
          />
        </div>
      </div>
    </div>
  )
}
