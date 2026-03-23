        import AppBar from "@mui/material/AppBar";
        import Toolbar from "@mui/material/Toolbar";
        import Typography from "@mui/material/Typography";
        import IconButton from "@mui/material/IconButton";
        import Button from "@mui/material/Button";
        import Box from "@mui/material/Box";
        import SwapVertIcon from "@mui/icons-material/SwapVert";
        import SwapHorizIcon from "@mui/icons-material/SwapHoriz";

        type HeaderProps = {
          userName?: string;
          roles: string[];
          isAuthenticated: boolean;
          onLogin: () => void;
          onLogout: () => void;
          menuHorizontal?: boolean;
          onToggleMenuLayout?: () => void;
        };

        export default function Header({
          userName,
          roles,
          isAuthenticated,
          onLogin,
          onLogout,
          menuHorizontal,
          onToggleMenuLayout,
        }: HeaderProps) {
          return (
            <AppBar position="static">
              <Toolbar sx={{ justifyContent: 'space-between' }}>
                <Box sx={{ display: 'flex', alignItems: 'center' }}>
                  {typeof menuHorizontal === "boolean" && onToggleMenuLayout && (
                    <IconButton onClick={onToggleMenuLayout} color="inherit" title={menuHorizontal ? "Horizontal menu" : "Vertical menu"}>
                      {menuHorizontal ? <SwapHorizIcon /> : <SwapVertIcon />}
                    </IconButton>
                  )}
                  <Typography variant="h6" sx={{ ml: 1 }}>
                    VNM Dashboard
                  </Typography>
                </Box>
                <Box sx={{ display: 'flex', alignItems: 'center' }}>
                  {isAuthenticated ? (
                    <>
                      <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'flex-end', mr: 2 }}>
                        <Typography>{userName}</Typography>
                        {roles && roles.length > 0 && (
                          <Typography sx={{ fontSize: 13 }}>
                            {roles.join(', ')}
                          </Typography>
                        )}
                      </Box>
                      <Button color="inherit" onClick={onLogout}>Logout</Button>
                    </>
                  ) : (
                    <Button color="inherit" onClick={onLogin}>Login</Button>
                  )}
                </Box>
              </Toolbar>
            </AppBar>
          );
        }