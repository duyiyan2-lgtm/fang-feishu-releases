import http from './http'

/** 后端 Notification 实体 → 前端展示结构 */
export function adaptNotification(n) {
  if (!n) return null
  const type = normalizeType(n.type, n.resourceType)
  return {
    id: n.id,
    type,
    title: n.title || '',
    content: n.content || '',
    source: n.source || n.senderName || '系统',
    createdAt: n.createdAt,
    read: !!n.isRead || !!n.read,
    color: typeColor(type),
    resourceType: normalizeResourceType(n.resourceType),
    resourceId: n.resourceId || null,
  }
}

export function normalizeType(type, resourceType) {
  const t = String(type || '').trim().toLowerCase()
  const rt = String(resourceType || '').trim().toLowerCase()
  if (['im', 'message', 'conversation', 'chat'].includes(t) || ['message', 'conversation'].includes(rt)) return 'im'
  if (['approve', 'approval'].includes(t) || rt === 'approval') return 'approval'
  if (['meeting', 'video'].includes(t) || rt === 'meeting') return 'meeting'
  if (['file', 'document', 'doc'].includes(t) || ['file', 'document'].includes(rt)) return 'file'
  if (['mention', 'at'].includes(t)) return 'mention'
  if (['comment', 'like'].includes(t)) return t
  return 'system'
}

export function normalizeResourceType(resourceType) {
  const rt = String(resourceType || '').trim().toLowerCase()
  return {
    conversation: 'conversation',
    message: 'message',
    document: 'document',
    approval: 'approval',
    meeting: 'meeting',
    file: 'file'
  }[rt] || null
}

function typeColor(t) {
  switch ((t || '').toLowerCase()) {
    case 'mention': return '#3370FF'
    case 'system':  return '#EB2F96'
    case 'approve':
    case 'approval': return '#F59E0B'
    case 'comment': return '#00B96B'
    case 'like':    return '#9F7AEA'
    case 'meeting': return '#52C41A'
    default:        return '#8C8C8C'
  }
}

export function listNotifications() {
  return http.get('/notifications').then((d) => {
    if (Array.isArray(d)) return d.map(adaptNotification)
    if (Array.isArray(d?.items)) return d.items.map(adaptNotification)
    return []
  })
}

export function getUnreadCount() {
  return http.get('/notifications/unread-count').then((d) => {
    if (typeof d === 'number') return d
    if (typeof d?.count === 'number') return d.count
    if (typeof d?.unreadCount === 'number') return d.unreadCount
    return 0
  })
}

export function markOneRead(id) {
  return http.patch(`/notifications/${id}/read`, {})
}

export function markAllReadApi() {
  return http.patch('/notifications/read-all', {})
}
