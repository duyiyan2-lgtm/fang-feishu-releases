<template>
  <view class="space-container">
    <!-- 空间信息头 -->
    <view class="space-header">
      <view class="space-title-area">
        <text class="space-title">{{ space?.name || '加载中...' }}</text>
        <text class="space-visibility">{{ space?.visibility === 'Organization' ? '全员可见' : '私有' }}</text>
      </view>
      <text class="space-desc">{{ space?.description || '' }}</text>
      <view class="header-actions">
        <button class="header-btn" @tap="showMembers = true">👥 成员</button>
        <button class="header-btn" @tap="openCreateNode">+ 新建文档</button>
        <button class="header-btn header-btn-exit" @tap="handleLeaveSpace">退出</button>
      </view>
    </view>

    <!-- 加载状态 -->
    <view v-if="loading" class="loading-state">
      <text>加载中...</text>
    </view>

    <!-- 节点树 -->
    <view v-else-if="nodes.length" class="node-tree">
      <view class="tree-title">文档目录</view>
      <!-- 根节点 -->
      <view v-for="node in rootNodes" :key="node.id" class="tree-item">
        <view class="tree-node" :class="{ selected: selectedNodeId === node.id }" @tap="selectNode(node)">
          <text class="node-icon">📄</text>
          <text class="node-title">{{ node.title }}</text>
          <view class="node-actions" @tap.stop>
            <text class="node-action" @tap.stop="openCreateChild(node)">+</text>
            <text class="node-action node-more" @tap.stop="showNodeMenu(node)">⋮</text>
          </view>
        </view>
        <!-- 子节点 -->
        <view v-for="child in childNodes(node.id)" :key="child.id" class="tree-item tree-item-child">
          <view class="tree-node" :class="{ selected: selectedNodeId === child.id }" @tap="selectNode(child)">
            <text class="node-icon">📄</text>
            <text class="node-title">{{ child.title }}</text>
            <view class="node-actions" @tap.stop>
              <text class="node-action node-more" @tap.stop="showNodeMenu(child)">⋮</text>
            </view>
          </view>
        </view>
      </view>
    </view>
    <view v-else class="empty-state">
      <view class="empty-icon">📄</view>
      <text class="empty-text">暂无文档</text>
      <button class="empty-btn" @tap="openCreateNode">创建第一个文档</button>
    </view>

    <!-- ===== 节点 ⋮ 菜单 ===== -->
    <view v-if="showActionSheet && actionNode" class="modal-overlay" @tap="closeNodeMenu">
      <view class="action-sheet" @tap.stop>
        <text class="action-sheet-title">{{ actionNode.title }}</text>
        <view class="action-sheet-item" @tap="doRename">✏️ 重命名</view>
        <view class="action-sheet-item action-danger" @tap="doDelete">🗑 删除</view>
        <view class="action-sheet-cancel" @tap="closeNodeMenu">取消</view>
      </view>
    </view>

    <!-- ===== 创建/编辑节点弹窗 ===== -->
    <view v-if="showNodeModal" class="modal-overlay" @tap="showNodeModal = false">
      <view class="modal-popup" @tap.stop>
        <text class="modal-title">{{ editingNode ? '重命名文档' : '新建文档' }}</text>
        <view class="form-group">
          <text class="form-label">文档标题 *</text>
          <input v-model="nodeForm.title" class="form-input" placeholder="输入文档标题" />
        </view>
        <view class="modal-btns">
          <button class="modal-cancel" @tap="showNodeModal = false">取消</button>
          <button class="modal-confirm" :disabled="!nodeForm.title.trim()" @tap="submitNode">保存</button>
        </view>
      </view>
    </view>

    <!-- ===== 成员管理弹窗 ===== -->
    <view v-if="showMembers" class="modal-overlay" @tap="showMembers = false">
      <view class="modal-popup" @tap.stop>
        <text class="modal-title">空间成员</text>
        <!-- 现有成员 -->
        <view v-if="members.length" class="member-list">
          <view v-for="m in members" :key="m.userId" class="member-item">
            <text class="member-name">{{ m.userName }}</text>
            <text class="member-perm">{{ m.permission === 'Admin' ? '管理员' : m.permission === 'Edit' ? '可编辑' : '可查看' }}</text>
            <text class="member-remove" @tap="removeMember(m)">✕</text>
          </view>
        </view>
        <view v-else class="member-empty">暂无成员</view>
        <!-- 添加成员 -->
        <view class="add-member-section">
          <text class="add-member-title">添加成员</text>
          <view class="add-member-row">
            <input v-model="memberKeyword" class="form-input" placeholder="搜索用户..." @input="searchMembers" confirm-type="search" />
            <picker mode="selector" :range="permOptions" @change="onPermChange">
              <view class="perm-picker">{{ permText(memberPerm) }}</view>
            </picker>
            <button class="add-btn" :disabled="!memberKeyword.trim() || !memberCandidates.length" @tap="addMember">添加</button>
          </view>
          <scroll-view scroll-y class="member-candidates">
            <view v-for="u in memberCandidates" :key="u.id" class="candidate-item">
              <text class="candidate-name">{{ u.realName || u.username }}</text>
              <text class="candidate-dept">{{ u.departmentName || '' }}</text>
            </view>
            <view v-if="memberKeyword.trim() && !memberCandidates.length" class="candidate-empty">未找到用户</view>
          </scroll-view>
        </view>
        <button class="modal-close-btn" @tap="showMembers = false">关闭</button>
      </view>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { onLoad, onShow } from '@dcloudio/uni-app'
