export type AppRole = 'Super' | 'Admin' | 'User'

export interface AuthResult {
  success: boolean
  accessToken?: string
  refreshToken?: string
  expiresAt?: string
  user?: UserInfo
  error?: string
}

export interface UserInfo {
  id: string
  email: string
  displayName: string | null
  hasProfile: boolean
  authProvider: 'Email' | 'Google' | 'Apple' | 'Microsoft'
  roles: AppRole[]
}

export interface RegisterRequest {
  email: string
  password: string
  displayName?: string
}

export interface LoginRequest {
  email: string
  password: string
}

export interface ForgotPasswordRequest {
  email: string
}

export interface ResetPasswordRequest {
  email: string
  token: string
  newPassword: string
}
