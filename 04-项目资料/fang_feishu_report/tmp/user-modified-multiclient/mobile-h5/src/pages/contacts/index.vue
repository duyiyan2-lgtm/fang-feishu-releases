<template>
  <view class="contacts-container">
    <!-- 搜索栏 -->
    <view class="search-bar">
      <view class="search-input-wrapper">
        <text class="search-icon">🔍</text>
        <input
          v-model="keyword"
          :placeholder="activeTab === 'discover' ? '搜索姓名添加好友...' : '搜索姓名、手机号...'"
          placeholder-class="placeholder"
          @input="onSearchInput"
        />
        <text v-if="keyword" class="search-clear" @tap="clearSearch">✕</text>
      </view>
    </view>

    <!-- 顶部分页栏 -->
    <view class="tab-bar">
      <view class="tab-item" :class="{ active: activeTab === 'friends' }" @tap="switchTab('friends')">
        <text class="tab-label">我的好友</text>
      </view>
      <view class="tab-item" :class="{ active: activeTab === 'discover' }" @tap="switchTab('discover')">
        <text class="tab-label">发现用户</text>
      </view>
      <view class="tab-item" :class="{ active: activeTab === 'requests' }" @tap="switchTab('requests')">
        <text class="tab-label">好友申请</text>
        <view v-if="pendingIncomingCount > 0" class="tab-badge">
          <text class="tab-badge-text">{{ pendingIncomingCount }}</text>
        </view>
      </view>
    </view>

    <!-- 部门筛选（仅好友列表显示） -->
    <view v-if="activeTab === 'friends'" class="dept-section">
      <scroll-view class="dept-scroll" scroll-x enhanced show-scrollbar>
        <view
          v-for="dept in topLevelDepts"
          :key="dept.id"
          class="dept-chip"
          :class="{ active: selectedParentId === dept.id }"
          @tap="selectTopDept(dept)"
        >
          <text class="dept-chip-text">{{ dept.name }}</text>
          <text v-if="hasSubDepts(dept.id)" class="chip-expand-indicator">{{ selectedParentId === dept.id ? '▼' : '▶' }}</text>
        </view>
      </scroll-view>
      <scroll-view v-if="subDepts.length" class="dept-scroll sub" scroll-x enhanced show-scrollbar>
        <view
          v-for="dept in subDepts"
          :key="dept.id"
          class="dept-chip sub-chip"
          :class="{ active: selectedDeptId === dept.id }"
          @tap="selectDept(dept)"
        >
          <text class="dept-chip-text">{{ dept.name }}</text>
        </view>
      </scroll-view>
    </view>

    <!-- 加载状态 -->
    <view v-if="loading" class="loading-state">
      <view class="loading-icon">⏳</view>
      <text class="loading-text">正在加载...</text>
    </view>

    <!-- 错误状态 -->
    <view v-else-if="error" class="error-state">
      <view class="error-icon">⚠️</view>
      <text class="error-text">{{ error }}</text>
      <button class="retry-btn" @tap="retryLoad">重新加载</button>
    </view>

    <!-- ===== 好友列表 Tab ===== -->
    <template v-else-if="activeTab === 'friends'">
      <view v-if="filteredFriends.length === 0" class="empty-state">
        <view class="empty-icon">👥</view>
        <text class="empty-text">暂无好友</text>
        <text class="empty-hint">切换到「发现用户」添加好友</text>
      </view>
      <view v-else class="member-section">
        <view class="member-header">
          <text class="member-title">好友</text>
          <text class="member-count">共 {{ filteredFriends.length }} 人</text>
        </view>
        <view class="member-list">
          <view
            v-for="member in pageFriends"
            :key="member.id"
            class="member-item"
            @tap="startChat(member)"
            @longpress="showFriendActions(member)"
          >
            <view
              class="member-avatar"
              :style="{ backgroundColor: getAvatarColor(member.id) }"
              @tap.stop="showMemberCard(member)"
            >
              <text class="avatar-text">{{ getNameInitial(member.realName || member.username) }}</text>
            </view>
            <view class="member-info">
              <text class="member-name">{{ member.realName || member.username }}</text>
              <view class="member-tags">
                <text class="member-tag dept-tag">{{ toChineseName(member.departmentName) }}</text>
                <text class="member-tag pos-tag">{{ toChinesePosition(member.position) }}</text>
              </view>
            </view>
            <text class="member-arrow">›</text>
          </view>
        </view>
        <view v-if="totalPages > 1" class="pagination">
          <text class="page-btn" :class="{ disabled: page <= 1 }" @tap="changePage(page - 1)">‹ 上一页</text>
          <text class="page-info">第 {{ page }} / {{ totalPages }} 页</text>
          <text class="page-btn" :class="{ disabled: page >= totalPages }" @tap="changePage(page + 1)">下一页 ›</text>
        </view>
      </view>
    </template>

    <!-- ===== 发现用户 Tab ===== -->
    <template v-else-if="activeTab === 'discover'">
      <view v-if="!keyword && discoverResults.length === 0" class="empty-state">
        <view class="empty-icon">🔍</view>
        <text class="empty-text">搜索用户添加好友</text>
        <text class="empty-hint">输入姓名或用户名搜索</text>
      </view>
      <view v-else-if="discoverResults.length === 0" class="empty-state">
        <view class="empty-icon">📭</view>
        <text class="empty-text">未找到相关用户</text>
      </view>
      <view v-else class="member-section">
        <view class="member-header">
          <text class="member-title">搜索结果</text>
          <text class="member-count">找到 {{ discoverResults.length }} 人</text>
        </view>
        <view class="member-list">
          <view
            v-for="user in discoverResults"
            :key="user.id"
            class="member-item"
          >
            <view
              class="member-avatar"
              :style="{ backgroundColor: getAvatarColor(user.id) }"
              @tap.stop="showMemberCard(user)"
            >
              <text class="avatar-text">{{ getNameInitial(user.realName || user.username) }}</text>
            </view>
            <view class="member-info">
              <text class="member-name">{{ user.realName || user.username }}</text>
              <view class="member-tags">
                <text class="member-tag dept-tag">{{ toChineseName(user.departmentName) }}</text>
                <text class="member-tag pos-tag">{{ toChinesePosition(user.position) }}</text>
              </view>
            </view>
            <view
              v-if="pendingUserIds.has(user.id)"
              class="action-btn pending-btn"
            >等待回应</view>
            <view
              v-else
              class="action-btn add-btn"
              @tap.stop="openAddFriend(user)"
            >＋ 添加</view>
          </view>
        </view>
      </view>
    </template>

    <!-- ===== 好友申请 Tab ===== -->
    <template v-else-if="activeTab === 'requests'">
      <!-- 收到的申请 -->
      <view class="request-section">
        <view class="request-header">
          <text class="request-title">收到的申请</text>
        </view>
        <view v-if="incomingRequests.length === 0" class="request-empty">
          <text>暂无收到的申请</text>
        </view>
        <view v-else class="request-list">
          <view v-for="req in incomingRequests" :key="req.id" class="request-item">
            <view class="request-user">
              <view class="member-avatar small" :style="{ backgroundColor: getAvatarColor(req.user.id) }">
                <text class="avatar-text sm">{{ getNameInitial(req.user.realName || req.user.username) }}</text>
              </view>
              <view class="request-info">
                <text class="request-name">{{ req.user.realName || req.user.username }}</text>
                <text class="request-greeting">{{ req.greeting || '请求添加你为好友' }}</text>
              </view>
            </view>
            <view class="request-actions">
              <button class="req-btn accept" @tap="doAccept(req.id)">接受</button>
              <button class="req-btn reject" @tap="doReject(req.id)">拒绝</button>
            </view>
          </view>
        </view>
      </view>

      <!-- 发出的申请 -->
      <view class="request-section">
        <view class="request-header">
          <text class="request-title">发出的申请</text>
        </view>
        <view v-if="outgoingRequests.length === 0" class="request-empty">
          <text>暂无发出的申请</text>
        </view>
        <view v-else class="request-list">
          <view v-for="req in outgoingRequests" :key="req.id" class="request-item">
            <view class="request-user">
              <view class="member-avatar small" :style="{ backgroundColor: getAvatarColor(req.user.id) }">
                <text class="avatar-text sm">{{ getNameInitial(req.user.realName || req.user.username) }}</text>
              </view>
              <view class="request-info">
                <text class="request-name">{{ req.user.realName || req.user.username }}</text>
                <text class="request-greeting">等待对方接受...</text>
              </view>
            </view>
            <view class="request-actions">
              <text class="pending-tag">等待中</text>
            </view>
          </view>
        </view>
      </view>
    </template>

    <!-- ===== 好友操作菜单（长按弹出） ===== -->
    <view v-if="actionMember" class="card-overlay" @tap="closeActionMenu">
      <view class="action-menu" @tap.stop>
        <view class="action-item danger" @tap="doRemoveFriend(actionMember)">
          <text class="action-icon">🗑</text>
          <text>删除好友</text>
        </view>
        <view class="action-item" @tap="closeActionMenu">
          <text class="action-icon">✕</text>
          <text>取消</text>
        </view>
      </view>
    </view>

    <!-- ===== 添加好友弹窗 ===== -->
    <view v-if="addTarget" class="card-overlay" @tap="closeAddFriend">
      <view class="add-popup" @tap.stop>
        <view class="add-head">
          <view class="member-avatar" :style="{ backgroundColor: getAvatarColor(addTarget.id) }">
            <text class="avatar-text">{{ getNameInitial(addTarget.realName || addTarget.username) }}</text>
          </view>
          <text class="add-name">{{ addTarget.realName || addTarget.username }}</text>
          <text class="add-dept">{{ toChineseName(addTarget.departmentName) }}</text>
        </view>
        <view class="add-body">
          <textarea
            v-model="friendGreeting"
            class="greeting-input"
            placeholder="写一段申请语（选填）"
            maxlength="280"
          />
        </view>
        <view class="add-footer">
          <button class="req-btn accept" @tap="doSendRequest">发送申请</button>
          <button class="req-btn reject" @tap="closeAddFriend">取消</button>
        </view>
      </view>
    </view>

    <!-- ===== 员工名片弹窗 ===== -->
    <view v-if="cardMember" class="card-overlay" @tap="closeCard">
      <view class="card-popup" @tap.stop>
        <view class="card-head" :style="{ backgroundColor: getAvatarColor(cardMember.id) }">
          <view class="card-avatar">
            <text class="card-avatar-text">{{ getNameInitial(cardMember.realName || cardMember.username) }}</text>
          </view>
          <text class="card-name">{{ cardMember.realName || cardMember.username }}</text>
          <text class="card-username">{{ cardMember.username }}</text>
        </view>
        <view class="card-body">
          <view class="card-row">
            <text class="card-label">部门</text>
            <text class="card-value">{{ toChineseName(cardMember.departmentName) || '-' }}</text>
          </view>
          <view class="card-divider" />
          <view class="card-row">
            <text class="card-label">职位</text>
            <text class="card-value">{{ toChinesePosition(cardMember.position) || '-' }}</text>
          </view>
          <view class="card-divider" />
          <view class="card-row">
            <text class="card-label">邮箱</text>
            <text class="card-value">{{ cardMember.email || '-' }}</text>
          </view>
          <view class="card-divider" />
          <view class="card-row">
            <text class="card-label">手机</text>
            <text class="card-value">{{ cardMember.phone || '-' }}</text>
          </view>
        </view>
        <view class="card-footer">
          <button class="card-chat-btn" @tap="startChatFromCard">💬 发起聊天</button>
          <button class="card-close-btn" @tap="closeCard">关闭</button>
        </view>
      </view>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { onShow, onPullDownRefresh, onShareAppMessage } from '@dcloudio/uni-app'
