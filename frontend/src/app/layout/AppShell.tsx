import AdminPanelSettingsRoundedIcon from '@mui/icons-material/AdminPanelSettingsRounded'
import AssignmentTurnedInRoundedIcon from '@mui/icons-material/AssignmentTurnedInRounded'
import DashboardRoundedIcon from '@mui/icons-material/DashboardRounded'
import EventNoteRoundedIcon from '@mui/icons-material/EventNoteRounded'
import GroupRoundedIcon from '@mui/icons-material/GroupRounded'
import Inventory2RoundedIcon from '@mui/icons-material/Inventory2Rounded'
import LogoutRoundedIcon from '@mui/icons-material/LogoutRounded'
import MenuRoundedIcon from '@mui/icons-material/MenuRounded'
import RateReviewRoundedIcon from '@mui/icons-material/RateReviewRounded'
import {
  AppBar,
  Avatar,
  Box,
  Divider,
  Drawer,
  IconButton,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Stack,
  Toolbar,
  Tooltip,
  Typography,
  useMediaQuery,
} from '@mui/material'
import { useTheme } from '@mui/material/styles'
import { useState, type ReactNode } from 'react'
import { NavLink, Outlet, useNavigate } from 'react-router'
import { applicationRoles, type ApplicationRole } from '../../features/auth/types'
import { useAuth } from '../../features/auth/useAuth'
import { BrandMark } from '../../shared/components/BrandMark'

const drawerWidth = 272

interface NavigationItem {
  label: string
  path: string
  icon: ReactNode
  roles: ApplicationRole[]
}

const navigationItems: NavigationItem[] = [
  {
    label: 'Dashboard',
    path: '/dashboard',
    icon: <DashboardRoundedIcon />,
    roles: [
      applicationRoles.participant,
      applicationRoles.organizer,
      applicationRoles.admin,
    ],
  },
  {
    label: 'Događaji',
    path: '/events',
    icon: <EventNoteRoundedIcon />,
    roles: [
      applicationRoles.participant,
      applicationRoles.organizer,
      applicationRoles.admin,
    ],
  },
  {
    label: 'Resursi',
    path: '/resources',
    icon: <Inventory2RoundedIcon />,
    roles: [applicationRoles.organizer, applicationRoles.admin],
  },
  {
    label: 'Prijave i rezervacije',
    path: '/registrations',
    icon: <AssignmentTurnedInRoundedIcon />,
    roles: [
      applicationRoles.participant,
      applicationRoles.organizer,
      applicationRoles.admin,
    ],
  },
  {
    label: 'Korisnici',
    path: '/admin/users',
    icon: <GroupRoundedIcon />,
    roles: [applicationRoles.admin],
  },
  {
    label: 'Zahtevi za organizatore',
    path: '/admin/organizer-requests',
    icon: <AdminPanelSettingsRoundedIcon />,
    roles: [applicationRoles.admin],
  },
  {
    label: 'Izveštaji',
    path: '/reports',
    icon: <RateReviewRoundedIcon />,
    roles: [applicationRoles.organizer, applicationRoles.admin],
  },
]

function getRoleLabel(roles: ApplicationRole[]) {
  if (roles.includes(applicationRoles.admin)) {
    return 'Admin'
  }

  if (roles.includes(applicationRoles.organizer)) {
    return 'Organizer'
  }

  return 'Participant'
}

export function AppShell() {
  const { session, logout } = useAuth()
  const navigate = useNavigate()
  const theme = useTheme()
  const isDesktop = useMediaQuery(theme.breakpoints.up('md'))
  const [mobileOpen, setMobileOpen] = useState(false)

  const user = session?.user
  const visibleNavigationItems = navigationItems.filter((item) =>
    item.roles.some((role) => user?.roles.includes(role)),
  )

  const handleLogout = async () => {
    await logout()
    navigate('/login', { replace: true })
  }

  const drawerContent = (
    <Stack sx={{ minHeight: '100%' }}>
      <Box sx={{ px: 2.5, py: 2 }}>
        <BrandMark />
      </Box>
      <Divider />
      <List sx={{ px: 1.5, py: 2 }}>
        {visibleNavigationItems.map((item) => (
          <ListItemButton
            key={item.path}
            component={NavLink}
            to={item.path}
            onClick={() => setMobileOpen(false)}
            sx={{
              borderRadius: 1,
              mb: 0.5,
              '&.active': {
                bgcolor: 'primary.main',
                color: 'primary.contrastText',
                '& .MuiListItemIcon-root': {
                  color: 'inherit',
                },
              },
            }}
          >
            <ListItemIcon sx={{ minWidth: 40 }}>{item.icon}</ListItemIcon>
            <ListItemText primary={item.label} />
          </ListItemButton>
        ))}
      </List>
      <Box sx={{ flexGrow: 1 }} />
      <Divider />
      <Stack spacing={1.5} sx={{ p: 2 }}>
        <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
          <Avatar sx={{ width: 36, height: 36 }}>
            {user?.fullName.charAt(0).toUpperCase()}
          </Avatar>
          <Box sx={{ minWidth: 0 }}>
            <Typography noWrap sx={{ fontWeight: 700 }}>
              {user?.fullName}
            </Typography>
            <Typography noWrap variant="body2" color="text.secondary">
              {getRoleLabel(user?.roles ?? [])}
            </Typography>
          </Box>
        </Stack>
        <ListItemButton onClick={handleLogout} sx={{ borderRadius: 1 }}>
          <ListItemIcon sx={{ minWidth: 40 }}>
            <LogoutRoundedIcon />
          </ListItemIcon>
          <ListItemText primary="Odjava" />
        </ListItemButton>
      </Stack>
    </Stack>
  )

  return (
    <Box sx={{ minHeight: '100vh', display: 'flex' }}>
      <AppBar
        position="fixed"
        color="inherit"
        elevation={0}
        sx={{
          display: { md: 'none' },
          borderBottom: 1,
          borderColor: 'divider',
        }}
      >
        <Toolbar sx={{ justifyContent: 'space-between' }}>
          <BrandMark />
          <Tooltip title="Otvori navigaciju">
            <IconButton onClick={() => setMobileOpen(true)}>
              <MenuRoundedIcon />
            </IconButton>
          </Tooltip>
        </Toolbar>
      </AppBar>

      <Box
        component="nav"
        sx={{ width: { md: drawerWidth }, flexShrink: { md: 0 } }}
      >
        <Drawer
          variant={isDesktop ? 'permanent' : 'temporary'}
          open={isDesktop || mobileOpen}
          onClose={() => setMobileOpen(false)}
          ModalProps={{ keepMounted: true }}
          sx={{
            '& .MuiDrawer-paper': {
              width: drawerWidth,
              boxSizing: 'border-box',
              borderRight: 1,
              borderColor: 'divider',
            },
          }}
        >
          {drawerContent}
        </Drawer>
      </Box>

      <Box
        component="main"
        sx={{
          flexGrow: 1,
          width: { md: `calc(100% - ${drawerWidth}px)` },
          px: { xs: 2, md: 4 },
          py: { xs: 10, md: 4 },
        }}
      >
        <Outlet />
      </Box>
    </Box>
  )
}
