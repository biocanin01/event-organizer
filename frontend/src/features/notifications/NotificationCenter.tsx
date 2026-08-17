import AccessTimeRoundedIcon from '@mui/icons-material/AccessTimeRounded'
import BellIcon from '@mui/icons-material/NotificationsRounded'
import BellOutlineIcon from '@mui/icons-material/NotificationsNoneRounded'
import CancelRoundedIcon from '@mui/icons-material/CancelRounded'
import CheckCircleRoundedIcon from '@mui/icons-material/CheckCircleRounded'
import CloseRoundedIcon from '@mui/icons-material/CloseRounded'
import DoneAllRoundedIcon from '@mui/icons-material/DoneAllRounded'
import EventBusyRoundedIcon from '@mui/icons-material/EventBusyRounded'
import RateReviewRoundedIcon from '@mui/icons-material/RateReviewRounded'
import RefreshRoundedIcon from '@mui/icons-material/RefreshRounded'
import {
  Alert,
  Badge,
  Box,
  Button,
  CircularProgress,
  Divider,
  Drawer,
  IconButton,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Popover,
  Stack,
  Tooltip,
  Typography,
} from '@mui/material'
import type { SvgIconProps } from '@mui/material/SvgIcon'
import { useState, type MouseEvent } from 'react'
import { useNavigate } from 'react-router'
import { ApiError } from '../../api/ApiError'
import { formatDateTime } from '../../shared/format/dateTime'
import { getNotificationPath } from './notificationNavigation'
import type { Notification, NotificationType } from './types'
import {
  useMarkAllNotificationsAsRead,
  useMarkNotificationAsRead,
  useNotificationList,
  useUnreadNotificationCount,
} from './useNotifications'

interface NotificationCenterProps {
  mobile?: boolean
}

interface NotificationIconDefinition {
  color: SvgIconProps['color']
  icon: typeof CheckCircleRoundedIcon
}

const notificationIcons: Record<NotificationType, NotificationIconDefinition> = {
  OrganizerRoleRequestApproved: {
    color: 'success',
    icon: CheckCircleRoundedIcon,
  },
  OrganizerRoleRequestRejected: { color: 'error', icon: CancelRoundedIcon },
  BookingApproved: { color: 'success', icon: CheckCircleRoundedIcon },
  BookingRejected: { color: 'error', icon: CancelRoundedIcon },
  BookingExpired: { color: 'warning', icon: AccessTimeRoundedIcon },
  RegistrationConfirmed: { color: 'success', icon: CheckCircleRoundedIcon },
  RegistrationRejected: { color: 'error', icon: CancelRoundedIcon },
  RegistrationCancelled: { color: 'warning', icon: CancelRoundedIcon },
  EventCancelled: { color: 'error', icon: EventBusyRoundedIcon },
  ReviewAvailable: { color: 'primary', icon: RateReviewRoundedIcon },
}

function getErrorMessage(error: unknown) {
  return error instanceof ApiError
    ? error.message
    : 'Akcija trenutno nije uspela.'
}

function NotificationItem({
  notification,
  disabled,
  onClick,
}: {
  notification: Notification
  disabled: boolean
  onClick: (notification: Notification) => void
}) {
  const definition = notificationIcons[notification.type]
  const NotificationIcon = definition.icon

  return (
    <ListItemButton
      disabled={disabled}
      onClick={() => onClick(notification)}
      sx={{
        alignItems: 'flex-start',
        gap: 0.5,
        px: 2,
        py: 1.75,
        borderLeft: 3,
        borderLeftColor: notification.isRead ? 'transparent' : 'primary.main',
        bgcolor: notification.isRead ? 'transparent' : 'action.hover',
      }}
    >
      <ListItemIcon sx={{ minWidth: 36, pt: 0.25 }}>
        <NotificationIcon color={definition.color} fontSize="small" />
      </ListItemIcon>
      <ListItemText
        disableTypography
        primary={
          <Stack direction="row" spacing={1} sx={{ alignItems: 'flex-start' }}>
            <Typography
              variant="body2"
              sx={{ flexGrow: 1, fontWeight: notification.isRead ? 600 : 750 }}
            >
              {notification.title}
            </Typography>
            {!notification.isRead && (
              <Box
                aria-label="Nepročitano"
                sx={{
                  width: 8,
                  height: 8,
                  mt: 0.75,
                  flexShrink: 0,
                  borderRadius: '50%',
                  bgcolor: 'primary.main',
                }}
              />
            )}
          </Stack>
        }
        secondary={
          <Stack spacing={0.75} sx={{ mt: 0.5 }}>
            <Typography variant="body2" color="text.secondary">
              {notification.message}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              {formatDateTime(notification.createdAtUtc)}
            </Typography>
          </Stack>
        }
      />
    </ListItemButton>
  )
}

