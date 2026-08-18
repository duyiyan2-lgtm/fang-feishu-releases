<template>
  <view class="meeting-container">
    <!-- 状态筛选 + 创建按钮 -->
    <view class="toolbar">
      <view class="status-filters">
        <text class="status-filter" :class="{ active: filterStatus === '' }" @tap="setFilter('')">全部</text>
        <text class="status-filter" :class="{ active: filterStatus === 'Active' }" @tap="setFilter('Active')">进行中</text>
        <text class="status-filter" :class="{ active: filterStatus === 'Ended' }" @tap="setFilter('Ended')">已结束</text>
      </view>
      <button class="create-btn" @tap="openCreate">+ 创建会议</button>
    </view>

    <!-- 会议列表 -->
    <view v-if="meetings.length" class="meeting-list">
      <view v-for="item in meetings" :key="item.id" class="meeting-item" @tap="openDetail(item)">
        <view class="meeting-header">
          <text class="meeting-title">{{ item.title }}</text>
          <text class="meeting-status" :class="'m-status-' + item.status">{{ item.status === 'Active' ? '进行中' : '已结束' }}</text>
        </view>
        <view class="meeting-meta">
          <text>创建者: {{ item.creatorName }}</text>
          <text class="meta-sep">|</text>
          <text>成员: {{ item.members?.length || 0 }}人</text>
        </view>
        <view class="meeting-time">
          <text>{{ formatDateTime(item.createdAt) }}</text>
          <text v-if="item.scheduledStartAt" class="time-schedule"> 📅 {{ formatDateTime(item.scheduledStartAt) }}</text>
        </view>
      </view>
    </view>
    <view v-else class="empty-state">
      <view class="empty-icon">📹</view>
      <text class="empty-text">暂无会议</text>
    </view>

    <!-- ===== 创建会议弹窗 ===== -->
    <view v-if="showCreateModal" class="modal-overlay" @tap="showCreateModal = false">
      <view class="modal-popup" @tap.stop>
        <text class="modal-title">创建会议</text>
        <view class="form-group">
          <text class="form-label">会议标题</text>
          <input v-model="form.title" class="form-input" placeholder="默认: 项目同步会议" />
        </view>
        <view class="form-group">
          <text class="form-label">会议室名称（可选）</text>
          <input v-model="form.roomName" class="form-input" placeholder="自动生成" />
        </view>
        <view class="form-group">
          <text class="form-label">预约开始时间（可选）</text>
          <picker mode="date" :value="form.startDate" @change="onStartDateChange">
            <view class="form-input">{{ form.startDate || '选择日期' }}</view>
          </picker>
          <picker mode="time" :value="form.startTime" @change="onStartTimeChange">
            <view class="form-input">{{ form.startTime || '14:00' }}</view>
          </picker>
        </view>
        <view class="form-group">
          <text class="form-label">预约结束时间（可选）</text>
          <picker mode="date" :value="form.endDate" @change="onEndDateChange">
            <view class="form-input">{{ form.endDate || '选择日期' }}</view>
          </picker>
          <picker mode="time" :value="form.endTime" @change="onEndTimeChange">
            <view class="form-input">{{ form.endTime || '15:00' }}</view>
          </picker>
        </view>
        <view class="modal-btns">
          <button class="modal-cancel" @tap="showCreateModal = false">取消</button>
          <button class="modal-confirm" @tap="submitMeeting">创建</button>
        </view>
      </view>
    </view>

    <!-- ===== 会议详情弹窗 ===== -->
    <view v-if="showDetailModal && detailItem" class="modal-overlay" @tap="showDetailModal = false">
      <view class="modal-popup detail-popup" @tap.stop>
        <text class="modal-title">{{ detailItem.title }}</text>
        <view class="detail-row"><text class="detail-label">状态</text><text class="detail-value" :class="'m-status-' + detailItem.status">{{ detailItem.status === 'Active' ? '进行中' : '已结束' }}</text></view>
        <view class="detail-row"><text class="detail-label">创建者</text><text class="detail-value">{{ detailItem.creatorName }}</text></view>
        <view class="detail-row"><text class="detail-label">会议 ID</text><text class="detail-value">{{ detailItem.roomId }}</text></view>
        <view class="detail-row"><text class="detail-label">频道名</text><text class="detail-value">{{ detailItem.channelName }}</text></view>
        <view class="detail-row"><text class="detail-label">创建时间</text><text class="detail-value">{{ formatDateTime(detailItem.createdAt) }}</text></view>
        <view v-if="detailItem.scheduledStartAt" class="detail-row"><text class="detail-label">预约开始</text><text class="detail-value">{{ formatDateTime(detailItem.scheduledStartAt) }}</text></view>
        <view v-if="detailItem.scheduledEndAt" class="detail-row"><text class="detail-label">预约结束</text><text class="detail-value">{{ formatDateTime(detailItem.scheduledEndAt) }}</text></view>
        <!-- 编辑日程按钮（仅预约了日程的会议可见） -->
        <view v-if="detailItem.scheduledStartAt" class="detail-row" style="border-bottom:none;justify-content:center;padding:8rpx 0;">
          <button class="edit-schedule-btn" @tap="openEditSchedule">编辑日程</button>
        </view>

        <!-- 视频通话区域 -->
        <view v-if="detailItem.status === 'Active'" class="video-section">
          <text class="video-section-title">📹 视频通话</text>
          <view class="video-info">
            <text class="video-info-text">服务器未配置声网 Agora，暂不支持小程序内视频通话。</text>
            <text class="video-info-text">如需测试，可使用以下信息通过 Agora RTC 客户端加入：</text>
          </view>
          <view class="video-cred-row">
            <text class="cred-label">频道名</text>
            <text class="cred-value" selectable>{{ detailItem.channelName }}</text>
          </view>
          <view class="video-cred-row">
            <text class="cred-label">App ID</text>
            <text class="cred-value" selectable>{{ agoraAppId || '未配置' }}</text>
          </view>
          <view class="video-cred-row">
            <text class="cred-label">Token</text>
            <text class="cred-value" selectable>{{ agoraToken || '未配置' }}</text>
          </view>
        </view>

        <!-- 成员列表 -->
        <view class="member-section">
          <text class="member-title">参会成员（{{ detailItem.members?.length || 0 }}人）</text>
          <view v-for="m in detailItem.members || []" :key="m.userId" class="member-item">
            <text class="member-name">{{ m.userName }}</text>
            <text class="member-role">{{ m.role === 'Owner' ? '创建者' : '成员' }}</text>
            <text class="member-status">{{ m.leftAt ? '已离开' : m.joinedAt ? '在线' : '未加入' }}</text>
          </view>
        </view>

        <!-- 邀请成员按钮（仅创建者/进行中可见） -->
        <view v-if="detailItem.status === 'Active' && detailItem.createdBy === myId" class="invite-section">
          <button class="invite-btn" @tap="openInviteMember">+ 邀请成员</button>
        </view>

        <!-- 操作 -->
        <view v-if="detailItem.status === 'Active'" class="detail-actions">
          <view class="modal-btns">
            <button class="btn-primary" @tap="handleJoin">✅ 签到加入</button>
            <button class="btn-leave" @tap="handleLeave">离开会议</button>
          </view>
          <view class="modal-btns" style="margin-top: 10rpx;">
            <button class="btn-end" @tap="handleEnd" v-if="detailItem.createdBy === myId || isAdmin">结束会议</button>
          </view>
        </view>
        <view v-if="detailItem.status === 'Ended'" class="detail-actions">
          <view class="modal-btns">
            <button class="btn-primary" @tap="handleViewStats">查看统计</button>
          </view>
        </view>
        <!-- 会议聊天 -->
        <view class="chat-section">
          <view class="chat-header" @tap="toggleChat">
            <text class="chat-header-title">会议聊天</text>
            <text class="chat-header-arrow">{{ showChat ? '收起 ▲' : '展开 ▼' }}</text>
          </view>
          <view v-if="showChat" class="chat-body">
            <scroll-view class="chat-messages" scroll-y :style="{ maxHeight: '300rpx' }">
              <view v-for="msg in chatMessages" :key="msg.id" class="chat-msg-item">
                <text class="chat-msg-sender">{{ msg.senderName || msg.senderId }}</text>
                <text class="chat-msg-content">{{ msg.content }}</text>
                <text class="chat-msg-time">{{ formatDateTime(msg.createdAt) }}</text>
              </view>
              <view v-if="chatMessages.length === 0" class="chat-empty">暂无消息</view>
            </scroll-view>
            <view class="chat-input-row">
              <input v-model="chatInput" class="chat-input" placeholder="输入消息..." confirm-type="send" @confirm="sendChat" />
              <button class="chat-send-btn" @tap="sendChat">发送</button>
            </view>
          </view>
        </view>
        <button class="modal-close-btn" @tap="showDetailModal = false">关闭</button>
      </view>
    </view>

    <!-- ===== 统计弹窗 ===== -->
    <view v-if="showStatsModal && statsData" class="modal-overlay" @tap="showStatsModal = false">
      <view class="modal-popup" @tap.stop>
        <text class="modal-title">会议统计</text>
        <view class="detail-row"><text class="detail-label">邀请人数</text><text class="detail-value">{{ statsData.invitedCount }}</text></view>
        <view class="detail-row"><text class="detail-label">已加入</text><text class="detail-value">{{ statsData.joinedCount }}</text></view>
        <view class="detail-row"><text class="detail-label">在线人数</text><text class="detail-value">{{ statsData.onlineCount }}</text></view>
        <view class="detail-row"><text class="detail-label">平均参与时长</text><text class="detail-value">{{ formatDuration(statsData.averageParticipationSeconds) }}</text></view>
        <view class="detail-row"><text class="detail-label">状态</text><text class="detail-value">{{ statsData.status === 'Active' ? '进行中' : '已结束' }}</text></view>
        <button class="modal-close-btn" @tap="showStatsModal = false">关闭</button>
      </view>
    </view>

    <!-- ===== 邀请成员弹窗 ===== -->
    <view v-if="showInviteModal" class="modal-overlay" @tap="showInviteModal = false">
      <view class="modal-popup" @tap.stop>
        <text class="modal-title">邀请成员</text>
        <view class="form-group">
          <input v-model="inviteKeyword" class="form-input" placeholder="搜索姓名..." @input="searchInviteCandidates" />
        </view>
        <view class="invite-candidate-list">
          <view v-for="u in inviteCandidates" :key="u.id" class="invite-candidate-item" @tap="toggleInviteUser(u)">
            <text class="invite-candidate-name">{{ u.realName || u.username }}</text>
            <text class="invite-candidate-check">{{ selectedInviteIds.has(u.id) ? '✓' : '' }}</text>
          </view>
          <view v-if="inviteKeyword && inviteCandidates.length === 0" class="invite-empty">未找到相关用户</view>
          <view v-if="!inviteKeyword" class="invite-empty">输入姓名搜索用户</view>
        </view>
        <view class="modal-btns">
          <button class="modal-cancel" @tap="showInviteModal = false">取消</button>
          <button class="modal-confirm" :disabled="selectedInviteIds.size === 0" @tap="submitInvite">确认邀请（{{ selectedInviteIds.size }}人）</button>
        </view>
      </view>
    </view>

    <!-- ===== 编辑日程弹窗 ===== -->
    <view v-if="showScheduleModal" class="modal-overlay" @tap="showScheduleModal = false">
      <view class="modal-popup" @tap.stop>
        <text class="modal-title">编辑日程</text>
        <view class="form-group">
          <text class="form-label">新的开始时间</text>
          <picker mode="date" :value="scheduleForm.startDate" @change="onScheduleStartDateChange">
            <view class="form-input">{{ scheduleForm.startDate || '选择日期' }}</view>
          </picker>
          <picker mode="time" :value="scheduleForm.startTime" @change="onScheduleStartTimeChange">
            <view class="form-input">{{ scheduleForm.startTime || '14:00' }}</view>
          </picker>
        </view>
        <view class="form-group">
          <text class="form-label">新的结束时间</text>
          <picker mode="date" :value="scheduleForm.endDate" @change="onScheduleEndDateChange">
            <view class="form-input">{{ scheduleForm.endDate || '选择日期' }}</view>
          </picker>
          <picker mode="time" :value="scheduleForm.endTime" @change="onScheduleEndTimeChange">
            <view class="form-input">{{ scheduleForm.endTime || '15:00' }}</view>
          </picker>
        </view>
        <view class="modal-btns">
          <button class="modal-cancel" @tap="showScheduleModal = false">取消</button>
          <button class="modal-confirm" @tap="submitSchedule">保存</button>
        </view>
      </view>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { useAuthStore } from '@/stores/auth'
