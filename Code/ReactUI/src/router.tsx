
import { createBrowserRouter } from "react-router-dom";
import InverterReadingsPage from "./pages/InverterReadingsPage";
import Inverters from "./pages/Inverters";
import AdrMgmt from "./pages/AdrMgmt";
import TransferRules from "./pages/TransferRules";
import NewTransfer from "./pages/NewTransfer";
import { MainMenuRouter } from "./pages/MainMenuRouter";
import AppLayoutRoute from "./AppLayoutRoute";

export const router = createBrowserRouter([
  {
    path: "/",
    element: <AppLayoutRoute />,
    children: [
      {
        element: <MainMenuRouterWrapper />, // see below
        children: [
          { index: true, element: <div>Home (placeholder)</div> },
          { path: "assets/addresses", element: <AdrMgmt /> },
          { path: "assets/inverters", element: <Inverters /> },       
          { path: "transfers/rules", element: <TransferRules /> },
          { path: "transfers/new", element: <NewTransfer /> },
          { path: "data/inverterreadings", element: <InverterReadingsPage permissions={[]}/> },
           { path: "data/consumptionreadings", element: <ConsumptionPage permissions={[]}/> },
          { path: "analytics", element: <div>Analytics (placeholder)</div> },
           { path: "analytics/overview", element: <DailyBalancePage permissions={[]}/> },
          { path: "admin", element: <div>Admin (placeholder)</div> },
        ],
      },
    ],
  },
]);

// Wrapper to get menuHorizontal from Outlet context
import { useOutletContext } from "react-router-dom";
import ConsumptionPage from "./pages/ConsumptionReadingsPage";
import DailyBalancePage from "./pages/DailyBalancePage";
function MainMenuRouterWrapper() {
  const { menuHorizontal } = useOutletContext<{ menuHorizontal: boolean }>();
  return <MainMenuRouter menuHorizontal={menuHorizontal} />;
}
