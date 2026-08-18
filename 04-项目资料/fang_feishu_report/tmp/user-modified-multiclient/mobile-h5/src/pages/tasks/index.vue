<template>
  <view class="task-container">
    <!-- 范围切换 -->
    <view class="scope-tabs">
      <text class="scope-tab" :class="{ active: scope === 'all' }" @tap="switchScope('all')">全部</text>
      <text class="scope-tab" :class="{ active: scope === 'assigned' }" @tap="switchScope('assigned')">分配给我的</text>
      <text class="scope-tab" :class="{ active: scope === 'created' }" @tap="switchScope('created')">我创建的</text>
    </view>

    <!-- 状态筛选 + 创建按钮 -->
    <view class="toolbar">
      <view class="status-filters">
        <text class="status-filter" :class="{ active: filterStatus === '' }" @tap="setFilter('')">全部</text>
        <text class="status-filter" :class="{ active: filterStatus === 'Todo' }" @tap="setFilter('Todo')">待办</text>
        <text class="status-filter" :class="{ active: filterStatus === 'InProgress' }" @tap="setFilter('InProgress')">进行中</text>
        <text class="status-filter" :class="{ active: filterStatus === 'Completed' }" @tap="setFilter('Completed')">已完成</text>
      </view>
      <button class="create-btn" @tap="openCreate">+ 新建</button>
    </view>

    <!-- 任务列表 -->
    <view v-if="tasks.length" class="task-list">
      <view v-for="item in tasks" :key="item.id" class="task-item" @tap="openDetail(item)">
        <view class="task-left">
          <text class="task-status-icon" :class="'icon-' + item.status" @tap.stop="toggleComplete(item)">{{ statusIcon(item.status) }}</text>
          <view class="task-info">
            <text class="task-title" :class="{ done: item.status === 'Completed' }">{{ item.title }}</text>
            <text class="task-meta">
              {{ item.assigneeName ? '负责人: ' + item.assigneeName : '未分配' }}
              <text v-if="item.dueAt"> | 截止: {{ formatDate(item.dueAt) }}</text>
            </text>
          </view>
        </view>
        <text class="task-status-tag" :class="'tag-' + item.status">{{ statusText(item.status) }}</text>
      </view>
    </view>
    <view v-else class="empty-state">
      <view class="empty-icon">✅</view>
      <text class="empty-text">暂无任务</text>
    </view>

    <!-- ===== 创建/编辑任务弹窗 ===== -->
    <view v-if="showCreateModal" class="modal-overlay" @tap="showCreateModal = false">
      <view class="modal-popup" @tap.stop>
        <text class="modal-title">{{ editingTask ? '编辑任务' : '新建任务' }}</text>
        <view class="form-group">
          <text class="form-label">任务标题 *</text>
          <input v-model="form.title" class="form-input" placeholder="输入任务标题" />
        </view>
        <view class="form-group">
          <text class="form-label">任务描述</text>
          <input v-model="form.description" class="form-input" placeholder="可选描述" />
        </view>
        <!-- 负责人选择 -->
        <view class="form-group">
          <text class="form-label">负责人</text>
          <view class="assignee-trigger" @tap="openUserPicker">
            <text v-if="form.assigneeName" class="assignee-selected">{{ form.assigneeName }}</text>
            <text v-else class="assignee-placeholder">选择负责人（可选）</text>
            <text v-if="form.assigneeId" class="assignee-clear" @tap.stop="clearAssignee">✕</text>
          </view>
          <!-- 用户搜索和选择 -->
          <view v-if="showUserPicker" class="user-picker">
            <input v-model="userKeyword" class="user-search-input" placeholder="搜索用户..." @input="searchUsers" />
            <scroll-view scroll-y class="user-list-scroll">
              <view
                v-for="u in userList"
                :key="u.id"
                class="user-item"
                :class="{ active: form.assigneeId === u.id }"
                @tap="selectUser(u)"
              >
                <text class="user-name">{{ u.realName || u.username }}</text>
                <text class="user-dept">{{ u.departmentName || '' }}</text>
              </view>
              <view v-if="!userList.length && userKeyword.length > 0" class="user-no-result">未找到用户</view>
            </scroll-view>
          </view>
        </view>
        <view class="form-group">
          <text class="form-label">截止时间</text>
          <picker mode="date" :value="form.dueDate" @change="onDueDateChange">
            <view class="form-input">{{ form.dueDate || '选择日期' }}</view>
          </picker>
        </view>
        <view class="modal-btns">
          <button class="modal-cancel" @tap="showCreateModal = false">取消</button>
          <button class="modal-confirm" :disabled="!form.title.trim()" @tap="submitTask">保存</button>
        </view>
      </view>
    </view>

    <!-- ===== 任务详情弹窗 ===== -->
    <view v-if="showDetailModal && detailItem" class="modal-overlay" @tap="showDetailModal = false">
      <view class="modal-popup detail-popup" @tap.stop>
        <text class="modal-title">{{ detailItem.title }}</text>
        <view class="detail-row"><text class="detail-label">描述</text><text class="detail-value">{{ detailItem.description || '-' }}</text></view>
        <view class="detail-row"><text class="detail-label">状态</text><text class="detail-value" :class="'tag-' + detailItem.status">{{ statusText(detailItem.status) }}</text></view>
        <view class="detail-row"><text class="detail-label">创建者</text><text class="detail-value">{{ detailItem.creatorName }}</text></view>
        <view class="detail-row"><text class="detail-label">负责人</text><text class="detail-value">{{ detailItem.assigneeName || '未分配' }}</text></view>
        <view class="detail-row"><text class="detail-label">截止时间</text><text class="detail-value">{{ detailItem.dueAt ? formatDateTime(detailItem.dueAt) : '无' }}</text></view>
        <view class="detail-row"><text class="detail-label">创建时间</text><text class="detail-value">{{ formatDateTime(detailItem.createdAt) }}</text></view>

        <!-- 操作按钮 -->
        <view class="detail-actions">
          <view class="modal-btns">
            <button v-if="detailItem.status !== 'Completed'" class="btn-primary" @tap="handleComplete">标记完成</button>
            <button v-if="detailItem.status === 'Completed'" class="btn-primary" @tap="handleReopen">重新打开</button>
            <button v-if="detailItem.status === 'Todo'" class="btn-primary" @tap="handleStart">开始执行</button>
          </view>
          <view class="modal-btns" style="margin-top: 10rpx;">
            <button class="btn-edit" @tap="handleEditFromDetail">✏️ 编辑</button>
            <button class="btn-danger" @tap="handleDelete">删除</button>
          </view>
        </view>
        <button class="modal-close-btn" @tap="showDetailModal = false">关闭</button>
      </view>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { getTasks, getTaskDetail, createTask, updateTask, completeTask, reopenTask, updateTaskStatus, deleteTask } from '@/api/tasks'
