import './App.css'
//import DashboardPage from "./pages/DashboardPage"
import DashboardPageQuery from "./pages/DashboardPageQuery"


export default function App() {
  return (
    <div style={{ padding: 40, fontFamily: "Arial"  }}>
      <h2>Dashboard</h2>
      <DashboardPageQuery />
    </div>
  )
}


