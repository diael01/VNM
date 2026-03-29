
import { createBrowserRouter } from "react-router-dom";
import InverterReadingsPage from "./pages/InverterReadingsPage";
import Inverters from "./pages/Inverters";
import AdrMgmt from "./pages/AdrMgmt";
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
          { path: "data/inverterreadings", element: <InverterReadingsPage permissions={[]}/> },
          { path: "analytics", element: <div>Analytics (placeholder)</div> },
          { path: "admin", element: <div>Admin (placeholder)</div> },
        ],
      },
    ],
  },
]);

// Wrapper to get menuHorizontal from Outlet context
import { useOutletContext } from "react-router-dom";
function MainMenuRouterWrapper() {
  const { menuHorizontal } = useOutletContext<{ menuHorizontal: boolean }>();
  return <MainMenuRouter menuHorizontal={menuHorizontal} />;
}
