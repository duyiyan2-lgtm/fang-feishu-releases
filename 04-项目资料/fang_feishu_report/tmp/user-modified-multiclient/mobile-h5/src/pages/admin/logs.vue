<template>
  <view class="page-container">
    <!-- 筛选区 -->
    <view class="filter-bar">
      <input v-model="filters.userName" class="filter-input" placeholder="操作人" />
      <picker :value="moduleIndex" :range="moduleOptions" @change="onModuleChange">
        <view class="filter-picker">{{ moduleOptions[moduleIndex] }}</view>
      </picker>
      <button class="search-btn" @tap="handleSearch">查询</button>
    </view>
    <view class="filter-bar">
      <picker mode="date" :value="filters.startDate" @change="(e) => filters.startDate = e.detail.value">
        <view class="filter-picker">{{ filters.startDate || '开始日期' }}</view>
      </picker>
      <text class="filter-sep">~</text>
      <picker mode="date" :value="filters.endDate" @change="(e) => filters.endDate = e.detail.value">
        <view class="filter-picker">{{ filters.endDate || '结束日期' }}</view>
      </picker>
    </view>

    <view v-if="loading" class="loading-state"><text>加载中...</text></view>

    <view v-else-if="list.length" class="list">
      <view v-for="item in list" :key="item.id" class="list-item">
        <text class="log-time">{{ formatTime(item.createdAt) }}</text>
        <text class="log-info">{{ item.userName }} {{ actionLabel(item.action) }} - {{ moduleLabel(item.module) }}</text>
        <text class="log-detail">{{ item.targetId ? `ID: ${item.targetId.slice(0,8)}...` : '' }}</text>
      </view>
      <view class="pagination">
        <text class="page-btn" :class="{ disabled: page <= 1 }" @tap="changePage(page - 1)">‹ 上一页</text>
        <text class="page-info">{{ page }} / {{ totalPages }}</text>
        <text class="page-btn" :class="{ disabled: page >= totalPages }" @tap="changePage(page + 1)">下一页 ›</text>
      </view>
    </view>

    <view v-else class="empty-state"><text>暂无操作日志</text></view>
  </view>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { getOperationLogs } from '@/api/admin'

const list = ref<any[]>([])
const page = ref(1)
const pageSize = 30
const totalPages = ref(1)
const loading = ref(false)
const moduleIndex = ref(0)
const moduleOptions = ['全部模块', 'User', 'Department', 'Role', 'Approval', 'Document', 'Drive', 'Calendar', 'IM']
const filters = ref({ userName: '', module: '', startDate: '', endDate: '' })

function onModuleChange(e: any) {
  moduleIndex.value = e.detail.value
  filters.value.module = moduleIndex.value === 0 ? '' : moduleOptions[moduleIndex.value]
}

function handleSearch() { page.value = 1; loadData() }
function changePage(p: number) { if (p < 1 || p > totalPages.value) return; page.value = p; loadData() }

async function loadData() {
  loading.value = true
  try {
    const params: any = { page: page.value, pageSize }
    if (filters.value.userName) params.userName = filters.value.userName
    if (filters.value.module) params.module = filters.value.module
    if (filters.value.startDate) params.startDate = filters.value.startDate
    if (filters.value.endDate) params.endDate = filters.value.endDate
    const res: any = await getOperationLogs(params)
    const items = Array.isArray(res) ? res : res?.items || res?.list || []
    list.value = items
    totalPages.value = Math.ceil((res?.total || items.length) / pageSize) || 1
  } catch { list.value = [] } finally { loading.value = false }
}

function actionLabel(action: string): string {
  const map: Record<string, string> = {
    Create: '新增', Update: '更新', Delete: '删除',
    Approve: '通过', Reject: '驳回', SetStatus: '状态变更',
    Upload: '上传', Download: '下载',
  }
  return map[action] || action
}

function moduleLabel(module: string): string {
  const map: Record<string, string> = {
    User: '用户管理', Department: '部门管理', Role: '角色管理',
    Approval: '审批', Document: '文档', Drive: '云盘', Calendar: '日程', IM: '聊天',
  }
  return map[module] || module
}

function formatTime(t: string) {
  if (!t) return ''
  const d = new Date(t)
  return `${d.getMonth() + 1}/${d.getDate()} ${String(d.getHours()).padStart(2,'0')}:${String(d.getMinutes()).padStart(2,'0')}`
}

onShow(() => loadData())
</script>

<style scoped>
.page-container { min-height: 100vh; background: #f0f2f5; padding: 24rpx; }
.filter-bar { display: flex; gap: 12rpx; margin-bottom: 12rpx; }
.filter-input { flex: 1; height: 60rpx; background: #fff; border-radius: 30rpx; padding: 0 20rpx; font-size: 24rpx; }
.filter-picker { height: 60rpx; line-height: 60rpx; background: #fff; border-radius: 30rpx; padding: 0 20rpx; font-size: 24rpx; color: #4e5969; min-width: 160rpx; text-align: center; }
.filter-sep { line-height: 60rpx; font-size: 24rpx; color: #c9cdd4; }
.search-btn { height: 60rpx; line-height: 60rpx; padding: 0 24rpx; background: #409EFF; color: #fff; font-size: 24rpx; border-radius: 30rpx; border: none; }
.list { background: #fff; border-radius: 16rpx; }
.list-item { padding: 20rpx 24rpx; border-bottom: 1rpx solid #f0f2f5; }
.list-item:last-child { border-bottom: none; }
.log-time { font-size: 22rpx; color: #c9cdd4; display: block; }
.log-info { font-size: 26rpx; color: #1d2129; display: block; margin: 4rpx 0; }
.log-detail { font-size: 20rpx; color: #86909c; }
.pagination { display: flex; justify-content: center; align-items: center; padding: 24rpx; gap: 24rpx; }
.page-btn { font-size: 24rpx; color: #409EFF; padding: 8rpx 20rpx; background: #f0f2f5; border-radius: 8rpx; }
.page-btn.disabled { color: #c9cdd4; }
.page-info { font-size: 24rpx; color: #86909c; }
.loading-state, .empty-state { padding: 120rpx 0; text-align: center; font-size: 28rpx; color: #86909c; }
</style>
