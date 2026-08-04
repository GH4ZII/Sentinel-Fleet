import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { LoginPage, RegisterPage } from './features/auth/AuthPages'
import { AssetsPage } from './features/assets/AssetsPage'
import { NewAssetPage } from './features/assets/NewAssetPage'
import { AssetDetailPage } from './features/assets/AssetDetailPage'
import { AppShell, RequireAuth } from './layouts/AppShell'

const queryClient = new QueryClient()

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route element={<RequireAuth />}>
            <Route element={<AppShell />}>
              <Route path="/" element={<Navigate to="/assets" replace />} />
              <Route path="/assets" element={<AssetsPage />} />
              <Route path="/assets/new" element={<NewAssetPage />} />
              <Route path="/assets/:assetId" element={<AssetDetailPage />} />
            </Route>
          </Route>
          <Route path="*" element={<Navigate to="/assets" replace />} />
        </Routes>
      </BrowserRouter>
    </QueryClientProvider>
  )
}
