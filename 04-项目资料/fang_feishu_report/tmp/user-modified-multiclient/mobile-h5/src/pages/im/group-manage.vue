<template>
  <view class="gm-container">
    <!-- 群头像 -->
    <view class="gm-avatar-wrap" @tap="changeAvatar">
      <image v-if="group.avatar" :src="group.avatar" class="gm-avatar-img" mode="aspectFill" />
      <view v-else class="gm-avatar-img gm-avatar-default" :style="{ backgroundColor: avatarColor }">
        <text class="gm-avatar-text">{{ (group.title || '群')[0] }}</text>
      </view>
      <view class="gm-avatar-mask">
        <text class="gm-avatar-mask-text">更换头像</text>
      </view>
    </view>

    <!-- 群公告 -->
    <view class="gm-card">
      <view class="gm-announce-row" @tap="editAnnouncement">
        <text class="gm-label">群公告</text>
        <view class="gm-announce-content">
          <text class="gm-announce-text" v-if="announcement">{{ announcement }}</text>
          <text class="gm-announce-empty" v-else>{{ isOwner || isAdminUser ? '点击添加群公告' : '暂无群公告' }}</text>
          <text class="gm-arrow" v-if="isOwner || isAdminUser">›</text>
        </view>
      </view>
    </view>

    <!-- 基本信息 -->
    <view class="gm-card">
      <view class="gm-info-row" @tap="editName">
        <text class="gm-label">群名称</text>
        <view class="gm-value-wrap">
          <text class="gm-value">{{ group.title || '未命名群聊' }}</text>
          <text class="gm-arrow" v-if="canEditName">›</text>
        </view>
      </view>
      <view class="gm-divider" />
      <view class="gm-info-row">
        <text class="gm-label">群主</text>
        <text class="gm-value">{{ ownerName || '未知' }}</text>
      </view>
      <view class="gm-divider" />
      <view class="gm-info-row">
        <text class="gm-label">群成员</text>
        <text class="gm-value">{{ members.length }} 人</text>
      </view>
      <view class="gm-divider" />
      <view class="gm-info-row">
        <text class="gm-label">群聊 ID</text>
        <text class="gm-value gm-id">{{ conversationId }}</text>
      </view>
    </view>

    <!-- 权限设置（仅群主可见） -->
    <view v-if="isOwner" class="gm-card">
      <text class="gm-section-title">权限设置</text>
      <view class="gm-setting-row">
        <text class="gm-setting-label">拉人权限</text>
        <picker
          :value="settings.invitePermission === 'admin' ? 1 : 0"
          :range="['所有人', '仅群主/管理员']"
          @change="(e) => updateSetting('invitePermission', e.detail.value === 1 ? 'admin' : 'all')"
        >
          <view class="gm-picker-trigger">
            <text class="gm-picker-text">{{ settings.invitePermission === 'admin' ? '仅群主/管理员' : '所有人' }}</text>
            <text class="gm-arrow">›</text>
          </view>
        </picker>
      </view>
      <view class="gm-setting-row">
        <text class="gm-setting-label">踢人权限</text>
        <picker
          :value="settings.kickPermission === 'admin' ? 1 : 0"
          :range="['所有人', '仅群主/管理员']"
          @change="(e) => updateSetting('kickPermission', e.detail.value === 1 ? 'admin' : 'all')"
        >
          <view class="gm-picker-trigger">
            <text class="gm-picker-text">{{ settings.kickPermission === 'admin' ? '仅群主/管理员' : '所有人' }}</text>
            <text class="gm-arrow">›</text>
          </view>
        </picker>
      </view>
      <view class="gm-setting-row">
        <text class="gm-setting-label">改群名权限</text>
        <picker
          :value="settings.editNamePermission === 'admin' ? 1 : 0"
          :range="['所有人', '仅群主/管理员']"
          @change="(e) => updateSetting('editNamePermission', e.detail.value === 1 ? 'admin' : 'all')"
        >
          <view class="gm-picker-trigger">
            <text class="gm-picker-text">{{ settings.editNamePermission === 'admin' ? '仅群主/管理员' : '所有人' }}</text>
            <text class="gm-arrow">›</text>
          </view>
        </picker>
      </view>
    </view>

    <!-- 管理员列表（仅群主可见） -->
    <view v-if="isOwner" class="gm-card">
      <view class="gm-section-hd">
        <text class="gm-section-title">管理员</text>
        <text class="gm-add-btn" @tap="showAddAdminModal">+ 添加</text>
      </view>
      <view v-if="admins.length === 0" class="gm-empty-row">暂无管理员</view>
      <view v-for="m in admins" :key="m.userId" class="gm-member-row">
        <view class="gm-member-avatar" :style="{ backgroundColor: getColor(m.userId) }">
          <text class="gm-member-avatar-text">{{ (m.realName || '?')[0] }}</text>
        </view>
        <text class="gm-member-name">{{ m.realName || m.username }}</text>
        <text class="gm-remove-btn" @tap="removeAdmin(m)">移除</text>
      </view>
    </view>

    <!-- 群成员 -->
    <view class="gm-card">
      <view class="gm-section-hd">
        <text class="gm-section-title">群成员（{{ members.length }}人）</text>
        <text v-if="canInvite" class="gm-add-btn" @tap="openInviteModal">+ 拉人</text>
      </view>
      <view v-for="m in members" :key="m.userId" class="gm-member-row">
        <view class="gm-member-avatar" :style="{ backgroundColor: getColor(m.userId) }">
          <text class="gm-member-avatar-text">{{ (m.realName || '?')[0] }}</text>
        </view>
        <view class="gm-member-info">
          <text class="gm-member-name">{{ m.realName || m.username }}</text>
          <text v-if="m.userId === ownerId" class="gm-role-tag gm-role-owner">群主</text>
          <text v-else-if="isAdmin(m.userId)" class="gm-role-tag gm-role-admin">管理员</text>
        </view>
        <text v-if="canKickMember(m.userId)" class="gm-kick-btn" @tap="kickMember(m)">踢出</text>
      </view>
    </view>

    <!-- 退出 / 解散群聊 -->
    <view class="gm-danger-section">
      <view class="gm-danger-row" @tap="clearChatHistory">
        <text class="gm-danger-text">删除聊天记录</text>
        <text class="gm-arrow">›</text>
      </view>
      <view class="gm-divider" />
      <view class="gm-danger-row" @tap="quitGroup">
        <text class="gm-danger-text gm-warn-text">退出群聊</text>
        <text class="gm-arrow">›</text>
      </view>
      <view v-if="isOwner" class="gm-divider" />
      <view v-if="isOwner" class="gm-danger-row" @tap="dissolveGroup">
        <text class="gm-danger-text gm-danger-warn">解散群聊</text>
        <text class="gm-arrow">›</text>
      </view>
    </view>

    <!-- ===== 修改群名弹窗 ===== -->
    <view v-if="showNameModal" class="gm-modal-overlay" @tap="showNameModal = false">
      <view class="gm-modal-popup" @tap.stop>
        <text class="gm-modal-title">修改群名称</text>
        <input v-model="editNameVal" class="gm-modal-input" placeholder="输入群名称" maxlength="50" />
        <view class="gm-modal-btns">
          <button class="gm-modal-cancel" @tap="showNameModal = false">取消</button>
          <button class="gm-modal-confirm" :disabled="!editNameVal.trim()" @tap="confirmEditName">确定</button>
        </view>
      </view>
    </view>

    <!-- ===== 群公告弹窗 ===== -->
    <view v-if="showAnnounceModal" class="gm-modal-overlay" @tap="showAnnounceModal = false">
      <view class="gm-modal-popup" @tap.stop>
        <text class="gm-modal-title">{{ announcement ? '编辑群公告' : '添加群公告' }}</text>
        <textarea v-model="editAnnounceVal" class="gm-modal-textarea" placeholder="输入群公告内容" maxlength="2000" />
        <view class="gm-modal-btns">
          <button class="gm-modal-cancel" @tap="showAnnounceModal = false">取消</button>
          <button class="gm-modal-confirm" :disabled="!editAnnounceVal.trim() && !announcement" @tap="confirmAnnouncement">保存</button>
        </view>
      </view>
    </view>

    <!-- ===== 添加管理员弹窗 ===== -->
    <view v-if="showAdminModal" class="gm-modal-overlay" @tap="showAdminModal = false">
      <view class="gm-modal-popup" @tap.stop>
        <text class="gm-modal-title">选择管理员</text>
        <input v-model="adminSearch" class="gm-modal-input" placeholder="搜索成员..." />
        <scroll-view class="gm-modal-scroll" scroll-y>
          <view
            v-for="m in adminCandidates"
            :key="m.userId"
            class="gm-modal-row"
            @tap="confirmAddAdmin(m)"
          >
            <view class="gm-member-avatar" :style="{ backgroundColor: getColor(m.userId) }">
              <text class="gm-member-avatar-text">{{ (m.realName || '?')[0] }}</text>
            </view>
            <text class="gm-member-name">{{ m.realName || m.username }}</text>
            <text class="gm-add-icon">+</text>
          </view>
          <view v-if="adminCandidates.length === 0" class="gm-modal-empty">无匹配成员</view>
        </scroll-view>
        <button class="gm-modal-cancel gm-modal-full" @tap="showAdminModal = false">取消</button>
      </view>
    </view>

    <!-- ===== 拉人入群弹窗 ===== -->
    <view v-if="inviteModalVisible" class="gm-modal-overlay" @tap="inviteModalVisible = false">
      <view class="gm-modal-popup" @tap.stop>
        <text class="gm-modal-title">拉人入群</text>
        <view class="gm-modal-selected">
          <text>已选 {{ selectedInvite.length }} 人</text>
        </view>
        <input v-model="inviteSearch" class="gm-modal-input" placeholder="搜索联系人..." />
        <scroll-view class="gm-modal-scroll" scroll-y>
          <view
            v-for="m in inviteCandidates"
            :key="m.userId"
            class="gm-modal-row"
            @tap="toggleInvite(m)"
          >
            <view class="gm-member-avatar" :style="{ backgroundColor: getColor(m.userId) }">
              <text class="gm-member-avatar-text">{{ (m.realName || '?')[0] }}</text>
            </view>
            <text class="gm-member-name">{{ m.realName || m.username }}</text>
            <view class="gm-invite-check" :class="{ checked: inviteSelectedIds.has(m.userId) }">
              <text v-if="inviteSelectedIds.has(m.userId)" class="gm-check-mark">✓</text>
            </view>
          </view>
          <view v-if="inviteCandidates.length === 0" class="gm-modal-empty">无匹配联系人</view>
        </scroll-view>
        <view class="gm-modal-btns">
          <button class="gm-modal-cancel" @tap="inviteModalVisible = false">取消</button>
          <button class="gm-modal-confirm" :disabled="selectedInvite.length === 0" @tap="confirmInvite">加入群聊</button>
        </view>
      </view>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { onLoad } from '@dcloudio/uni-app'
