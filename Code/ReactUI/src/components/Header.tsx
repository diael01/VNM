
type HeaderProps = {
  userName?: string
  roles: string[]
  isAuthenticated: boolean
  onLogin: () => void
  onLogout: () => void
  menuHorizontal?: boolean
  onToggleMenuLayout?: () => void
}


export default function Header({
  userName,
  roles,
  isAuthenticated,
  onLogin,
  onLogout,
  menuHorizontal,
  onToggleMenuLayout,
}: HeaderProps) {
  return (
    <header
      style={{
        height: "72px",
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        padding: "0 24px",
        borderBottom: "1px solid #e5e7eb",
        backgroundColor: "#ffffff",
      }}
    >
      <div style={{ display: "flex", alignItems: "center", gap: "12px" }}>
        {/* Menu layout toggle button */}
        {typeof menuHorizontal === "boolean" && onToggleMenuLayout && (
          <button
            onClick={onToggleMenuLayout}
            title={menuHorizontal ? "Horizontal menu" : "Vertical menu"}
            style={{
              width: 28,
              height: 28,
              borderRadius: 6,
              border: "1px solid #d1d5db",
              background: menuHorizontal ? "#2563eb" : "#fff",
              color: menuHorizontal ? "#fff" : "#2563eb",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              fontSize: 18,
              marginRight: 8,
              cursor: "pointer",
              padding: 0,
            }}
          >
            {menuHorizontal ? (
              <span style={{ display: "inline-block", transform: "rotate(90deg)" }}>↔</span>
            ) : (
              <span>↕</span>
            )}
          </button>
        )}
        <div
          style={{
            width: "40px",
            height: "40px",
            borderRadius: "8px",
            backgroundColor: "#dbeafe",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            fontWeight: 700,
          }}
        >
          V
        </div>

        <div>
          <div style={{ fontWeight: 700, fontSize: "18px" }}>VNM Dashboard</div>
          <div style={{ fontSize: "12px", color: "#6b7280" }}>
            Energy Monitoring Platform
          </div>
        </div>
      </div>

      <div style={{ display: "flex", alignItems: "center", gap: "16px" }}>
        {isAuthenticated ? (
          <>
            <div style={{ textAlign: "right" }}>
              <div style={{ fontWeight: 600 }}>{userName ?? "Authenticated User"}</div>
              <div style={{ fontSize: "12px", color: "#6b7280" }}>
                {roles.length > 0 ? roles.join(", ") : "No roles"}
              </div>
            </div>

            <button
              onClick={onLogout}
              style={{
                padding: "8px 14px",
                borderRadius: "8px",
                border: "1px solid #d1d5db",
                backgroundColor: "#ffffff",
                cursor: "pointer",
              }}
            >
              Logout
            </button>
          </>
        ) : (
          <button
            onClick={onLogin}
            style={{
              padding: "8px 14px",
              borderRadius: "8px",
              border: "none",
              backgroundColor: "#2563eb",
              color: "white",
              cursor: "pointer",
            }}
          >
            Login
          </button>
        )}
      </div>
    </header>
  )
}