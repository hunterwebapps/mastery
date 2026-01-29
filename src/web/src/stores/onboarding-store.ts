import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type {
  UserValueDto,
  UserRoleDto,
  PreferencesDto,
  ConstraintsDto,
  CreateSeasonRequest,
} from '@/types'
import { defaultPreferences } from '@/types/preferences'
import { defaultConstraints } from '@/types/constraints'

export interface BasicsData {
  timezone: string
  locale: string
}

export interface OnboardingData {
  basics: BasicsData | null
  values: UserValueDto[]
  roles: UserRoleDto[]
  preferences: PreferencesDto
  constraints: ConstraintsDto
  season: CreateSeasonRequest | null
}

interface OnboardingState {
  currentStep: number
  totalSteps: number
  data: OnboardingData
  isSubmitting: boolean

  // Navigation
  nextStep: () => void
  prevStep: () => void
  goToStep: (step: number) => void

  // Data setters
  setBasics: (basics: BasicsData) => void
  setValues: (values: UserValueDto[]) => void
  setRoles: (roles: UserRoleDto[]) => void
  setPreferences: (preferences: PreferencesDto) => void
  setConstraints: (constraints: ConstraintsDto) => void
  setSeason: (season: CreateSeasonRequest | null) => void

  // Submission
  setSubmitting: (isSubmitting: boolean) => void

  // Helpers
  hasPendingData: () => boolean

  // Reset
  reset: () => void
}

const initialData: OnboardingData = {
  basics: null,
  values: [],
  roles: [],
  preferences: defaultPreferences,
  constraints: defaultConstraints,
  season: null,
}

export const useOnboardingStore = create<OnboardingState>()(
  persist(
    (set, get) => ({
      currentStep: 1,
      totalSteps: 7,
      data: initialData,
      isSubmitting: false,

      nextStep: () =>
        set((state) => ({
          currentStep: Math.min(state.currentStep + 1, state.totalSteps),
        })),

      prevStep: () =>
        set((state) => ({
          currentStep: Math.max(state.currentStep - 1, 1),
        })),

      goToStep: (step) =>
        set((state) => ({
          currentStep: Math.min(Math.max(step, 1), state.totalSteps),
        })),

      setBasics: (basics) =>
        set((state) => ({
          data: { ...state.data, basics },
        })),

      setValues: (values) =>
        set((state) => ({
          data: { ...state.data, values },
        })),

      setRoles: (roles) =>
        set((state) => ({
          data: { ...state.data, roles },
        })),

      setPreferences: (preferences) =>
        set((state) => ({
          data: { ...state.data, preferences },
        })),

      setConstraints: (constraints) =>
        set((state) => ({
          data: { ...state.data, constraints },
        })),

      setSeason: (season) =>
        set((state) => ({
          data: { ...state.data, season },
        })),

      setSubmitting: (isSubmitting) => set({ isSubmitting }),

      hasPendingData: () => {
        const state = get()
        return state.data.basics !== null
      },

      reset: () =>
        set({
          currentStep: 1,
          data: initialData,
          isSubmitting: false,
        }),
    }),
    {
      name: 'onboarding-storage',
      partialize: (state) => ({
        currentStep: state.currentStep,
        data: state.data,
      }),
    }
  )
)