import { getMeetings, createMeeting, joinMeeting, leaveMeeting, endMeeting, getMeetingStatistics, inviteMeetingMembers, updateMeetingSchedule, getMeetingChatMessages, sendMeetingChatMessage } from '@/api/meetings'
import { getFriends, discoverUsers } from '@/api/contacts'

const authStore = useAuthStore()
const myId = computed(() => authStore.userInfo?.id || '')
const isAdmin = computed(() => authStore.isAdmin)

const filterStatus = ref('')
const meetings = ref<any[]>([])
const showCreateModal = ref(false)
const showDetailModal = ref(false)
const showStatsModal = ref(false)
const detailItem = ref<any>(null)
const statsData = ref<any>(null)

// 邀请成员
const showInviteModal = ref(false)
const inviteKeyword = ref('')
const inviteCandidates = ref<any[]>([])
const selectedInviteIds = ref<Set<string>>(new Set())

// 编辑日程
const showScheduleModal = ref(false)
const scheduleForm = ref({
  startDate: '',
  startTime: '14:00',
  endDate: '',
  endTime: '15:00',
})

// 会议聊天
const showChat = ref(false)
const chatMessages = ref<any[]>([])
const chatInput = ref('')

// 保存最近一次 join 返回的 Agora 信息用于展示
const agoraAppId = ref('')
const agoraToken = ref('')

