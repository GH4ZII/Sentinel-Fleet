const apiBase = import.meta.env.VITE_API_BASE_URL ?? ''

const ACCESS_KEY = 'sf_access_token'
const REFRESH_KEY = 'sf_refresh_token'

export type User = {
  id: string
  email: string
  firstName: string
  lastName: string
  lastLoginAt: string | null
  organizationId?: string | null
  organizationRole?: string | null
}

export type AuthResponse = {
  user: User
  accessToken: string
  refreshToken: string
  accessTokenExpiresAt: string
  refreshTokenExpiresAt: string
}

export type Asset = {
  id: string
  assetTypeId: string
  assetTypeName: string
  name: string
  assetNumber: string | null
  registrationNumber: string | null
  serialNumber: string | null
  manufacturer: string | null
  model: string | null
  status: string
  criticality: string
  currentUserId: string | null
  mapLatitude: number | null
  mapLongitude: number | null
  createdAt: string
  updatedAt: string
}

export type CreateAssetResponse = {
  asset: Asset
  deviceApiKey: string | null
}

export function getAccessToken() {
  return localStorage.getItem(ACCESS_KEY)
}

export function setTokens(accessToken: string, refreshToken: string) {
  localStorage.setItem(ACCESS_KEY, accessToken)
  localStorage.setItem(REFRESH_KEY, refreshToken)
}

export function clearTokens() {
  localStorage.removeItem(ACCESS_KEY)
  localStorage.removeItem(REFRESH_KEY)
}

export function isAuthenticated() {
  return Boolean(getAccessToken())
}

async function parseError(response: Response): Promise<string> {
  try {
    const body = (await response.json()) as { error?: string }
    return body.error ?? `${response.status} ${response.statusText}`
  } catch {
    return `${response.status} ${response.statusText}`
  }
}

export async function apiFetch<T>(
  path: string,
  init: RequestInit = {},
): Promise<T> {
  const headers = new Headers(init.headers)
  if (!headers.has('Content-Type') && init.body) {
    headers.set('Content-Type', 'application/json')
  }

  const token = getAccessToken()
  if (token) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  const response = await fetch(`${apiBase}${path}`, { ...init, headers })
  if (!response.ok) {
    throw new Error(await parseError(response))
  }

  if (response.status === 204) {
    return undefined as T
  }

  return response.json() as Promise<T>
}

export async function register(input: {
  email: string
  password: string
  firstName: string
  lastName: string
  organizationName?: string
}) {
  const data = await apiFetch<AuthResponse>('/api/v1/auth/register', {
    method: 'POST',
    body: JSON.stringify(input),
  })
  setTokens(data.accessToken, data.refreshToken)
  return data
}

export async function login(input: { email: string; password: string }) {
  const data = await apiFetch<AuthResponse>('/api/v1/auth/login', {
    method: 'POST',
    body: JSON.stringify(input),
  })
  setTokens(data.accessToken, data.refreshToken)
  return data
}

export async function logout() {
  const refreshToken = localStorage.getItem(REFRESH_KEY)
  if (refreshToken) {
    try {
      await apiFetch('/api/v1/auth/logout', {
        method: 'POST',
        body: JSON.stringify({ refreshToken }),
      })
    } catch {
      // ignore logout errors
    }
  }
  clearTokens()
}

export async function listAssets() {
  return apiFetch<Asset[]>('/api/v1/assets')
}

export async function getAsset(assetId: string) {
  return apiFetch<Asset>(`/api/v1/assets/${assetId}`)
}

export async function createAsset(input: {
  name: string
  registrationNumber?: string
  manufacturer?: string
  model?: string
}) {
  return apiFetch<CreateAssetResponse>('/api/v1/assets', {
    method: 'POST',
    body: JSON.stringify({ ...input, createDevice: true }),
  })
}