import { useAuthStore } from '@/stores/auth'
import { createDocument } from '@/api/documents'
import { getWikiSpaceDetail, getWikiSpaceMembers, setWikiSpaceMembers, createWikiNode, updateWikiNode, deleteWikiNode } from '@/api/wiki'
import { getFriends, discoverUsers } from '@/api/contacts'

const spaceId = ref('')

const space = ref<any>(null)
const nodes = ref<any[]>([])
const members = ref<any[]>([])
const loading = ref(true)
const selectedNodeId = ref<string | null>(null)
const showNodeModal = ref(false)
const showMembers = ref(false)
const editingNode = ref<any>(null)

const nodeForm = ref({ title: '' })
const parentIdForNew = ref<string | null>(null)

// ⋮ 菜单
const showActionSheet = ref(false)
const actionNode = ref<any>(null)

// 成员管理
const memberKeyword = ref('')
const memberPerm = ref('View')
const memberCandidates = ref<any[]>([])
const permOptions = ['View', 'Edit', 'Admin']

function permText(p: string): string {
  const map: Record<string, string> = { View: '可查看', Edit: '可编辑', Admin: '管理员' }
  return map[p] || p
}
function onPermChange(e: any) { memberPerm.value = permOptions[e.detail.value] }

async function searchMembers() {
  const keyword = memberKeyword.value.trim()
  if (!keyword) { memberCandidates.value = []; return }
  // 排除已在成员列表中的用户
  const existingIds = new Set(members.value.map((m: any) => m.userId))
  const all: any[] = []
  const seen = new Set<string>()
  try {
    const friends: any = await getFriends()
    if (Array.isArray(friends)) {
      for (const f of friends) {
        if (!existingIds.has(f.id) && !seen.has(f.id) && (f.realName || f.username)?.includes(keyword)) {
          seen.add(f.id); all.push(f)
        }
      }
    }
  } catch (e) { console.warn('[Wiki] getFriends failed', e) }
  try {
    const discovered: any = await discoverUsers(keyword)
    if (Array.isArray(discovered)) {
      for (const u of discovered) {
        if (!existingIds.has(u.id) && !seen.has(u.id)) { seen.add(u.id); all.push(u) }
      }
    }
  } catch (e2) { console.warn('[Wiki] discoverUsers failed', e2) }
  memberCandidates.value = all
}