const form = ref({
  title: '',
  roomName: '',
  startDate: '',
  startTime: '14:00',
  endDate: '',
  endTime: '15:00',
})

/** 把后端英文错误转成中文 */
function translateError(msg: string): string {
  if (!msg) return '操作失败'
  if (msg.includes('Agora AppId is not configured')) return '视频服务（声网）未配置，无法进行视频通话'
  if (msg.includes('Meeting has ended')) return '会议已结束'
  if (msg.includes('Meeting not found')) return '会议不存在'
  if (msg.includes('No meeting permission')) return '无权限操作此会议'
  if (msg.includes('permission')) return '无权限操作'
  if (msg.includes('not found') || msg.includes('NOT FOUND')) return '资源不存在'
  if (msg.includes('network') || msg.includes('Network')) return '网络连接失败'
  return msg
}

function setFilter(s: string) {
  filterStatus.value = s
  loadMeetings()
}

async function loadMeetings() {
  try {
    const params: any = {}
    if (filterStatus.value) params.status = filterStatus.value
    const res: any = await getMeetings(params)
    meetings.value = Array.isArray(res) ? res : []
  } catch {
    meetings.value = []
  }
}

function formatDateTime(t?: string): string {
  if (!t) return '-'
  const d = new Date(t)
  return `${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')} ${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`
}

