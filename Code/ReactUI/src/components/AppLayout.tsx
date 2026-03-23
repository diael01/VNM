import type { ReactNode } from "react"
import Footer from "./Footer"
import Header from "./Header"

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
    <>
      <Header
        userName={userName}
        roles={roles}
        isAuthenticated={isAuthenticated}
        onLogin={onLogin}
        onLogout={onLogout}
        menuHorizontal={menuHorizontal}
        onToggleMenuLayout={onToggleMenuLayout}
      />
      <main style={{ flex: 1, width: '100%' }}>{children}</main>
      <Footer />
    </>
  )
}