async function addMember() {
  if (!memberCandidates.value.length) return
  // setWikiSpaceMembers 是全量替换，需合并已有成员+新成员
  const existingIds = members.value.map((m: any) => m.userId)
  const newIds = memberCandidates.value.map((u: any) => u.id)
  const allIds = [...new Set([...existingIds, ...newIds])]
  try {
    await setWikiSpaceMembers(spaceId.value, allIds, memberPerm.value)
    uni.showToast({ title: '添加成功', icon: 'success' })
    memberKeyword.value = ''
    memberCandidates.value = []
    loadMembers()
  } catch {
    uni.showToast({ title: '添加失败', icon: 'none' })
  }
}

const rootNodes = computed(() => nodes.value.filter(n => !n.parentId))

function childNodes(parentId: string) {
  return nodes.value.filter(n => n.parentId === parentId)
}

async function loadSpace() {
  loading.value = true
  try {
    const res: any = await getWikiSpaceDetail(spaceId.value)
    space.value = res?.space || null
    nodes.value = Array.isArray(res?.nodes) ? res.nodes : []
  } catch {
    uni.showToast({ title: '加载失败', icon: 'none' })
  } finally {
    loading.value = false
  }
}

/** 踢出成员：移除该成员后全量更新 */
async function removeMember(m: any) {
  uni.showModal({
    title: '确认移除',
    content: `确定将「${m.userName}」移出空间吗？`,
    success: async (res) => {
      if (!res.confirm) return
      const keepIds = members.value
        .filter((x: any) => x.userId !== m.userId)
        .map((x: any) => x.userId)
      try {
        await setWikiSpaceMembers(spaceId.value, keepIds, 'View')
        uni.showToast({ title: '已移除', icon: 'success' })
        loadMembers()
      } catch {
        uni.showToast({ title: '操作失败', icon: 'none' })
      }
    },
  })
}

/** 退出空间（把自己从成员里移除） */
async function handleLeaveSpace() {
  uni.showModal({
    title: '确认退出',
    content: '确定退出此知识库空间吗？',
    success: async (res) => {
      if (!res.confirm) return
      const myId = useAuthStore().userInfo?.id
      if (!myId) return
      const keepIds = members.value
        .filter((x: any) => x.userId !== myId)
        .map((x: any) => x.userId)
      try {
        await setWikiSpaceMembers(spaceId.value, keepIds, 'View')
        uni.showToast({ title: '已退出空间', icon: 'success' })
        uni.navigateBack()
      } catch {
        uni.showToast({ title: '操作失败', icon: 'none' })
      }
    },
  })
}

async function loadMembers() {
  try {
    const res: any = await getWikiSpaceMembers(spaceId.value)
    members.value = Array.isArray(res) ? res : []
  } catch {
    members.value = []
  }
}

function selectNode(node: any) {
  selectedNodeId.value = node.id
  if (!node.documentId) {
    uni.showToast({ title: '文档数据异常，请重新创建', icon: 'none' })
    return
  }
  uni.navigateTo({
    url: `/pages/documents/edit?id=${node.documentId}&title=${encodeURIComponent(node.title)}`,
  })
}

// ===== ⋮ 菜单 =====
function showNodeMenu(node: any) {
  actionNode.value = node
  showActionSheet.value = true
}

function closeNodeMenu() {
  showActionSheet.value = false
  actionNode.value = null
}

function doRename() {
  const node = actionNode.value
  if (!node) return
  showActionSheet.value = false
  editingNode.value = node
  parentIdForNew.value = node.parentId
  nodeForm.value = { title: node.title }
  showNodeModal.value = true
}

function doDelete() {
  const node = actionNode.value
  if (!node) return
  showActionSheet.value = false
  actionNode.value = null
  uni.showModal({
    title: '确认删除',
    content: `确定删除「${node.title}」吗？`,
    success: async (res) => {
      if (res.confirm) {
        try {
          await deleteWikiNode(spaceId.value, node.id)
          uni.showToast({ title: '已删除', icon: 'success' })
          loadSpace()
        } catch {
          uni.showToast({ title: '删除失败', icon: 'none' })
        }
      }
    },
  })
}

function openCreateNode() {
  editingNode.value = null
  parentIdForNew.value = null
  nodeForm.value = { title: '' }
  showNodeModal.value = true
}

function openCreateChild(parent: any) {
  editingNode.value = null
  parentIdForNew.value = parent.id
  nodeForm.value = { title: '' }
  showNodeModal.value = true
}

