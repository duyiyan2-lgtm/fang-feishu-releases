import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import {
  listNotifications,
  getUnreadCount,
  markOneRead,
  markAllReadApi,
} from '@/api/notifications'

/**
 * 通知 store：接真后端 /api/v1/notifications
 * - 列表由 listNotifications 拉取（启动时 + 收到 ReceiveNotification 时合并/更新）
 * - 未读数由 unread-count 接口精确返回
 * - 标已读用 PATCH /{id}/read / read-all
 */
export const useNotificationsStore = defineStore('notifications', () => {
  const items = ref([])
  const loaded = ref(false)
  const loading = ref(false)
  const error = ref(null)

  const unreadCount = computed(() => items.value.filter((n) => !n.read).length)

  function upsert(n) {
    if (!n || !n.id) return
    const idx = items.value.findIndex((x) => x.id === n.id)
    if (idx >= 0) items.value[idx] = { ...items.value[idx], ...n }
    else items.value = [n, ...items.value]
  }

  async function load() {
    if (loading.value) return
    loading.value = true
    error.value = null
    try {
      const list = await listNotifications()
      items.value = list
      loaded.value = true
    } catch (e) {
      error.value = e?.message || '加载通知失败'
    } finally {
      loading.value = false
    }
  }

  async function refreshUnread() {
    try {
      const n = await getUnreadCount()
      // 只在「未读 > 列表内未读」或「未读 < 列表内未读」时做对齐
      const known = items.value.filter((x) => !x.read).length
      if (n > known) {
        // 后端有新通知未在列表里 → 重新拉一次
        await load()
      }
    } catch { /* silent */ }
  }

  async function markRead(id) {
    const n = items.value.find((x) => x.id === id)
    if (!n || n.read) return
    n.read = true // 乐观更新
    try {
      await markOneRead(id)
    } catch (e) {
      n.read = false
      throw e
    }
  }

  async function markAllRead() {
    if (unreadCount.value === 0) return
    const snapshot = items.value.map((n) => ({ ...n }))
    items.value.forEach((n) => (n.read = true)) // 乐观更新
    try {
      await markAllReadApi()
    } catch (e) {
      items.value = snapshot
      throw e
    }
  }

  /** 由 SignalR 推入单条新通知 */
  function onReceive(n) {
    upsert(n)
  }

  return {
    items,
    loaded,
    loading,
    error,
    unreadCount,
    load,
    refreshUnread,
    markRead,
    markAllRead,
    onReceive,
  }
})