import { getConversation, getConversations, getMessages, updateConversation, addMembers, removeMember, setAdmins, deleteConversation, dissolveConversation, leaveConversation, getAnnouncement, updateAnnouncement } from '@/api/im'
import { get } from '@/api/request'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
const myId = authStore.userInfo?.id || ''

const conversationId = ref('')
const group = ref<any>({})
const members = ref<any[]>([])
const ownerId = ref('')
const adminIds = ref<string[]>([])
const settings = ref({
  invitePermission: 'all',
  kickPermission: 'all',
  editNamePermission: 'all',
})

// ======== 计算属性 ========

const isOwner = computed(() => myId === ownerId.value)
const isAdminUser = computed(() => adminIds.value.includes(myId) || isOwner.value)

const ownerName = computed(() => {
  const m = members.value.find((m) => m.userId === ownerId.value)
  return m?.realName || m?.username || '未知'
})

const admins = computed(() =>
  members.value.filter((m) => adminIds.value.includes(m.userId) && m.userId !== ownerId.value)
)

/** 当前用户是否有改群名权限 */
const canEditName = computed(() => {
  if (isOwner.value) return true
  if (settings.value.editNamePermission === 'all') return true
  if (settings.value.editNamePermission === 'admin' && isAdminUser.value) return true
  return false
})

