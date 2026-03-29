import { useState } from "react";
import Box from "@mui/material/Box";
import Tabs from "@mui/material/Tabs";
import Tab from "@mui/material/Tab";
import MuiMenu from "@mui/material/Menu";
import MenuItem from "@mui/material/MenuItem";
import { Link, useLocation } from "react-router-dom";

export type MenuItem = {
  key: string;
  label: string;
  children?: MenuItem[];
};

const menuItems: MenuItem[] = [
  {
    key: "assets",
    label: "Assets",
    children: [
      { key: "assets-addresses", label: "Addresses" }, 
      { key: "assets-inverters", label: "Inverters" },
    
    ],
  },
  {
    key: "data",
    label: "Data Readings",
    children: [
      { key: "data-inverterreadings", label: "Inverter" },   
      { key: "data-consumptionreadings", label: "Consumption" },   
    ],
  },
  {
    key: "analytics",
    label: "Analytics",
    children: [
      { key: "analytics-overview", label: "Overview (Today)" },
      { key: "analytics-trends", label: "Trends" },
      { key: "analytics-financial", label: "Financial" },
    ],
  },
  {
    key: "transfers",
    label: "Transfers",
    children: [
      { key: "transfers-available", label: "Available Energy" },
      { key: "transfers-new", label: "New Transfer" },
      { key: "transfers-history", label: "History" },
    ],
  },
  {
    key: "company",
    label: "Ext. Providers",
    children: [
      { key: "company-add", label: "Add" },
      { key: "company-edit", label: "Edit" },
      { key: "company-delete", label: "Delete" },
      { key: "company-simulator", label: "Provider Simulator" },
      { key: "company-prosumer", label: "Prosumer Account" },
      { key: "company-settlements", label: "Settlements" },
      { key: "company-external-data", label: "External Data" },
    ],
  },
  { key: "admin", label: "Admin" },
];

export default function AppMenu({ horizontal = false }: { horizontal?: boolean }) {
  const location = useLocation();
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [openMenuIdx, setOpenMenuIdx] = useState<number | null>(null);

  // Open submenu on tab click only
  const handleMenuClose = () => {
    setAnchorEl(null);
    setOpenMenuIdx(null);
  };

  return (
    <>
      <Tabs
        value={(() => {
          // Find the tab index based on the current path
          const path = location.pathname;
          const idx = menuItems.findIndex(item => {
            if (!item.children) return path.startsWith(`/${item.key}`);
            // If any child matches
            return item.children.some(child => path.includes(child.key.replace('-', '/')));
          });
          return idx === -1 ? 0 : idx;
        })()}
        orientation={horizontal ? 'horizontal' : 'vertical'}
        variant={horizontal ? 'scrollable' : 'standard'}
        scrollButtons={horizontal ? 'auto' : false}
        aria-label="main menu tabs"
      >
        {menuItems.map((item, idx) => (
          <Tab
            key={item.key}
            label={item.label}
            aria-controls={item.children ? `${item.key}-menu` : undefined}
            aria-haspopup={!!item.children}
            component={item.children ? 'button' : Link}
            to={item.children ? undefined : `/${item.key.replace(/-/g, '/')}`}
            onClick={item.children ? (e: React.MouseEvent<HTMLElement>) => {
              setAnchorEl(e.currentTarget);
              setOpenMenuIdx(idx);
            } : undefined}
          />
        ))}
      </Tabs>
      {openMenuIdx !== null && menuItems[openMenuIdx].children && (
        <MuiMenu
          key={menuItems[openMenuIdx].key}
          id={`${menuItems[openMenuIdx].key}-menu`}
          anchorEl={anchorEl}
          open={Boolean(anchorEl)}
          onClose={handleMenuClose}
          MenuListProps={{ onMouseLeave: handleMenuClose, 'aria-labelledby': `${menuItems[openMenuIdx].key}-tab` }}
        >
          {menuItems[openMenuIdx].children!.map(child => (
            <MenuItem
              key={child.key}
              component={Link}
              to={`/${child.key.replace(/-/g, '/')}`}
              onClick={handleMenuClose}
            >
              {child.label}
            </MenuItem>
          ))}
        </MuiMenu>
      )}
    </>
  );
}
