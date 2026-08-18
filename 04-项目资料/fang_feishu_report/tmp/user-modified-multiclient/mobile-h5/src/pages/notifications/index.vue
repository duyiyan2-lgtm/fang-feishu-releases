<template>
  <view class="notif-container">
    <!-- 顶部操作栏 -->
    <view class="notif-header">
      <text class="notif-title">通知中心</text>
      <text class="read-all-btn" @tap="handleMarkAllRead">全部已读</text>
    </view>

    <!-- 类型筛选 -->
    <scroll-view class="type-scroll" scroll-x enhanced show-scrollbar>
      <view
        v-for="t in types"
        :key="t.key"
        class="type-chip"
        :class="{ active: selectedType === t.key }"
        @tap="selectType(t.key)"
      >
        <text class="type-chip-text">{{ t.label }}</text>
      </view>
    </scroll-view>

    <!-- 加载中 -->
    <view v-if="loading" class="loading-state">
      <text class="loading-text">加载中...</text>
    </view>

    <!-- 通知列表 -->
    <view v-else-if="list.length" class="notif-list">
      <view
        v-for="item in list"
        :key="item.id"
        class="notif-item"
        :class="{ unread: !item.isRead }"
        @tap="handleTap(item)"
      >
        <view class="notif-dot" :class="{ 'dot-read': item.isRead }" />
        <view class="notif-body">
          <view class="notif-title-row">
            <text class="notif-type-tag">{{ typeLabel(item.type) }}</text>
            <text class="notif-time">{{ formatTime(item.createdAt) }}</text>
          </view>
          <text class="notif-content">{{ item.content }}</text>
          <view v-if="!item.isRead" class="notif-actions">
            <text class="notif-action-btn" @tap.stop="markRead(item)">标为已读</text>
          </view>
        </view>
      </view>

      <!-- 分页 -->
      <view v-if="totalPages > 1" class="pagination">
        <text class="page-btn" :class="{ disabled: page <= 1 }" @tap="changePage(page - 1)">‹ 上一页</text>
        <text class="page-info">{{ page }} / {{ totalPages }}</text>
        <text class="page-btn" :class="{ disabled: page >= totalPages }" @tap="changePage(page + 1)">下一页 ›</text>
      </view>
    </view>

    <!-- 空状态 -->
    <view v-else class="empty-state">
      <text class="empty-icon">🔔</text>
      <text class="empty-text">暂无通知</text>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { onShow, onShareAppMessage } from '@dcloudio/uni-app'
import { getNotifications, markNotificationRead, markAllRead } from '@/api/notifications'
import { useAuthStore } from '@/stores/auth'
import { updateBadges } from '@/utils/badge'

const authStore = useAuthStore()

const types = [
  { key: '', label: '全部' },
  { key: 'unread', label: '未读' },
  { key: 'IM', label: '聊天' },
  { key: 'Approval', label: '审批' },
  { key: 'Document', label: '文档' },
  { key: 'System', label: '系统' },
]

const list = ref<any[]>([])
const selectedType = ref('')
const page = ref(1)
const pageSize = 20
const totalPages = ref(1)
const loading = ref(false)

async function loadData() {
  loading.value = true
  try {
    const params: any = { page: page.value, pageSize }
    if (selectedType.value === 'unread') {
      params.unreadOnly = true
    } else if (selectedType.value) {
      params.type = selectedType.value
    }
    const res: any = await getNotifications(params)
    const items = Array.isArray(res) ? res : res?.items || res?.list || []
    list.value = items
    const total = res?.total || res?.count || items.length
    totalPages.value = Math.ceil(total / pageSize) || 1
  } catch {
    list.value = []
  } finally {
    loading.value = false
  }
}

function selectType(key: string) {
  selectedType.value = key
  page.value = 1
  loadData()
}

function changePage(p: number) {
  if (p < 1 || p > totalPages.value) return
  page.value = p
  loadData()
}

async function markRead(item: any) {
  try {
    await markNotificationRead(item.id)
    item.isRead = true
    updateBadges()
  } catch (e) { console.warn('[Notif] markRead failed', e) }
}

async function handleMarkAllRead() {
  try {
    await markAllRead()
    list.value.forEach((item) => { item.isRead = true })
    uni.showToast({ title: '已全部标为已读', icon: 'success' })
    updateBadges()
  } catch {
    uni.showToast({ title: '操作失败', icon: 'none' })
  }
}

/** IM 通知 → 尝试通过内容匹配找到对应的会话并直接跳到聊天页 */
async function navigateIMNotification(item: any) {
  try {
    const { getConversations } = await import('@/api/im')
    const res: any = await getConversations()
    const list = Array.isArray(res) ? res : res?.items || res?.list || []
    const content = item.content || ''
    // 通知的 Content 是消息前 80 个字符，尝试精确匹配或前缀匹配
    const match = list.find((c: any) => {
      const last = c.lastMessage?.content || ''
      return last === content || (last.length > 80 && last.startsWith(content))
    })
    if (match) {
      const other = match.type === 'Private'
        ? (match.members || []).find((m: any) => m.userId !== authStore.userInfo?.id)
        : null
      const name = other ? other.realName : (match.title || '聊天')
      uni.navigateTo({
        url: `/pages/im/chat?conversationId=${match.id}&name=${encodeURIComponent(name)}&type=${match.type}`,
      })
      return
    }
  } catch (e) { console.warn('[Notif] getConversations failed', e) }
  // 没匹配到 → 退回到 IM 列表页
  uni.switchTab({ url: '/pages/im/index' })
}

