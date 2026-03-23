import Paper from "@mui/material/Paper";

export default function Footer() {
  return (
    <Paper component="footer" square elevation={2} sx={{ textAlign: 'center', py: 2, fontSize: 12 }}>
      © 2026 VNM Energy Platform
    </Paper>
  );
}