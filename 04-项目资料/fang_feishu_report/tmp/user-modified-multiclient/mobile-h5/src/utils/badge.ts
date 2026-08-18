/**
 * TabBar 角标管理
 */
import { getUnreadCount } from '@/api/notifications'
import { getConversations } from '@/api/im'

/** 更新所有 TabBar 角标 */
export async function updateBadges() {
  try {
    // 消息未读数
    const convs: any = await getConversations()
    const list = Array.isArray(convs) ? convs : []
    const totalUnread = list.reduce((sum: number, c: any) => sum + (c.unreadCount || 0), 0)
    if (totalUnread > 0) {
      uni.setTabBarBadge({ index: 0, text: String(totalUnread > 99 ? 99 : totalUnread) })
    } else {
      uni.removeTabBarBadge({ index: 0 })
    }

    // 通知未读数
    try {
      const notifRes: any = await getUnreadCount()
      const count = notifRes?.unreadCount || 0
      // 通知中心在首页有入口，用全局事件传递未读数
      uni.$emit('badge-update', { notifications: count })
    } catch {}
  } catch {}
}