/** 当前用户是否有拉人权限 */
const canInvite = computed(() => {
  if (isOwner.value) return true
  if (settings.value.invitePermission === 'all') return true
  if (settings.value.invitePermission === 'admin' && isAdminUser.value) return true
  return false
})

/** 当前用户是否能踢指定成员 */
function canKickMember(userId: string): boolean {
  if (userId === ownerId.value) return false // 不能踢群主
  if (isOwner.value) return true
  if (settings.value.kickPermission === 'all') return true
  if (settings.value.kickPermission === 'admin' && isAdminUser.value) return true
  return false
}

function isAdmin(userId: string): boolean {
  return adminIds.value.includes(userId) && userId !== ownerId.value
}

// ======== 数据加载 ========

function applyConvData(conv: any) {
  group.value = conv

  // 1. 从 members 里提取角色信息
  const rawMembers = conv.members || []
  members.value = rawMembers.map((m: any) => {
    return { ...m, _role: m.role || m.type || '' }
  })

  // 2. 确定群主：尝试 API 字段 + 本地缓存（不靠猜）
  ownerId.value =
    conv.ownerId || conv.createdBy || conv.owner || conv.creatorId || ''

  if (!ownerId.value) {
    const ownerMember = members.value.find(
      (m: any) =>
        m._role === 'Owner' || m._role === 'owner' || m._role === '群主'
    )
    if (ownerMember) ownerId.value = ownerMember.userId
  }

  if (!ownerId.value) {
    const cached = uni.getStorageSync(`group_owner_${conversationId.value}`)
    if (cached) ownerId.value = cached
  }

  // 3. 提取 adminIds
  if (conv.adminIds?.length) {
    adminIds.value = conv.adminIds
  } else {
    adminIds.value = members.value
      .filter(
        (m: any) =>
          m._role === 'Admin' || m._role === 'admin' || m._role === '管理员'
      )
      .map((m: any) => m.userId)
  }

  // 4. 权限设置
  if (conv.settings) {
    settings.value = {
      invitePermission: conv.settings.invitePermission || 'all',
      kickPermission: conv.settings.kickPermission || 'all',
      editNamePermission: conv.settings.editNamePermission || 'all',
    }
  }
}

