<template>
  <view class="wiki-container">
    <!-- 搜索栏 -->
    <view class="search-bar">
      <input v-model="searchText" class="search-input" placeholder="搜索知识库标题或内容..." @confirm="doSearch" />
      <button v-if="!searchText" class="create-btn" @tap="openCreateSpace">+ 新建空间</button>
      <button v-else class="search-btn" @tap="doSearch">搜索</button>
    </view>

    <!-- 搜索结果 -->
    <template v-if="searchText">
      <view v-if="searchResult.spaces?.length || searchResult.nodes?.length" class="search-results">
        <view v-if="searchResult.spaces?.length" class="result-section">
          <text class="result-section-title">空间</text>
          <view v-for="s in searchResult.spaces" :key="s.id" class="result-item" @tap="goToSpace(s.id)">
            <text class="result-name">{{ s.name }}</text>
            <text class="result-desc">{{ s.description || '' }}</text>
          </view>
        </view>
        <view v-if="searchResult.nodes?.length" class="result-section">
          <text class="result-section-title">文档节点</text>
          <view v-for="n in searchResult.nodes" :key="n.id" class="result-item" @tap="goToSpace(n.wikiSpaceId)">
            <text class="result-name">{{ n.title }}</text>
            <text class="result-desc">{{ n.documentTitle || '' }}</text>
          </view>
        </view>
      </view>
      <view v-else class="empty-state">
        <view class="empty-icon">🔍</view>
        <text class="empty-text">未找到相关内容</text>
      </view>
    </template>

    <!-- 空间列表 -->
    <template v-else>
      <view v-if="spaces.length" class="space-list">
        <view v-for="s in spaces" :key="s.id" class="space-card" @tap="goToSpace(s.id)">
          <view class="space-top">
            <text class="space-icon">📚</text>
            <text class="space-name">{{ s.name }}</text>
            <text class="space-more-btn" @tap.stop="toggleActionMenu(s.id)">⋮</text>
          </view>
          <text class="space-desc">{{ s.description || '暂无描述' }}</text>
          <view class="space-footer">
            <text class="space-meta">{{ s.ownerName }} · {{ s.visibility === 'Organization' ? '全员可见' : '私有' }}</text>
            <text class="space-count">{{ s.nodeCount || 0 }} 个文档</text>
          </view>
          <!-- 操作菜单 -->
          <view v-if="actionSpaceId === s.id" class="action-menu" @tap.stop>
            <view class="action-item" @tap="openEditSpace(s)">✏️ 编辑</view>
            <view class="action-item action-delete" @tap="confirmDeleteSpace(s.id)">🗑️ 删除</view>
          </view>
        </view>
        <!-- 点击遮罩关闭菜单 -->
        <view v-if="actionSpaceId" class="menu-overlay" @tap="actionSpaceId = null" />
      </view>
      <view v-else class="empty-state">
        <view class="empty-icon">📚</view>
        <text class="empty-text">暂无知识库空间</text>
        <button class="empty-btn" @tap="openCreateSpace">创建第一个空间</button>
      </view>
    </template>

    <!-- ===== 创建/编辑空间弹窗 ===== -->
    <view v-if="showSpaceModal" class="modal-overlay" @tap="closeSpaceModal">
      <view class="modal-popup" @tap.stop>
        <text class="modal-title">{{ isEditMode ? '编辑知识库空间' : '创建知识库空间' }}</text>
        <view class="form-group">
          <text class="form-label">空间名称 *</text>
          <input v-model="spaceForm.name" class="form-input" placeholder="输入空间名称" />
        </view>
        <view class="form-group">
          <text class="form-label">空间描述</text>
          <input v-model="spaceForm.description" class="form-input" placeholder="可选描述" />
        </view>
        <view class="form-group">
          <text class="form-label">可见范围</text>
          <picker mode="selector" :range="['Organization', 'Private']" @change="onVisibilityChange">
            <view class="form-input">{{ spaceForm.visibility === 'Organization' ? '全员可见' : '私有' }}</view>
          </picker>
        </view>
        <view class="modal-btns">
          <button class="modal-cancel" @tap="closeSpaceModal">取消</button>
          <button class="modal-confirm" :disabled="!spaceForm.name.trim()" @tap="submitSpace">{{ isEditMode ? '保存' : '创建' }}</button>
        </view>
      </view>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { getWikiSpaces, createWikiSpace, searchWiki, updateWikiSpace, deleteWikiSpace } from '@/api/wiki'

