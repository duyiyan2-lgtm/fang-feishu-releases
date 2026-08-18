// 会议 Pinia store
import { defineStore } from 'pinia'
import { ref } from 'vue'
import {
  listMeetings, createMeeting, getMeetingDetail,
  joinMeeting as joinMeetingApi, leaveMeetingApi,
  inviteMeetingMembers,
  endMeeting as endMeetingApi, postMeetingChat
} from '@/api/meetings'
import { ElMessage } from '@/api/toast'

export const useMeetingStore = defineStore('meeting', () => {
  /** 会议列表 */
  const list = ref([])
  /** 当前活跃会议 */
  const current = ref(null)
  /** 当前会议 join 后的 payload（含 Agora token） */
  const joinPayload = ref(null)
  /** 加载状态 */
  const loading = ref(false)

  /** 拉会议列表 */
  async function fetchList(status) {
    loading.value = true
    try {
      const data = await listMeetings(status)
      list.value = Array.isArray(data) ? data : (data?.items || [])
      return list.value
    } catch (e) {
      console.error('[meeting] fetchList failed', e)
      return []
    } finally {
      loading.value = false
    }
  }

  /** 创建会议（title 可选，后端 schema bug） */
  async function create(payload = {}) {
    try {
      const m = await createMeeting(payload)
      current.value = m
      list.value.unshift(m)
      return m
    } catch (e) {
      ElMessage({ message: '创建会议失败：' + e.message, type: 'error' })
      throw e
    }
  }

  /** 加入会议：返回 join payload（含 Agora token） */
  async function join(id) {
    try {
      const data = await joinMeetingApi(id)
      joinPayload.value = data
      current.value = data.meeting
      return data
    } catch (e) {
      ElMessage({ message: '加入会议失败：' + e.message, type: 'error' })
      throw e
    }
  }

  /** 离开会议 */
  async function leave(id) {
    try {
      await leaveMeetingApi(id)
      ElMessage({ message: '已离开会议', type: 'info' })
      if (current.value?.id === id) {
        current.value = null
        joinPayload.value = null
      }
    } catch (e) {
      console.warn('[meeting] leave failed', e)
    }
  }

  /** 结束会议 */
  async function end(id) {
    try {
      await endMeetingApi(id)
      ElMessage({ message: '会议已结束', type: 'success' })
      if (current.value?.id === id) {
        current.value = null
        joinPayload.value = null
      }
    } catch (e) {
      ElMessage({ message: '结束会议失败：' + e.message, type: 'error' })
    }
  }

  /** 邀请成员加入会议（拉他们进同一个 Agora 频道） */
  async function inviteMembers(id, memberUserIds) {
    if (!Array.isArray(memberUserIds) || memberUserIds.length === 0) return null
    try {
      return await inviteMeetingMembers(id, memberUserIds)
    } catch (e) {
      console.warn('[meeting] inviteMembers failed', e)
      return null
    }
  }

  return {
    list, current, joinPayload, loading,
    fetchList, create, join, leave, end, inviteMembers
  }
})