async function loadGroup() {
  let conv: any

  // 方案一：尝试详情接口
  try {
    conv = await getConversation(conversationId.value)
  } catch (e) {
    console.warn('[Group] getConversation failed, trying list', e)
    // 方案二：从会话列表查找
    try {
      const res: any = await getConversations()
      const list = Array.isArray(res) ? res : res?.items || res?.list || []
      conv = list.find((c: any) => c.id === conversationId.value)
    } catch (e2) { console.warn('[Group] getConversations list failed', e2) }
  }

  if (conv) {
    applyConvData(conv)
  }

  // 如果还没找到群主，从消息记录找系统入群消息的发送者（最准）
  if (!ownerId.value) {
    try {
      const msgsRes: any = await getMessages(conversationId.value, 1, 50)
      const msgs = Array.isArray(msgsRes) ? msgsRes : msgsRes?.items || msgsRes?.list || []
      const joinMsg = msgs.find((m: any) =>
        m.content?.startsWith?.('__SYSTEM_GROUP_JOIN__')
      )
      if (joinMsg) {
        ownerId.value = joinMsg.senderId
        // 写入缓存，下次直接读
        uni.setStorageSync(`group_owner_${conversationId.value}`, joinMsg.senderId)
      }
    } catch (e) { console.warn('[Group] find join msg failed', e) }
  }

  if (!conv && !ownerId.value) {
    uni.showToast({ title: '加载群信息失败', icon: 'none' })
  }

  // 加载群公告
  try {
    const annRes: any = await getAnnouncement(conversationId.value)
    if (annRes?.announcement) announcement.value = annRes.announcement
  } catch (e) { console.warn('[Group] announcement failed', e) }
}

const announcement = ref('')

// ======== 群公告 ========
const showAnnounceModal = ref(false)
const editAnnounceVal = ref('')

function editAnnouncement() {
  if (!isOwner.value && !isAdminUser.value) {
    uni.showToast({ title: '暂无权限修改公告', icon: 'none' })
    return
  }
  editAnnounceVal.value = announcement.value || ''
  showAnnounceModal.value = true
}

async function confirmAnnouncement() {
  const content = editAnnounceVal.value.trim()
  try {
    await updateAnnouncement(conversationId.value, content)
    announcement.value = content
    showAnnounceModal.value = false
    uni.showToast({ title: '公告已更新', icon: 'success' })
  } catch {
    uni.showToast({ title: '更新公告失败', icon: 'none' })
  }
}

// ======== 修改群名 ========
const showNameModal = ref(false)
const editNameVal = ref('')

function editName() {
  if (!canEditName.value) {
    uni.showToast({ title: '暂无权限修改群名', icon: 'none' })
    return
  }
  editNameVal.value = group.value.title || ''
  showNameModal.value = true
}

async function confirmEditName() {
  const name = editNameVal.value.trim()
  if (!name) return
  try {
    await updateConversation(conversationId.value, { title: name })
    group.value.title = name
    showNameModal.value = false
    uni.showToast({ title: '群名已修改', icon: 'success' })
  } catch {
    uni.showToast({ title: '修改失败', icon: 'none' })
  }
}

// ======== 更新权限设置 ========
async function updateSetting(key: string, value: string) {
  const newSettings = { ...settings.value, [key]: value }
  try {
    await updateConversation(conversationId.value, { settings: newSettings })
    settings.value = newSettings
    uni.showToast({ title: '权限已更新', icon: 'success' })
  } catch {
    uni.showToast({ title: '更新失败', icon: 'none' })
  }
}

