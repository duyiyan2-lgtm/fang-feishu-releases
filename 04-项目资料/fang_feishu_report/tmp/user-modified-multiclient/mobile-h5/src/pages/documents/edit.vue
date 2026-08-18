<template>
  <view class="edit-container">
    <!-- 顶部操作栏 -->
    <view class="edit-header">
      <text class="back-btn" @tap="goBack">← 返回</text>
      <text class="save-btn" @tap="handleSave">保存</text>
      <text class="version-btn" @tap="showVersions">版本记录</text>
      <text v-if="docId !== '0' && isOwner" class="meta-btn share-btn" @tap="openShare">🔗 分享</text>
      <text v-if="docId !== '0'" class="delete-btn" @tap="handleDelete">删除</text>
    </view>

    <!-- 标题输入 -->
    <input
      v-model="title"
      class="title-input"
      placeholder="输入文档标题..."
      placeholder-class="placeholder"
    />

    <!-- 正文（简易编辑器 + 格式工具栏） -->
    <view class="editor-toolbar">
      <text class="fmt-btn" @tap="insertFmt('bold')"><b>B</b></text>
      <text class="fmt-btn" @tap="insertFmt('italic')"><i>I</i></text>
      <text class="fmt-btn" @tap="insertFmt('underline')"><u>U</u></text>
      <text class="fmt-btn" @tap="insertFmt('heading')">H</text>
      <text class="fmt-btn" @tap="insertFmt('list')">•</text>
      <text class="fmt-btn" @tap="insertFmt('quote')">❝</text>
      <text class="fmt-btn" @tap="insertFmt('code')">{ }</text>
    </view>
    <textarea
      id="doc-editor"
      v-model="content"
      class="content-editor"
      placeholder="在这里编辑文档正文..."
      placeholder-class="placeholder"
      @input="updateCursorPos"
    />

    <!-- 评论区 -->
    <view class="comment-section">
      <text class="section-title">评论</text>
      <view v-for="c in comments" :key="c.id" class="comment-item">
        <text class="comment-author">{{ c.userName || c.createdBy || '匿名' }}</text>
        <text class="comment-text">{{ c.content }}</text>
        <text class="comment-time">{{ formatTime(c.createdAt) }}</text>
        <text class="comment-delete" @tap="handleDeleteComment(c.id)">🗑️</text>
      </view>
      <view v-if="!comments.length" class="comment-empty">暂无评论</view>
      <view class="comment-input-bar">
        <input
          v-model="commentText"
          class="comment-input"
          placeholder="输入评论..."
          placeholder-class="placeholder"
        />
        <button class="comment-submit" :disabled="!commentText.trim() || commentSubmitting" @tap="handleComment">发表</button>
      </view>
    </view>

    <!-- 版本记录弹窗 -->
    <view v-if="showVersionModal" class="modal-overlay" @tap="showVersionModal = false">
      <view class="modal-popup" @tap.stop>
        <text class="modal-title">版本记录（共 {{ versions.length }} 条）</text>
        <view v-for="(v, i) in versions" :key="v.id" class="version-item">
          <text class="version-info">{{ i === 0 ? '✏️ 最新' : '📄 较早' }} · {{ formatVersionDate(v.createdAt) }}</text>
          <text v-if="v.contentSnapshot" class="version-preview">{{ v.contentSnapshot }}</text>
          <text v-if="i > 0" class="version-restore" @tap="handleRestoreVersion(v.id)">↩ 恢复</text>
        </view>
        <view v-if="!versions.length" class="comment-empty">暂无版本记录</view>
        <button class="modal-close" @tap="showVersionModal = false">关闭</button>
      </view>
    </view>

    <!-- 分享弹窗（可见性设置） -->
    <view v-if="showShareModal" class="modal-overlay" @tap="showShareModal = false">
      <view class="modal-popup" @tap.stop>
        <text class="modal-title">文档可见性</text>
        <view class="share-vis-row">
          <text class="share-vis-label">可见范围</text>
          <view class="share-vis-tags">
            <text class="share-vis-tag" :class="{ active: visibility === 'Organization' }" @tap="setVisibility('Organization')">🔗 他人可见</text>
            <text class="share-vis-tag" :class="{ active: visibility === 'Private' }" @tap="setVisibility('Private')">🔒 仅自己可见</text>
          </view>
        </view>
        <view class="share-hint">
          <text v-if="visibility === 'Organization'">他人可见：组织内所有人都可以查看此文档</text>
          <text v-else>仅自己可见：只有你可以查看此文档</text>
        </view>
        <button class="modal-close" @tap="showShareModal = false">关闭</button>
      </view>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { onLoad, onShareAppMessage } from '@dcloudio/uni-app'
