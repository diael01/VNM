const DEFAULT_API_BASE_URL = "https://localhost:7144"

function trimTrailingSlash(value: string): string {
  return value.endsWith("/") ? value.slice(0, -1) : value
}

function resolveFrontendBaseUrl(): string {
  const configured = import.meta.env.VITE_FRONTEND_BASE_URL
  if (configured && configured.trim().length > 0) {
    return trimTrailingSlash(configured)
  }

  if (typeof window !== "undefined" && window.location?.origin) {
    return trimTrailingSlash(window.location.origin)
  }

  return "http://localhost:5173"
}

const apiBaseUrl = trimTrailingSlash(import.meta.env.VITE_API_BASE_URL || DEFAULT_API_BASE_URL)
const frontendBaseUrl = resolveFrontendBaseUrl()

const authMePath = import.meta.env.VITE_AUTH_ME_PATH || "/api/v1/auth/me"
const loginPath = import.meta.env.VITE_LOGIN_PATH || "/login"
const logoutPath = import.meta.env.VITE_LOGOUT_PATH || "/logout"
const backendReadyPath = import.meta.env.VITE_BACKEND_READY_PATH || "/api/v1/system/ready"
const dashboardPath = import.meta.env.VITE_DASHBOARD_PATH || "/api/v1/dashboard"
const dashboardBFFRedirectMeterInverter = import.meta.env.VITE_DASHBOARD_PATH_TO_METER || "/api/v1/dashboard/inverterreadings"
const inverterDataPath = import.meta.env.VITE_INVERTER_DATA_PATH || "/api/v1/inverter/data"

function combine(baseUrl: string, path: string): string {
  if (path.startsWith("http://") || path.startsWith("https://")) {
    return path
  }

  return `${baseUrl}${path.startsWith("/") ? path : `/${path}`}`
}

export const appConfig = {
  apiBaseUrl,
  frontendBaseUrl,
  paths: {
    authMe: authMePath,
    login: loginPath,
    logout: logoutPath,
    backendReady: backendReadyPath,
    dashboard: dashboardPath,
    inverterData: inverterDataPath,
  },
  urls: {
    authMe: combine(apiBaseUrl, authMePath),
    login: combine(apiBaseUrl, loginPath),
    logout: combine(apiBaseUrl, logoutPath),
    backendReady: combine(apiBaseUrl, backendReadyPath),
    dashboard: combine(apiBaseUrl, dashboardPath),
    inverterData: combine(apiBaseUrl, inverterDataPath),
    inverterReadings: combine(apiBaseUrl, dashboardBFFRedirectMeterInverter),
  },
}

export function buildLoginUrl(): string {
  const loginUrl = new URL(appConfig.urls.login)
  loginUrl.searchParams.set("returnUrl", appConfig.frontendBaseUrl)
  return loginUrl.toString()
}