import { getFriends, discoverUsers } from '@/api/contacts'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()

const scope = ref<'all' | 'assigned' | 'created'>('all')
const filterStatus = ref('')
const tasks = ref<any[]>([])
const showCreateModal = ref(false)
const showDetailModal = ref(false)
const detailItem = ref<any>(null)
const editingTask = ref<any>(null)

const form = ref({
  title: '',
  description: '',
  dueDate: '',
  assigneeId: '',
  assigneeName: '',
})

// 用户选择器
const showUserPicker = ref(false)
const userKeyword = ref('')
const userList = ref<any[]>([])
const allFriends = ref<any[]>([]) // 缓存好友列表，避免每次搜索都请求

function openUserPicker() {
  showUserPicker.value = true
  userKeyword.value = ''
  // 显示好友作为初始列表，让用户可以直接选
  userList.value = allFriends.value
  // 首次打开时缓存好友列表
  if (allFriends.value.length === 0) {
    getFriends().then((res: any) => {
      allFriends.value = Array.isArray(res) ? res : []
      userList.value = allFriends.value
    }).catch(() => {})
  }
}

/** 实际执行搜索 */
async function doSearchUsers() {
  const keyword = userKeyword.value.trim()
  if (!keyword) {
    userList.value = allFriends.value
    return
  }
  const all: any[] = []
  const seen = new Set<string>()
  // 当前用户
  const me = authStore.userInfo
  if (me && (me.realName || me.username)?.includes(keyword)) {
    seen.add(me.id)
    all.push({ id: me.id, realName: me.realName, username: me.username, departmentName: me.departmentName })
  }
  // 从缓存的好友列表搜索
  for (const f of allFriends.value) {
    if (!seen.has(f.id) && (f.realName || f.username)?.includes(keyword)) {
      seen.add(f.id)
      all.push(f)
    }
  }
  try {
    const discovered: any = await discoverUsers(keyword)
    if (Array.isArray(discovered)) {
      for (const u of discovered) {
        if (!seen.has(u.id)) {
          seen.add(u.id)
          all.push(u)
        }
      }
    }
  } catch (e) { console.warn('[Tasks] discoverUsers failed', e) }
  userList.value = all
}

