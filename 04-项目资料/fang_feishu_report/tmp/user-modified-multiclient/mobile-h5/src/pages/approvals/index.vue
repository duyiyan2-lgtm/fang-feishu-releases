<template>
  <view class="approval-container">
    <!-- 切换视角 -->
    <view class="view-tabs">
      <text
        class="view-tab"
        :class="{ active: viewMode === 'user' }"
        @tap="switchView('user')"
      >我的申请</text>
      <text
        v-if="isAdmin"
        class="view-tab"
        :class="{ active: viewMode === 'admin' }"
        @tap="switchView('admin')"
      >审批管理</text>
    </view>

    <!-- ===== 用户视角：我的申请 ===== -->
    <template v-if="viewMode === 'user'">
      <view class="toolbar">
        <text class="toolbar-title">我的申请</text>
        <button class="create-btn" @tap="openCreate">+ 提交申请</button>
      </view>

      <view v-if="approvals.length" class="approval-list">
        <view class="list-header">
          <text class="h-title">标题</text>
          <text class="h-type">类型</text>
          <text class="h-time">时间范围</text>
          <text class="h-status">状态</text>
          <text class="h-actions">操作</text>
        </view>
        <view v-for="item in pagedApprovals" :key="item.id" class="list-item">
          <text class="h-title">{{ item.title || item.type || '申请' }}</text>
          <text class="h-type">{{ item.type || '-' }}</text>
          <text class="h-time">{{ getApprovalTime(item.content) }}</text>
          <text class="h-status" :class="'status-' + item.status">{{ statusText(item.status) }}</text>
          <text class="h-actions detail-link" @tap="openDetail(item)">查看</text>
        </view>
      </view>
      <view v-else class="empty-state">
        <view class="empty-icon">📋</view>
        <text class="empty-text">暂无申请记录</text>
      </view>
    </template>

    <!-- ===== 管理员视角：审批管理 ===== -->
    <template v-if="viewMode === 'admin'">
      <view class="filter-tabs">
        <text class="filter-tab" :class="{ active: filterStatus === '' }" @tap="setFilter('')">全部</text>
        <text class="filter-tab" :class="{ active: filterStatus === 'Pending' }" @tap="setFilter('Pending')">待处理</text>
        <text class="filter-tab" :class="{ active: filterStatus === 'Approved' }" @tap="setFilter('Approved')">已通过</text>
        <text class="filter-tab" :class="{ active: filterStatus === 'Rejected' }" @tap="setFilter('Rejected')">已驳回</text>
      </view>
      <view class="toolbar">
        <input v-model="searchText" class="search-input" placeholder="搜索申请人..." @confirm="handleSearch" />
        <button class="search-btn" @tap="handleSearch">查询</button>
      </view>

      <view v-if="approvals.length" class="approval-list">
        <view class="list-header">
          <text class="h-title">申请人</text>
          <text class="h-title">标题</text>
          <text class="h-type">类型</text>
          <text class="h-time">提交时间</text>
          <text class="h-status">状态</text>
          <text class="h-actions">操作</text>
        </view>
        <view v-for="item in pagedApprovals" :key="item.id" class="list-item">
          <text class="h-title">{{ item.applicantName || '未知' }}</text>
          <text class="h-title">{{ item.title || item.type || '申请' }}</text>
          <text class="h-type">{{ item.type || '-' }}</text>
          <text class="h-time">{{ formatDateTime(item.createdAt) }}</text>
          <text class="h-status" :class="'status-' + item.status">{{ statusText(item.status) }}</text>
          <text class="h-actions detail-link" @tap="openDetail(item)">
            {{ item.status === 'Pending' ? '审批' : '查看' }}
          </text>
        </view>
      </view>
      <view v-else class="empty-state">
        <view class="empty-icon">📋</view>
        <text class="empty-text">暂无审批</text>
      </view>
    </template>

    <!-- 分页 -->
    <view v-if="totalPages > 1" class="pagination">
      <text class="page-btn" :class="{ disabled: page <= 1 }" @tap="changePage(page - 1)">上一页</text>
      <text class="page-info">{{ page }} / {{ totalPages }}</text>
      <text class="page-btn" :class="{ disabled: page >= totalPages }" @tap="changePage(page + 1)">下一页</text>
    </view>

    <!-- 提交申请弹窗（支持请假/报销/加班） -->
    <view v-if="showCreateModal" class="modal-overlay" @tap="showCreateModal = false">
      <view class="modal-popup" @tap.stop>
        <text class="modal-title">提交申请</text>
        <view class="form-group">
          <text class="form-label">选择模板（可选）</text>
          <view class="template-row" @tap="loadTemplates">
            <text class="template-trigger">{{ selectedTemplate ? selectedTemplate.name : '点击选择模板快速填写' }}</text>
          </view>
          <view v-if="showTemplatePicker" class="template-list">
            <view v-for="t in templates" :key="t.id" class="template-item" @tap="applyTemplate(t)">
              <text class="template-name">{{ t.name }}</text>
              <text class="template-desc">{{ t.description || '' }}</text>
            </view>
            <view v-if="!templates.length" class="template-empty">暂无模板</view>
          </view>
        </view>
        <view class="form-group">
          <text class="form-label">申请类型 *</text>
          <picker mode="selector" :range="approvalTypes" @change="onTypeChange">
            <view class="form-input">{{ form.type || '请选择' }}</view>
          </picker>
        </view>
        <!-- ===== 请假字段 ===== -->
        <template v-if="form.type === '年假' || form.type === '事假' || form.type === '病假'">
          <view class="form-group">
            <text class="form-label">开始时间 *</text>
            <picker mode="date" :value="form.startDate" @change="onStartDateChange">
              <view class="form-input">{{ form.startDate || '选择日期' }}</view>
            </picker>
            <picker mode="time" :value="form.startTime" @change="onStartTimeChange">
              <view class="form-input">{{ form.startTime || '09:00' }}</view>
            </picker>
          </view>
          <view class="form-group">
            <text class="form-label">结束时间 *</text>
            <picker mode="date" :value="form.endDate" @change="onEndDateChange">
              <view class="form-input">{{ form.endDate || '选择日期' }}</view>
            </picker>
            <picker mode="time" :value="form.endTime" @change="onEndTimeChange">
              <view class="form-input">{{ form.endTime || '18:00' }}</view>
            </picker>
          </view>
          <view class="form-group">
            <text class="form-label">请假原因 *</text>
            <input v-model="form.content" class="form-input" placeholder="请输入原因" />
          </view>
        </template>
        <!-- ===== 报销字段 ===== -->
        <template v-if="form.type === '报销'">
          <view class="form-group">
            <text class="form-label">报销金额（元）*</text>
            <input v-model="form.amount" class="form-input" placeholder="请输入金额" type="digit" />
          </view>
          <view class="form-group">
            <text class="form-label">费用说明 *</text>
            <input v-model="form.content" class="form-input" placeholder="请说明费用用途" />
          </view>
        </template>
        <!-- ===== 加班字段 ===== -->
        <template v-if="form.type === '加班'">
          <view class="form-group">
            <text class="form-label">加班日期 *</text>
            <picker mode="date" :value="form.startDate" @change="onStartDateChange">
              <view class="form-input">{{ form.startDate || '选择日期' }}</view>
            </picker>
          </view>
          <view class="form-group">
            <text class="form-label">开始时间 *</text>
            <picker mode="time" :value="form.startTime || '18:00'" @change="onStartTimeChange">
              <view class="form-input">{{ form.startTime || '18:00' }}</view>
            </picker>
          </view>
          <view class="form-group">
            <text class="form-label">结束时间 *</text>
            <picker mode="time" :value="form.endTime || '20:00'" @change="onEndTimeChange">
              <view class="form-input">{{ form.endTime || '20:00' }}</view>
            </picker>
          </view>
          <view class="form-group">
            <text class="form-label">加班原因 *</text>
            <input v-model="form.content" class="form-input" placeholder="请输入加班原因" />
          </view>
        </template>
        <view class="modal-btns">
          <button class="modal-cancel" @tap="showCreateModal = false">取消</button>
          <button class="modal-confirm" :disabled="!canSubmit" @tap="submitApproval">提交</button>
        </view>
      </view>
    </view>

    <!-- 审批详情抽屉 -->
    <view v-if="showDetailModal" class="modal-overlay" @tap="showDetailModal = false">
      <view class="modal-popup detail-popup" @tap.stop>
        <text class="modal-title">审批详情</text>
        <view class="detail-row"><text class="detail-label">标题</text><text class="detail-value">{{ detailItem?.title || '-' }}</text></view>
        <view class="detail-row"><text class="detail-label">申请人</text><text class="detail-value">{{ detailItem?.applicantName || '我' }}</text></view>
        <view class="detail-row"><text class="detail-label">类型</text><text class="detail-value">{{ detailItem?.type || '-' }}</text></view>
        <view class="detail-row">
          <text class="detail-label">{{ detailItem?.type === '报销' ? '金额' : detailItem?.type === '加班' ? '加班时间' : '时间' }}</text>
          <text class="detail-value">{{ getApprovalTime(detailItem?.content) }}</text>
        </view>
        <view class="detail-row"><text class="detail-label">{{ detailItem?.type === '报销' ? '说明' : '原因' }}</text><text class="detail-value">{{ getApprovalReason(detailItem?.content) }}</text></view>
        <view class="detail-row"><text class="detail-label">状态</text><text class="detail-value" :class="'status-' + detailItem?.status">{{ statusText(detailItem?.status) }}</text></view>

        <!-- 管理员审批操作 -->
        <view v-if="detailItem?.status === 'Pending' && viewMode === 'admin'" class="approval-ops">
          <view class="form-group">
            <text class="form-label">审批意见</text>
            <input v-model="approvalComment" class="form-input" placeholder="可选" />
          </view>
          <view class="modal-btns">
            <button class="btn-reject" @tap="handleReject">驳回</button>
            <button class="btn-approve" @tap="handleApprove">通过</button>
          </view>
        </view>

        <!-- 申请人操作：撤回 + 提醒（仅待审批状态） -->
        <view v-if="detailItem?.status === 'Pending' && viewMode === 'user'" class="approval-ops">
          <view class="modal-btns">
            <button class="btn-reject" @tap="handleWithdraw">撤回申请</button>
            <button class="btn-remind" @tap="handleRemind">提醒审批人</button>
          </view>
        </view>

        <button class="modal-close-btn" @tap="showDetailModal = false">关闭</button>
      </view>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { getApprovals, createApproval, approveApproval, rejectApproval, withdrawApproval, remindApproval, getTemplates } from '@/api/approvals'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()