// ======== 管理员管理 ========
const showAdminModal = ref(false)
const adminSearch = ref('')

function showAddAdminModal() {
  adminSearch.value = ''
  showAdminModal.value = true
}

const adminCandidates = computed(() => {
  let list = members.value.filter(
    (m) => m.userId !== ownerId.value && !adminIds.value.includes(m.userId)
  )
  if (adminSearch.value.trim()) {
    const kw = adminSearch.value.trim().toLowerCase()
    list = list.filter((m) => (m.realName || m.username || '').toLowerCase().includes(kw))
  }
  return list
})

async function confirmAddAdmin(m: any) {
  const newAdminIds = [...adminIds.value, m.userId]
  try {
    await setAdmins(conversationId.value, newAdminIds)
    adminIds.value = newAdminIds
    showAdminModal.value = false
    uni.showToast({ title: `已将 ${m.realName || m.username} 设为管理员`, icon: 'success' })
  } catch {
    uni.showToast({ title: '设置失败', icon: 'none' })
  }
}

async function removeAdmin(m: any) {
  uni.showModal({
    title: '提示',
    content: `确定移除 ${m.realName || m.username} 的管理员权限吗？`,
    success: async (res) => {
      if (!res.confirm) return
      const newAdminIds = adminIds.value.filter((id) => id !== m.userId)
      try {
        await setAdmins(conversationId.value, newAdminIds)
        adminIds.value = newAdminIds
        uni.showToast({ title: '已移除管理员', icon: 'success' })
      } catch {
        uni.showToast({ title: '移除失败', icon: 'none' })
      }
    },
  })
}

// ======== 拉人入群 ========
const inviteModalVisible = ref(false)
const inviteSearch = ref('')
const inviteSelectedIds = ref<Set<string>>(new Set())
const allContacts = ref<any[]>([])

const selectedInvite = computed(() =>
  allContacts.value.filter((c) => inviteSelectedIds.value.has(c.id))
)

const inviteCandidates = computed(() => {
  // 只显示还不是群成员的联系人
  const memberIds = new Set(members.value.map((m: any) => m.userId))
  let list = allContacts.value.filter((c: any) => !memberIds.has(c.id) && c.id !== myId)
  if (inviteSearch.value.trim()) {
    const kw = inviteSearch.value.trim().toLowerCase()
    list = list.filter((m: any) => (m.realName || m.username || '').toLowerCase().includes(kw))
  }
  return list
})

function openInviteModal() {
  inviteSearch.value = ''
  inviteSelectedIds.value = new Set()
  inviteModalVisible.value = true
  // 加载联系人
  get('/contacts').then((res: any) => {
    allContacts.value = Array.isArray(res) ? res : []
  }).catch(() => {
    allContacts.value = []
  })
}

function toggleInvite(m: any) {
  if (inviteSelectedIds.value.has(m.id)) {
    inviteSelectedIds.value.delete(m.id)
  } else {
    inviteSelectedIds.value.add(m.id)
  }
  inviteSelectedIds.value = new Set(inviteSelectedIds.value)
}

async function confirmInvite() {
  if (selectedInvite.value.length === 0) return
  const userIds = Array.from(inviteSelectedIds.value)
  try {
    await addMembers(conversationId.value, userIds)
    inviteModalVisible.value = false
    uni.showToast({ title: `已邀请 ${selectedInvite.value.length} 人`, icon: 'success' })
    // 刷新群成员
    await loadGroup()
  } catch {
    uni.showToast({ title: '邀请失败', icon: 'none' })
  }
}

// ======== 踢人 ========
async function kickMember(m: any) {
  if (m.userId === ownerId.value) {
    uni.showToast({ title: '不能踢出群主', icon: 'none' })
    return
  }
  uni.showModal({
    title: '提示',
    content: `确定将 ${m.realName || m.username} 移出群聊吗？`,
    success: async (res) => {
      if (!res.confirm) return
      try {
        await removeMember(conversationId.value, m.userId)
        members.value = members.value.filter((mm) => mm.userId !== m.userId)
        uni.showToast({ title: '已移出群聊', icon: 'success' })
      } catch {
        uni.showToast({ title: '操作失败', icon: 'none' })
      }
    },
  })
}

