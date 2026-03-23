import * as React from "react";
import { createTheme, ThemeProvider } from "@mui/material/styles";

// You can customize the theme here if needed
const theme = createTheme({
  // palette: {
  //   primary: { main: '#1976d2' },
  // },
});

export default function MUIRootProvider({ children }: { children: React.ReactNode }) {
  return <ThemeProvider theme={theme}>{children}</ThemeProvider>;
}
