import { useEffect, useState } from "react"
import AppLayout from "./components/AppLayout"
import AnonymousHome from "./components/AnonymousHome"
import DashboardPageQuery from "./pages/DashboardPageQuery"
import InverterReadingsPage from "./pages/InverterReadingsPage"
import MainMenuRouter from "./pages/MainMenuRouter"
import { fetchBackendReady, fetchCurrentUser, login, logout, type BackendReadiness, type UserInfo } from "./api/bffApi"

const readyBackendState: BackendReadiness = {
  ready: true,
  meterReady: true,
  inverterReady: true,
}

const initialBackendState: BackendReadiness = {
  ready: false,
  meterReady: false,
  inverterReady: false,
}

function App() {
  const [user, setUser] = useState<UserInfo | null>(null)
  const [backendStatus, setBackendStatus] = useState<BackendReadiness>(initialBackendState)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [menuHorizontal, setMenuHorizontal] = useState(false)

  useEffect(() => {
    async function loadUser() {
      try {
        setLoading(true)
        setError(null)

        const currentUser = await fetchCurrentUser()
        setUser(currentUser)
        if (!currentUser) {
          setBackendStatus(await fetchBackendReady())
        } else {
          setBackendStatus(readyBackendState)
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : "Unexpected error")
        setBackendStatus(await fetchBackendReady())
      } finally {
        setLoading(false)
      }
    }

    loadUser()
  }, [])

  useEffect(() => {
    if (user || backendStatus.ready) {
      return
    }

    const intervalId = window.setInterval(async () => {
      const nextStatus = await fetchBackendReady()
      setBackendStatus(currentStatus => {
        if (
          currentStatus.ready === nextStatus.ready
          && currentStatus.meterReady === nextStatus.meterReady
          && currentStatus.inverterReady === nextStatus.inverterReady
        ) {
          return currentStatus
        }

        return nextStatus
      })
    }, 3000)

    return () => {
      window.clearInterval(intervalId)
    }
  }, [user, backendStatus.ready])

  return (
    <AppLayout
      userName={user?.name}
      roles={user?.roles ?? []}
      isAuthenticated={!!user}
      onLogin={login}
      onLogout={logout}
      menuHorizontal={menuHorizontal}
      onToggleMenuLayout={() => setMenuHorizontal(h => !h)}
    >
      {loading ? (
        <p>Loading...</p>
      ) : error ? (
        !user ? <AnonymousHome backendStatus={backendStatus} /> : <p>{error}</p>
      ) : !user ? (
        <AnonymousHome backendStatus={backendStatus} />
      ) : (
        <MainMenuRouter menuHorizontal={menuHorizontal} />
      )}
    </AppLayout>
  )
}

export default App

// Import MainMenuRouter at the top
// import MainMenuRouter from "./pages/MainMenuRouter"