const searchText = ref('')
const searchResult = ref<any>({ spaces: [], nodes: [] })
const spaces = ref<any[]>([])
const showSpaceModal = ref(false)
const isEditMode = ref(false)
const editSpaceId = ref('')
const actionSpaceId = ref<string | null>(null)
const spaceForm = ref({ name: '', description: '', visibility: 'Organization' })

async function loadSpaces() {
  try {
    const res: any = await getWikiSpaces()
    spaces.value = Array.isArray(res) ? res : []
  } catch {
    spaces.value = []
  }
}

async function doSearch() {
  if (!searchText.value.trim()) { searchText.value = ''; return }
  try {
    const res: any = await searchWiki(searchText.value.trim())
    searchResult.value = res || { spaces: [], nodes: [] }
  } catch {
    searchResult.value = { spaces: [], nodes: [] }
  }
}

function goToSpace(id: string) {
  uni.navigateTo({ url: `/pages/wiki/space?id=${id}` })
}

function openCreateSpace() {
  isEditMode.value = false
  editSpaceId.value = ''
  spaceForm.value = { name: '', description: '', visibility: 'Organization' }
  showSpaceModal.value = true
}

function closeSpaceModal() {
  showSpaceModal.value = false
  actionSpaceId.value = null
}

function toggleActionMenu(id: string) {
  actionSpaceId.value = actionSpaceId.value === id ? null : id
}

function openEditSpace(space: any) {
  actionSpaceId.value = null
  isEditMode.value = true
  editSpaceId.value = space.id
  spaceForm.value = {
    name: space.name || '',
    description: space.description || '',
    visibility: space.visibility || 'Organization',
  }
  showSpaceModal.value = true
}

function confirmDeleteSpace(id: string) {
  actionSpaceId.value = null
  uni.showModal({
    title: '确认删除',
    content: '确定要删除该知识库空间吗？删除后不可恢复。',
    success: async (res) => {
      if (res.confirm) {
        try {
          await deleteWikiSpace(id)
          uni.showToast({ title: '删除成功', icon: 'success' })
          loadSpaces()
        } catch {
          uni.showToast({ title: '删除失败', icon: 'none' })
        }
      }
    },
  })
}

async function submitSpace() {
  if (!spaceForm.value.name.trim()) return
  try {
    if (isEditMode.value) {
      await updateWikiSpace(editSpaceId.value, {
        name: spaceForm.value.name.trim(),
        description: spaceForm.value.description.trim() || undefined,
        visibility: spaceForm.value.visibility,
      })
      uni.showToast({ title: '更新成功', icon: 'success' })
    } else {
      await createWikiSpace({
        name: spaceForm.value.name.trim(),
        description: spaceForm.value.description.trim() || undefined,
        visibility: spaceForm.value.visibility,
      })
      uni.showToast({ title: '创建成功', icon: 'success' })
    }
    closeSpaceModal()
    loadSpaces()
  } catch {
    uni.showToast({ title: isEditMode.value ? '更新失败' : '创建失败', icon: 'none' })
  }
}

function onVisibilityChange(e: any) {
  spaceForm.value.visibility = ['Organization', 'Private'][e.detail.value]
}

onShow(() => {
  if (!searchText.value) loadSpaces()
})
</script>