// ======== 更换群头像 ========
async function changeAvatar() {
  if (!isOwner.value && !isAdminUser.value) {
    uni.showToast({ title: '暂无权限修改群头像', icon: 'none' })
    return
  }
  try {
    const res = await uni.chooseImage({ count: 1 })
    if (!res.tempFilePaths?.length) return
    const tempPath = res.tempFilePaths[0]

    // 上传文件
    const token = uni.getStorageSync('token') || ''
    const uploadRes = await new Promise<any>((resolve, reject) => {
      uni.uploadFile({
        url: 'https://alxy.fun/api/v1/files/upload',
        filePath: tempPath,
        name: 'file',
        header: { Authorization: `Bearer ${token}` },
        success: (r) => {
          try {
            const data = JSON.parse(r.data as string)
            if ((r.statusCode === 200 || r.statusCode === 201) && data.code === 0) {
              resolve(data.data)
            } else reject(new Error(data.message || '上传失败'))
          } catch { reject(new Error('上传返回格式异常')) }
        },
        fail: () => reject(new Error('网络错误')),
      })
    })

    const fileId = uploadRes?.id || uploadRes?.fileId || ''
    if (!fileId) throw new Error('未获取到文件 ID')

    // 更新群头像
    const avatarUrl = `https://alxy.fun/api/v1/files/${fileId}/download?token=${encodeURIComponent(token)}`
    await updateConversation(conversationId.value, { avatar: avatarUrl })
    group.value.avatar = avatarUrl
    uni.showToast({ title: '群头像已更新', icon: 'success' })
  } catch (err: any) {
    uni.showToast({ title: err.message || '更换头像失败', icon: 'none' })
  }
}

// ======== 删除聊天记录（本地） ========
function clearChatHistory() {
  uni.showModal({
    title: '提示',
    content: '确定删除本地的聊天记录吗？此操作不可恢复。',
    success: (res) => {
      if (!res.confirm) return
      // 标记删除时间戳，下次进入只显示之后的消息
      const delKey = `del_at_${conversationId.value}_${myId}`
      uni.setStorageSync(delKey, new Date().toISOString())
      uni.showToast({ title: '已清除聊天记录', icon: 'success' })
      uni.navigateBack()
    },
  })
}

// ======== 退出群聊（使用标准退群接口，普通成员可用） ========
function quitGroup() {
  uni.showModal({
    title: '提示',
    content: '确定退出群聊吗？退群后将无法接收消息。',
    success: async (res) => {
      if (!res.confirm) return
      try {
        await leaveConversation(conversationId.value)
        // 清除本地聊天记录
        const delKey = `del_at_${conversationId.value}_${myId}`
        uni.setStorageSync(delKey, new Date().toISOString())
        uni.showToast({ title: '已退出群聊', icon: 'success' })
        uni.navigateBack()
      } catch {
        uni.showToast({ title: '操作失败', icon: 'none' })
      }
    },
  })
}

// ======== 解散群聊（仅群主） ========
async function tryDissolveApi(): Promise<boolean> {
  try {
    await dissolveConversation(conversationId.value)
    return true
  } catch {
    return false
  }
}

function dissolveGroup() {
  uni.showModal({
    title: '解散群聊',
    content: '确定解散此群聊吗？所有成员的聊天记录将被删除，且不可恢复。',
    success: async (res) => {
      if (!res.confirm) return
      const ok = await tryDissolveApi()
      const delKey = `del_at_${conversationId.value}_${myId}`
      uni.setStorageSync(delKey, new Date().toISOString())
      if (ok) {
        uni.showToast({ title: '群聊已解散', icon: 'success' })
      } else {
        uni.showToast({ title: '已清除本地记录（服务器可能仍保留）', icon: 'none' })
      }
      uni.navigateBack()
    },
  })
}

// ======== 头像颜色 ========
const colors = ['#409EFF', '#67C23A', '#E6A23C', '#F56C6C', '#909399', '#B37FEB', '#00BFA5', '#FF7043']
const avatarColor = ref('#409EFF')

function getColor(id: string): string {
  let hash = 0
  for (let i = 0; i < id.length; i++) hash = ((hash << 5) - hash) + id.charCodeAt(i)
  return colors[Math.abs(hash) % colors.length]
}

onLoad((options) => {
  conversationId.value = options?.conversationId || ''
  avatarColor.value = getColor(conversationId.value || '0')
  if (conversationId.value) loadGroup()
})
</script>

<style scoped>
.gm-container {
  min-height: 100vh;
  background: #f6f8fc;
  padding: 24rpx;
  padding-bottom: 60rpx;
}

