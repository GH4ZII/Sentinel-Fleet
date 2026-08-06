import { useEffect, useState } from 'react'
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr'
import { getAccessToken } from './api'

export type LivePosition = {
  assetId: string
  latitude: number
  longitude: number
  speedKph?: number | null
  heading?: number | null
  recordedAt: string
}

const hubBase = import.meta.env.VITE_API_BASE_URL ?? ''

export function useFleetPositions() {
  const [positions, setPositions] = useState<Record<string, LivePosition>>({})
  const [connected, setConnected] = useState(false)

  useEffect(() => {
    const token = getAccessToken()
    if (!token) return

    let connection: HubConnection | null = null
    let cancelled = false

    const start = async () => {
      connection = new HubConnectionBuilder()
        .withUrl(`${hubBase}/hubs/fleet`, {
          accessTokenFactory: () => getAccessToken() ?? '',
        })
        .withAutomaticReconnect()
        .configureLogging(LogLevel.Warning)
        .build()

      connection.on('PositionUpdated', (payload: LivePosition) => {
        if (!payload?.assetId) return
        setPositions((prev) => ({
          ...prev,
          [payload.assetId]: {
            assetId: payload.assetId,
            latitude: payload.latitude,
            longitude: payload.longitude,
            speedKph: payload.speedKph,
            heading: payload.heading,
            recordedAt: payload.recordedAt,
          },
        }))
      })

      connection.onreconnected(() => setConnected(true))
      connection.onclose(() => setConnected(false))

      try {
        await connection.start()
        if (!cancelled) setConnected(true)
      } catch (err) {
        console.warn('SignalR connect failed', err)
        if (!cancelled) setConnected(false)
      }
    }

    void start()

    return () => {
      cancelled = true
      setConnected(false)
      if (connection && connection.state !== HubConnectionState.Disconnected) {
        void connection.stop()
      }
    }
  }, [])

  return { positions, connected }
}