import { get } from '@/api/request'
import { getConversations, createConversation } from '@/api/im'
import {
  getFriends,
  discoverUsers,
  getFriendRequests,
  sendFriendRequest,
  acceptFriendRequest,
  rejectFriendRequest,
  removeFriend,
} from '@/api/contacts'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()

// 未登录重定向
if (!authStore.isLoggedIn) {
  uni.reLaunch({ url: '/pages/login/index' })
}

// ======== 后端英文错误 → 中文映射 ========

const ERROR_ZH: Record<string, string> = {
  'You cannot add yourself as a friend.': '不能添加自己为好友',
  'Contact not found.': '用户不存在或已禁用',
  'Friend greeting must be at most 280 characters.': '申请语不能超过280字',
  'already friends': '你们已经是好友了',
  'A friend request is already pending.': '已发送过申请，请等待对方处理',
  'Friend request not found.': '好友申请不存在',
  'Only the recipient can process this friend request.': '只有接收人才能处理该申请',
  'Friend request has already been processed.': '该申请已被处理',
  'Friend relationship not found.': '好友关系不存在',
}

function toChineseError(msg: string): string {
  return ERROR_ZH[msg] || msg
}

// ======== 中英文映射 ========

function toChineseName(englishName: string): string {
  const map: Record<string, string> = {
    'Technology': '技术部',
    'Product': '产品部',
    'Operations': '运营部',
    'Design': '设计部',
    'Marketing': '市场部',
    'Human Resources': '人力资源部',
    'Finance': '财务部',
    'Administration': '行政部',
    'Sales': '销售部',
    'HR': '人力资源部',
    'Admin': '行政部',
    'FangFeishu Demo Company': '仿飞书 Demo 公司',
  }
  return map[englishName] || englishName
}

