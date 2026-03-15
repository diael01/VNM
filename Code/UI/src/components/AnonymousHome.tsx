import type { BackendReadiness } from "../api/bffApi"

type Props = {
  backendStatus: BackendReadiness
}

export default function AnonymousHome({ backendStatus }: Props) {
  const { ready } = backendStatus

  return (
    <div>
      <h2>Welcome</h2>
      {ready ? (
        <p>Please log in to view the dashboard.</p>
      ) : (
        <p>Dependent backend services are initializing...</p>
      )}
    </div>
  )
}