export function NotificationCenter({ mobile = false }: NotificationCenterProps) {
  const navigate = useNavigate()
  const [anchorElement, setAnchorElement] = useState<HTMLElement | null>(null)
  const [mobileOpen, setMobileOpen] = useState(false)
  const isOpen = mobile ? mobileOpen : Boolean(anchorElement)
  const notificationsQuery = useNotificationList(isOpen)
  const unreadCountQuery = useUnreadNotificationCount()
  const markAsReadMutation = useMarkNotificationAsRead()
  const markAllAsReadMutation = useMarkAllNotificationsAsRead()
  const unreadCount = unreadCountQuery.data?.unreadCount ?? 0
  const notifications = notificationsQuery.data ?? []
  const mutationError = markAsReadMutation.error ?? markAllAsReadMutation.error

  const handleOpen = (event: MouseEvent<HTMLElement>) => {
    if (mobile) {
      setMobileOpen(true)
      return
    }

    setAnchorElement(event.currentTarget)
  }

  const handleClose = () => {
    setAnchorElement(null)
    setMobileOpen(false)
    markAsReadMutation.reset()
    markAllAsReadMutation.reset()
  }

  const handleNotificationClick = async (notification: Notification) => {
    if (!notification.isRead) {
      try {
        await markAsReadMutation.mutateAsync(notification.id)
      } catch {
        return
      }
    }

    const path = getNotificationPath(notification)
    if (path) {
      handleClose()
      navigate(path)
    }
  }

  const panel = (
    <Stack sx={{ width: '100%', height: '100%', minHeight: 0 }}>
      <Stack
        direction="row"
        spacing={1}
        sx={{ alignItems: 'center', px: 2, py: 1.5 }}
      >
        <Typography variant="h6" sx={{ flexGrow: 1, fontSize: '1.05rem' }}>
          Obaveštenja
        </Typography>
        <Tooltip title="Označi sve kao pročitano">
          <span>
            <IconButton
              aria-label="Označi sve kao pročitano"
              disabled={unreadCount === 0 || markAllAsReadMutation.isPending}
              onClick={() => markAllAsReadMutation.mutate()}
              size="small"
            >
              {markAllAsReadMutation.isPending ? (
                <CircularProgress size={20} />
              ) : (
                <DoneAllRoundedIcon fontSize="small" />
              )}
            </IconButton>
          </span>
        </Tooltip>
        <Tooltip title="Zatvori">
          <IconButton aria-label="Zatvori obaveštenja" onClick={handleClose} size="small">
            <CloseRoundedIcon fontSize="small" />
          </IconButton>
        </Tooltip>
      </Stack>
      <Divider />

      {mutationError && (
        <Alert severity="error" sx={{ borderRadius: 0 }}>
          {getErrorMessage(mutationError)}
        </Alert>
      )}

      <Box sx={{ flexGrow: 1, minHeight: 0, overflowY: 'auto' }}>
        {notificationsQuery.isLoading ? (
          <Stack sx={{ minHeight: 220, alignItems: 'center', justifyContent: 'center' }}>
            <CircularProgress size={28} aria-label="Učitavanje obaveštenja" />
          </Stack>
        ) : notificationsQuery.isError ? (
          <Stack
            spacing={1.5}
            sx={{ minHeight: 220, px: 3, alignItems: 'center', justifyContent: 'center' }}
          >
            <Typography color="text.secondary" sx={{ textAlign: 'center' }}>
              Obaveštenja trenutno nisu dostupna.
            </Typography>
            <Button
              startIcon={<RefreshRoundedIcon />}
              onClick={() => notificationsQuery.refetch()}
            >
              Pokušaj ponovo
            </Button>
          </Stack>
        ) : notifications.length === 0 ? (
          <Stack
            spacing={1.25}
            sx={{ minHeight: 220, px: 3, alignItems: 'center', justifyContent: 'center' }}
          >
            <BellOutlineIcon color="disabled" sx={{ fontSize: 40 }} />
            <Typography color="text.secondary">Još nema obaveštenja.</Typography>
          </Stack>
        ) : (
          <List disablePadding>
            {notifications.map((notification, index) => (
              <Box key={notification.id} component="li" sx={{ listStyle: 'none' }}>
                {index > 0 && <Divider />}
                <NotificationItem
                  notification={notification}
                  disabled={markAsReadMutation.isPending}
                  onClick={handleNotificationClick}
                />
              </Box>
            ))}
          </List>
        )}
      </Box>
    </Stack>
  )

  return (
    <>
      <Tooltip title="Obaveštenja">
        <IconButton
          aria-label={
            unreadCount > 0
              ? `Obaveštenja, ${unreadCount} nepročitanih`
              : 'Obaveštenja'
          }
          onClick={handleOpen}
        >
          <Badge badgeContent={unreadCount} color="error" max={99}>
            <BellIcon />
          </Badge>
        </IconButton>
      </Tooltip>

      {mobile ? (
        <Drawer
          anchor="right"
          open={mobileOpen}
          onClose={handleClose}
          sx={{
            '& .MuiDrawer-paper': {
              width: 'min(100%, 420px)',
            },
          }}
        >
          {panel}
        </Drawer>
      ) : (
        <Popover
          anchorEl={anchorElement}
          open={Boolean(anchorElement)}
          onClose={handleClose}
          anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
          transformOrigin={{ vertical: 'top', horizontal: 'left' }}
          slotProps={{
            paper: {
              sx: {
                width: 'min(400px, calc(100vw - 32px))',
                height: 'min(560px, calc(100vh - 48px))',
                mt: 1,
                borderRadius: 1,
              },
            },
          }}
        >
          {panel}
        </Popover>
      )}
    </>
  )
}
