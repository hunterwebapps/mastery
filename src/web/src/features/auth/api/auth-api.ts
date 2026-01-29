import { apiClient } from '@/lib/api-client'
import type { AuthResult, LoginRequest, RegisterRequest } from '@/types'

export const authApi = {
  register: (data: RegisterRequest) =>
    apiClient.post<AuthResult>('/auth/register', data),

  registerWithProfile: (data: RegisterRequest & {
    timezone: string
    locale: string
    values: unknown[]
    roles: unknown[]
    preferences?: unknown
    constraints?: unknown
    initialSeason?: unknown
  }) =>
    apiClient.post<AuthResult>('/auth/register-with-profile', data),

  login: (data: LoginRequest) =>
    apiClient.post<AuthResult>('/auth/login', data),

  getOAuthUrl: (provider: 'Google' | 'Apple' | 'Microsoft') =>
    `${apiClient.defaults.baseURL}/auth/external/${provider}`,

  refresh: (refreshToken: string) =>
    apiClient.post<AuthResult>('/auth/refresh', { refreshToken }),

  logout: (refreshToken: string) =>
    apiClient.post('/auth/logout', { refreshToken }),

  forgotPassword: (email: string) =>
    apiClient.post('/auth/forgot-password', { email }),

  resetPassword: (data: { email: string; token: string; newPassword: string }) =>
    apiClient.post('/auth/reset-password', data),

  me: () =>
    apiClient.get<AuthResult['user']>('/auth/me'),
}
