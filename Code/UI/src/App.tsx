import { useEffect, useState } from "react"
import AppLayout from "./components/AppLayout"
import AnonymousHome from "./components/AnonymousHome"
import DashboardPageQuery from "./pages/DashboardPageQuery"
import { fetchCurrentUser, login, logout, type UserInfo } from "./api/bffApi"

function App() {
  const [user, setUser] = useState<UserInfo | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    async function loadUser() {
      try {
        setLoading(true)
        setError(null)

        const currentUser = await fetchCurrentUser()
        setUser(currentUser)
      } catch (err) {
        setError(err instanceof Error ? err.message : "Unexpected error")
      } finally {
        setLoading(false)
      }
    }

    loadUser()
  }, [])

  return (
    <AppLayout
      userName={user?.name}
      roles={user?.roles ?? []}
      isAuthenticated={!!user}
      onLogin={login}
      onLogout={logout}
    >
      {loading ? (
        <p>Loading...</p>
      ) : error ? (
        <p>{error}</p>
      ) : !user ? (
         <AnonymousHome />
      ) : (
        <DashboardPageQuery permissions={user.permissions} />
      )}
    </AppLayout>
  )
}

export default App