function formatDuration(seconds?: number): string {
  if (!seconds) return '-'
  const h = Math.floor(seconds / 3600)
  const m = Math.floor((seconds % 3600) / 60)
  return h > 0 ? `${h}小时${m}分钟` : `${m}分钟`
}

function openCreate() {
  form.value = { title: '', roomName: '', startDate: '', startTime: '14:00', endDate: '', endTime: '15:00' }
  showCreateModal.value = true
}

async function submitMeeting() {
  try {
    const data: any = {}
    if (form.value.title.trim()) data.title = form.value.title.trim()
    if (form.value.roomName.trim()) data.roomName = form.value.roomName.trim()
    if (form.value.startDate && form.value.endDate) {
      data.scheduledStartAt = new Date(`${form.value.startDate} ${form.value.startTime}`).toISOString()
      data.scheduledEndAt = new Date(`${form.value.endDate} ${form.value.endTime}`).toISOString()
    }
    await createMeeting(data)
    uni.showToast({ title: '创建成功', icon: 'success' })
    showCreateModal.value = false
    loadMeetings()
  } catch (e: any) {
    uni.showToast({ title: translateError(e.message), icon: 'none' })
  }
}

function openDetail(item: any) {
  detailItem.value = item
  // 重置 Agora 信息
  agoraAppId.value = ''
  agoraToken.value = ''
  showDetailModal.value = true
}

