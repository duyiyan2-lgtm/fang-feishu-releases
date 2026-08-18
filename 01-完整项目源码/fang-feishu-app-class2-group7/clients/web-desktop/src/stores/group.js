// 群管理 Pinia store（对齐后端真实 API 路径）
import { defineStore } from 'pinia'
import { ref } from 'vue'
import {
  getGroupDetailById,
  getAnnouncement,
  addMembers as addMembersApi,
  removeMember,
  setAdmins,
  updateAnnouncement,
  updateConversation,
  leaveGroup,
  dissolveGroup,
  getConversationReadReceipts
} from '@/api/im'
import { ElMessage } from '@/api/toast'

export const useGroupStore = defineStore('group', () => {
  /** 群详情缓存: { [convId]: GroupDetail } */
  const details = ref({})

  /** 加载中集合 */
  const loading = ref({})

  /** 当前活跃 convId（用于 @ 列表） */
  const activeId = ref(null)

  function isLoading(id) { return !!loading.value[id] }

  /**
   * 拉取群详情（GET /im/conversations/{id}）
   * 注意：后端响应字段是 PascalCase（Id/Title/OwnerId/Notice/Members），
   *       转换为 camelCase 存储。
   */
  async function fetchDetail(id) {
    if (!id) return null
    loading.value[id] = true
    try {
      const data = await getGroupDetailById(id)
      // 优先用 dedicated /announcement 端点补全公告（保证是最新的）
      let notice = data.announcement ?? data.notice
      try {
        const a = await getAnnouncement(id)
        if (a && typeof a.announcement === 'string') notice = a.announcement
      } catch { /* ignore — fallback to inline */ }
      details.value[id] = {
        id: data.id,
        title: data.title,
        avatar: data.avatar,
        ownerId: data.ownerId,
        notice,
        members: (data.members || []).map(m => ({
          userId: m.userId,
          realName: m.realName || m.username,
          username: m.username,
          role: m.role || 'Member',
          isAdmin: m.isAdmin || false,
          isOwner: m.isOwner || false
        })),
        adminIds: data.adminIds || [],
        createdAt: data.createdAt
      }
      return details.value[id]
    } catch (e) {
      return null
    } finally {
      loading.value[id] = false
    }
  }

  /** 加成员 */
  async function addMembers(conversationId, memberUserIds) {
    try {
      await addMembersApi(conversationId, memberUserIds)
      ElMessage({ message: `已邀请 ${memberUserIds.length} 人`, type: 'success' })
      await fetchDetail(conversationId)
    } catch (e) {
      ElMessage({ message: '加成员失败：' + e.message, type: 'error' })
      throw e
    }
  }

  /** 移除成员（踢人） */
  async function removeMemberApi(conversationId, userId) {
    try {
      await removeMember(conversationId, userId)
      ElMessage({ message: '已移除成员', type: 'success' })
      await fetchDetail(conversationId)
    } catch (e) {
      ElMessage({ message: '操作失败：' + e.message, type: 'error' })
      throw e
    }
  }

  /** 退群 */
  async function leave(conversationId) {
    try {
      await leaveGroup(conversationId)
      ElMessage({ message: '已退出群', type: 'success' })
      delete details.value[conversationId]
    } catch (e) {
      ElMessage({ message: '退群失败：' + e.message, type: 'error' })
      throw e
    }
  }

  /** 修改群资料（title 用 PUT /im/conversations/{id}，notice 用 PUT /announcement） */
  async function update(conversationId, payload) {
    try {
      if (payload.title !== undefined) {
        await updateConversation(conversationId, { title: payload.title })
      }
      if (payload.notice !== undefined) {
        await updateAnnouncement(conversationId, payload.notice)
      }
      ElMessage({ message: '已更新群资料', type: 'success' })
      await fetchDetail(conversationId)
    } catch (e) {
      ElMessage({ message: '更新失败：' + e.message, type: 'error' })
      throw e
    }
  }

  /** 解散群 */
  async function dissolve(conversationId) {
    try {
      await dissolveGroup(conversationId)
      ElMessage({ message: '群已解散', type: 'success' })
      delete details.value[conversationId]
    } catch (e) {
      ElMessage({ message: '解散失败：' + e.message, type: 'error' })
      throw e
    }
  }

  /** 拉会话已读回执 */
  async function fetchConversationReads(conversationId) {
    try {
      const data = await getConversationReadReceipts(conversationId)
      return data
    } catch (e) {
      return { receipts: [] }
    }
  }

  /** SignalR: 成员加入 */
  function handleMemberAdded(p) {
    if (!p?.conversationId) return
    const d = details.value[p.conversationId]
    if (d) {
      const existing = new Set(d.members.map(m => m.userId))
      ;(p.addedUserIds || []).forEach(uid => {
        if (!existing.has(uid)) {
          d.members.push({ userId: uid, realName: '?', username: '?', role: 'Member' })
        }
      })
    } else {
      fetchDetail(p.conversationId)
    }
  }

  /** SignalR: 成员移除 */
  function handleMemberRemoved(p) {
    if (!p?.conversationId) return
    const d = details.value[p.conversationId]
    if (d) {
      d.members = d.members.filter(m => m.userId !== p.removedUserId)
    }
  }

  /** SignalR: 群资料更新 */
  function handleGroupUpdated(p) {
    if (!p?.conversationId) return
    const d = details.value[p.conversationId]
    if (d) {
      if (p.title !== undefined) d.title = p.title
      if (p.notice !== undefined) d.notice = p.notice
    }
  }

  /** SignalR: 群解散 */
  function handleDissolved(p) {
    if (p?.conversationId) delete details.value[p.conversationId]
  }

  return {
    details, loading, activeId, isLoading,
    fetchDetail, addMembers, removeMemberApi, leave, update, dissolve,
    fetchConversationReads,
    handleMemberAdded, handleMemberRemoved,
    handleGroupUpdated, handleDissolved
  }
})