function toChinesePosition(position: string): string {
  if (!position) return ''
  const map: Record<string, string> = {
    'Project Lead': '项目主管',
    'Senior Developer': '高级开发',
    'Frontend Developer': '前端开发',
    'Backend Developer': '后端开发',
    'DevOps Engineer': '运维工程师',
    'Product Manager': '产品经理',
    'Designer': '设计师',
    'Tester': '测试工程师',
    'Operations Specialist': '运营专员',
    'Marketing Specialist': '市场专员',
    'HR Specialist': '人力资源专员',
    'Finance Specialist': '财务专员',
    'Admin Assistant': '行政助理',
  }
  return map[position] || position
}

// ======== 部门 ========

interface Dept {
  id: string
  name: string
  parentId?: string | null
}

const departments = ref<Dept[]>([])
const selectedDeptId = ref<string | null>(null)
const selectedParentId = ref<string | null>(null)

const topLevelDepts = computed(() => departments.value.filter((d) => !d.parentId))

const subDepts = computed(() => {
  if (!selectedParentId.value) return []
  return departments.value.filter((d) => d.parentId === selectedParentId.value)
})

function collectChildDeptIds(deptId: string): Set<string> {
  const ids = new Set<string>([deptId])
  let prevSize = 0
  while (prevSize !== ids.size) {
    prevSize = ids.size
    for (const d of departments.value) {
      if (d.parentId && ids.has(d.parentId)) ids.add(d.id)
    }
  }
  return ids
}

async function loadDepartments() {
  try {
    const tree: any = await get('/departments/tree')
    const treeList = Array.isArray(tree) ? tree : []
    const flat: Dept[] = []
    const list = treeList.length === 1 && treeList[0].children?.length
      ? treeList[0].children
      : treeList
    function flatten(items: any[], parentId?: string | null) {
      items.forEach((item: any) => {
        flat.push({ id: item.id, name: toChineseName(item.name), parentId: parentId || null })
        if (item.children?.length) flatten(item.children, item.id)
      })
    }
    flatten(list, null)
    departments.value = flat
  } catch {
    departments.value = [
      { id: '1', name: '技术部' },
      { id: '2', name: '产品部' },
    ]
  }
}