async function handleJoin() {
  if (!detailItem.value) return
  try {
    const res: any = await joinMeeting(detailItem.value.id)
    uni.showToast({ title: '已签到加入', icon: 'success' })
    // 保存 Agora 信息以便展示
    if (res?.appId) agoraAppId.value = res.appId
    if (res?.rtcToken) agoraToken.value = res.rtcToken
    showDetailModal.value = false
    loadMeetings()
  } catch (e: any) {
    uni.showToast({ title: translateError(e.message), icon: 'none' })
  }
}

async function handleLeave() {
  if (!detailItem.value) return
  try {
    await leaveMeeting(detailItem.value.id)
    uni.showToast({ title: '已离开会议', icon: 'success' })
    showDetailModal.value = false
    loadMeetings()
  } catch (e: any) {
    uni.showToast({ title: translateError(e.message), icon: 'none' })
  }
}

async function handleEnd() {
  if (!detailItem.value) return
  uni.showModal({
    title: '确认结束',
    content: '确定结束此会议吗？',
    success: async (res) => {
      if (res.confirm) {
        try {
          await endMeeting(detailItem.value.id)
          uni.showToast({ title: '会议已结束', icon: 'success' })
          showDetailModal.value = false
          loadMeetings()
        } catch (e: any) {
          uni.showToast({ title: translateError(e.message), icon: 'none' })
        }
      }
    },
  })
}

async function handleViewStats() {
  if (!detailItem.value) return
  try {
    const res: any = await getMeetingStatistics(detailItem.value.id)
    statsData.value = res
    showStatsModal.value = true
  } catch {
    uni.showToast({ title: '获取统计失败', icon: 'none' })
  }
}

function onStartDateChange(e: any) { form.value.startDate = e.detail.value }
function onStartTimeChange(e: any) { form.value.startTime = e.detail.value }
function onEndDateChange(e: any) { form.value.endDate = e.detail.value }
function onEndTimeChange(e: any) { form.value.endTime = e.detail.value }

// ========== 邀请成员 ==========

/** 打开邀请成员弹窗 */
function openInviteMember() {
  inviteKeyword.value = ''
  inviteCandidates.value = []
  selectedInviteIds.value = new Set()
  showInviteModal.value = true
}

/** 搜索可邀请的用户（好友 + 发现用户，排除已在会议中的成员） */
let inviteSearchTimer: ReturnType<typeof setTimeout> | null = null
async function searchInviteCandidates() {
  if (inviteSearchTimer) clearTimeout(inviteSearchTimer)
  const kw = inviteKeyword.value.trim()
  if (!kw) { inviteCandidates.value = []; return }
  inviteSearchTimer = setTimeout(async () => {
    const existingIds = new Set((detailItem.value?.members || []).map((m: any) => m.userId))
    const all: any[] = []
    const seen = new Set<string>()
    try {
      const friends: any = await getFriends()
      if (Array.isArray(friends)) {
        for (const f of friends) {
          if (!existingIds.has(f.id) && !seen.has(f.id) && (f.realName || f.username)?.includes(kw)) {
            seen.add(f.id); all.push(f)
          }
        }
      }
    } catch (e) { console.warn('[Meeting] getFriends failed', e) }
    try {
      const discovered: any = await discoverUsers(kw)
      if (Array.isArray(discovered)) {
        for (const u of discovered) {
          if (!existingIds.has(u.id) && !seen.has(u.id)) { seen.add(u.id); all.push(u) }
        }
      }
    } catch (e2) { console.warn('[Meeting] discoverUsers failed', e2) }
    inviteCandidates.value = all
  }, 300)
}

/** 切换选中用户 */
function toggleInviteUser(u: any) {
  const s = selectedInviteIds.value
  if (s.has(u.id)) { s.delete(u.id) }
  else { s.add(u.id) }
  // 触发响应式更新
  selectedInviteIds.value = new Set(s)
}

/** 提交邀请 */
async function submitInvite() {
  if (!detailItem.value || selectedInviteIds.value.size === 0) return
  try {
    await inviteMeetingMembers(detailItem.value.id, Array.from(selectedInviteIds.value))
    uni.showToast({ title: '邀请成功', icon: 'success' })
    showInviteModal.value = false
    // 刷新详情
    await loadMeetings()
    const updated = meetings.value.find((m: any) => m.id === detailItem.value.id)
    if (updated) detailItem.value = updated
  } catch (e: any) {
    uni.showToast({ title: translateError(e.message), icon: 'none' })
  }
}

