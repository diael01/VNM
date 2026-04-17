
import Menu from "../components/Menu";
import { Outlet } from "react-router-dom";

type MainMenuRouterProps = {
  menuHorizontal: boolean;
}

export function MainMenuRouter({ menuHorizontal }: MainMenuRouterProps) {
  return (
    <div style={{ display: menuHorizontal ? "flex" : "flex", flexDirection: menuHorizontal ? "column" : "row", minHeight: "70vh", width: '100%', overflowX: 'hidden' }}>
      <Menu horizontal={menuHorizontal} />
      <div style={{ flex: 1, padding: 0, width: '100%' }}>
        <Outlet />
      </div>
    </div>
  );
}
