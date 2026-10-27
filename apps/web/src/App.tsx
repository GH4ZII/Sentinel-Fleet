import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { LoginPage, RegisterPage } from './features/auth/AuthPages'
import { AcceptInvitationPage } from './features/auth/AcceptInvitationPage'
import { AssetsPage } from './features/assets/AssetsPage'
import { NewAssetPage } from './features/assets/NewAssetPage'
import { AssetDetailPage } from './features/assets/AssetDetailPage'
import { DevicesPage } from './features/devices/DevicesPage'
import { NewDevicePage } from './features/devices/NewDevicePage'
import { DeviceDetailPage } from './features/devices/DeviceDetailPage'
import { GeofencesPage } from './features/geofences/GeofencesPage'
import { NewGeofencePage } from './features/geofences/NewGeofencePage'
import { GeofenceDetailPage } from './features/geofences/GeofenceDetailPage'
import { DetectionsPage } from './features/detections/DetectionsPage'
import { IncidentsPage } from './features/incidents/IncidentsPage'
import { IncidentDetailPage } from './features/incidents/IncidentDetailPage'
import { OrganizationSettingsPage } from './features/settings/OrganizationSettingsPage'
import { UsersSettingsPage } from './features/settings/UsersSettingsPage'
import { AppShell, RequireAuth } from './layouts/AppShell'
import { ErrorBoundary } from './components/ErrorBoundary'

const queryClient = new QueryClient()

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/accept-invitation" element={<AcceptInvitationPage />} />
          <Route element={<RequireAuth />}>
            <Route element={<AppShell />}>
              <Route path="/" element={<Navigate to="/assets" replace />} />
              <Route path="/assets" element={<AssetsPage />} />
              <Route path="/assets/new" element={<NewAssetPage />} />
              <Route path="/assets/:assetId" element={<AssetDetailPage />} />
              <Route path="/devices" element={<DevicesPage />} />
              <Route path="/devices/new" element={<NewDevicePage />} />
              <Route path="/devices/:deviceId" element={<DeviceDetailPage />} />
              <Route path="/geofences" element={<GeofencesPage />} />
              <Route path="/geofences/new" element={<NewGeofencePage />} />
              <Route path="/geofences/:geofenceId" element={<GeofenceDetailPage />} />
              <Route path="/detections" element={<DetectionsPage />} />
              <Route path="/incidents" element={<IncidentsPage />} />
              <Route
                path="/incidents/:incidentId"
                element={
                  <ErrorBoundary fallbackTitle="Incident page crashed">
                    <IncidentDetailPage />
                  </ErrorBoundary>
                }
              />
              <Route path="/settings" element={<Navigate to="/settings/organization" replace />} />
              <Route path="/settings/organization" element={<OrganizationSettingsPage />} />
              <Route path="/settings/users" element={<UsersSettingsPage />} />
            </Route>
          </Route>
          <Route path="*" element={<Navigate to="/assets" replace />} />
        </Routes>
      </BrowserRouter>
    </QueryClientProvider>
  )
}