// ======== 成员 / 好友 ========

interface Member {
  id: string
  username: string
  realName?: string
  departmentName: string
  position: string
  email: string
  phone: string
  departmentId?: string
}

interface FriendRequest {
  id: string
  status: string
  direction: 'Incoming' | 'Outgoing'
  greeting?: string
  createdAt: string
  user: Member
}

// 好友列表
const allFriends = ref<Member[]>([])
const friendsLoaded = ref(false)

// 搜索结果
const discoverResults = ref<Member[]>([])

// 好友申请
const incomingRequests = ref<FriendRequest[]>([])
const outgoingRequests = ref<FriendRequest[]>([])

const pendingIncomingCount = computed(() => incomingRequests.value.length)

// 所有待处理申请中的用户ID（包括已发送和已接收的）
const pendingUserIds = computed(() => {
  const ids = new Set<string>()
  for (const r of incomingRequests.value) ids.add(r.user.id)
  for (const r of outgoingRequests.value) ids.add(r.user.id)
  return ids
})

// Tab
const activeTab = ref<'friends' | 'discover' | 'requests'>('friends')

// 搜索
const keyword = ref('')
const page = ref(1)
const pageSize = 20
const loading = ref(false)
const error = ref('')

// 好友列表筛选（部门 + 关键字）
const filteredFriends = computed(() => {
  let list = allFriends.value
  if (selectedDeptId.value) {
    const deptIds = collectChildDeptIds(selectedDeptId.value)
    list = list.filter((m) => m.departmentId && deptIds.has(m.departmentId))
  }
  if (keyword.value.trim()) {
    const kw = keyword.value.trim().toLowerCase()
    list = list.filter((m) => {
      const name = (m.realName || m.username || '').toLowerCase()
      const phone = (m.phone || '').toLowerCase()
      return name.includes(kw) || phone.includes(kw)
    })
  }
  return list
})

const pageFriends = computed(() => {
  const start = (page.value - 1) * pageSize
  return filteredFriends.value.slice(start, start + pageSize)
})

const totalPages = computed(() => Math.ceil(filteredFriends.value.length / pageSize) || 1)

// ======== 数据加载 ========

async function loadFriends() {
  try {
    const res: any = await getFriends()
    allFriends.value = Array.isArray(res) ? res : []
    friendsLoaded.value = true
  } catch {
    // 静默失败
  }
}

async function loadRequests() {
  try {
    const res: any = await getFriendRequests()
    const list = Array.isArray(res) ? res : []
    incomingRequests.value = list.filter((r: FriendRequest) => r.direction === 'Incoming')
    outgoingRequests.value = list.filter((r: FriendRequest) => r.direction === 'Outgoing')
  } catch {
    // 静默失败
  }
}

async function doSearch() {
  const kw = keyword.value.trim()
  if (!kw) {
    discoverResults.value = []
    return
  }
  loading.value = true
  error.value = ''
  try {
    // 同时加载申请列表，用于判断哪些用户已发过申请
    const [discoverRes, reqRes] = await Promise.allSettled([
      discoverUsers(kw),
      getFriendRequests(),
    ])
    if (discoverRes.status === 'fulfilled') {
      discoverResults.value = Array.isArray(discoverRes.value) ? discoverRes.value : []
    } else {
      discoverResults.value = []
    }
    if (reqRes.status === 'fulfilled') {
      const list = Array.isArray(reqRes.value) ? reqRes.value : []
      outgoingRequests.value = list.filter((r: FriendRequest) => r.direction === 'Outgoing')
    }
  } catch (e: any) {
    error.value = e?.message || '搜索失败'
    discoverResults.value = []
  } finally {
    loading.value = false
  }
}

// ======== 加载（进入页面 + 切换 Tab） ========

async function loadCurrentTab() {
  loading.value = true
  error.value = ''
  try {
    if (activeTab.value === 'friends') {
      await loadFriends()
    } else if (activeTab.value === 'discover') {
      await doSearch()
    } else if (activeTab.value === 'requests') {
      await loadRequests()
    }
  } catch (e: any) {
    error.value = e?.message || '加载失败'
  } finally {
    loading.value = false
  }
}

function switchTab(tab: 'friends' | 'discover' | 'requests') {
  activeTab.value = tab
  keyword.value = ''
  page.value = 1
  discoverResults.value = []
  loadCurrentTab()
}

// ======== 交互 ========

function hasSubDepts(deptId: string): boolean {
  return departments.value.some((d) => d.parentId === deptId)
}

function selectTopDept(dept: Dept) {
  if (selectedParentId.value === dept.id) {
    selectedParentId.value = null
    selectedDeptId.value = null
  } else {
    selectedParentId.value = dept.id
    selectedDeptId.value = dept.id
  }
  keyword.value = ''
  page.value = 1
}

function selectDept(dept: Dept) {
  if (selectedDeptId.value === dept.id) {
    selectedDeptId.value = selectedParentId.value
  } else {
    selectedDeptId.value = dept.id
  }
  keyword.value = ''
  page.value = 1
}

// 搜索防抖
let searchTimer: ReturnType<typeof setTimeout> | null = null
const SEARCH_DEBOUNCE_MS = 300

