import type { BackendReadiness } from "../api/bffApi"

type Props = {
  backendStatus: BackendReadiness
}

export default function AnonymousHome({ backendStatus }: Props) {
  void backendStatus

  return (
    <div>
      <h2>Welcome</h2>
      <p>Please login...</p>
    </div>
  )
}