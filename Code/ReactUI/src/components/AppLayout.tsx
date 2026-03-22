import type { ReactNode } from "react"
import Header from "./Header"
import Footer from "./Footer"

type AppLayoutProps = {
  children: ReactNode
  userName?: string
  roles: string[]
  isAuthenticated: boolean
  onLogin: () => void
  onLogout: () => void
  menuHorizontal?: boolean
  onToggleMenuLayout?: () => void
}

export default function AppLayout({
  children,
  userName,
  roles,
  isAuthenticated,
  onLogin,
  onLogout,
  menuHorizontal,
  onToggleMenuLayout,
}: AppLayoutProps) {
  return (
    <div
      style={{
        minHeight: "100vh",
        display: "grid",
        gridTemplateRows: "72px 1fr auto",
        backgroundColor: "#f9fafb",
      }}
    >
      <Header
        userName={userName}
        roles={roles}
        isAuthenticated={isAuthenticated}
        onLogin={onLogin}
        onLogout={onLogout}
        menuHorizontal={menuHorizontal}
        onToggleMenuLayout={onToggleMenuLayout}
      />

      <main style={{ padding: "24px" }}>{children}</main>

      <Footer />
    </div>
  )
}