<style scoped>
.wiki-container { min-height: 100vh; background: #f6f8fc; }

.search-bar {
  display: flex; align-items: center; padding: 20rpx 24rpx; gap: 12rpx;
}
.search-input {
  flex: 1; height: 68rpx; background: #fff; border-radius: 22rpx;
  padding: 0 24rpx; font-size: 24rpx; border: 1rpx solid #edf1f7;
}
.create-btn, .search-btn {
  height: 68rpx; line-height: 68rpx; padding: 0 22rpx;
  background: linear-gradient(135deg, #1f6fff, #18b7ff); color: #fff;
  font-size: 24rpx; border-radius: 30rpx; border: none; flex-shrink: 0; font-weight: 700;
  box-shadow: 0 8rpx 20rpx rgba(31,111,255,0.2);
}

.space-list { margin: 0 24rpx; }
.space-card {
  background: #fff; border-radius: 28rpx; padding: 28rpx;
  margin-bottom: 18rpx; box-shadow: 0 14rpx 36rpx rgba(31,49,84,0.07);
}
.space-card:active { background: #f8fbff; }
.space-top { display: flex; align-items: center; gap: 14rpx; margin-bottom: 10rpx; }
.space-icon { font-size: 40rpx; }
.space-name { font-size: 30rpx; font-weight: 700; color: #111827; }
.space-desc { font-size: 24rpx; color: #7b8494; display: block; margin-bottom: 14rpx; }
.space-footer { display: flex; justify-content: space-between; }
.space-meta { font-size: 22rpx; color: #a8b0c2; }
.space-count { font-size: 22rpx; color: #1f6fff; font-weight: 500; }

.space-more-btn {
  margin-left: auto; font-size: 36rpx; color: #a8b0c2; padding: 4rpx 8rpx;
  line-height: 1; font-weight: 700; letter-spacing: 2rpx;
}
.space-more-btn:active { color: #1f6fff; }

.action-menu {
  position: absolute; right: 28rpx; top: 72rpx; background: #fff;
  border-radius: 16rpx; box-shadow: 0 8rpx 32rpx rgba(0,0,0,0.15);
  z-index: 20; overflow: hidden; min-width: 160rpx;
}
.action-item {
  padding: 22rpx 28rpx; font-size: 26rpx; color: #111827;
  display: flex; align-items: center; gap: 10rpx;
}
.action-item:active { background: #f6f8fc; }
.action-delete { color: #ef4444; }

.menu-overlay {
  position: fixed; top: 0; left: 0; right: 0; bottom: 0;
  z-index: 10; background: transparent;
}

/* 搜索结果 */
.search-results { margin: 0 24rpx; }
.result-section { margin-bottom: 24rpx; }
.result-section-title {
  font-size: 26rpx; font-weight: 700; color: #111827;
  display: block; margin-bottom: 12rpx;
}
.result-item {
  background: #fff; border-radius: 18rpx; padding: 20rpx;
  margin-bottom: 10rpx; box-shadow: 0 8rpx 24rpx rgba(31,49,84,0.05);
}
.result-item:active { background: #f8fbff; }
.result-name { font-size: 26rpx; font-weight: 600; color: #111827; display: block; }
.result-desc { font-size: 22rpx; color: #7b8494; display: block; margin-top: 4rpx; }

.empty-state { margin: 24rpx; text-align: center; padding: 120rpx 0; background: #fff; border-radius: 28rpx; }
.empty-icon { font-size: 72rpx; }
.empty-text { font-size: 28rpx; color: #64748b; display: block; margin-top: 16rpx; }
.empty-btn {
  margin-top: 24rpx; height: 72rpx; line-height: 72rpx; padding: 0 36rpx;
  background: #1f6fff; color: #fff; font-size: 26rpx; border-radius: 36rpx; border: none; display: inline-block;
}

.modal-overlay {
  position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0,0,0,0.45);
  z-index: 999; display: flex; align-items: center; justify-content: center; padding: 60rpx;
}
.modal-popup {
  width: 100%; max-width: 560rpx; background: #fff; border-radius: 28rpx;
  padding: 32rpx; max-height: 80vh; overflow-y: auto;
}
.modal-title { font-size: 32rpx; font-weight: 600; display: block; text-align: center; margin-bottom: 24rpx; }
.form-group { margin-bottom: 20rpx; }
.form-label { font-size: 24rpx; color: #7b8494; display: block; margin-bottom: 6rpx; }
.form-input {
  height: 64rpx; background: #f6f8fc; border-radius: 16rpx; padding: 0 20rpx;
  font-size: 26rpx; color: #111827; display: flex; align-items: center;
}
.modal-btns { display: flex; gap: 20rpx; margin-top: 20rpx; }
.modal-cancel, .modal-confirm {
  flex: 1; height: 72rpx; line-height: 72rpx; font-size: 26rpx;
  border-radius: 36rpx; border: none; text-align: center;
}
.modal-cancel { background: #f6f8fc; color: #374151; }
.modal-confirm { background: #1f6fff; color: #fff; }
.modal-confirm[disabled] { opacity: 0.4; }
</style>