function onSearchInput() {
  if (searchTimer) clearTimeout(searchTimer)
  if (activeTab.value === 'discover') {
    if (!keyword.value.trim()) {
      discoverResults.value = []
      return
    }
    searchTimer = setTimeout(() => {
      doSearch()
    }, SEARCH_DEBOUNCE_MS)
  } else {
    // 好友列表是 computed 自动过滤，只需重置分页
    page.value = 1
  }
}

function clearSearch() {
  keyword.value = ''
  if (activeTab.value === 'discover') {
    discoverResults.value = []
  } else {
    selectedDeptId.value = null
    selectedParentId.value = null
    page.value = 1
  }
}

function changePage(p: number) {
  if (p < 1 || p > totalPages.value) return
  page.value = p
}

function retryLoad() {
  loadDepartments()
  loadCurrentTab()
}

// ======== 好友操作 ========

// 添加好友弹窗
const addTarget = ref<Member | null>(null)
const friendGreeting = ref('')

function openAddFriend(member: Member) {
  addTarget.value = member
  friendGreeting.value = ''
}

function closeAddFriend() {
  addTarget.value = null
  friendGreeting.value = ''
}

async function doSendRequest() {
  if (!addTarget.value) return
  const targetId = addTarget.value.id
  try {
    await sendFriendRequest(addTarget.value.id, friendGreeting.value || undefined)
    uni.showToast({ title: '好友申请已发送', icon: 'success' })
    closeAddFriend()
    // 刷新好友列表和申请列表，pendingUserIds 自动把按钮变为"等待回应"
    loadFriends()
    loadRequests()
  } catch (e: any) {
    const msg = e?.message || ''
    if (msg.includes('pending')) {
      uni.showToast({ title: '已发送过申请，请等待对方处理', icon: 'none' })
      closeAddFriend()
      loadRequests()
    } else {
      uni.showToast({ title: toChineseError(msg) || '发送失败', icon: 'none' })
    }
  }
}

// 接受 / 拒绝好友申请
async function doAccept(id: string) {
  try {
    await acceptFriendRequest(id)
    uni.showToast({ title: '已添加好友', icon: 'success' })
    loadRequests()
    loadFriends()
  } catch (e: any) {
    uni.showToast({ title: toChineseError(e?.message) || '操作失败', icon: 'none' })
  }
}

async function doReject(id: string) {
  try {
    await rejectFriendRequest(id)
    uni.showToast({ title: '已拒绝', icon: 'none' })
    loadRequests()
  } catch (e: any) {
    uni.showToast({ title: toChineseError(e?.message) || '操作失败', icon: 'none' })
  }
}

// 长按好友 → 删除
const actionMember = ref<Member | null>(null)

function showFriendActions(member: Member) {
  actionMember.value = member
}

function closeActionMenu() {
  actionMember.value = null
}

async function doRemoveFriend(member: Member) {
  closeActionMenu()
  try {
    await removeFriend(member.id)
    uni.showToast({ title: '已删除好友', icon: 'none' })
    allFriends.value = allFriends.value.filter((f) => f.id !== member.id)
  } catch (e: any) {
    uni.showToast({ title: toChineseError(e?.message) || '删除失败', icon: 'none' })
  }
}

// ======== 点击成员行 → 发起聊天 ========

async function startChatFromCard() {
  if (!cardMember.value) return
  const member = cardMember.value
  closeCard()
  await startChatInner(member)
}

async function startChat(member: Member) {
  if (actionMember.value) return // 长按菜单打开时不触发
  await startChatInner(member)
}

async function startChatInner(member: Member) {
  const myId = authStore.userInfo?.id || ''
  if (member.id === myId) {
    uni.showToast({ title: '不能和自己聊天', icon: 'none' })
    return
  }
  try {
    const convs: any = await getConversations()
    const list = Array.isArray(convs) ? convs : []
    const existing = list
      .filter((c: any) =>
        c.type === 'Private' &&
        c.members?.some((m: any) => m.userId === member.id)
      )
      .sort((a: any, b: any) => {
        const ta = a.lastMessage?.createdAt || a.createdAt
        const tb = b.lastMessage?.createdAt || b.createdAt
        return new Date(tb).getTime() - new Date(ta).getTime()
      })[0]
    if (existing) {
      const hiddenKey = `hidden_convs_${myId}`
      try {
        const raw = uni.getStorageSync(hiddenKey) || '[]'
        const ids: string[] = JSON.parse(raw)
        if (ids.includes(existing.id)) {
          uni.setStorageSync(hiddenKey, JSON.stringify(ids.filter((id: string) => id !== existing.id)))
        }
      } catch {}
      uni.navigateTo({
        url: `/pages/im/chat?conversationId=${existing.id}&name=${encodeURIComponent(member.realName || member.username)}&type=Private`,
      })
    } else {
      const newConv = await createConversation({
        type: 'Private',
        title: member.realName || member.username,
        memberUserIds: [member.id],
      })
      if (newConv?.id) {
        uni.navigateTo({
          url: `/pages/im/chat?conversationId=${newConv.id}&name=${encodeURIComponent(member.realName || member.username)}&type=Private`,
        })
      }
    }
  } catch {
    uni.showToast({ title: '无法发起会话', icon: 'none' })
  }
}

// ======== 名片弹窗 ========

const cardMember = ref<Member | null>(null)

function showMemberCard(member: Member) {
  cardMember.value = member
}

