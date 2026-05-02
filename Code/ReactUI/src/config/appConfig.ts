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

export function buildLoginUrl(): string {
  const loginUrl = new URL(appConfig.urls.login)
  loginUrl.searchParams.set("returnUrl", appConfig.frontendBaseUrl)
  return loginUrl.toString()
}

function combine(baseUrl: string, path: string): string {
  if (path.startsWith("http://") || path.startsWith("https://")) {
    return path
  }

  return `${baseUrl}${path.startsWith("/") ? path : `/${path}`}`
}

const apiBaseUrl = trimTrailingSlash(import.meta.env.VITE_API_BASE_URL || DEFAULT_API_BASE_URL)
const frontendBaseUrl = resolveFrontendBaseUrl()

const authMePath = import.meta.env.VITE_AUTH_ME_PATH || "/api/v1/auth/me"
const loginPath = import.meta.env.VITE_LOGIN_PATH || "/login"
const logoutPath = import.meta.env.VITE_LOGOUT_PATH || "/logout"
const backendReadyPath = import.meta.env.VITE_BACKEND_READY_PATH || "/api/v1/system/ready"

const dashboardPath = import.meta.env.VITE_DASHBOARD_PATH || "/api/v1/dashboard"
const inverterReadingsPath = (import.meta.env.VITE_INVERTERS_PATH || "/api/v1/dashboard/inverterInfo") + "/inverterreadings"
const addressesPath = import.meta.env.VITE_ADDRESSES_PATH || "/api/v1/dashboard/addressInfo"
const invertersPath = import.meta.env.VITE_INVERTERS_PATH || "/api/v1/dashboard/inverterInfo"
const consumptionPath = import.meta.env.VITE_CONSUMPTION_PATH || "/api/v1/dashboard/consumptionReadings"
const dailyBalancePath = import.meta.env.VITE_DAILY_BALANCE_PATH || "/api/v1/dashboard/analytics/dailybalance"
const transferRulesPath = import.meta.env.VITE_TRANSFER_RULE_PATH || "/api/v1/dashboard/transferRules"
const transferWorkflowsPath = import.meta.env.VITE_TRANSFER_WORKFLOW_PATH || "/api/v1/dashboard/transferWorkflows"
const transferHistoryPath = import.meta.env.VITE_TRANSFER_HISTORY_PATH || "/api/v1/dashboard/transfers/history"
const sourcePoliciesPath = import.meta.env.VITE_SOURCE_POLICIES_PATH || "/api/v1/dashboard/sourcePolicies"

export const appConfig = {
  apiBaseUrl,
  frontendBaseUrl,
  paths: {
    authMe: authMePath,
    login: loginPath,
    logout: logoutPath,
    backendReady: backendReadyPath,
    dashboard: dashboardPath,
    addresses: addressesPath,
    inverters: invertersPath,
    consumption: consumptionPath,
    dailyBalance: dailyBalancePath,
    transferRules: transferRulesPath,
    transferWorkflows: transferWorkflowsPath,
    transferHistory: transferHistoryPath,
    sourcePolicies: sourcePoliciesPath,
  },
  urls: {
    authMe: combine(apiBaseUrl, authMePath),
    login: combine(apiBaseUrl, loginPath),
    logout: combine(apiBaseUrl, logoutPath),
    backendReady: combine(apiBaseUrl, backendReadyPath),
    dashboard: combine(apiBaseUrl, dashboardPath),
    inverterReadings: combine(apiBaseUrl, inverterReadingsPath),
    addresses: combine(apiBaseUrl, addressesPath),
    inverters: combine(apiBaseUrl, invertersPath),
    consumptionReadings: combine(apiBaseUrl, consumptionPath),
    dailyBalance: combine(apiBaseUrl, dailyBalancePath),
    transferRules: combine(apiBaseUrl, transferRulesPath),
    transferWorkflows: combine(apiBaseUrl, transferWorkflowsPath),
    transferHistory: combine(apiBaseUrl, transferHistoryPath),
    sourcePolicies: combine(apiBaseUrl, sourcePoliciesPath),
  },
}