import { getDocument, updateDocument, createDocument, addComment, getVersions, deleteDocument, updateVisibility, restoreVersion, deleteComment } from '@/api/documents'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()

const docId = ref('')
const title = ref('')
const content = ref('')
const comments = ref<any[]>([])
const commentText = ref('')
const versions = ref<any[]>([])
const showVersionModal = ref(false)
const commentSubmitting = ref(false)

// ---- 可见性 ----
const visibility = ref<'Organization' | 'Private'>('Organization')
const ownerId = ref('')
const isOwner = computed(() => ownerId.value && authStore.userInfo?.id === ownerId.value)

// ---- 分享弹窗 ----
const showShareModal = ref(false)

// ---- 格式工具栏 ----
const cursorPos = ref(0)

function updateCursorPos(e: any) {
  cursorPos.value = e.detail?.cursor ?? content.value.length
}

const fmtMap: Record<string, [string, string]> = {
  bold: ['<b>', '</b>'],
  italic: ['<i>', '</i>'],
  underline: ['<u>', '</u>'],
  heading: ['\n## ', '\n'],
  list: ['\n- ', ''],
  quote: ['\n> ', '\n'],
  code: ['`', '`'],
}

function insertFmt(type: string) {
  const pair = fmtMap[type]
  if (!pair) return
  const [open, close] = pair
  const pos = cursorPos.value
  const before = content.value.slice(0, pos)
  const after = content.value.slice(pos)
  content.value = before + open + close + after
  cursorPos.value = pos + open.length
}
async function loadDoc(id: string) {
  try {
    const res: any = await getDocument(id)
    title.value = res.title || ''
    content.value = res.content || ''
    comments.value = Array.isArray(res.comments) ? res.comments : []
    versions.value = Array.isArray(res.versions) ? res.versions : []
    ownerId.value = res.ownerId || ''
    visibility.value = res.visibility === 'Private' ? 'Private' : 'Organization'
  } catch {
    uni.showToast({ title: '加载文档失败', icon: 'none' })
  }
}

/** 设置可见性 */
async function setVisibility(v: 'Organization' | 'Private') {
  if (docId.value === '0' || visibility.value === v) return
  try {
    await updateVisibility(docId.value, v)
    visibility.value = v
    uni.showToast({ title: v === 'Organization' ? '已设为他人可见' : '已设为仅自己可见', icon: 'success' })
  } catch {
    uni.showToast({ title: '设置失败', icon: 'none' })
  }
}

/** 打开分享弹窗 */
function openShare() {
  if (docId.value === '0') return
  showShareModal.value = true
}

/** 恢复历史版本 */
async function handleRestoreVersion(versionId: string) {
  if (docId.value === '0') return
  uni.showModal({
    title: '恢复版本',
    content: '确定要恢复到此版本吗？当前编辑内容将被替换。',
    success: async (res) => {
      if (res.confirm) {
        try {
          await restoreVersion(docId.value, versionId)
          uni.showToast({ title: '已恢复', icon: 'success' })
          showVersionModal.value = false
          // 重新加载文档内容
          await loadDoc(docId.value)
        } catch {
          uni.showToast({ title: '恢复失败', icon: 'none' })
        }
      }
    },
  })
}

/** 删除评论 */
async function handleDeleteComment(commentId: string) {
  if (docId.value === '0') return
  uni.showModal({
    title: '删除评论',
    content: '确定要删除此评论吗？',
    success: async (res) => {
      if (res.confirm) {
        try {
          await deleteComment(docId.value, commentId)
          comments.value = comments.value.filter((c: any) => c.id !== commentId)
          uni.showToast({ title: '评论已删除', icon: 'success' })
        } catch {
          uni.showToast({ title: '删除失败', icon: 'none' })
        }
      }
    },
  })
}

async function handleSave() {
  if (!title.value.trim()) {
    uni.showToast({ title: '请输入文档标题', icon: 'none' })
    return
  }
  try {
    if (docId.value === '0') {
      const res: any = await createDocument({ title: title.value, content: content.value })
      docId.value = res?.id || res?.toString() || ''
      uni.showToast({ title: '创建成功', icon: 'success' })
    } else {
      await updateDocument(docId.value, { title: title.value, content: content.value })
      uni.showToast({ title: '保存成功', icon: 'success' })
      // 保存后刷新版本记录（从版本 API 获取含内容快照的列表）
      const versionsRes: any = await getVersions(docId.value)
      versions.value = Array.isArray(versionsRes) ? versionsRes : versionsRes?.items || versionsRes?.list || []
    }
  } catch {
    uni.showToast({ title: '保存失败', icon: 'none' })
  }
}