function closeCard() {
  cardMember.value = null
}

// ======== 头像工具 ========

const colors = ['#409EFF', '#67C23A', '#E6A23C', '#F56C6C', '#909399', '#B37FEB', '#00BFA5', '#FF7043']

function getAvatarColor(id: string): string {
  let hash = 0
  for (let i = 0; i < id.length; i++) hash = ((hash << 5) - hash) + id.charCodeAt(i)
  return colors[Math.abs(hash) % colors.length]
}

function getNameInitial(name: string): string {
  return name ? name.charAt(0).toUpperCase() : '?'
}

// ======== 通讯录分享 ========

onShareAppMessage(() => {
  return {
    title: '仿飞书 - 公司通讯录',
    path: '/pages/contacts/index',
  }
})

// ======== 初始化 ========

onShow(() => {
  loadDepartments()
  loadCurrentTab()
})

onPullDownRefresh(() => {
  loadDepartments()
  loadCurrentTab()
  uni.stopPullDownRefresh()
})
</script>

<style scoped>
/* ===== 全局 ===== */
.contacts-container {
  min-height: 100vh;
  background: #f6f8fc;
  padding-bottom: 20rpx;
  box-sizing: border-box;
}

/* ===== 搜索栏 ===== */
.search-bar {
  padding: 22rpx 24rpx 8rpx;
  background: #f6f8fc;
  position: sticky;
  top: 0;
  z-index: 10;
}
.search-input-wrapper {
  display: flex;
  align-items: center;
  background: #fff;
  border-radius: 28rpx;
  padding: 18rpx 28rpx;
  box-shadow: 0 10rpx 30rpx rgba(31, 49, 84, 0.06);
  border: 1rpx solid #edf1f7;
}
.search-icon {
  font-size: 28rpx;
  margin-right: 12rpx;
  color: #1f6fff;
}
.search-input-wrapper input {
  flex: 1;
  font-size: 26rpx;
  color: #1d2129;
  height: 36rpx;
}
.search-clear {
  font-size: 28rpx;
  color: #a8b0c2;
  padding: 4rpx 8rpx;
}
.placeholder {
  color: #a8b0c2;
  font-size: 26rpx;
}