const isAdmin = computed(() => authStore.isAdmin)

const viewMode = ref<'user' | 'admin'>('user')
const approvals = ref<any[]>([])
/** 当前页审批（客户端分页切片） */
const pagedApprovals = computed(() => {
  const start = (page.value - 1) * pageSize
  return approvals.value.slice(start, start + pageSize)
})
const page = ref(1)
const totalPages = ref(1)
const pageSize = 20
const filterStatus = ref('')
const searchText = ref('')

// 申请表单（支持请假/报销/加班）
const showCreateModal = ref(false)
const approvalTypes = ['年假', '事假', '病假', '报销', '加班']
const form = ref({
  type: '',
  startDate: '',
  startTime: '09:00',
  endDate: '',
  endTime: '18:00',
  content: '',
  amount: '',      // 报销金额
})

// 审批模板
const showTemplatePicker = ref(false)
const templates = ref<any[]>([])
const selectedTemplate = ref<any>(null)

async function loadTemplates() {
  if (templates.value.length > 0) {
    showTemplatePicker.value = !showTemplatePicker.value
    return
  }
  try {
    const res: any = await getTemplates()
    templates.value = Array.isArray(res) ? res : []
    showTemplatePicker.value = true
  } catch {
    uni.showToast({ title: '加载模板失败', icon: 'none' })
  }
}

