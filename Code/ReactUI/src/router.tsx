import { lazy, Suspense } from "react";
import { createBrowserRouter, useOutletContext } from "react-router-dom";
import { MainMenuRouter } from "./pages/MainMenuRouter";
import AppLayoutRoute from "./AppLayoutRoute";

const InverterReadingsPage = lazy(() => import("./pages/InverterReadingsPage"));
const Inverters = lazy(() => import("./pages/Inverters"));
const AdrMgmt = lazy(() => import("./pages/AdrMgmt"));
const TransferRules = lazy(() => import("./pages/TransferRules"));
const NewTransfer = lazy(() => import("./pages/NewTransfer"));
const ConsumptionPage = lazy(() => import("./pages/ConsumptionReadingsPage"));
const DailyBalancePage = lazy(() => import("./pages/DailyBalancePage"));

function withSuspense(element: React.ReactNode) {
  return <Suspense fallback={<p>Loading page...</p>}>{element}</Suspense>;
}

export const router = createBrowserRouter([
  {
    path: "/",
    element: <AppLayoutRoute />,
    children: [
      {
        element: <MainMenuRouterWrapper />, // see below
        children: [
          { index: true, element: <div>Home (placeholder)</div> },
          { path: "assets/addresses", element: withSuspense(<AdrMgmt />) },
          { path: "assets/inverters", element: withSuspense(<Inverters />) },
          { path: "transfers/rules", element: withSuspense(<TransferRules />) },
          { path: "transfers/new", element: withSuspense(<NewTransfer />) },
          { path: "data/inverterreadings", element: withSuspense(<InverterReadingsPage permissions={[]} />) },
          { path: "data/consumptionreadings", element: withSuspense(<ConsumptionPage permissions={[]} />) },
          { path: "analytics", element: <div>Analytics (placeholder)</div> },
          { path: "analytics/overview", element: withSuspense(<DailyBalancePage permissions={[]} />) },
          { path: "admin", element: <div>Admin (placeholder)</div> },
        ],
      },
    ],
  },
]);

function MainMenuRouterWrapper() {
  const { menuHorizontal } = useOutletContext<{ menuHorizontal: boolean }>();
  return <MainMenuRouter menuHorizontal={menuHorizontal} />;
}
