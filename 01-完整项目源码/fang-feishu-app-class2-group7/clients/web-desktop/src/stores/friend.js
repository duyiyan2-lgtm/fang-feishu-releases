// 好友 Pinia store（对齐后端 v1.1(2) 响应结构）
import { defineStore } from 'pinia'
import { ref, reactive, computed } from 'vue'
import {
  listFriends, listRequests,
  sendFriendRequest as sendApi,
  acceptFriendRequest as acceptApi,
  rejectFriendRequest as rejectApi,
  removeFriend as removeApi
} from '@/api/friend'
import { useUserStore } from '@/stores/user'
import { ElMessage } from '@/api/toast'

/** 关系状态枚举（前端 UI 用） */
export const FriendStatus = {
  SELF: 'self',
  NOT_FRIEND: 'not_friend',
  PENDING_SENT: 'pending_sent',
  PENDING_RECEIVED: 'pending_received',
  FRIEND: 'friend'
}

export const useFriendStore = defineStore('friend', () => {
  const userStore = useUserStore()

  /** 好友列表（已 accepted）Map<userId, friendObj> */
  const friends = reactive({})

  /** 收到的请求（direction=Incoming） */
  const pendingReceived = ref([])

  /** 发出的请求（direction=Outgoing）Map<addresseeId, request> */
  const pendingSent = reactive({})

  const loading = ref(false)

  /**
   * 拉取好友 + 请求（GET /contacts + GET /contacts/requests）
   */
  async function fetchAll() {
    loading.value = true
    try {
      const [fs, reqs] = await Promise.all([
        listFriends().catch(() => []),
        listRequests().catch(() => [])
      ])
      // 重置
      for (const k of Object.keys(friends)) delete friends[k]
      for (const k of Object.keys(pendingSent)) delete pendingSent[k]
      const friendList = Array.isArray(fs) ? fs : []
      friendList.forEach(f => {
        friends[f.id] = f
      })
      const requestList = Array.isArray(reqs) ? reqs : []
      pendingReceived.value = requestList.filter(r => r.direction === 'Incoming')
      requestList
        .filter(r => r.direction === 'Outgoing')
        .forEach(r => {
          if (r.user?.id) pendingSent[r.user.id] = r
        })
    } catch (e) {
      console.warn('[friend] fetchAll failed:', e)
    } finally {
      loading.value = false
    }
  }

  /** 发起加好友请求 */
  async function sendRequest(userId, greeting) {
    if (userId === userStore.userInfo?.id) {
      ElMessage({ message: '不能加自己为好友', type: 'warning' })
      return null
    }
    if (friends[userId]) {
      ElMessage({ message: '已经是好友了', type: 'info' })
      return null
    }
    if (pendingSent[userId]) {
      ElMessage({ message: '已发送过请求，等待对方处理', type: 'warning' })
      return null
    }
    try {
      const r = await sendApi(userId, greeting)
      if (r && r.id) {
        pendingSent[userId] = r
        ElMessage({ message: '好友请求已发送', type: 'success' })
      }
      return r
    } catch (e) {
      ElMessage({ message: '发送失败：' + e.message, type: 'error' })
      return null
    }
  }

  /** 接受好友请求 */
  async function acceptRequest(requestId) {
    try {
      const r = await acceptApi(requestId)
      const req = pendingReceived.value.find(x => x.id === requestId)
      if (req && req.user?.id) {
        // 加为好友（从 request.user 拿对方信息）
        friends[req.user.id] = { id: req.user.id, ...req.user }
      }
      pendingReceived.value = pendingReceived.value.filter(x => x.id !== requestId)
      ElMessage({ message: '已添加好友', type: 'success' })
      return r
    } catch (e) {
      ElMessage({ message: '操作失败：' + e.message, type: 'error' })
      return null
    }
  }

  /** 拒绝好友请求 */
  async function rejectRequest(requestId) {
    try {
      await rejectApi(requestId)
      pendingReceived.value = pendingReceived.value.filter(x => x.id !== requestId)
      ElMessage({ message: '已拒绝', type: 'info' })
    } catch (e) {
      ElMessage({ message: '操作失败：' + e.message, type: 'error' })
    }
  }

  /** 删除好友 */
  async function remove(userId) {
    try {
      await removeApi(userId)
      delete friends[userId]
      ElMessage({ message: '已删除好友', type: 'success' })
    } catch (e) {
      // 404 = 关系已不存在（之前已删过 / 关系变更），从本地状态移除即可
      const status = e?.response?.status || e?.status
      if (status === 404) {
        delete friends[userId]
        ElMessage({ message: '已删除好友', type: 'success' })
        return
      }
      ElMessage({ message: '删除失败：' + e.message, type: 'error' })
    }
  }

  /** 查询与某 userId 的关系状态 */
  function statusOf(userId) {
    if (userId === userStore.userInfo?.id) return FriendStatus.SELF
    if (friends[userId]) return FriendStatus.FRIEND
    if (pendingSent[userId]) return FriendStatus.PENDING_SENT
    if (pendingReceived.value.some(r => r.user?.id === userId || r.userId === userId)) {
      return FriendStatus.PENDING_RECEIVED
    }
    return FriendStatus.NOT_FRIEND
  }

  /** 是否有未处理请求（用于 Sidebar 角标） */
  const hasPending = computed(() => pendingReceived.value.length > 0)

  /**
   * 列表分页：好友列表（按加入时间倒序）
   */
  const friendsList = computed(() => {
    return Object.values(friends).sort((a, b) => {
      const ta = a.updatedAt || a.createdAt || ''
      const tb = b.updatedAt || b.createdAt || ''
      return tb.localeCompare(ta)
    })
  })

  return {
    friends, pendingReceived, pendingSent, loading, hasPending, friendsList,
    fetchAll, sendRequest, acceptRequest, rejectRequest, remove,
    statusOf
  }
})