// ========== 编辑日程 ==========

/** 打开编辑日程弹窗 */
function openEditSchedule() {
  if (!detailItem.value) return
  const s = detailItem.value.scheduledStartAt
  const e = detailItem.value.scheduledEndAt
  if (s) {
    const d = new Date(s)
    scheduleForm.value.startDate = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
    scheduleForm.value.startTime = `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`
  } else {
    scheduleForm.value.startDate = ''
    scheduleForm.value.startTime = '14:00'
  }
  if (e) {
    const d = new Date(e)
    scheduleForm.value.endDate = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
    scheduleForm.value.endTime = `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`
  } else {
    scheduleForm.value.endDate = ''
    scheduleForm.value.endTime = '15:00'
  }
  showScheduleModal.value = true
}

function onScheduleStartDateChange(e: any) { scheduleForm.value.startDate = e.detail.value }
function onScheduleStartTimeChange(e: any) { scheduleForm.value.startTime = e.detail.value }
function onScheduleEndDateChange(e: any) { scheduleForm.value.endDate = e.detail.value }
function onScheduleEndTimeChange(e: any) { scheduleForm.value.endTime = e.detail.value }

/** 提交日程编辑 */
async function submitSchedule() {
  if (!detailItem.value) return
  if (!scheduleForm.value.startDate || !scheduleForm.value.endDate) {
    uni.showToast({ title: '请选择完整的开始和结束时间', icon: 'none' })
    return
  }
  try {
    const scheduledStartAt = new Date(`${scheduleForm.value.startDate} ${scheduleForm.value.startTime}`).toISOString()
    const scheduledEndAt = new Date(`${scheduleForm.value.endDate} ${scheduleForm.value.endTime}`).toISOString()
    await updateMeetingSchedule(detailItem.value.id, { scheduledStartAt, scheduledEndAt })
    uni.showToast({ title: '日程已更新', icon: 'success' })
    showScheduleModal.value = false
    // 刷新详情
    await loadMeetings()
    const updated = meetings.value.find((m: any) => m.id === detailItem.value.id)
    if (updated) detailItem.value = updated
  } catch (e: any) {
    uni.showToast({ title: translateError(e.message), icon: 'none' })
  }
}

// ========== 会议聊天 ==========

/** 切换聊天面板 */
async function toggleChat() {
  showChat.value = !showChat.value
  if (showChat.value && detailItem.value) {
    await loadChatMessages()
  }
}

/** 加载聊天消息 */
async function loadChatMessages() {
  if (!detailItem.value) return
  try {
    const res: any = await getMeetingChatMessages(detailItem.value.id)
    chatMessages.value = Array.isArray(res) ? res : (res?.records || res?.items || res?.list || [])
  } catch {
    chatMessages.value = []
  }
}

/** 发送聊天消息 */
async function sendChat() {
  const content = chatInput.value.trim()
  if (!content || !detailItem.value) return
  try {
    await sendMeetingChatMessage(detailItem.value.id, content)
    chatInput.value = ''
    await loadChatMessages()
  } catch (e: any) {
    uni.showToast({ title: translateError(e.message), icon: 'none' })
  }
}

onShow(() => loadMeetings())
</script>