function applyTemplate(t: any) {
  selectedTemplate.value = t
  showTemplatePicker.value = false
  form.value.type = t.type || form.value.type
  if (t.defaultContent) form.value.content = t.defaultContent
  uni.showToast({ title: `已应用模板「${t.name}」`, icon: 'success' })
}

// 切换申请类型时自动调整默认值
function onTypeChange(e: any) {
  const type = approvalTypes[e.detail.value]
  form.value.type = type
  if (type === '加班') {
    form.value.startTime = '18:00'
    form.value.endTime = '20:00'
  } else if (type === '报销') {
    form.value.amount = ''
  } else {
    form.value.startTime = '09:00'
    form.value.endTime = '18:00'
  }
}

// 详情
const showDetailModal = ref(false)
const detailItem = ref<any>(null)
const approvalComment = ref('')

const canSubmit = computed(() => {
  if (!form.value.type) return false
  const t = form.value.type
  if (t === '报销') {
    return form.value.amount && parseFloat(form.value.amount) > 0 && form.value.content.trim()
  }
  if (t === '加班') {
    return form.value.startDate && form.value.startTime && form.value.endTime && form.value.content.trim()
  }
  // 请假类
  return form.value.startDate && form.value.endDate && form.value.content.trim()
})

function switchView(mode: 'user' | 'admin') {
  viewMode.value = mode
  page.value = 1
  filterStatus.value = ''
  searchText.value = ''
  loadApprovals()
}