/** 防抖搜索 — 300ms 延迟 */
let searchTimer: ReturnType<typeof setTimeout> | null = null
function searchUsers() {
  if (searchTimer) clearTimeout(searchTimer)
  searchTimer = setTimeout(doSearchUsers, 300)
}

function selectUser(u: any) {
  form.value.assigneeId = u.id
  form.value.assigneeName = u.realName || u.username
  showUserPicker.value = false
  userKeyword.value = ''
}

function clearAssignee() {
  form.value.assigneeId = ''
  form.value.assigneeName = ''
}

function switchScope(s: 'all' | 'assigned' | 'created') {
  scope.value = s
  loadTasks()
}

function setFilter(s: string) {
  filterStatus.value = s
  loadTasks()
}

async function loadTasks() {
  try {
    const params: any = { scope: scope.value }
    if (filterStatus.value) params.status = filterStatus.value
    const res: any = await getTasks(params)
    tasks.value = Array.isArray(res) ? res : []
  } catch {
    tasks.value = []
  }
}

function statusText(status?: string): string {
  const map: Record<string, string> = { Todo: '待办', InProgress: '进行中', Completed: '已完成' }
  return map[status || ''] || status || '-'
}

function statusIcon(status?: string): string {
  const map: Record<string, string> = { Todo: '○', InProgress: '◐', Completed: '●' }
  return map[status || ''] || '○'
}

function formatDate(t?: string): string {
  if (!t) return ''
  const d = new Date(t)
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}

function formatDateTime(t?: string): string {
  if (!t) return '-'
  const d = new Date(t)
  return `${formatDate(t)} ${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`
}

function openCreate() {
  editingTask.value = null
  form.value = { title: '', description: '', dueDate: '', assigneeId: '', assigneeName: '' }
  showUserPicker.value = false
  showCreateModal.value = true
}

async function submitTask() {
  if (!form.value.title.trim()) return
  try {
    const data: any = { title: form.value.title.trim() }
    if (form.value.description.trim()) data.description = form.value.description.trim()
    if (form.value.dueDate) data.dueAt = new Date(form.value.dueDate).toISOString()
    if (form.value.assigneeId) data.assigneeId = form.value.assigneeId

    if (editingTask.value) {
      await updateTask(editingTask.value.id, data)
      uni.showToast({ title: '更新成功', icon: 'success' })
    } else {
      await createTask(data)
      uni.showToast({ title: '创建成功', icon: 'success' })
    }
    showCreateModal.value = false
    loadTasks()
  } catch {
    uni.showToast({ title: '操作失败', icon: 'none' })
  }
}