async function handleComment() {
  if (!commentText.value.trim() || docId.value === '0' || commentSubmitting.value) return
  commentSubmitting.value = true
  try {
    await addComment(docId.value, { content: commentText.value })
    // 先清空输入框（乐观更新），再重新加载
    commentText.value = ''
    const res: any = await getDocument(docId.value)
    comments.value = Array.isArray(res.comments) ? res.comments : []
  } catch {
    uni.showToast({ title: '评论失败', icon: 'none' })
  } finally {
    commentSubmitting.value = false
  }
}

/** 显示版本记录弹窗并从 API 加载含内容快照的版本列表 */
async function showVersions() {
  showVersionModal.value = true
  if (docId.value !== '0') {
    try {
      const res: any = await getVersions(docId.value)
      versions.value = Array.isArray(res) ? res : res?.items || res?.list || []
    } catch (e) { console.warn('[Doc] load versions failed', e) }
  }
}

async function handleDelete() {
  if (docId.value === '0') return
  uni.showModal({
    title: '确认删除',
    content: '确定要删除此文档吗？删除后不可在列表查看',
    success: async (res) => {
      if (res.confirm) {
        try {
          await deleteDocument(docId.value)
          uni.showToast({ title: '已删除', icon: 'success' })
          setTimeout(() => uni.navigateBack(), 300)
        } catch {
          uni.showToast({ title: '删除失败', icon: 'none' })
        }
      }
    },
  })
}

function goBack() { uni.navigateBack() }

/** 右上角分享配置 */
onShareAppMessage(() => {
  return {
    title: title.value || '仿飞书文档',
    path: `/pages/documents/edit?id=${docId.value}&title=${encodeURIComponent(title.value || '')}`,
  }
})

function formatTime(t: string) {
  if (!t) return ''
  const d = new Date(t)
  return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`
}

function formatVersionDate(t: string) {
  if (!t) return ''
  const d = new Date(t)
  return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')} ${String(d.getHours()).padStart(2,'0')}:${String(d.getMinutes()).padStart(2,'0')}`
}

onLoad((options) => {
  docId.value = options?.id || '0'
  if (options?.title && options.title !== 'undefined') {
    title.value = decodeURIComponent(options.title)
  }
  if (docId.value !== '0') loadDoc(docId.value)
})
</script>