function setFilter(status: string) {
  filterStatus.value = status
  page.value = 1
  loadApprovals()
}

function handleSearch() {
  page.value = 1
  loadApprovals()
}

async function loadApprovals() {
  try {
    const params: any = { page: page.value, pageSize }
    if (filterStatus.value) params.status = filterStatus.value
    if (searchText.value.trim()) params.search = searchText.value.trim()
    const res: any = await getApprovals(params)
    let list = Array.isArray(res) ? res : res?.items || res?.list || []
    // "我的申请"视图只显示当前用户的申请
    if (viewMode.value === 'user') {
      const myId = authStore.userInfo?.id || ''
      list = list.filter((item: any) => item.applicantId === myId)
    }
    approvals.value = list
    totalPages.value = Math.ceil(list.length / pageSize) || 1
  } catch {
    approvals.value = []
  }
}

function changePage(p: number) {
  if (p < 1 || p > totalPages.value) return
  page.value = p
  loadApprovals()
}

function openCreate() {
  form.value = { type: '', startDate: '', startTime: '09:00', endDate: '', endTime: '18:00', content: '', amount: '' }
  selectedTemplate.value = null
  showTemplatePicker.value = false
  showCreateModal.value = true
}

async function submitApproval() {
  if (!canSubmit.value) return
  try {
    let content = form.value.content
    let title = ''
    const type = form.value.type
    if (type === '报销') {
      title = '报销申请'
      content = `💰${form.value.amount}💰${content}`
    } else if (type === '加班') {
      title = '加班申请'
      const start = `${form.value.startDate} ${form.value.startTime}`
      const end = `${form.value.endTime}`
      content = `🌙${start}→${end}🌙${content}`
    } else {
      title = '请假申请'
      const start = `${form.value.startDate} ${form.value.startTime}`
      const end = `${form.value.endDate} ${form.value.endTime}`
      content = `⏰${start}→${end}⏰${content}`
    }
    await createApproval({
      type,
      title,
      content,
    })
    uni.showToast({ title: '提交成功', icon: 'success' })
    showCreateModal.value = false
    loadApprovals()
  } catch {
    uni.showToast({ title: '提交失败', icon: 'none' })
  }
}

