<template>
  <view class="doc-container">
    <!-- 搜索 + 新建 -->
    <view class="doc-toolbar">
      <input
        v-model="searchText"
        class="search-input"
        placeholder="搜索文档..."
        placeholder-class="placeholder"
        @input="searchText = $event.detail.value"
      />
      <button class="create-btn" @tap="handleCreate">+ 新建文档</button>
    </view>

    <view class="doc-tabs">
      <text
        class="doc-tab"
        :class="{ active: docTab === 'recent' }"
        @tap="docTab = 'recent'"
      >最近</text>
      <text
        class="doc-tab"
        :class="{ active: docTab === 'mine' }"
        @tap="docTab = 'mine'"
      >我的</text>
      <text
        class="doc-tab"
        :class="{ active: docTab === 'fav' }"
        @tap="docTab = 'fav'"
      >收藏</text>
    </view>

    <!-- 文档列表 -->
    <view v-if="documents.length" class="doc-list">
      <view
        v-for="doc in pagedDocuments"
        :key="doc.id"
        class="doc-item"
        @tap="openDoc(doc)"
      >
        <view class="doc-icon">📄</view>
        <view class="doc-info">
          <text class="doc-title">{{ doc.title }}</text>
          <text class="doc-meta"><text v-if="doc.ownerId !== myId" class="shared-badge">共享</text>{{ doc.ownerName || '未知' }} · {{ formatTime(doc.updatedAt) }}</text>
        </view>
        <view class="doc-actions">
          <text class="fav-btn" @tap.stop="toggleFav(doc)">{{ isFaved(doc.id) ? '⭐' : '☆' }}</text>
          <text class="edit-btn" @tap.stop="openDoc(doc)">编辑</text>
        </view>
      </view>
    </view>

    <!-- 空状态 -->
    <view v-else class="empty-state">
      <view class="empty-icon">📄</view>
      <text class="empty-text">{{ docTab === 'fav' ? '暂无收藏' : '暂无文档' }}</text>
      <text class="empty-hint">{{ docTab === 'fav' ? '在文档列表中点击 ☆ 即可收藏' : '点击「+ 新建文档」开始写作' }}</text>
    </view>

    <!-- 分页 -->
    <view v-if="totalPages > 1" class="pagination">
      <text
        class="page-btn"
        :class="{ disabled: page <= 1 }"
        @tap="changePage(page - 1)"
      >上一页</text>
      <text class="page-info">{{ page }} / {{ totalPages }}</text>
      <text
        class="page-btn"
        :class="{ disabled: page >= totalPages }"
        @tap="changePage(page + 1)"
      >下一页</text>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { onShow, onShareAppMessage } from '@dcloudio/uni-app'
import { getDocuments } from '@/api/documents'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()

// ======== 收藏功能（本地存储 + 响应式） ========
const FAV_KEY = `doc_favs_${authStore.userInfo?.id || ''}`

function getFavIds(): Set<string> {
  try {
    const raw = uni.getStorageSync(FAV_KEY) || '[]'
    return new Set(JSON.parse(raw))
  } catch { return new Set() }
}

function saveFavIds(ids: Set<string>) {
  uni.setStorageSync(FAV_KEY, JSON.stringify(Array.from(ids)))
}

/** 响应式收藏 ID 集合 */
const favIds = ref<Set<string>>(getFavIds())

function isFaved(id: string): boolean {
  return favIds.value.has(id)
}

function toggleFav(doc: any) {
  if (favIds.value.has(doc.id)) {
    favIds.value.delete(doc.id)
    uni.showToast({ title: '已取消收藏', icon: 'none' })
  } else {
    favIds.value.add(doc.id)
    uni.showToast({ title: '已收藏', icon: 'success' })
  }
  // 触发响应式更新（Set 的修改不会自动触发）
  favIds.value = new Set(favIds.value)
  saveFavIds(favIds.value)
}
// ========

const allDocuments = ref<any[]>([]) // 完整列表
const searchText = ref('')
const docTab = ref('recent') // recent / mine / fav
const page = ref(1)
const pageSize = 20
const myId = computed(() => authStore.userInfo?.id || '')

/** 总页数（基于过滤后的列表） */
const totalPages = computed(() => Math.ceil(documents.value.length / pageSize) || 1)

/** Tab 切换时重置页码 */
watch(docTab, () => { page.value = 1 })

/** 实时过滤的文档列表 */
const documents = computed(() => {
  let list = allDocuments.value

  // Tab 过滤
  if (docTab.value === 'mine') {
    list = list.filter((doc: any) => doc.ownerId === myId.value)
  } else if (docTab.value === 'fav') {
    list = list.filter((doc: any) => favIds.value.has(doc.id))
  }
  // "最近" = 全部（本人 + 协作者）

  // 搜索关键字过滤
  if (searchText.value.trim()) {
    const kw = searchText.value.trim().toLowerCase()
    list = list.filter((doc: any) =>
      (doc.title || '').toLowerCase().includes(kw)
    )
  }

  return list
})

/** 当前页要展示的文档（客户端分页切片） */
const pagedDocuments = computed(() => {
  const start = (page.value - 1) * pageSize
  return documents.value.slice(start, start + pageSize)
})

