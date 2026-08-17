import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useAuth } from '../auth/useAuth'
import { useAuthenticatedRequest } from '../auth/useAuthenticatedRequest'
import {
  getUnreadNotificationCount,
  listNotifications,
  markAllNotificationsAsRead,
  markNotificationAsRead,
} from './notificationsApi'
import { notificationQueryKeys } from './notificationQueryKeys'

export const notificationPollingIntervalMs = 30_000

function useNotificationSession() {
  const { session, status } = useAuth()

  return {
    isAuthenticated:
      status === 'authenticated' && Boolean(session?.accessToken),
    userId: session?.user.userId ?? 'anonymous',
  }
}

export function useNotificationList(isOpen: boolean) {
  const authenticatedRequest = useAuthenticatedRequest()
  const { isAuthenticated, userId } = useNotificationSession()

  return useQuery({
    queryKey: notificationQueryKeys.list(userId),
    queryFn: () => listNotifications(authenticatedRequest),
    enabled: isAuthenticated && isOpen,
    staleTime: 0,
  })
}

export function useUnreadNotificationCount() {
  const authenticatedRequest = useAuthenticatedRequest()
  const { isAuthenticated, userId } = useNotificationSession()

  return useQuery({
    queryKey: notificationQueryKeys.unreadCount(userId),
    queryFn: () => getUnreadNotificationCount(authenticatedRequest),
    enabled: isAuthenticated,
    refetchInterval: isAuthenticated ? notificationPollingIntervalMs : false,
    refetchIntervalInBackground: false,
  })
}

export function useMarkNotificationAsRead() {
  const authenticatedRequest = useAuthenticatedRequest()
  const queryClient = useQueryClient()
  const { userId } = useNotificationSession()

  return useMutation({
    mutationFn: (notificationId: string) =>
      markNotificationAsRead(authenticatedRequest, notificationId),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: notificationQueryKeys.list(userId),
        }),
        queryClient.invalidateQueries({
          queryKey: notificationQueryKeys.unreadCount(userId),
        }),
      ])
    },
  })
}

export function useMarkAllNotificationsAsRead() {
  const authenticatedRequest = useAuthenticatedRequest()
  const queryClient = useQueryClient()
  const { userId } = useNotificationSession()

  return useMutation({
    mutationFn: () => markAllNotificationsAsRead(authenticatedRequest),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: notificationQueryKeys.list(userId),
        }),
        queryClient.invalidateQueries({
          queryKey: notificationQueryKeys.unreadCount(userId),
        }),
      ])
    },
  })
}
