import { Box, Button, Stack, Typography } from "@mui/material";
import { Link } from "react-router";

export function NotFoundPage() {
  return (
    <Box
      sx={{ minHeight: "100vh", display: "grid", placeItems: "center", p: 2 }}
    >
      <Stack spacing={2} sx={{ alignItems: "center", textAlign: "center" }}>
        <Typography
          color="primary"
          sx={{ fontSize: { xs: 64, md: 96 }, fontWeight: 750, lineHeight: 1 }}
        >
          404
        </Typography>
        <Typography component="h1" variant="h4">
          Stranica nije pronađena
        </Typography>
        <Typography color="text.secondary">
          Proverite adresu ili se vratite na početnu stranicu.
        </Typography>
        <Button component={Link} to="/" variant="contained">
          Početna stranica
        </Button>
      </Stack>
    </Box>
  );
}