async function submitNode() {
  if (!nodeForm.value.title.trim()) return
  try {
    if (editingNode.value) {
      await updateWikiNode(spaceId.value, editingNode.value.id, {
        title: nodeForm.value.title.trim(),
        parentId: editingNode.value.parentId || undefined,
        sortOrder: editingNode.value.sortOrder,
      })
      uni.showToast({ title: '重命名成功', icon: 'success' })
    } else {
      const doc: any = await createDocument({ title: nodeForm.value.title.trim(), content: '' })
      const docId = doc?.id || doc?.Id
      if (!docId) {
        uni.showToast({ title: '文档创建失败', icon: 'none' })
        return
      }
      await createWikiNode(spaceId.value, {
        title: nodeForm.value.title.trim(),
        parentId: parentIdForNew.value || undefined,
        documentId: docId,
        sortOrder: 0,
      })
      uni.showToast({ title: '创建成功', icon: 'success' })
    }
    showNodeModal.value = false
    loadSpace()
  } catch {
    uni.showToast({ title: '操作失败', icon: 'none' })
  }
}

onLoad((query) => {
  if (query?.id) spaceId.value = query.id as string
  loadSpace()
  loadMembers()
})

// 从编辑器返回时刷新节点列表，确保 documentId 最新
onShow(() => {
  if (spaceId.value) loadSpace()
})
</script>

