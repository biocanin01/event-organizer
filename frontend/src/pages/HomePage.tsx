import ArrowForwardRoundedIcon from "@mui/icons-material/ArrowForwardRounded";
import AutoAwesomeRoundedIcon from "@mui/icons-material/AutoAwesomeRounded";
import EventNoteRoundedIcon from "@mui/icons-material/EventNoteRounded";
import Inventory2RoundedIcon from "@mui/icons-material/Inventory2Rounded";
import {
  AppBar,
  Box,
  Button,
  Chip,
  Container,
  Paper,
  Stack,
  Toolbar,
  Typography,
} from "@mui/material";
import { Link } from "react-router";
import { BrandMark } from "../shared/components/BrandMark";

const capabilities = [
  {
    icon: <EventNoteRoundedIcon color="primary" />,
    title: "Organizacija događaja",
    description: "Planiranje događaja, prijave učesnika i praćenje statusa.",
  },
  {
    icon: <Inventory2RoundedIcon color="primary" />,
    title: "Upravljanje resursima",
    description:
      "Pregled resursa i objedinjeno planiranje potrebnih rezervacija.",
  },
  {
    icon: <AutoAwesomeRoundedIcon color="primary" />,
    title: "Pametne preporuke",
    description:
      "Predlog odgovarajuće kombinacije resursa prema uslovima događaja.",
  },
];

export function HomePage() {
  return (
    <Box sx={{ minHeight: "100vh" }}>
      <AppBar
        position="static"
        color="transparent"
        elevation={0}
        sx={{ borderBottom: 1, borderColor: "divider" }}
      >
        <Container maxWidth="lg">
          <Toolbar disableGutters sx={{ justifyContent: "space-between" }}>
            <BrandMark />
            <Button component={Link} to="/login" variant="outlined">
              Prijava
            </Button>
          </Toolbar>
        </Container>
      </AppBar>

      <Container maxWidth="lg">
        <Stack
          component="main"
          spacing={{ xs: 7, md: 10 }}
          sx={{ py: { xs: 7, md: 12 } }}
        >
          <Stack spacing={3} sx={{ maxWidth: 780 }}>
            <Chip
              label="Planirajte. Organizujte. Realizujte."
              color="primary"
              variant="outlined"
              sx={{ alignSelf: "flex-start" }}
            />
            <Typography component="h1" variant="h1">
              Sve što je potrebno za uspešan događaj.
            </Typography>
            <Typography
              color="text.secondary"
              sx={{ fontSize: { xs: 18, md: 21 } }}
            >
              EventOrganizer povezuje događaje, učesnike i resurse u jedinstven
              i pregledan sistem.
            </Typography>
            <Button
              component={Link}
              to="/login"
              variant="contained"
              size="large"
              endIcon={<ArrowForwardRoundedIcon />}
              sx={{ alignSelf: "flex-start" }}
            >
              Započni
            </Button>
          </Stack>

          <Box
            sx={{
              display: "grid",
              gridTemplateColumns: { xs: "1fr", md: "repeat(3, 1fr)" },
              gap: 2.5,
            }}
          >
            {capabilities.map((capability) => (
              <Paper
                key={capability.title}
                variant="outlined"
                sx={{ p: 3, minHeight: 190 }}
              >
                <Stack spacing={2}>
                  {capability.icon}
                  <Typography variant="h6" sx={{ fontWeight: 700 }}>
                    {capability.title}
                  </Typography>
                  <Typography color="text.secondary">
                    {capability.description}
                  </Typography>
                </Stack>
              </Paper>
            ))}
          </Box>
        </Stack>
      </Container>
    </Box>
  );
}