<style scoped>
.edit-container {
  min-height: 100vh;
  background: #f6f8fc;
  display: flex;
  flex-direction: column;
}
.edit-header {
  display: flex;
  align-items: center;
  padding: 18rpx 24rpx;
  background: #fff;
  border-bottom: 1rpx solid #edf1f7;
  gap: 20rpx;
  box-shadow: 0 6rpx 20rpx rgba(31, 49, 84, 0.04);
}
.back-btn {
  font-size: 28rpx;
  color: #1f6fff;
}
.save-btn {
  font-size: 28rpx;
  color: #1f6fff;
  font-weight: 600;
  margin-left: auto;
}
.version-btn {
  font-size: 24rpx;
  color: #64748b;
  background: #f1f6ff;
  padding: 8rpx 16rpx;
  border-radius: 999rpx;
}
.delete-btn {
  font-size: 24rpx;
  color: #ef4444;
  margin-left: 12rpx;
}
.title-input {
  height: 104rpx;
  margin: 24rpx 24rpx 0;
  padding: 0 28rpx;
  font-size: 34rpx;
  font-weight: 800;
  color: #111827;
  background: #fff;
  border-radius: 28rpx 28rpx 0 0;
  border-bottom: 1rpx solid #edf1f7;
}
.content-editor {
  flex: 1;
  min-height: 300rpx;
  margin: 0 24rpx;
  padding: 24rpx 28rpx;
  font-size: 28rpx;
  color: #111827;
  line-height: 1.8;
  background: #fff;
  border-radius: 0 0 28rpx 28rpx;
  box-shadow: 0 14rpx 36rpx rgba(31, 49, 84, 0.06);
}
.placeholder { color: #a8b0c2; }

/* 格式工具栏 */
.editor-toolbar {
  display: flex;
  gap: 8rpx;
  padding: 14rpx 24rpx;
  margin: 0 24rpx;
  background: #fff;
  border-bottom: 1rpx solid #edf1f7;
}
.fmt-btn {
  width: 56rpx;
  height: 48rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #f6f8fc;
  border-radius: 14rpx;
  font-size: 24rpx;
  color: #374151;
  border: 1rpx solid #edf1f7;
}

/* 评论 */
.comment-section {
  margin: 24rpx;
  padding: 26rpx;
  background: #fff;
  border-radius: 28rpx;
  box-shadow: 0 14rpx 36rpx rgba(31, 49, 84, 0.06);
}
.section-title {
  font-size: 28rpx;
  font-weight: 600;
  color: #111827;
  display: block;
  margin-bottom: 16rpx;
}
.comment-item {
  padding: 16rpx 0;
  border-bottom: 1rpx solid #f0f2f5;
}
.comment-author {
  font-size: 24rpx;
  color: #1f6fff;
  font-weight: 500;
  display: block;
}
.comment-text {
  font-size: 26rpx;
  color: #111827;
  display: block;
  margin: 4rpx 0;
}
.comment-time {
  font-size: 20rpx;
  color: #a8b0c2;
}
.comment-empty {
  text-align: center;
  font-size: 24rpx;
  color: #c9cdd4;
  padding: 24rpx 0;
}
.comment-input-bar {
  display: flex;
  gap: 16rpx;
  margin-top: 16rpx;
}
.comment-input {
  flex: 1;
  height: 64rpx;
  background: #f6f8fc;
  border-radius: 32rpx;
  padding: 0 24rpx;
  font-size: 26rpx;
}
.comment-submit {
  height: 64rpx;
  line-height: 64rpx;
  padding: 0 24rpx;
  background: #1f6fff;
  color: #fff;
  font-size: 24rpx;
  border-radius: 32rpx;
  border: none;
  flex-shrink: 0;
}
.comment-submit[disabled] { opacity: 0.4; }

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
  max-height: 70vh;
  background: #fff;
  border-radius: 28rpx;
  padding: 32rpx;
  overflow-y: auto;
}
.modal-title {
  font-size: 32rpx;
  font-weight: 600;
  display: block;
  text-align: center;
  margin-bottom: 20rpx;
}
.version-item {
  padding: 20rpx 0;
  border-bottom: 1rpx solid #f0f2f5;
}
.version-item:last-child { border-bottom: none; }
.version-info { font-size: 24rpx; color: #4e5969; display: block; }
.version-preview {
  font-size: 22rpx;
  color: #a8b0c2;
  margin-top: 4rpx;
  display: block;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.modal-close {
  width: 100%;
  height: 72rpx;
  line-height: 72rpx;
  background: #f6f8fc;
  color: #374151;
  font-size: 26rpx;
  border-radius: 36rpx;
  border: none;
  margin-top: 24rpx;
}

/* meta-btn */
.meta-btn {
  font-size: 22rpx;
  color: #1f6fff;
  background: #f1f6ff;
  padding: 6rpx 14rpx;
  border-radius: 999rpx;
  flex-shrink: 0;
}

/* 版本恢复按钮 */
.version-restore {
  font-size: 22rpx;
  color: #1f6fff;
  display: inline-block;
  margin-top: 6rpx;
  padding: 4rpx 12rpx;
  background: #eef4ff;
  border-radius: 999rpx;
}

/* 评论删除 */
.comment-delete {
  font-size: 22rpx;
  display: inline-block;
  margin-top: 4rpx;
  padding: 2rpx 6rpx;
}

/* 分享弹窗可见性切换 */
.share-vis-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20rpx;
}
.share-vis-label {
  font-size: 26rpx;
  color: #4e5969;
  flex-shrink: 0;
}
.share-vis-tags {
  display: flex;
  gap: 12rpx;
}
.share-vis-tag {
  font-size: 24rpx;
  padding: 8rpx 16rpx;
  border-radius: 999rpx;
  background: #f6f8fc;
  color: #64748b;
  border: 1rpx solid #edf1f7;
}
.share-vis-tag.active {
  background: #eef4ff;
  color: #1f6fff;
  border-color: #1f6fff;
}
.share-hint {
  padding: 20rpx;
  background: #f6f8fc;
  border-radius: 16rpx;
  text-align: center;
}
.share-hint text {
  font-size: 24rpx;
  color: #7b8494;
  line-height: 1.6;
}
</style>