function handleTap(item: any) {
  if (!item.isRead) markRead(item)
  switch (item.type) {
    case 'IM':
      navigateIMNotification(item)
      break
    case 'Approval':
      uni.navigateTo({ url: '/pages/approvals/index' })
      break
    case 'Document':
      uni.switchTab({ url: '/pages/documents/index' })
      break
    default:
      break
  }
}

function typeLabel(type: string): string {
  const map: Record<string, string> = {
    IM: '💬 聊天',
    Approval: '📋 审批',
    Document: '📄 文档',
    System: '🔔 系统',
  }
  return map[type] || type
}

function formatTime(t: string) {
  if (!t) return ''
  const d = new Date(t)
  const now = new Date()
  const hh = String(d.getHours()).padStart(2, '0')
  const mm = String(d.getMinutes()).padStart(2, '0')
  if (d.toDateString() === now.toDateString()) return `${hh}:${mm}`
  return `${d.getMonth() + 1}/${d.getDate()} ${hh}:${mm}`
}

onShow(() => loadData())

/** 右上角分享 */
onShareAppMessage(() => {
  return {
    title: '仿飞书 - 通知中心',
    path: '/pages/notifications/index',
  }
})
</script>

<style scoped>
.notif-container {
  min-height: 100vh;
  background: #f6f8fc;
}
.notif-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 22rpx 24rpx;
  background: #fff;
  box-shadow: 0 8rpx 24rpx rgba(31, 49, 84, 0.04);
}
.notif-title {
  font-size: 32rpx;
  font-weight: 800;
  color: #111827;
}
.read-all-btn {
  font-size: 24rpx;
  color: #1f6fff;
  padding: 9rpx 18rpx;
  background: #eef4ff;
  border-radius: 999rpx;
}

/* 类型筛选 */
.type-scroll {
  display: flex;
  white-space: nowrap;
  padding: 16rpx 24rpx;
  background: #fff;
  border-bottom: 1rpx solid #edf1f7;
}
.type-chip {
  display: inline-flex;
  padding: 10rpx 24rpx;
  margin-right: 12rpx;
  background: #f6f8fc;
  border-radius: 999rpx;
}
.type-chip.active {
  background: #eef4ff;
  color: #1f6fff;
  font-weight: 500;
}
.type-chip-text {
  font-size: 24rpx;
  color: #4b5563;
}
.type-chip.active .type-chip-text {
  color: #1f6fff;
}

/* 列表 */
.notif-list {
  margin: 16rpx 24rpx;
  background: #fff;
  border-radius: 28rpx;
  overflow: hidden;
  box-shadow: 0 14rpx 36rpx rgba(31, 49, 84, 0.07);
}
.notif-item {
  display: flex;
  padding: 26rpx;
  border-bottom: 1rpx solid #f0f2f5;
}
.notif-item:last-child { border-bottom: none; }
.notif-item:active { background: #f8fbff; }
.notif-item.unread { background: #fbfdff; }
.notif-dot {
  width: 12rpx;
  height: 12rpx;
  border-radius: 50%;
  background: #1f6fff;
  margin-top: 10rpx;
  margin-right: 16rpx;
  flex-shrink: 0;
}
.dot-read { background: transparent; }
.notif-body { flex: 1; min-width: 0; }
.notif-title-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 6rpx;
}
.notif-type-tag {
  font-size: 24rpx;
  color: #1f6fff;
  font-weight: 600;
}
.notif-time {
  font-size: 20rpx;
  color: #a8b0c2;
}
.notif-content {
  font-size: 26rpx;
  color: #111827;
  display: block;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.notif-actions {
  margin-top: 8rpx;
}
.notif-action-btn {
  font-size: 22rpx;
  color: #64748b;
  padding: 6rpx 14rpx;
  background: #f6f8fc;
  border-radius: 999rpx;
}

/* 状态 */
.loading-state, .empty-state {
  margin: 24rpx;
  padding: 120rpx 0;
  text-align: center;
  background: #fff;
  border-radius: 28rpx;
  box-shadow: 0 14rpx 36rpx rgba(31, 49, 84, 0.06);
}
.loading-text, .empty-text {
  font-size: 28rpx;
  color: #64748b;
}
.empty-icon { font-size: 64rpx; margin-bottom: 16rpx; }

/* 分页 */
.pagination {
  display: flex;
  justify-content: center;
  align-items: center;
  padding: 24rpx 0;
  gap: 24rpx;
}
.page-btn {
  font-size: 24rpx;
  color: #1f6fff;
  padding: 8rpx 20rpx;
  background: #f6f8fc;
  border-radius: 12rpx;
}
.page-btn.disabled { color: #a8b0c2; background: transparent; }
.page-info { font-size: 24rpx; color: #7b8494; }
</style>