async function loadDocuments() {
  try {
    const res: any = await getDocuments()
    let list = Array.isArray(res) ? res : res?.items || res?.list || []
    // 过滤已删除的文档，后端已按权限返回可见的文档
    allDocuments.value = list.filter((doc: any) => doc.title !== '(已删除)')
  } catch {
    allDocuments.value = []
  }
}

function changePage(p: number) {
  if (p < 1 || p > totalPages.value) return
  page.value = p
}

function openDoc(doc: any) {
  uni.navigateTo({ url: `/pages/documents/edit?id=${doc.id}&title=${encodeURIComponent(doc.title || '')}` })
}

function handleCreate() {
  uni.navigateTo({ url: '/pages/documents/edit?id=0' })
}

function formatTime(t: string) {
  if (!t) return ''
  const d = new Date(t)
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}

onShow(() => loadDocuments())

/** 分享 */
onShareAppMessage(() => {
  return {
    title: '仿飞书 - 文档中心',
    path: '/pages/documents/index',
  }
})
</script>

<style scoped>
.doc-container {
  min-height: 100vh;
  background: #f6f8fc;
  padding: 24rpx;
}
.doc-toolbar {
  display: flex;
  gap: 16rpx;
  margin-bottom: 22rpx;
}
.search-input {
  flex: 1;
  height: 76rpx;
  background: #fff;
  border-radius: 24rpx;
  padding: 0 28rpx;
  font-size: 26rpx;
  box-shadow: 0 10rpx 28rpx rgba(31, 49, 84, 0.06);
  border: 1rpx solid #edf1f7;
}
.placeholder { color: #a8b0c2; }
.create-btn {
  height: 76rpx;
  line-height: 76rpx;
  padding: 0 24rpx;
  background: linear-gradient(135deg, #1f6fff, #18b7ff);
  color: #fff;
  font-size: 24rpx;
  border-radius: 24rpx;
  border: none;
  flex-shrink: 0;
  font-weight: 700;
  box-shadow: 0 12rpx 28rpx rgba(31, 111, 255, 0.2);
}
.doc-tabs {
  display: flex;
  gap: 14rpx;
  margin-bottom: 18rpx;
}
.doc-tab {
  padding: 10rpx 26rpx;
  border-radius: 999rpx;
  background: #fff;
  color: #64748b;
  font-size: 25rpx;
  box-shadow: 0 8rpx 22rpx rgba(31, 49, 84, 0.05);
}
.doc-tab.active {
  background: #eef4ff;
  color: #1f6fff;
  font-weight: 700;
}
.doc-list {
  background: #fff;
  border-radius: 28rpx;
  overflow: hidden;
  box-shadow: 0 14rpx 36rpx rgba(31, 49, 84, 0.07);
}
.doc-item {
  display: flex;
  align-items: center;
  padding: 26rpx 28rpx;
  border-bottom: 1rpx solid #f0f2f5;
}
.doc-item:last-child { border-bottom: none; }
.doc-item:active { background: #f8fbff; }
.doc-icon {
  width: 72rpx;
  height: 72rpx;
  margin-right: 20rpx;
  border-radius: 22rpx;
  background: #eaf2ff;
  color: #1f6fff;
  font-size: 38rpx;
  display: flex;
  align-items: center;
  justify-content: center;
}
.doc-info { flex: 1; min-width: 0; }
.doc-title {
  font-size: 29rpx;
  font-weight: 700;
  color: #111827;
  display: block;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.doc-meta {
  font-size: 22rpx;
  color: #7b8494;
  margin-top: 4rpx;
  display: block;
}
.doc-actions { flex-shrink: 0; margin-left: 16rpx; }
.fav-btn {
  font-size: 28rpx;
  padding: 4rpx 8rpx;
  margin-right: 6rpx;
}
.edit-btn {
  font-size: 24rpx;
  color: #1f6fff;
  padding: 8rpx 18rpx;
  background: #eef4ff;
  border-radius: 999rpx;
}
.empty-state {
  text-align: center;
  padding: 120rpx 0;
  background: #fff;
  border-radius: 28rpx;
  box-shadow: 0 14rpx 36rpx rgba(31, 49, 84, 0.06);
}
.empty-icon { font-size: 72rpx; }
.empty-text {
  font-size: 28rpx;
  color: #64748b;
  display: block;
  margin-top: 16rpx;
}
.empty-hint {
  font-size: 24rpx;
  color: #a8b0c2;
  display: block;
  margin-top: 8rpx;
}
.pagination {
  display: flex;
  justify-content: center;
  align-items: center;
  padding: 32rpx 0;
  gap: 24rpx;
}
.page-btn {
  font-size: 26rpx;
  color: #1f6fff;
  padding: 8rpx 20rpx;
  background: #fff;
  border-radius: 8rpx;
}
.page-btn.disabled { color: #c9cdd4; }
.page-info {
  font-size: 26rpx;
  color: #7b8494;
}
.shared-badge {
  font-size: 20rpx;
  color: #fff;
  background: #67c23a;
  padding: 2rpx 10rpx;
  border-radius: 999rpx;
  margin-right: 8rpx;
}
</style>