/* ===== Tab 栏 ===== */
.tab-bar {
  display: flex;
  padding: 8rpx 24rpx 12rpx;
  background: #f6f8fc;
  gap: 8rpx;
}
.tab-item {
  flex: 1;
  text-align: center;
  padding: 16rpx 0;
  background: #fff;
  border-radius: 16rpx;
  position: relative;
  box-shadow: 0 6rpx 20rpx rgba(31, 49, 84, 0.04);
  border: 1rpx solid #edf1f7;
}
.tab-item.active {
  background: linear-gradient(135deg, #1f6fff, #18b7ff);
  border-color: transparent;
  box-shadow: 0 10rpx 24rpx rgba(31, 111, 255, 0.18);
}
.tab-label {
  font-size: 26rpx;
  color: #4b5563;
  font-weight: 500;
}
.tab-item.active .tab-label {
  color: #fff;
}
.tab-badge {
  position: absolute;
  top: -4rpx;
  right: -4rpx;
  min-width: 32rpx;
  height: 32rpx;
  background: #ff4d4f;
  border-radius: 16rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0 6rpx;
  border: 2rpx solid #fff;
}
.tab-badge-text {
  color: #fff;
  font-size: 18rpx;
  font-weight: 600;
}

/* ===== 部门列表（横向） ===== */
.dept-section {
  padding: 0 24rpx 12rpx;
  background: #f6f8fc;
}
.dept-scroll {
  display: flex;
  flex-direction: row;
  white-space: nowrap;
  width: 100%;
}
.dept-chip {
  display: inline-flex;
  align-items: center;
  padding: 13rpx 28rpx;
  margin-right: 16rpx;
  background: #fff;
  border-radius: 999rpx;
  box-shadow: 0 8rpx 24rpx rgba(31, 49, 84, 0.05);
  border: 1rpx solid #edf1f7;
  transition: all 0.2s;
}
.dept-chip.active {
  background: linear-gradient(135deg, #1f6fff, #18b7ff);
  box-shadow: 0 12rpx 26rpx rgba(31, 111, 255, 0.22);
  border-color: transparent;
}
.dept-chip-text {
  font-size: 26rpx;
  color: #4b5563;
}
.dept-chip.active .dept-chip-text {
  color: #fff;
  font-weight: 500;
}
.dept-section .sub {
  margin-top: 12rpx;
}
.sub-chip {
  background: #eef4ff;
  font-size: 24rpx;
  padding: 8rpx 24rpx;
}
.sub-chip.active {
  background: linear-gradient(135deg, #00b8a9, #1fddc5);
  box-shadow: 0 12rpx 26rpx rgba(0, 184, 169, 0.2);
}
.chip-expand-indicator {
  font-size: 16rpx;
  color: #c9cdd4;
  margin-left: 4rpx;
}
.dept-chip.active .chip-expand-indicator {
  color: rgba(255,255,255,0.7);
}

/* ===== 加载 / 错误 / 空状态 ===== */
.loading-state,
.error-state {
  margin: 24rpx;
  padding: 80rpx 0;
  background: #fff;
  border-radius: 28rpx;
  text-align: center;
  box-shadow: 0 12rpx 32rpx rgba(31, 49, 84, 0.06);
}
.loading-icon,
.error-icon {
  font-size: 64rpx;
  margin-bottom: 16rpx;
}
.loading-text {
  font-size: 28rpx;
  color: #86909c;
}
.error-text {
  font-size: 26rpx;
  color: #f56c6c;
  display: block;
  margin-bottom: 24rpx;
}
.retry-btn {
  display: inline-block;
  padding: 12rpx 40rpx;
  background: #1f6fff;
  color: #fff;
  font-size: 26rpx;
  border-radius: 32rpx;
  border: none;
}
.empty-state {
  margin: 24rpx;
  padding: 80rpx 0;
  background: #fff;
  border-radius: 28rpx;
  text-align: center;
  box-shadow: 0 12rpx 32rpx rgba(31, 49, 84, 0.06);
}
.empty-icon {
  font-size: 64rpx;
  margin-bottom: 16rpx;
}
.empty-text {
  font-size: 28rpx;
  color: #86909c;
  display: block;
}
.empty-hint {
  font-size: 24rpx;
  color: #c9cdd4;
  margin-top: 8rpx;
  display: block;
}

/* ===== 成员列表 ===== */
.member-section {
  margin: 0 24rpx;
  background: #fff;
  border-radius: 28rpx;
  overflow: hidden;
  box-shadow: 0 14rpx 36rpx rgba(31, 49, 84, 0.07);
}
.member-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 24rpx 28rpx 16rpx;
}
.member-title {
  font-size: 28rpx;
  font-weight: 600;
  color: #111827;
}
.member-count {
  font-size: 22rpx;
  color: #64748b;
  background: #f1f6ff;
  padding: 4rpx 14rpx;
  border-radius: 20rpx;
}
.member-list {
  padding: 0;
}
.member-item {
  display: flex;
  align-items: center;
  padding: 22rpx 28rpx;
  transition: background 0.15s;
  position: relative;
}
.member-item:active {
  background: #f8fbff;
}
.member-item::after {
  content: '';
  position: absolute;
  left: 28rpx;
  right: 0;
  bottom: 0;
  height: 1rpx;
  background: #f0f2f5;
}
.member-item:last-child::after {
  display: none;
}
.member-avatar {
  width: 82rpx;
  height: 82rpx;
  border-radius: 24rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-right: 20rpx;
  flex-shrink: 0;
}
.member-avatar.small {
  width: 64rpx;
  height: 64rpx;
  border-radius: 18rpx;
}
.avatar-text {
  color: #fff;
  font-size: 32rpx;
  font-weight: 600;
}
.avatar-text.sm {
  font-size: 26rpx;
}
.member-info {
  flex: 1;
  min-width: 0;
}
.member-name {
  font-size: 28rpx;
  font-weight: 500;
  color: #111827;
  margin-bottom: 6rpx;
  display: block;
}
.member-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 8rpx;
}
.member-tag {
  font-size: 20rpx;
  padding: 4rpx 12rpx;
  border-radius: 999rpx;
  display: inline-block;
}
.dept-tag {
  background: #eaf2ff;
  color: #1f6fff;
}
.pos-tag {
  background: #e8fbf7;
  color: #00a889;
}
.member-arrow {
  font-size: 32rpx;
  color: #c9cdd4;
  margin-left: 12rpx;
}

/* ===== 添加好友按钮 ===== */
.action-btn {
  padding: 10rpx 20rpx;
  border-radius: 12rpx;
  font-size: 22rpx;
  font-weight: 500;
  flex-shrink: 0;
}
.add-btn {
  background: #eaf2ff;
  color: #1f6fff;
}
.add-btn:active {
  background: #d6e5ff;
}
.pending-btn {
  background: #f0f2f5;
  color: #86909c;
}

/* ===== 分页 ===== */
.pagination {
  display: flex;
  justify-content: center;
  align-items: center;
  padding: 28rpx 0 36rpx;
  gap: 24rpx;
}
.page-btn {
  font-size: 24rpx;
  color: #1f6fff;
  padding: 8rpx 20rpx;
  background: #f0f2f5;
  border-radius: 8rpx;
}
.page-btn.disabled {
  color: #c9cdd4;
  background: transparent;
}
.page-info {
  font-size: 24rpx;
  color: #86909c;
}

/* ===== 好友申请 ===== */
.request-section {
  margin: 16rpx 24rpx;
  background: #fff;
  border-radius: 28rpx;
  box-shadow: 0 14rpx 36rpx rgba(31, 49, 84, 0.07);
  overflow: hidden;
}
.request-header {
  padding: 24rpx 28rpx 12rpx;
}
.request-title {
  font-size: 28rpx;
  font-weight: 600;
  color: #111827;
}
.request-empty {
  padding: 36rpx 28rpx;
  text-align: center;
  color: #86909c;
  font-size: 26rpx;
}
.request-list {
  padding: 0;
}
.request-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 20rpx 28rpx;
  border-top: 1rpx solid #f0f2f5;
}
.request-user {
  display: flex;
  align-items: center;
  flex: 1;
  min-width: 0;
}
.request-info {
  margin-left: 16rpx;
  min-width: 0;
}
.request-name {
  font-size: 26rpx;
  color: #1d2129;
  font-weight: 500;
  display: block;
}
.request-greeting {
  font-size: 22rpx;
  color: #86909c;
  margin-top: 4rpx;
  display: block;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.request-actions {
  display: flex;
  gap: 12rpx;
  flex-shrink: 0;
}
.req-btn {
  padding: 10rpx 24rpx;
  border-radius: 12rpx;
  font-size: 24rpx;
  border: none;
  font-weight: 500;
}
.req-btn.accept {
  background: #1f6fff;
  color: #fff;
}
.req-btn.accept:active {
  background: #1861e0;
}
.req-btn.reject {
  background: #f0f2f5;
  color: #64748b;
}
.req-btn.reject:active {
  background: #e5e8ee;
}
.pending-tag {
  font-size: 22rpx;
  color: #86909c;
  padding: 8rpx 16rpx;
  background: #f0f2f5;
  border-radius: 999rpx;
}

/* ===== 好友操作菜单 ===== */
.action-menu {
  background: #fff;
  border-radius: 28rpx;
  width: 80%;
  max-width: 500rpx;
  overflow: hidden;
  animation: slideUp 0.2s ease;
}
.action-item {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 32rpx;
  font-size: 28rpx;
  color: #1d2129;
  border-bottom: 1rpx solid #f0f2f5;
  gap: 8rpx;
}
.action-item:last-child {
  border-bottom: none;
}
.action-item.danger {
  color: #ef4444;
}
.action-icon {
  font-size: 30rpx;
}

/* ===== 添加好友弹窗 ===== */
.add-popup {
  background: #fff;
  border-radius: 28rpx;
  width: 80%;
  max-width: 520rpx;
  overflow: hidden;
  animation: slideUp 0.25s ease;
}
.add-head {
  padding: 36rpx 0 24rpx;
  text-align: center;
}
.add-head .member-avatar {
  margin: 0 auto 12rpx;
}
.add-name {
  font-size: 32rpx;
  font-weight: 600;
  color: #1d2129;
  display: block;
}
.add-dept {
  font-size: 24rpx;
  color: #86909c;
  margin-top: 4rpx;
  display: block;
}
.add-body {
  padding: 0 28rpx 20rpx;
}
.greeting-input {
  width: 100%;
  height: 120rpx;
  background: #f6f8fc;
  border-radius: 16rpx;
  padding: 16rpx;
  font-size: 26rpx;
  color: #1d2129;
  box-sizing: border-box;
  resize: none;
  border: 1rpx solid #edf1f7;
}
.add-footer {
  display: flex;
  gap: 16rpx;
  padding: 0 28rpx 32rpx;
}
.add-footer .req-btn {
  flex: 1;
  text-align: center;
}

/* ===== 员工卡片弹窗 ===== */
.card-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.45);
  z-index: 999;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 60rpx;
  animation: fadeIn 0.2s ease;
}
@keyframes fadeIn {
  from { opacity: 0; }
  to { opacity: 1; }
}
.card-popup {
  width: 100%;
  max-width: 560rpx;
  background: #fff;
  border-radius: 32rpx;
  overflow: hidden;
  animation: slideUp 0.25s ease;
}
@keyframes slideUp {
  from { transform: translateY(60rpx); opacity: 0; }
  to { transform: translateY(0); opacity: 1; }
}
.card-head {
  padding: 48rpx 0 36rpx;
  text-align: center;
}
.card-avatar {
  width: 100rpx;
  height: 100rpx;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.25);
  display: flex;
  align-items: center;
  justify-content: center;
  margin: 0 auto 12rpx;
}
.card-avatar-text {
  color: #fff;
  font-size: 40rpx;
  font-weight: 700;
}
.card-name {
  color: #fff;
  font-size: 36rpx;
  font-weight: 600;
  display: block;
}
.card-username {
  color: rgba(255, 255, 255, 0.8);
  font-size: 24rpx;
  display: block;
  margin-top: 4rpx;
}
.card-body {
  padding: 28rpx 36rpx 20rpx;
}
.card-row {
  display: flex;
  align-items: center;
  padding: 16rpx 0;
}
.card-label {
  font-size: 26rpx;
  color: #86909c;
  width: 80rpx;
  flex-shrink: 0;
}
.card-value {
  font-size: 28rpx;
  color: #1d2129;
  flex: 1;
}
.card-divider {
  height: 1rpx;
  background: #f0f2f5;
}
.card-footer {
  padding: 0 36rpx 36rpx;
  display: flex;
  gap: 16rpx;
}
.card-chat-btn {
  flex: 1;
  height: 80rpx;
  line-height: 80rpx;
  background: linear-gradient(135deg, #1f6fff, #18b7ff);
  color: #fff;
  font-size: 28rpx;
  border-radius: 40rpx;
  text-align: center;
  border: none;
  font-weight: 500;
  box-shadow: 0 8rpx 24rpx rgba(31, 111, 255, 0.2);
}
.card-chat-btn:active {
  opacity: 0.85;
}
.card-close-btn {
  flex: 1;
  height: 80rpx;
  line-height: 80rpx;
  background: #f6f8fc;
  color: #374151;
  font-size: 28rpx;
  border-radius: 40rpx;
  text-align: center;
  border: none;
  font-weight: 500;
}
.card-close-btn:active {
  background: #edf2fb;
}
</style>
