export const notificationQueryKeys = {
  all: ['notifications'] as const,
  list: (userId: string) => ['notifications', userId, 'list'] as const,
  unreadCount: (userId: string) =>
    ['notifications', userId, 'unread-count'] as const,
}
