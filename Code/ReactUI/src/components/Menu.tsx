import { useState } from "react";

export type MenuItem = {
  key: string;
  label: string;
  children?: MenuItem[];
};

const menuItems: MenuItem[] = [
  {
    key: "location",
    label: "My Locations",
    children: [
      { key: "location-add", label: "Add" },
      { key: "location-edit", label: "Edit" },
      { key: "location-delete", label: "Delete" },
    ],
  },
  { key: "analytics", label: "Analytics" },
  {
    key: "company",
    label: "Energy Providers",
    children: [
      { key: "company-add", label: "Add" },
      { key: "company-edit", label: "Edit" },
      { key: "company-delete", label: "Delete" },
    ],
  },
  { key: "admin", label: "Admin" },
];

export default function Menu({ onSelect, horizontal = false }: { onSelect: (key: string) => void, horizontal?: boolean }) {
  const [open, setOpen] = useState<string | null>(null);

  const handleClick = (key: string, hasChildren: boolean) => {
    if (hasChildren) {
      setOpen(open === key ? null : key);
    } else {
      onSelect(key);
    }
  };

  const navStyle = horizontal
    ? { width: "100%", background: "#fff", borderBottom: "1px solid #e5e7eb", padding: 8 }
    : { width: 260, background: "#fff", borderRight: "1px solid #e5e7eb", padding: 16 };

  const ulStyle = horizontal
    ? { listStyle: "none", padding: 0, margin: 0, display: "flex", gap: 24 }
    : { listStyle: "none", padding: 0, margin: 0 };

  return (
    <nav style={navStyle}>
      <ul style={ulStyle}>
        {menuItems.map(item => (
          <li key={item.key} style={horizontal ? { position: "relative" } : { marginBottom: 8 }}>
            <div
              style={{ fontWeight: 600, cursor: "pointer", padding: horizontal ? "8px 16px" : "8px 0", display: "inline-block" }}
              onClick={() => handleClick(item.key, !!item.children)}
            >
              {item.label}
            </div>
            {item.children && open === item.key && (
              <ul
                style={horizontal
                  ? {
                      listStyle: "none",
                      padding: 0,
                      margin: 0,
                      position: "absolute",
                      top: "100%",
                      left: 0,
                      background: "#fff",
                      boxShadow: "0 2px 8px rgba(0,0,0,0.08)",
                      border: "1px solid #e5e7eb",
                      zIndex: 10,
                    }
                  : { listStyle: "none", paddingLeft: 16, margin: 0 }}
              >
                {item.children.map(child => (
                  <li key={child.key} style={horizontal ? { minWidth: 120 } : { marginBottom: 4 }}>
                    <div
                      style={{ cursor: "pointer", padding: "6px 12px", color: "#2563eb" }}
                      onClick={() => handleClick(child.key, false)}
                    >
                      {child.label}
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </li>
        ))}
      </ul>
    </nav>
  );
}