function openDetail(item: any) {
  detailItem.value = item
  approvalComment.value = ''
  showDetailModal.value = true
}

async function handleWithdraw() {
  if (!detailItem.value?.id) return
  try {
    await withdrawApproval(detailItem.value.id)
    uni.showToast({ title: '已撤回', icon: 'success' })
    showDetailModal.value = false
    loadApprovals()
  } catch {
    uni.showToast({ title: '撤回失败', icon: 'none' })
  }
}

async function handleRemind() {
  if (!detailItem.value?.id) return
  try {
    await remindApproval(detailItem.value.id)
    uni.showToast({ title: '已提醒审批人', icon: 'success' })
  } catch {
    uni.showToast({ title: '提醒失败', icon: 'none' })
  }
}

async function handleApprove() {
  if (!detailItem.value) return
  try {
    await approveApproval(detailItem.value.id, { comment: approvalComment.value })
    uni.showToast({ title: '已通过', icon: 'success' })
    showDetailModal.value = false
    loadApprovals()
  } catch {
    uni.showToast({ title: '操作失败', icon: 'none' })
  }
}

async function handleReject() {
  if (!detailItem.value) return
  try {
    await rejectApproval(detailItem.value.id, { comment: approvalComment.value })
    uni.showToast({ title: '已驳回', icon: 'success' })
    showDetailModal.value = false
    loadApprovals()
  } catch {
    uni.showToast({ title: '操作失败', icon: 'none' })
  }
}

function statusText(status?: string): string {
  const map: Record<string, string> = { Pending: '待审批', Approved: '已通过', Rejected: '已驳回' }
  return map[status || ''] || status || '-'
}

/** 从 content 中提取时间/金额摘要 */
function getApprovalTime(content?: string): string {
  if (!content) return '-'
  // 报销：💰金额💰说明
  const expenseM = content.match(/💰(.+?)💰/)
  if (expenseM) return `¥${parseFloat(expenseM[1]).toFixed(2)}`
  // 加班：🌙时间→时间🌙原因
  const overtimeM = content.match(/🌙(.+?)🌙/)
  if (overtimeM) return overtimeM[1].replace('→', ' 至 ')
  // 请假：⏰时间→时间⏰原因（兼容新旧格式）
  const leaveM = content.match(/⏰(.+?)⏰/)
  if (leaveM) return leaveM[1].replace('→', ' 至 ')
  const oldM = content.match(/【(.+?)】/)
  if (oldM) return oldM[1]
  return '-'
}

/** 从 content 中提取原因（去掉前缀标记） */
function getApprovalReason(content?: string): string {
  if (!content) return '-'
  // 报销：💰金额💰说明
  const expenseM = content.match(/💰.+?💰(.+)/)
  if (expenseM) return expenseM[1].trim() || '-'
  // 加班：🌙时间🌙原因
  const overtimeM = content.match(/🌙.+?🌙(.+)/)
  if (overtimeM) return overtimeM[1].trim() || '-'
  // 请假：⏰时间⏰原因（兼容新旧格式）
  const cleaned = content.replace(/⏰.+?⏰/, '').replace(/【.+?】/, '')
  return cleaned.trim() || '-'
}

function formatDateTime(t: string) {
  if (!t) return '-'
  const d = new Date(t)
  return `${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')} ${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`
}

// Picker callbacks（onTypeChange 在上方已定义，支持请假/报销/加班）
function onStartDateChange(e: any) { form.value.startDate = e.detail.value }
function onStartTimeChange(e: any) { form.value.startTime = e.detail.value }
function onEndDateChange(e: any) { form.value.endDate = e.detail.value }
function onEndTimeChange(e: any) { form.value.endTime = e.detail.value }

onShow(() => loadApprovals())
</script>

<style scoped>
.approval-container {
  min-height: 100vh;
  background: #f6f8fc;
}

