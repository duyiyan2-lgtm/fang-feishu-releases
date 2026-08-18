// 浏览器原生通知工具
// - 系统级 Notification（即使不在 IM 页面也能看到）
// - 标题闪烁（tab 未激活时）
// - favicon 切换（可选）
// - 通知点击 → 跳到对应会话

let originalTitle = document.title
let titleInterval = null
let unreadCount = 0

/** 检查浏览器是否支持原生通知 */
export function isNotificationSupported() {
  return 'Notification' in window
}

/** 当前授权状态 */
export function notificationPermission() {
  if (!isNotificationSupported()) return 'unsupported'
  return Notification.permission  // 'default' | 'granted' | 'denied'
}

/** 申请通知权限（用户点击"开启"时调用） */
export async function requestNotificationPermission() {
  if (!isNotificationSupported()) {
    return { ok: false, reason: 'unsupported' }
  }
  if (Notification.permission === 'granted') {
    return { ok: true, permission: 'granted' }
  }
  try {
    const perm = await Notification.requestPermission()
    return { ok: perm === 'granted', permission: perm }
  } catch (e) {
    return { ok: false, reason: e.message, permission: Notification.permission }
  }
}

/**
 * 显示系统级通知
 * @param {string} title
 * @param {object} options - { body, icon, tag, data }
 * @returns {Notification|null}
 */
export function showSystemNotification(title, options = {}) {
  if (!isNotificationSupported()) return null
  if (Notification.permission !== 'granted') return null
  try {
    const n = new Notification(title, {
      icon: '/favicon.ico',
      badge: '/favicon.ico',
      tag: options.tag || 'feishu-message',
      renotify: true,
      ...options
    })
    return n
  } catch (e) {
    console.warn('[notification] failed:', e)
    return null
  }
}

/**
 * 处理新消息时调用
 * @param {object} msg - { id, senderName, content, conversationId, type }
 * @param {function} onClick - 点击通知回调
 */
export function notifyNewMessage(msg, onClick, options = {}) {
  const { activeConversationId } = options
  const isOnCurrentConv = activeConversationId === msg.conversationId
  const isPageVisible = !document.hidden

  // 系统级原生通知触发条件：
  // 1. 页面隐藏（用户在别的标签/窗口）→ 必弹
  // 2. 页面可见但**不是当前会话**（用户没看到这条消息）→ 弹
  const shouldShowSystemNotif = document.hidden || !isOnCurrentConv

  if (shouldShowSystemNotif) {
    const notif = showSystemNotification(
      msg.senderName || '新消息',
      {
        body: msg.content?.slice(0, 80) || '(媒体/文件)',
        tag: `msg-${msg.id}`,
        data: { conversationId: msg.conversationId, messageId: msg.id }
      }
    )
    if (notif && onClick) {
      notif.onclick = () => {
        window.focus()
        onClick(msg)
        notif.close()
      }
    }
  }

  // 标题闪烁（始终触发，但当前会话可见时可考虑免闪）
  // 策略：只要消息未读就闪，让用户感知
  startTitleFlash()
}

/** 启动标题闪烁（如 "🔔 (3) 消息 - Feishu..."） */
export function startTitleFlash() {
  if (titleInterval) return
  const baseTitle = originalTitle || 'Feishu Workspace'
  titleInterval = setInterval(() => {
    document.title = document.title.startsWith('🔔')
      ? baseTitle
      : `🔔 (${++unreadCount}) ${baseTitle}`
  }, 1500)
}

/** 停止标题闪烁 */
export function stopTitleFlash() {
  if (titleInterval) {
    clearInterval(titleInterval)
    titleInterval = null
  }
  unreadCount = 0
  document.title = originalTitle
}

/** 标记所有已读，停止闪烁 */
export function markAllRead() {
  stopTitleFlash()
}

/** 设置原始标题（页面加载时调用一次） */
export function captureOriginalTitle() {
  if (document.title) originalTitle = document.title
}