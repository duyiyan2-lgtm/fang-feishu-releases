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
  let loadPromise = null
  let loadGeneration = 0

  const unreadCount = computed(() => items.value.filter((n) => !n.read).length)

  function upsert(n) {
    if (!n || !n.id) return
    const idx = items.value.findIndex((x) => x.id === n.id)
    if (idx >= 0) items.value[idx] = { ...items.value[idx], ...n }
    else items.value = [n, ...items.value]
  }

  async function load() {
    if (loadPromise) return loadPromise
    const generation = loadGeneration
    loading.value = true
    error.value = null
    loadPromise = listNotifications()
      .then((list) => {
        if (generation !== loadGeneration) return items.value
        items.value = list
        loaded.value = true
        return list
      })
      .catch((e) => {
        if (generation !== loadGeneration) return items.value
        error.value = e?.message || '加载通知失败'
        throw e
      })
      .finally(() => {
        if (generation !== loadGeneration) return
        loading.value = false
        loadPromise = null
      })
    return loadPromise
  }

  async function refreshUnread() {
    try {
      const n = await getUnreadCount()
      const known = items.value.filter((x) => !x.read).length
      // 双向校准：其他设备已读、实时事件丢失或新通知到达时，都重新拉取真实列表。
      if (n !== known) {
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
      await refreshUnread()
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
      await refreshUnread()
    } catch (e) {
      items.value = snapshot
      throw e
    }
  }

  /** 由 SignalR 推入单条新通知 */
  function onReceive(n) {
    upsert(n)
  }

  function resetSession() {
    loadGeneration += 1
    items.value = []
    loaded.value = false
    loading.value = false
    error.value = null
    loadPromise = null
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
    resetSession,
  }
})
