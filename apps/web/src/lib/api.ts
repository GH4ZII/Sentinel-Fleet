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

export type Coordinate = { longitude: number; latitude: number }

export type PolygonGeometry = {
  type: string
  coordinates: number[][][]
}

export type Geofence = {
  id: string
  name: string
  description: string | null
  geofenceType: 'Allowed' | 'Restricted' | string
  isActive: boolean
  geometry: PolygonGeometry
  createdAt: string
  updatedAt: string
}

export type AssetGeofence = {
  id: string
  assetId: string
  geofenceId: string
  ruleType: 'Enter' | 'Exit' | 'Both' | string
  validFrom: string | null
  validTo: string | null
}

export type Detection = {
  id: string
  assetId: string
  ruleId: string | null
  detectionType: string
  severity: string
  confidence: number
  riskContribution: number
  title: string
  description: string | null
  triggeredAt: string
  sourceEventIds: string | null
  metadata: string | null
  incidentId: string | null
  createdAt: string
}

export async function listGeofences() {
  return apiFetch<Geofence[]>('/api/v1/geofences')
}

export async function getGeofence(geofenceId: string) {
  return apiFetch<Geofence>(`/api/v1/geofences/${geofenceId}`)
}

export async function createGeofence(input: {
  name: string
  description?: string
  geofenceType: string
  coordinates: Coordinate[]
  isActive?: boolean
}) {
  return apiFetch<Geofence>('/api/v1/geofences', {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export async function deleteGeofence(geofenceId: string) {
  return apiFetch<void>(`/api/v1/geofences/${geofenceId}`, { method: 'DELETE' })
}

export async function listGeofenceAssets(geofenceId: string) {
  return apiFetch<AssetGeofence[]>(`/api/v1/geofences/${geofenceId}/assets`)
}

export async function linkGeofenceAsset(
  geofenceId: string,
  input: { assetId: string; ruleType?: string },
) {
  return apiFetch<AssetGeofence>(`/api/v1/geofences/${geofenceId}/assets`, {
    method: 'POST',
    body: JSON.stringify(input),
  })
}

export async function unlinkGeofenceAsset(geofenceId: string, assetId: string) {
  return apiFetch<void>(`/api/v1/geofences/${geofenceId}/assets/${assetId}`, {
    method: 'DELETE',
  })
}

export async function listDetections(params?: {
  assetId?: string
  detectionType?: string
  limit?: number
}) {
  const search = new URLSearchParams()
  if (params?.assetId) search.set('assetId', params.assetId)
  if (params?.detectionType) search.set('detectionType', params.detectionType)
  if (params?.limit) search.set('limit', String(params.limit))
  const qs = search.toString()
  return apiFetch<Detection[]>(`/api/v1/detections${qs ? `?${qs}` : ''}`)
}

export type Incident = {
  id: string
  primaryAssetId: string
  title: string
  description: string | null
  incidentType: string
  status: string
  severity: string
  riskScore: number
  confidence: number
  startedAt: string
  endedAt: string | null
  detectedAt: string
  assignedToUserId: string | null
  createdAt: string
  updatedAt: string
  detectionCount: number
  latestRisk: RiskAssessment | null
}

export type RiskAssessment = {
  id: string
  score: number
  riskLevel: string
  factors: string
  modelVersion: string
  calculatedAt: string
}

export type IncidentTimelineEntry = {
  id: string
  entryType: string
  timestamp: string
  title: string
  description: string | null
  sourceType: string | null
  sourceId: string | null
  latitude: number | null
  longitude: number | null
  metadata: string | null
  createdByUserId: string | null
  createdAt: string
}

export type IncidentEntity = {
  id: string
  entityType: string
  entityId: string
  relationshipType: string
  firstObservedAt: string
  lastObservedAt: string
  metadata: string | null
}

export type IncidentComment = {
  id: string
  userId: string
  content: string
  createdAt: string
  updatedAt: string
}

export type IncidentAttachment = {
  id: string
  uploadedByUserId: string
  name: string
  contentType: string
  size: number
  createdAt: string
}

export type IncidentPosition = {
  eventId: string
  latitude: number
  longitude: number
  speedKph: number | null
  heading: number | null
  recordedAt: string
}

export type DetectionSummary = {
  id: string
  detectionType: string
  severity: string
  riskContribution: number
  title: string
  description: string | null
  triggeredAt: string
  metadata: string | null
}

export type AuditLog = {
  id: string
  userId: string | null
  action: string
  entityType: string
  entityId: string
  oldValues: string | null
  newValues: string | null
  ipAddress: string | null
  createdAt: string
}

export type IncidentDetail = {
  incident: Incident
  detections: DetectionSummary[]
  timeline: IncidentTimelineEntry[]
  relationships: IncidentEntity[]
  comments: IncidentComment[]
  attachments: IncidentAttachment[]
  audit: AuditLog[]
}

export async function listIncidents(params?: {
  assetId?: string
  status?: string
  limit?: number
}) {
  const search = new URLSearchParams()
  if (params?.assetId) search.set('assetId', params.assetId)
  if (params?.status) search.set('status', params.status)
  if (params?.limit) search.set('limit', String(params.limit))
  const qs = search.toString()
  return apiFetch<Incident[]>(`/api/v1/incidents${qs ? `?${qs}` : ''}`)
}

export async function getIncident(incidentId: string) {
  return apiFetch<IncidentDetail>(`/api/v1/incidents/${incidentId}`)
}

export async function getIncidentPositions(incidentId: string) {
  return apiFetch<IncidentPosition[]>(`/api/v1/incidents/${incidentId}/positions`)
}

export async function updateIncident(
  incidentId: string,
  input: { status?: string; title?: string; description?: string },
) {
  return apiFetch<Incident>(`/api/v1/incidents/${incidentId}`, {
    method: 'PATCH',
    body: JSON.stringify(input),
  })
}

export async function resolveIncident(incidentId: string, resolutionNote?: string) {
  return apiFetch<Incident>(`/api/v1/incidents/${incidentId}/resolve`, {
    method: 'POST',
    body: JSON.stringify({ resolutionNote: resolutionNote ?? null }),
  })
}

export async function addIncidentComment(incidentId: string, content: string) {
  return apiFetch<IncidentComment>(`/api/v1/incidents/${incidentId}/comments`, {
    method: 'POST',
    body: JSON.stringify({ content }),
  })
}

export async function uploadIncidentAttachment(incidentId: string, file: File) {
  const headers = new Headers()
  const token = getAccessToken()
  if (token) headers.set('Authorization', `Bearer ${token}`)

  const form = new FormData()
  form.append('file', file)

  const response = await fetch(`${apiBase}/api/v1/incidents/${incidentId}/attachments`, {
    method: 'POST',
    headers,
    body: form,
  })
  if (!response.ok) {
    throw new Error(await parseError(response))
  }
  return response.json() as Promise<IncidentAttachment>
}

export function incidentAttachmentUrl(incidentId: string, attachmentId: string) {
  return `${apiBase}/api/v1/incidents/${incidentId}/attachments/${attachmentId}`
}