/* 视角切换 */
.view-tabs {
  display: flex;
  background: #fff;
  padding: 18rpx 24rpx;
  gap: 18rpx;
  border-bottom: 1rpx solid #edf1f7;
  box-shadow: 0 8rpx 24rpx rgba(31, 49, 84, 0.04);
}
.view-tab {
  font-size: 28rpx;
  color: #7b8494;
  padding: 12rpx 28rpx;
  position: relative;
  background: #f6f8fc;
  border-radius: 999rpx;
}
.view-tab.active {
  color: #ffffff;
  font-weight: 600;
  background: #1f6fff;
}
.view-tab.active::after {
  display: none;
}

.toolbar {
  display: flex;
  align-items: center;
  padding: 20rpx 24rpx;
  gap: 16rpx;
}
.toolbar-title { font-size: 30rpx; font-weight: 700; color: #111827; flex: 1; }
.create-btn {
  height: 72rpx;
  line-height: 72rpx;
  padding: 0 24rpx;
  background: linear-gradient(135deg, #1f6fff, #18b7ff);
  color: #fff;
  font-size: 24rpx;
  border-radius: 999rpx;
  border: none;
  flex-shrink: 0;
  font-weight: 700;
  box-shadow: 0 12rpx 28rpx rgba(31, 111, 255, 0.2);
}

/* 筛选（管理员） */
.filter-tabs {
  display: flex;
  background: #fff;
  padding: 18rpx 24rpx;
  gap: 14rpx;
}
.filter-tab {
  font-size: 26rpx;
  color: #64748b;
  padding: 10rpx 22rpx;
  position: relative;
  background: #f6f8fc;
  border-radius: 999rpx;
}
.filter-tab.active {
  color: #1f6fff;
  font-weight: 600;
  background: #eef4ff;
}
.filter-tab.active::after {
  display: none;
}

.search-input {
  flex: 1;
  height: 68rpx;
  background: #fff;
  border-radius: 22rpx;
  padding: 0 24rpx;
  font-size: 24rpx;
  border: 1rpx solid #edf1f7;
  box-shadow: 0 8rpx 22rpx rgba(31, 49, 84, 0.05);
}
.search-btn {
  height: 68rpx;
  line-height: 68rpx;
  padding: 0 20rpx;
  background: #1f6fff;
  color: #fff;
  font-size: 24rpx;
  border-radius: 30rpx;
  border: none;
  flex-shrink: 0;
}

/* 列表 */
.approval-list {
  margin: 0 24rpx;
  background: #fff;
  border-radius: 28rpx;
  overflow: hidden;
  box-shadow: 0 14rpx 36rpx rgba(31, 49, 84, 0.07);
}
.list-header, .list-item {
  display: flex;
  align-items: center;
  padding: 22rpx 18rpx;
  font-size: 22rpx;
}
.list-header {
  background: #f8fbff;
  font-weight: 500;
  color: #64748b;
  border-bottom: 1rpx solid #f0f2f5;
}
.list-item {
  border-bottom: 1rpx solid #f0f2f5;
}
.list-item:last-child { border-bottom: none; }
.list-item:active { background: #f8fbff; }
.h-title { flex: 1.5; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.h-type { flex: 1; text-align: center; }
.h-time { flex: 1.5; text-align: center; font-size: 20rpx; }
.h-status { flex: 1; text-align: center; }
.h-actions { flex: 0.8; text-align: center; }
.detail-link { color: #1f6fff; font-weight: 600; }
.status-Pending { color: #d97706; font-weight: 600; }
.status-Approved { color: #00a889; font-weight: 600; }
.status-Rejected { color: #ef4444; font-weight: 600; }

.empty-state {
  margin: 24rpx;
  text-align: center;
  padding: 120rpx 0;
  background: #fff;
  border-radius: 28rpx;
  box-shadow: 0 14rpx 36rpx rgba(31, 49, 84, 0.06);
}
.empty-icon { font-size: 72rpx; }
.empty-text { font-size: 28rpx; color: #64748b; display: block; margin-top: 16rpx; }

.pagination {
  display: flex;
  justify-content: center;
  align-items: center;
  padding: 32rpx 0;
  gap: 24rpx;
}
.page-btn { font-size: 26rpx; color: #1f6fff; padding: 8rpx 20rpx; background: #fff; border-radius: 12rpx; }
.page-btn.disabled { color: #a8b0c2; }
.page-info { font-size: 26rpx; color: #7b8494; }

/* 弹窗 */
.modal-overlay {
  position: fixed;
  top: 0; left: 0; right: 0; bottom: 0;
  background: rgba(0,0,0,0.45);
  z-index: 999;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 60rpx;
}
.modal-popup {
  width: 100%;
  max-width: 560rpx;
  background: #fff;
  border-radius: 28rpx;
  padding: 32rpx;
  max-height: 80vh;
  overflow-y: auto;
}
.detail-popup { max-width: 600rpx; }
.modal-title { font-size: 32rpx; font-weight: 600; display: block; text-align: center; margin-bottom: 24rpx; }
.form-group { margin-bottom: 20rpx; }
.form-label { font-size: 24rpx; color: #7b8494; display: block; margin-bottom: 6rpx; }
.form-input {
  height: 64rpx;
  background: #f6f8fc;
  border-radius: 16rpx;
  padding: 0 20rpx;
  font-size: 26rpx;
  color: #111827;
  display: flex;
  align-items: center;
}
.modal-btns { display: flex; gap: 20rpx; margin-top: 20rpx; }
.modal-cancel, .modal-confirm, .modal-close-btn {
  flex: 1;
  height: 72rpx;
  line-height: 72rpx;
  font-size: 26rpx;
  border-radius: 36rpx;
  border: none;
  text-align: center;
}
.modal-cancel { background: #f6f8fc; color: #374151; }
.modal-confirm { background: #1f6fff; color: #fff; }
.modal-confirm[disabled] { opacity: 0.4; }
.modal-close-btn { width: 100%; background: #f6f8fc; color: #374151; margin-top: 16rpx; }
.btn-approve { flex: 1; height: 72rpx; line-height: 72rpx; background: #00a889; color: #fff; border-radius: 36rpx; border: none; font-size: 26rpx; }
.btn-reject { flex: 1; height: 72rpx; line-height: 72rpx; background: #ef4444; color: #fff; border-radius: 36rpx; border: none; font-size: 26rpx; }
.btn-remind { flex: 1; height: 72rpx; line-height: 72rpx; background: #1f6fff; color: #fff; border-radius: 36rpx; border: none; font-size: 26rpx; }

/* 详情 */
.detail-row {
  display: flex;
  padding: 14rpx 0;
  border-bottom: 1rpx solid #f0f2f5;
}
.detail-label {
  font-size: 24rpx;
  color: #7b8494;
  width: 100rpx;
  flex-shrink: 0;
}
.detail-value {
  font-size: 26rpx;
  color: #111827;
  flex: 1;
}
.approval-ops {
  margin-top: 20rpx;
  padding-top: 20rpx;
  border-top: 1rpx solid #f0f2f5;
}

/* 模板选择 */
.template-row { margin-bottom: 8rpx; }
.template-trigger { font-size: 24rpx; color: #1f6fff; padding: 8rpx 0; display: inline-block; }
.template-list { border: 1rpx solid #edf1f7; border-radius: 16rpx; max-height: 250rpx; overflow-y: auto; }
.template-item { padding: 16rpx 20rpx; border-bottom: 1rpx solid #f0f2f5; }
.template-item:active { background: #eef4ff; }
.template-item:last-child { border-bottom: none; }
.template-name { font-size: 26rpx; color: #1d2129; font-weight: 500; display: block; }
.template-desc { font-size: 22rpx; color: #7b8494; margin-top: 4rpx; display: block; }
.template-empty { text-align: center; padding: 24rpx; font-size: 24rpx; color: #a8b0c2; }
</style>