/* ===== 头像 ===== */
.gm-avatar-wrap {
  width: 160rpx;
  height: 160rpx;
  margin: 20rpx auto 32rpx;
  position: relative;
  border-radius: 40rpx;
  overflow: hidden;
}
.gm-avatar-img {
  width: 100%;
  height: 100%;
  border-radius: 40rpx;
}
.gm-avatar-default {
  display: flex;
  align-items: center;
  justify-content: center;
}
.gm-avatar-text {
  color: #fff;
  font-size: 56rpx;
  font-weight: 700;
}
.gm-avatar-mask {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  height: 56rpx;
  background: rgba(0,0,0,0.4);
  display: flex;
  align-items: center;
  justify-content: center;
}
.gm-avatar-mask-text {
  color: #fff;
  font-size: 20rpx;
}

/* ===== 卡片 ===== */
.gm-card {
  background: #fff;
  border-radius: 28rpx;
  padding: 0 28rpx;
  margin-bottom: 20rpx;
  box-shadow: 0 12rpx 32rpx rgba(31,49,84,0.06);
}
.gm-info-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 28rpx 0;
}
.gm-label {
  font-size: 28rpx;
  color: #7b8494;
  flex-shrink: 0;
}
.gm-value-wrap {
  display: flex;
  align-items: center;
  max-width: 65%;
}
.gm-value {
  font-size: 28rpx;
  color: #111827;
  text-align: right;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.gm-id {
  font-size: 22rpx;
  color: #a8b0c2;
}
.gm-arrow {
  font-size: 32rpx;
  color: #c9cdd4;
  margin-left: 8rpx;
}
.gm-divider {
  height: 1rpx;
  background: #f0f2f5;
}

/* ===== 权限设置 ===== */
.gm-section-title {
  display: block;
  font-size: 28rpx;
  font-weight: 700;
  color: #111827;
  padding: 28rpx 0 16rpx;
}
.gm-setting-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 18rpx 0;
  border-bottom: 1rpx solid #f0f2f5;
}
.gm-setting-row:last-child {
  border-bottom: none;
}
.gm-setting-label {
  font-size: 26rpx;
  color: #374151;
}
.gm-picker-trigger {
  display: flex;
  align-items: center;
}
.gm-picker-text {
  font-size: 26rpx;
  color: #1f6fff;
}