<style scoped>
.space-container { min-height: 100vh; background: #f6f8fc; }

.space-header {
  background: linear-gradient(135deg, #7c4dff 0%, #b388ff 100%);
  padding: 28rpx 28rpx 34rpx;
  color: #fff;
}
.space-title-area {
  display: flex; align-items: center; gap: 16rpx;
  margin-bottom: 8rpx;
}
.space-title { font-size: 36rpx; font-weight: 800; }
.space-visibility {
  font-size: 20rpx; background: rgba(255,255,255,0.2);
  padding: 4rpx 16rpx; border-radius: 999rpx;
}
.space-desc { font-size: 24rpx; opacity: 0.85; display: block; margin-bottom: 20rpx; }
.header-actions { display: flex; gap: 14rpx; }
.header-btn {
  height: 60rpx; line-height: 60rpx; padding: 0 22rpx;
  background: rgba(255,255,255,0.2); color: #fff;
  font-size: 24rpx; border-radius: 30rpx; border: none;
}
.header-btn-exit { background: rgba(239,68,68,0.35); }

.node-tree { margin: 24rpx; }
.tree-title {
  font-size: 28rpx; font-weight: 700; color: #111827;
  margin-bottom: 16rpx; display: block;
}
.tree-item { margin-bottom: 8rpx; }
.tree-item-child { margin-left: 44rpx; }
.tree-node {
  display: flex; align-items: center; background: #fff;
  padding: 20rpx 22rpx; border-radius: 18rpx;
  box-shadow: 0 8rpx 24rpx rgba(31,49,84,0.05);
  gap: 12rpx;
}
.tree-node:active { background: #f3f0ff; }
.tree-node.selected { background: #f3f0ff; border: 1rpx solid #7c4dff; }
.node-icon { font-size: 28rpx; }
.node-title { flex: 1; font-size: 26rpx; color: #374151; font-weight: 500; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.node-actions { display: flex; gap: 8rpx; flex-shrink: 0; }
.node-action {
  width: 44rpx; height: 44rpx; display: flex; align-items: center;
  justify-content: center; border-radius: 12rpx; font-size: 24rpx;
}
.node-action:active { background: #f0edff; }
.node-more { font-size: 30rpx; font-weight: 700; color: #7c4dff; letter-spacing: 2rpx; }

.loading-state { text-align: center; padding: 120rpx 0; color: #7b8494; font-size: 28rpx; }

.empty-state { margin: 24rpx; text-align: center; padding: 120rpx 0; background: #fff; border-radius: 28rpx; }
.empty-icon { font-size: 72rpx; }
.empty-text { font-size: 28rpx; color: #64748b; display: block; margin-top: 16rpx; }
.empty-btn {
  margin-top: 24rpx; height: 72rpx; line-height: 72rpx; padding: 0 36rpx;
  background: #7c4dff; color: #fff; font-size: 26rpx; border-radius: 36rpx; border: none; display: inline-block;
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
.modal-cancel, .modal-confirm, .modal-close-btn {
  flex: 1; height: 72rpx; line-height: 72rpx; font-size: 26rpx;
  border-radius: 36rpx; border: none; text-align: center;
}
.modal-cancel { background: #f6f8fc; color: #374151; }
.modal-confirm { background: #7c4dff; color: #fff; }
.modal-confirm[disabled] { opacity: 0.4; }
.modal-close-btn { width: 100%; background: #f6f8fc; color: #374151; margin-top: 16rpx; }

.member-list { margin-bottom: 16rpx; }
.member-item {
  display: flex; align-items: center; padding: 14rpx 0;
  border-bottom: 1rpx solid #f0f2f5;
}
.member-name { flex: 1; font-size: 26rpx; color: #374151; }
.member-perm { font-size: 22rpx; color: #7c4dff; }
.member-remove { font-size: 24rpx; color: #ef4444; padding: 8rpx; margin-left: 12rpx; }
.member-empty { text-align: center; padding: 32rpx 0; font-size: 24rpx; color: #a8b0c2; }

.add-member-section { margin-top: 20rpx; padding-top: 16rpx; border-top: 1rpx solid #f0f2f5; }
.add-member-title { font-size: 24rpx; font-weight: 600; color: #111827; display: block; margin-bottom: 10rpx; }
.add-member-row { display: flex; gap: 10rpx; align-items: center; }
.add-member-row .form-input { flex: 1; height: 60rpx; font-size: 24rpx; }
.perm-picker {
  height: 60rpx; line-height: 60rpx; padding: 0 16rpx;
  background: #f6f8fc; border-radius: 12rpx; font-size: 22rpx; color: #7c4dff;
  white-space: nowrap;
}
.add-btn {
  height: 60rpx; line-height: 60rpx; padding: 0 18rpx;
  background: #7c4dff; color: #fff; font-size: 22rpx;
  border-radius: 12rpx; border: none; flex-shrink: 0;
}
.add-btn[disabled] { opacity: 0.4; }
.member-candidates { max-height: 200rpx; margin-top: 8rpx; border: 1rpx solid #edf1f7; border-radius: 12rpx; }
.candidate-item { padding: 12rpx 16rpx; display: flex; border-bottom: 1rpx solid #f0f2f5; }
.candidate-item:active { background: #f3f0ff; }
.candidate-name { font-size: 24rpx; color: #374151; flex: 1; }
.candidate-dept { font-size: 20rpx; color: #7b8494; }
.candidate-empty { text-align: center; padding: 20rpx; font-size: 22rpx; color: #a8b0c2; }

/* ⋮ ActionSheet 样式 */
.action-sheet {
  position: fixed; bottom: 0; left: 0; right: 0;
  background: #fff; border-radius: 28rpx 28rpx 0 0;
  padding: 20rpx 28rpx 48rpx;
  animation: slideUp 0.2s ease;
  z-index: 1000;
}
@keyframes slideUp {
  from { transform: translateY(100%); }
  to { transform: translateY(0); }
}
.action-sheet-title {
  font-size: 28rpx; font-weight: 700; color: #111827;
  display: block; text-align: center; margin-bottom: 20rpx;
  overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
}
.action-sheet-item {
  height: 88rpx; line-height: 88rpx; font-size: 28rpx;
  color: #374151; text-align: center;
  border-bottom: 1rpx solid #f0f2f5;
}
.action-sheet-item:active { background: #f3f0ff; }
.action-danger { color: #ef4444; font-weight: 600; }
.action-sheet-cancel {
  margin-top: 12rpx; height: 80rpx; line-height: 80rpx;
  font-size: 28rpx; color: #7b8494; text-align: center;
  background: #f6f8fc; border-radius: 20rpx;
}
.action-sheet-cancel:active { background: #edf2fb; }
</style>