async function openDetail(item: any) {
  try {
    const res: any = await getTaskDetail(item.id)
    detailItem.value = res || item
  } catch {
    detailItem.value = item
  }
  showDetailModal.value = true
}

async function toggleComplete(item: any) {
  try {
    if (item.status === 'Completed') {
      await reopenTask(item.id)
    } else {
      await completeTask(item.id)
    }
    loadTasks()
  } catch {
    uni.showToast({ title: '操作失败', icon: 'none' })
  }
}

async function handleComplete() {
  if (!detailItem.value) return
  try {
    await completeTask(detailItem.value.id)
    uni.showToast({ title: '已标记完成', icon: 'success' })
    showDetailModal.value = false
    loadTasks()
  } catch {
    uni.showToast({ title: '操作失败', icon: 'none' })
  }
}

async function handleReopen() {
  if (!detailItem.value) return
  try {
    await reopenTask(detailItem.value.id)
    uni.showToast({ title: '已重新打开', icon: 'success' })
    showDetailModal.value = false
    loadTasks()
  } catch {
    uni.showToast({ title: '操作失败', icon: 'none' })
  }
}

async function handleStart() {
  if (!detailItem.value) return
  try {
    await updateTaskStatus(detailItem.value.id, 'InProgress')
    uni.showToast({ title: '已开始执行', icon: 'success' })
    showDetailModal.value = false
    loadTasks()
  } catch {
    uni.showToast({ title: '操作失败', icon: 'none' })
  }
}

function handleEditFromDetail() {
  const item = detailItem.value
  if (!item) return
  editingTask.value = item
  form.value = {
    title: item.title || '',
    description: item.description || '',
    dueDate: item.dueAt ? formatDate(item.dueAt) : '',
    assigneeId: item.assigneeId || '',
    assigneeName: item.assigneeName || '',
  }
  showDetailModal.value = false
  showUserPicker.value = false
  showCreateModal.value = true
}

async function handleDelete() {
  if (!detailItem.value) return
  uni.showModal({
    title: '确认删除',
    content: '确定删除此任务吗？',
    success: async (res) => {
      if (res.confirm) {
        try {
          await deleteTask(detailItem.value.id)
          uni.showToast({ title: '已删除', icon: 'success' })
          showDetailModal.value = false
          loadTasks()
        } catch {
          uni.showToast({ title: '删除失败', icon: 'none' })
        }
      }
    },
  })
}

function onDueDateChange(e: any) {
  form.value.dueDate = e.detail.value
}

onShow(() => loadTasks())
</script>