/* ===== 成员/管理员 列表 ===== */
.gm-section-hd {
  display: flex;
  align-items: center;
  justify-content: space-between;
}
.gm-section-hd .gm-section-title {
  padding: 28rpx 0 16rpx;
}
.gm-add-btn {
  font-size: 26rpx;
  color: #1f6fff;
  font-weight: 600;
  padding: 12rpx 16rpx;
}
.gm-empty-row {
  text-align: center;
  color: #a8b0c2;
  font-size: 24rpx;
  padding: 30rpx 0;
}
.gm-member-row {
  display: flex;
  align-items: center;
  padding: 20rpx 0;
  border-bottom: 1rpx solid #f0f2f5;
}
.gm-member-row:last-child {
  border-bottom: none;
}
.gm-member-avatar {
  width: 56rpx;
  height: 56rpx;
  border-radius: 16rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-right: 20rpx;
  flex-shrink: 0;
}
.gm-member-avatar-text {
  color: #fff;
  font-size: 24rpx;
  font-weight: 600;
}
.gm-member-info {
  flex: 1;
  display: flex;
  align-items: center;
  gap: 12rpx;
  min-width: 0;
}
.gm-member-name {
  font-size: 28rpx;
  color: #111827;
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.gm-role-tag {
  font-size: 20rpx;
  padding: 2rpx 12rpx;
  border-radius: 999rpx;
  flex-shrink: 0;
}
.gm-role-owner {
  background: #fff2e0;
  color: #e6a23c;
}
.gm-role-admin {
  background: #eef4ff;
  color: #1f6fff;
}
.gm-remove-btn,
.gm-kick-btn {
  font-size: 24rpx;
  padding: 8rpx 20rpx;
  border-radius: 999rpx;
  flex-shrink: 0;
}
.gm-remove-btn {
  color: #e6a23c;
  background: #fff8f0;
}
.gm-kick-btn {
  color: #ef4444;
  background: #fff1f2;
}

/* ===== 退出按钮 ===== */
.gm-quit-btn {
  width: 100%;
  height: 88rpx;
  line-height: 88rpx;
  background: #fff;
  color: #ef4444;
  font-size: 28rpx;
  border-radius: 24rpx;
  border: none;
  margin-top: 20rpx;
  box-shadow: 0 10rpx 28rpx rgba(31,49,84,0.05);
}

/* ===== 弹窗通用 ===== */
.gm-modal-overlay {
  position: fixed;
  top: 0; left: 0; right: 0; bottom: 0;
  background: rgba(0,0,0,0.45);
  z-index: 999;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 60rpx;
}
.gm-modal-popup {
  width: 100%;
  max-width: 580rpx;
  max-height: 75vh;
  background: #fff;
  border-radius: 28rpx;
  padding: 32rpx;
  display: flex;
  flex-direction: column;
}
.gm-modal-title {
  font-size: 34rpx;
  font-weight: 700;
  text-align: center;
  display: block;
  margin-bottom: 24rpx;
  color: #111827;
}
.gm-modal-input {
  height: 72rpx;
  background: #f6f8fc;
  border-radius: 16rpx;
  padding: 0 20rpx;
  font-size: 26rpx;
  color: #111827;
  border: 1rpx solid #edf1f7;
  margin-bottom: 20rpx;
  box-sizing: border-box;
}
.gm-modal-scroll {
  max-height: 400rpx;
  margin-bottom: 20rpx;
  border: 1rpx solid #edf1f7;
  border-radius: 16rpx;
  background: #fafbfc;
}
.gm-modal-row {
  display: flex;
  align-items: center;
  padding: 16rpx 20rpx;
  border-bottom: 1rpx solid #f0f2f5;
}
.gm-modal-row:active { background: #eef4ff; }
.gm-modal-row:last-child { border-bottom: none; }
.gm-modal-row .gm-member-avatar {
  width: 48rpx;
  height: 48rpx;
  border-radius: 12rpx;
  margin-right: 16rpx;
}
.gm-modal-row .gm-member-avatar-text {
  font-size: 22rpx;
}
.gm-modal-row .gm-member-name {
  font-size: 26rpx;
}
.gm-add-icon {
  font-size: 28rpx;
  color: #1f6fff;
  font-weight: 700;
}
.gm-modal-empty {
  text-align: center;
  padding: 40rpx 0;
  font-size: 24rpx;
  color: #a8b0c2;
}
.gm-modal-btns {
  display: flex;
  gap: 20rpx;
  margin-top: 8rpx;
}
.gm-modal-cancel, .gm-modal-confirm {
  flex: 1;
  height: 76rpx;
  line-height: 76rpx;
  font-size: 28rpx;
  border-radius: 20rpx;
  border: none;
  text-align: center;
}
.gm-modal-cancel { background: #f6f8fc; color: #374151; }
.gm-modal-confirm { background: #1f6fff; color: #fff; }
.gm-modal-confirm[disabled] { opacity: 0.4; }
.gm-modal-full { margin-top: 0; }

/* 邀请勾选框 */
.gm-invite-check {
  width: 34rpx;
  height: 34rpx;
  border: 2rpx solid #cfd6e3;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}
.gm-invite-check.checked { background: #1f6fff; border-color: #1f6fff; }
.gm-check-mark { color: #fff; font-size: 20rpx; font-weight: bold; }
.gm-modal-selected {
  text-align: center;
  color: #7b8494;
  font-size: 24rpx;
  margin-bottom: 12rpx;
}

/* ===== 危险操作区 ===== */
.gm-danger-section {
  background: #fff;
  border-radius: 28rpx;
  overflow: hidden;
  box-shadow: 0 12rpx 32rpx rgba(31,49,84,0.06);
}
.gm-danger-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 28rpx;
}
.gm-danger-row:active {
  background: #f8fbff;
}
.gm-danger-text {
  font-size: 28rpx;
  color: #374151;
}
.gm-warn-text {
  color: #e6a23c;
}
.gm-danger-warn {
  color: #ef4444;
  font-weight: 600;
}

/* ===== 群公告 ===== */
.gm-announce-row {
  display: flex;
  align-items: flex-start;
  padding: 28rpx 0;
}
.gm-announce-content {
  flex: 1;
  display: flex;
  align-items: flex-start;
  max-width: 75%;
  min-width: 0;
}
.gm-announce-text {
  font-size: 26rpx;
  color: #111827;
  line-height: 1.5;
  word-break: break-all;
  flex: 1;
}
.gm-announce-empty {
  font-size: 26rpx;
  color: #a8b0c2;
  flex: 1;
}
.gm-modal-textarea {
  width: 100%;
  min-height: 160rpx;
  background: #f6f8fc;
  border-radius: 16rpx;
  padding: 20rpx;
  font-size: 26rpx;
  color: #111827;
  border: 1rpx solid #edf1f7;
  margin-bottom: 20rpx;
  box-sizing: border-box;
}
</style>