<style scoped>
.meeting-container { min-height: 100vh; background: #f6f8fc; }

.toolbar {
  display: flex; align-items: center; padding: 20rpx 24rpx; gap: 12rpx;
  flex-wrap: wrap;
}
.status-filters { display: flex; gap: 10rpx; flex: 1; flex-wrap: wrap; }
.status-filter {
  font-size: 22rpx; color: #64748b; padding: 6rpx 16rpx;
  background: #fff; border-radius: 999rpx; border: 1rpx solid #edf1f7;
}
.status-filter.active { color: #1f6fff; border-color: #1f6fff; background: #eef4ff; }
.create-btn {
  height: 64rpx; line-height: 64rpx; padding: 0 22rpx;
  background: linear-gradient(135deg, #1f6fff, #18b7ff); color: #fff;
  font-size: 24rpx; border-radius: 999rpx; border: none; flex-shrink: 0; font-weight: 700;
  box-shadow: 0 8rpx 20rpx rgba(31,111,255,0.2);
}

.meeting-list { margin: 0 24rpx; background: #fff; border-radius: 28rpx; overflow: hidden; box-shadow: 0 14rpx 36rpx rgba(31,49,84,0.07); }
.meeting-item { padding: 22rpx 18rpx; border-bottom: 1rpx solid #f0f2f5; }
.meeting-item:last-child { border-bottom: none; }
.meeting-item:active { background: #f8fbff; }
.meeting-header { display: flex; align-items: center; justify-content: space-between; }
.meeting-title { font-size: 28rpx; font-weight: 600; color: #111827; flex: 1; }
.meeting-status { font-size: 20rpx; padding: 4rpx 14rpx; border-radius: 999rpx; }
.m-status-Active { background: #d1fae5; color: #00a889; }
.m-status-Ended { background: #f3f4f6; color: #6b7280; }
.meeting-meta { font-size: 22rpx; color: #7b8494; margin-top: 8rpx; }
.meta-sep { margin: 0 8rpx; }
.meeting-time { font-size: 20rpx; color: #a8b0c2; margin-top: 6rpx; }
.time-schedule { color: #1f6fff; }

.empty-state { margin: 24rpx; text-align: center; padding: 120rpx 0; background: #fff; border-radius: 28rpx; }
.empty-icon { font-size: 72rpx; }
.empty-text { font-size: 28rpx; color: #64748b; display: block; margin-top: 16rpx; }

.modal-overlay {
  position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0,0,0,0.45);
  z-index: 999; display: flex; align-items: center; justify-content: center; padding: 60rpx;
}
.modal-popup {
  width: 100%; max-width: 600rpx; background: #fff; border-radius: 28rpx;
  padding: 32rpx; max-height: 80vh; overflow-y: auto;
}
.detail-popup { max-width: 640rpx; }
.modal-title { font-size: 32rpx; font-weight: 600; display: block; text-align: center; margin-bottom: 24rpx; }
.form-group { margin-bottom: 20rpx; }
.form-label { font-size: 24rpx; color: #7b8494; display: block; margin-bottom: 6rpx; }
.form-input {
  height: 64rpx; background: #f6f8fc; border-radius: 16rpx; padding: 0 20rpx;
  font-size: 26rpx; color: #111827; display: flex; align-items: center;
}
.modal-btns { display: flex; gap: 20rpx; margin-top: 20rpx; }
.modal-cancel, .modal-confirm, .modal-close-btn {
  flex: 1; height: 72rpx; line-height: 72rpx; font-size: 26rpx;
  border-radius: 36rpx; border: none; text-align: center;
}
.modal-cancel { background: #f6f8fc; color: #374151; }
.modal-confirm { background: #1f6fff; color: #fff; }
.modal-close-btn { width: 100%; background: #f6f8fc; color: #374151; margin-top: 16rpx; }

.detail-row { display: flex; padding: 14rpx 0; border-bottom: 1rpx solid #f0f2f5; }
.detail-label { font-size: 24rpx; color: #7b8494; width: 110rpx; flex-shrink: 0; }
.detail-value { font-size: 26rpx; color: #111827; flex: 1; }

/* 视频通话区域 */
.video-section {
  margin-top: 20rpx; padding: 20rpx; background: #fefce8;
  border-radius: 18rpx; border: 1rpx solid #fde68a;
}
.video-section-title { font-size: 26rpx; font-weight: 700; color: #92400e; display: block; margin-bottom: 8rpx; }
.video-info { margin-bottom: 12rpx; }
.video-info-text { font-size: 22rpx; color: #a16207; display: block; line-height: 1.6; }
.video-cred-row { display: flex; padding: 6rpx 0; }
.cred-label { font-size: 22rpx; color: #92400e; width: 100rpx; flex-shrink: 0; }
.cred-value { font-size: 22rpx; color: #1f6fff; font-weight: 600; flex: 1; user-select: text; }

.member-section { margin-top: 16rpx; padding-top: 16rpx; border-top: 1rpx solid #f0f2f5; }
.member-title { font-size: 26rpx; font-weight: 600; color: #111827; display: block; margin-bottom: 12rpx; }
.member-item { display: flex; align-items: center; padding: 8rpx 0; }
.member-name { flex: 1; font-size: 24rpx; color: #374151; }
.member-role { font-size: 20rpx; color: #7b8494; margin-right: 16rpx; }
.member-status { font-size: 20rpx; color: #00a889; }

.detail-actions { margin-top: 20rpx; padding-top: 16rpx; border-top: 1rpx solid #f0f2f5; }
.btn-primary { flex: 1; height: 72rpx; line-height: 72rpx; background: #1f6fff; color: #fff; border-radius: 36rpx; border: none; font-size: 26rpx; }
.btn-leave { flex: 1; height: 72rpx; line-height: 72rpx; background: #f59e0b; color: #fff; border-radius: 36rpx; border: none; font-size: 26rpx; }
.btn-end { flex: 1; height: 72rpx; line-height: 72rpx; background: #ef4444; color: #fff; border-radius: 36rpx; border: none; font-size: 26rpx; }

/* 邀请成员 */
.invite-section { padding: 12rpx 0; border-top: 1rpx solid #f0f2f5; margin-top: 12rpx; text-align: center; }
.invite-btn { height: 64rpx; line-height: 64rpx; padding: 0 32rpx; background: linear-gradient(135deg, #1f6fff, #18b7ff); color: #fff; font-size: 24rpx; border-radius: 32rpx; border: none; font-weight: 600; display: inline-block; }
.invite-btn:active { opacity: 0.85; }
.invite-candidate-list { max-height: 360rpx; overflow-y: auto; margin: 12rpx 0; }
.invite-candidate-item { display: flex; align-items: center; padding: 14rpx 16rpx; border-bottom: 1rpx solid #f0f2f5; }
.invite-candidate-item:active { background: #f8fbff; }
.invite-candidate-name { flex: 1; font-size: 26rpx; color: #374151; }
.invite-candidate-check { width: 36rpx; height: 36rpx; line-height: 36rpx; text-align: center; background: #1f6fff; color: #fff; border-radius: 50%; font-size: 20rpx; font-weight: 700; flex-shrink: 0; }
.invite-empty { text-align: center; padding: 30rpx 0; font-size: 24rpx; color: #a8b0c2; }

/* 编辑日程 */
.edit-schedule-btn { height: 56rpx; line-height: 56rpx; padding: 0 28rpx; background: #eef4ff; color: #1f6fff; font-size: 22rpx; border-radius: 28rpx; border: none; font-weight: 500; }
.edit-schedule-btn:active { background: #d6e5ff; }

/* 会议聊天 */
.chat-section { margin-top: 16rpx; border-top: 1rpx solid #f0f2f5; padding-top: 12rpx; }
.chat-header { display: flex; align-items: center; justify-content: space-between; padding: 8rpx 0; }
.chat-header:active { opacity: 0.7; }
.chat-header-title { font-size: 26rpx; font-weight: 600; color: #111827; }
.chat-header-arrow { font-size: 22rpx; color: #7b8494; }
.chat-body { margin-top: 8rpx; }
.chat-messages { background: #f9fafb; border-radius: 12rpx; padding: 12rpx; max-height: 300rpx; overflow-y: auto; }
.chat-msg-item { padding: 6rpx 0; border-bottom: 1rpx solid #f0f2f5; font-size: 22rpx; line-height: 1.6; }
.chat-msg-item:last-child { border-bottom: none; }
.chat-msg-sender { font-weight: 600; color: #1f6fff; margin-right: 6rpx; }
.chat-msg-content { color: #374151; }
.chat-msg-time { font-size: 18rpx; color: #a8b0c2; margin-left: 8rpx; }
.chat-empty { text-align: center; padding: 20rpx 0; font-size: 22rpx; color: #a8b0c2; }
.chat-input-row { display: flex; gap: 12rpx; margin-top: 8rpx; align-items: center; }
.chat-input { flex: 1; height: 60rpx; background: #f6f8fc; border-radius: 12rpx; padding: 0 16rpx; font-size: 24rpx; color: #111827; border: 1rpx solid #edf1f7; }
.chat-send-btn { height: 60rpx; line-height: 60rpx; padding: 0 24rpx; background: #1f6fff; color: #fff; font-size: 24rpx; border-radius: 12rpx; border: none; flex-shrink: 0; }
.chat-send-btn:active { opacity: 0.85; }
</style>
