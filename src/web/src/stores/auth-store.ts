import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { AppRole, AuthResult, UserInfo } from '@/types'

interface AuthState {
  user: UserInfo | null
  accessToken: string | null
  refreshToken: string | null
  isAuthenticated: boolean
  isLoading: boolean

  setAuth: (result: AuthResult) => void
  updateTokens: (accessToken: string, refreshToken: string) => void
  setHasProfile: (hasProfile: boolean) => void
  logout: () => void
  setLoading: (loading: boolean) => void
  hasRole: (role: AppRole) => boolean
  hasAnyRole: (roles: AppRole[]) => boolean
  isAdmin: () => boolean
  isSuper: () => boolean
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      user: null,
      accessToken: null,
      refreshToken: null,
      isAuthenticated: false,
      isLoading: true,

      setAuth: (result) => {
        if (result.success && result.accessToken && result.user) {
          localStorage.setItem('auth_token', result.accessToken)
          set({
            user: result.user,
            accessToken: result.accessToken,
            refreshToken: result.refreshToken ?? null,
            isAuthenticated: true,
            isLoading: false,
          })
        }
      },

      updateTokens: (accessToken, refreshToken) => {
        localStorage.setItem('auth_token', accessToken)
        set({ accessToken, refreshToken })
      },

      setHasProfile: (hasProfile) => {
        set((state) => ({
          user: state.user ? { ...state.user, hasProfile } : null,
        }))
      },

      logout: () => {
        localStorage.removeItem('auth_token')
        set({
          user: null,
          accessToken: null,
          refreshToken: null,
          isAuthenticated: false,
          isLoading: false,
        })
      },

      setLoading: (loading) => set({ isLoading: loading }),

      hasRole: (role: AppRole) => {
        const state = get()
        return state.user?.roles?.includes(role) ?? false
      },

      hasAnyRole: (roles: AppRole[]) => {
        const state = get()
        return roles.some((role) => state.user?.roles?.includes(role)) ?? false
      },

      isAdmin: () => {
        const state = get()
        return state.user?.roles?.some((r: AppRole) => r === 'Super' || r === 'Admin') ?? false
      },

      isSuper: () => {
        const state = get()
        return state.user?.roles?.includes('Super') ?? false
      },
    }),
    {
      name: 'auth-storage',
      partialize: (state) => ({
        user: state.user,
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
      }),
      onRehydrateStorage: () => (state) => {
        if (state) {
          state.isAuthenticated = !!state.accessToken
          state.isLoading = false
          // Sync localStorage with store
          if (state.accessToken) {
            localStorage.setItem('auth_token', state.accessToken)
          }
        }
      },
    }
  )
)
