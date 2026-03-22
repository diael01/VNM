import { useState } from "react";
import Menu from "../components/Menu";
import InverterReadingsPage from "./InverterReadingsPage";

const pageMap: Record<string, JSX.Element> = {
  "location-add": <div>Add Location (form placeholder)</div>,
  "location-edit": <InverterReadingsPage permissions={[]} />, // Temporary: show inverter readings
  "location-delete": <div>Delete Location (form placeholder)</div>,
  "analytics": <div>Analytics (placeholder)</div>,
  "company-add": <div>Add Company (form placeholder)</div>,
  "company-edit": <div>Edit Company (form placeholder)</div>,
  "company-delete": <div>Delete Company (form placeholder)</div>,
  "admin": <div>Admin (placeholder)</div>,
};


type MainMenuRouterProps = {
  menuHorizontal: boolean
}

export default function MainMenuRouter({ menuHorizontal }: MainMenuRouterProps) {
  const [selected, setSelected] = useState<string>("location-add");

  return (
    <div style={{ display: menuHorizontal ? "block" : "flex", minHeight: "70vh" }}>
      <Menu onSelect={setSelected} horizontal={menuHorizontal} />
      <div style={{ flex: 1, padding: 32 }}>{pageMap[selected]}</div>
    </div>
  );
}