<style scoped>
.task-container { min-height: 100vh; background: #f6f8fc; }

.scope-tabs {
  display: flex; background: #fff; padding: 18rpx 24rpx; gap: 14rpx;
  border-bottom: 1rpx solid #edf1f7;
}
.scope-tab {
  font-size: 26rpx; color: #64748b; padding: 10rpx 22rpx;
  background: #f6f8fc; border-radius: 999rpx;
}
.scope-tab.active { color: #1f6fff; font-weight: 600; background: #eef4ff; }

.toolbar {
  display: flex; align-items: center; padding: 16rpx 24rpx; gap: 12rpx;
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

.task-list { margin: 0 24rpx; background: #fff; border-radius: 28rpx; overflow: hidden; box-shadow: 0 14rpx 36rpx rgba(31,49,84,0.07); }
.task-item {
  display: flex; align-items: center; padding: 22rpx 18rpx;
  border-bottom: 1rpx solid #f0f2f5;
}
.task-item:last-child { border-bottom: none; }
.task-item:active { background: #f8fbff; }
.task-left { flex: 1; display: flex; align-items: center; gap: 14rpx; min-width: 0; }
.task-status-icon { font-size: 32rpx; width: 44rpx; text-align: center; flex-shrink: 0; }
.icon-Todo { color: #a8b0c2; }
.icon-InProgress { color: #d97706; }
.icon-Completed { color: #00a889; }
.task-info { flex: 1; min-width: 0; }
.task-title { font-size: 26rpx; color: #111827; font-weight: 500; display: block; }
.task-title.done { color: #a8b0c2; text-decoration: line-through; }
.task-meta { font-size: 20rpx; color: #7b8494; display: block; margin-top: 4rpx; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.task-status-tag { font-size: 20rpx; padding: 4rpx 14rpx; border-radius: 999rpx; flex-shrink: 0; }
.tag-Todo { background: #fef3c7; color: #d97706; }
.tag-InProgress { background: #dbeafe; color: #2563eb; }
.tag-Completed { background: #d1fae5; color: #00a889; }

.empty-state { margin: 24rpx; text-align: center; padding: 120rpx 0; background: #fff; border-radius: 28rpx; }
.empty-icon { font-size: 72rpx; }
.empty-text { font-size: 28rpx; color: #64748b; display: block; margin-top: 16rpx; }

.modal-overlay {
  position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0,0,0,0.45);
  z-index: 999; display: flex; align-items: center; justify-content: center; padding: 60rpx;
}
.modal-popup {
  width: 100%; max-width: 560rpx; background: #fff; border-radius: 28rpx;
  padding: 32rpx; max-height: 80vh; overflow-y: auto;
}
.detail-popup { max-width: 600rpx; }
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
.modal-confirm[disabled] { opacity: 0.4; }
.modal-close-btn { width: 100%; background: #f6f8fc; color: #374151; margin-top: 16rpx; }

.detail-row { display: flex; padding: 14rpx 0; border-bottom: 1rpx solid #f0f2f5; }
.detail-label { font-size: 24rpx; color: #7b8494; width: 100rpx; flex-shrink: 0; }
.detail-value { font-size: 26rpx; color: #111827; flex: 1; }

.detail-actions { margin-top: 20rpx; padding-top: 16rpx; border-top: 1rpx solid #f0f2f5; }
.btn-primary { flex: 1; height: 72rpx; line-height: 72rpx; background: #1f6fff; color: #fff; border-radius: 36rpx; border: none; font-size: 26rpx; }
.btn-edit { flex: 1; height: 72rpx; line-height: 72rpx; background: #f59e0b; color: #fff; border-radius: 36rpx; border: none; font-size: 26rpx; }
.btn-danger { flex: 1; height: 72rpx; line-height: 72rpx; background: #ef4444; color: #fff; border-radius: 36rpx; border: none; font-size: 26rpx; }

/* 负责人选择器样式 */
.assignee-trigger {
  display: flex; align-items: center; height: 64rpx;
  background: #f6f8fc; border-radius: 16rpx; padding: 0 20rpx;
}
.assignee-placeholder { font-size: 26rpx; color: #a8b0c2; flex: 1; }
.assignee-selected { font-size: 26rpx; color: #1f6fff; font-weight: 500; flex: 1; }
.assignee-clear { font-size: 28rpx; color: #ef4444; padding: 8rpx; }
.user-picker {
  margin-top: 8rpx; border: 1rpx solid #edf1f7; border-radius: 16rpx;
  background: #fff; overflow: hidden;
}
.user-search-input {
  height: 60rpx; padding: 0 16rpx; font-size: 24rpx;
  border-bottom: 1rpx solid #f0f2f5;
}
.user-list-scroll { max-height: 280rpx; }
.user-item {
  display: flex; align-items: center; padding: 16rpx 20rpx;
  border-bottom: 1rpx solid #f0f2f5;
}
.user-item:active { background: #eef4ff; }
.user-item.active { background: #eef4ff; }
.user-name { font-size: 26rpx; color: #111827; flex: 1; }
.user-dept { font-size: 22rpx; color: #7b8494; }
.user-no-result { text-align: center; padding: 24rpx; font-size: 24rpx; color: #a8b0c2; }
</style>
