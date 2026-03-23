
import { createBrowserRouter } from "react-router-dom";
import InverterReadingsPage from "./pages/InverterReadingsPage";
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
          { path: "locations/add", element: <div>Add Location (form placeholder)</div> },
          { path: "locations/edit", element: <InverterReadingsPage permissions={[]} /> },
          { path: "locations/delete", element: <div>Delete Location (form placeholder)</div> },
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
