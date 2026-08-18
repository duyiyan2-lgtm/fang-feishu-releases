<template>
  <view class="drive-container">
    <!-- ===== Tab 栏 ===== -->
    <view class="tab-bar">
      <text
        class="tab-item"
        :class="{ active: currentTab === 'all' }"
        @tap="switchTab('all')"
      >全部</text>
      <text
        class="tab-item"
        :class="{ active: currentTab === 'trash' }"
        @tap="switchTab('trash')"
      >回收站</text>
    </view>

    <!-- ===================== 全部 Tab ===================== -->
    <template v-if="currentTab === 'all'">
      <!-- 工具栏 -->
      <view class="drive-toolbar">
        <input
          v-model="searchText"
          class="search-input"
          placeholder="搜索文件..."
          placeholder-class="placeholder"
          @input="onSearchInput"
        />
        <button class="tool-btn folder-btn" @tap="showNewFolderModal = true">📁 新建</button>
        <button class="tool-btn upload-btn" @tap="handleUpload">↑ 上传</button>
      </view>

      <!-- 面包屑 -->
      <view class="breadcrumb">
        <text
          v-for="(seg, i) in folderPath"
          :key="i"
          class="crumb-item"
          :class="{ 'crumb-current': i === folderPath.length - 1 }"
          @tap="navigateTo(seg.id, true)"
        >
          <text v-if="i > 0" class="crumb-sep">›</text>
          {{ seg.name }}
        </text>
      </view>

      <!-- 文件夹列表 -->
      <view v-if="folders.length" class="section-label">
        <text class="section-title">文件夹</text>
      </view>
      <view v-if="folders.length" class="folder-grid">
        <view
          v-for="folder in folders"
          :key="folder.id"
          class="folder-card"
        >
          <view
            class="folder-card-body"
            @tap="navigateTo(folder.id, false, folder.name)"
          >
            <text class="folder-card-icon">📁</text>
            <text class="folder-card-name">{{ folder.name }}</text>
          </view>
          <text class="folder-card-more" @tap.stop="showActions(folder)">⋮</text>
        </view>
      </view>

      <!-- 文件列表 -->
      <view v-if="files.length" class="section-label">
        <text class="section-title">文件</text>
      </view>
      <view v-if="files.length" class="file-list">
        <view class="file-header">
          <text class="col-name">文件名</text>
          <text class="col-size">大小</text>
          <text class="col-user">上传人</text>
          <text class="col-date">日期</text>
          <text class="col-actions">操作</text>
        </view>
        <view v-for="file in pagedFiles" :key="file.id" class="file-item" @tap="handleDownloadFile(file)">
          <view class="col-name">
            <text class="file-icon">{{ getFileIcon(file.fileName) }}</text>
            <text class="file-name">{{ file.fileName }}</text>
          </view>
          <text class="col-size">{{ formatSize(file.fileSize) }}</text>
          <text class="col-user">{{ file.uploaderName || '未知' }}</text>
          <text class="col-date">{{ formatDate(file.createdAt) }}</text>
          <view class="col-actions">
            <text class="action-btn more-btn" @tap.stop="showActions(file)">⋮</text>
          </view>
        </view>
      </view>

      <!-- 空状态（当前文件夹下无文件和文件夹） -->
      <view v-if="!files.length && !folders.length" class="empty-state">
        <view class="empty-icon">☁️</view>
        <text class="empty-text">{{ searchText ? '未找到匹配的文件' : '暂无文件' }}</text>
        <text v-if="!searchText" class="empty-hint">点击「上传」或「新建文件夹」开始使用</text>
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
    </template>

    <!-- ===================== 回收站 Tab ===================== -->
    <template v-if="currentTab === 'trash'">
      <view class="trash-tip">
        <text>回收站中的文件 30 天后自动清理</text>
      </view>
      <view v-if="trashFiles.length" class="file-list">
        <view class="file-header">
          <text class="col-name">文件名</text>
          <text class="col-size">大小</text>
          <text class="col-date">删除时间</text>
          <text class="col-actions">操作</text>
        </view>
        <view v-for="file in trashFiles" :key="file.id" class="file-item">
          <view class="col-name">
            <text class="file-icon">{{ getFileIcon(file.fileName) }}</text>
            <text class="file-name file-name-deleted">{{ file.fileName }}</text>
          </view>
          <text class="col-size">{{ formatSize(file.fileSize) }}</text>
          <text class="col-date">{{ formatDate(file.deletedAt) }}</text>
          <view class="col-actions">
            <text class="action-btn restore-btn" @tap.stop="handleRestore(file)">↩ 恢复</text>
            <text class="action-btn perm-delete-btn" @tap.stop="confirmPermanentDelete(file)">✕ 删除</text>
          </view>
        </view>
      </view>
      <view v-else class="empty-state">
        <view class="empty-icon">🗑️</view>
        <text class="empty-text">回收站为空</text>
      </view>
    </template>

    <!-- ==================== 弹窗区域 ==================== -->

    <!-- 操作菜单（⋮）— 文件和文件夹共用，按类型显示菜单项 -->
    <view v-if="showActionSheet && actionTarget" class="modal-overlay" @tap="closeActionSheet">
      <view class="action-sheet" @tap.stop>
        <text class="action-sheet-title">{{ actionTarget.fileName || actionTarget.name }}</text>
        <!-- 文件：下载 -->
        <view v-if="isFileTarget" class="action-sheet-item" @tap="handleDownload">
          <text class="asi-icon">⬇️</text>
          <text>下载到本地</text>
        </view>
        <!-- 文件：分享 -->
        <view v-if="isFileTarget" class="action-sheet-item" @tap="handleShare">
          <text class="asi-icon">🔗</text>
          <text>分享</text>
        </view>
        <!-- 文件：移动到 -->
        <view v-if="isFileTarget" class="action-sheet-item" @tap="handleMove">
          <text class="asi-icon">📂</text>
          <text>移动到</text>
        </view>
        <!-- 文件夹：重命名 -->
        <view v-if="!isFileTarget" class="action-sheet-item" @tap="handleRenameFolder">
          <text class="asi-icon">✏️</text>
          <text>重命名</text>
        </view>
        <!-- 文件夹：移动到 -->
        <view v-if="!isFileTarget" class="action-sheet-item" @tap="handleMove">
          <text class="asi-icon">📂</text>
          <text>移动到</text>
        </view>
        <!-- 全部：删除 -->
        <view class="action-sheet-item action-danger" @tap="handleDeleteAction">
          <text class="asi-icon">🗑</text>
          <text>删除</text>
        </view>
        <view class="action-sheet-cancel" @tap="closeActionSheet">取消</view>
      </view>
    </view>

    <!-- 新建文件夹 -->
    <view v-if="showNewFolderModal" class="modal-overlay" @tap="showNewFolderModal = false">
      <view class="modal-popup" @tap.stop>
        <text class="modal-title">新建文件夹</text>
        <input
          v-model="newFolderName"
          class="modal-input"
          placeholder="请输入文件夹名称"
          maxlength="80"
        />
        <view class="modal-btns">
          <button class="modal-cancel" @tap="showNewFolderModal = false">取消</button>
          <button
            class="modal-confirm"
            :disabled="!newFolderName.trim()"
            @tap="doCreateFolder"
          >创建</button>
        </view>
      </view>
    </view>

    <!-- 文件夹重命名 -->
    <view v-if="showRenameFolderModal" class="modal-overlay" @tap="showRenameFolderModal = false">
      <view class="modal-popup" @tap.stop>
        <text class="modal-title">重命名文件夹</text>
        <input
          v-model="renameFolderName"
          class="modal-input"
          placeholder="请输入新名称"
          maxlength="80"
        />
        <view class="modal-btns">
          <button class="modal-cancel" @tap="showRenameFolderModal = false">取消</button>
          <button
            class="modal-confirm"
            :disabled="!renameFolderName.trim()"
            @tap="doRenameFolder"
          >确认</button>
        </view>
      </view>
    </view>

    <!-- 分享弹窗 -->
    <view v-if="showShareModal" class="modal-overlay" @tap="showShareModal = false">
      <view class="modal-popup share-popup" @tap.stop>
        <text class="modal-title">分享文件</text>
        <text class="share-file-name">{{ actionTarget?.fileName }}</text>

        <input
          v-model="shareKeyword"
          class="modal-input"
          placeholder="搜索用户..."
          @input="onShareSearch"
        />

        <view class="share-user-list">
          <view
            v-for="user in shareCandidates"
            :key="user.id"
            class="share-user-item"
            :class="{ selected: shareSelectedIds.includes(user.id) }"
            @tap="toggleShareUser(user)"
          >
            <view class="sui-avatar">{{ (user.realName || user.username)[0] }}</view>
            <text class="sui-name">{{ user.realName || user.username }}</text>
            <text class="sui-check">{{ shareSelectedIds.includes(user.id) ? '✓' : '' }}</text>
          </view>
          <text v-if="!shareCandidates.length" class="share-empty">
            {{ shareKeyword ? '未找到匹配的用户' : '暂无好友，搜索用户名添加' }}
          </text>
        </view>

        <!-- 权限选择 -->
        <view class="share-permission">
          <text class="sp-label">权限：</text>
          <view class="sp-options">
            <text
              class="sp-opt"
              :class="{ active: sharePermission === 'View' }"
              @tap="sharePermission = 'View'"
            >只读</text>
            <text
              class="sp-opt"
              :class="{ active: sharePermission === 'Edit' }"
              @tap="sharePermission = 'Edit'"
            >可编辑</text>
          </view>
        </view>

        <view class="modal-btns">
          <button class="modal-cancel" @tap="closeShare">取消</button>
          <button
            class="modal-confirm"
            :disabled="!shareSelectedIds.length"
            @tap="doShare"
          >确认分享 ({{ shareSelectedIds.length }})</button>
        </view>
      </view>
    </view>

    <!-- 移动到弹窗 -->
    <view v-if="showMoveModal" class="modal-overlay" @tap="showMoveModal = false">
      <view class="modal-popup move-popup" @tap.stop>
        <text class="modal-title">移动到文件夹</text>
        <view class="move-folder-list">
          <view class="move-folder-item" :class="{ active: moveTargetId === null }" @tap="moveTargetId = null">
            <text class="mfi-icon">📁</text>
            <text>根目录</text>
          </view>
          <view
            v-for="folder in moveFolderList"
            :key="folder.id"
            class="move-folder-item"
            :class="{ active: moveTargetId === folder.id }"
            @tap="moveTargetId = folder.id"
          >
            <text class="mfi-icon">📁</text>
            <text>{{ folder.name }}</text>
          </view>
          <text v-if="!moveFolderList.length" class="share-empty">暂无文件夹</text>
        </view>
        <view class="modal-btns">
          <button class="modal-cancel" @tap="showMoveModal = false">取消</button>
          <button class="modal-confirm" @tap="doMove">确认移动</button>
        </view>
      </view>
    </view>

    <!-- ===== 原有的弹窗 ===== -->

    <!-- 上传弹窗 -->
    <view v-if="showUploadModal" class="modal-overlay" @tap="showUploadModal = false">
      <view class="modal-popup" @tap.stop>
        <text class="modal-title">上传文件</text>

        <!-- 未选文件：两种选择入口 -->
        <view v-if="!selectedFile" class="upload-options">
          <view class="upload-option" @tap="chooseImage">
            <text class="upload-option-icon">📷</text>
            <text class="upload-option-label">拍照 / 相册</text>
          </view>
          <view class="upload-option" @tap="chooseChatFile">
            <text class="upload-option-icon">📁</text>
            <text class="upload-option-label">聊天文件</text>
          </view>
        </view>

        <!-- 已选文件：显示文件名，可换一个 -->
        <view v-else class="selected-file-row">
          <text class="selected-file-icon">{{ isImageFile(selectedFile) ? '🖼' : '📄' }}</text>
          <text class="selected-file-name">{{ selectedFile }}</text>
          <text class="selected-file-change" @tap="resetFilePicker">换一个</text>
        </view>

        <view v-if="uploadProgress > 0" class="progress-bar">
          <view class="progress-fill" :style="{ width: uploadProgress + '%' }" />
          <text class="progress-text">{{ uploadProgress }}%</text>
        </view>
        <view class="modal-btns">
          <button class="modal-cancel" @tap="showUploadModal = false">取消</button>
          <button class="modal-confirm" :disabled="!selectedFile" @tap="confirmUpload">确定上传</button>
        </view>
      </view>
    </view>

    <!-- 删除确认弹窗 -->
    <view v-if="showDeleteModal" class="modal-overlay" @tap="showDeleteModal = false">
      <view class="modal-popup delete-popup" @tap.stop>
        <text class="delete-title">确认删除</text>
        <text class="delete-text">确定要删除「{{ deleteTarget?.fileName }}」吗？</text>
        <text class="delete-hint">删除后可在回收站恢复</text>
        <view class="modal-btns">
          <button class="modal-cancel" @tap="showDeleteModal = false">取消</button>
          <button class="modal-danger" @tap="doDeleteFile">确定删除</button>
        </view>
      </view>
    </view>

    <!-- 永久删除确认弹窗 -->
    <view v-if="showPermDeleteModal" class="modal-overlay" @tap="showPermDeleteModal = false">
      <view class="modal-popup delete-popup" @tap.stop>
        <text class="delete-title">永久删除</text>
        <text class="delete-text">确定要永久删除「{{ permDeleteTarget?.fileName }}」吗？</text>
        <text class="delete-hint">此操作不可恢复</text>
        <view class="modal-btns">
          <button class="modal-cancel" @tap="showPermDeleteModal = false">取消</button>
          <button class="modal-danger" @tap="doPermanentDelete">永久删除</button>
        </view>
      </view>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref, computed, nextTick } from 'vue'
import { onShow, onPullDownRefresh } from '@dcloudio/uni-app'
import {
  getFiles,
  uploadFile,
  deleteFile,
  restoreFile,
  permanentDeleteFile,
  moveFile,
  getTrash,
  getFolders,
  createFolder,
  updateFolder,
  deleteFolder,
  getFileShares,
  setFileShares,
} from '@/api/drive'
import { getFriends, discoverUsers } from '@/api/contacts'
import { BASE_URL } from '@/api/request'

// ==================== 状态 ====================
const currentTab = ref<'all' | 'trash'>('all')

// 文件列表
const files = ref<any[]>([])
const pagedFiles = computed(() => {
  const start = (page.value - 1) * pageSize
  return files.value.slice(start, start + pageSize)
})
const searchText = ref('')
const page = ref(1)
const totalPages = ref(1)
const pageSize = 20

// 文件夹
const folders = ref<any[]>([])
const currentFolderId = ref<string | null>(null)
const folderPath = ref<{ id: string | null; name: string }[]>([{ id: null, name: '全部' }])

// 回收站
const trashFiles = ref<any[]>([])

// 搜索防抖
let searchTimer: ReturnType<typeof setTimeout> | null = null
function onSearchInput() {
  if (searchTimer) clearTimeout(searchTimer)
  searchTimer = setTimeout(() => {
    page.value = 1
    loadFiles()
  }, 300)
}

// ==================== 数据加载 ====================

async function loadFiles() {
  try {
    const params: any = { page: page.value, pageSize }
    if (searchText.value.trim()) params.keyword = searchText.value.trim()
    // 进入文件夹：只显示该文件夹内的文件
    if (currentFolderId.value) params.folderId = currentFolderId.value
    const res: any = await getFiles(params)
    let list = Array.isArray(res) ? res : res?.items || res?.list || []
    // 根目录：只显示无文件夹归属的文件（后端不传 folderId 会返回所有文件）
    if (!currentFolderId.value) {
      list = list.filter((f: any) => f.folderId === null || f.folderId === undefined)
    }
    files.value = list
    totalPages.value = Math.ceil(list.length / pageSize) || 1
  } catch {
    files.value = []
  }
}

async function loadFolders() {
  try {
    const res: any = await getFolders(currentFolderId.value ?? undefined)
    folders.value = Array.isArray(res) ? res : res?.items || res?.list || []
  } catch {
    folders.value = []
  }
}

async function loadTrash() {
  try {
    const res: any = await getTrash()
    trashFiles.value = Array.isArray(res) ? res : res?.items || res?.list || []
  } catch {
    trashFiles.value = []
  }
}

async function loadAll() {
  if (currentTab.value === 'all') {
    await Promise.all([loadFiles(), loadFolders()])
  } else {
    await loadTrash()
  }
}

// ==================== Tab & 导航 ====================

function switchTab(tab: 'all' | 'trash') {
  currentTab.value = tab
  loadAll()
}

function navigateTo(folderId: string | null, fromBreadcrumb = false, folderName?: string) {
  if (folderId === currentFolderId.value) return
  currentFolderId.value = folderId

  if (fromBreadcrumb) {
    // 面包屑导航：截断到点击的位置
    const idx = folderPath.value.findIndex((s) => s.id === folderId)
    if (idx >= 0) {
      folderPath.value = folderPath.value.slice(0, idx + 1)
    } else {
      folderPath.value = [{ id: null, name: '全部' }]
    }
  } else {
    // 点击文件夹进入
    folderPath.value.push({ id: folderId, name: folderName || '文件夹' })
  }

  page.value = 1
  searchText.value = ''
  loadAll()
}

function changePage(p: number) {
  if (p < 1 || p > totalPages.value) return
  page.value = p
  loadFiles()
}

// ==================== 统一操作菜单（⋮） ====================

const showActionSheet = ref(false)
const actionTarget = ref<any>(null)
/** true=文件, false=文件夹 */
const isFileTarget = computed(() => !!(actionTarget.value?.fileName))

function showActions(target: any) {
  actionTarget.value = target
  showActionSheet.value = true
}

function closeActionSheet() {
  showActionSheet.value = false
  actionTarget.value = null
}

// --- 删除（文件和文件夹统一入口）---
const showDeleteModal = ref(false)
const deleteTarget = ref<any>(null)

function handleDeleteAction() {
  showActionSheet.value = false
  if (!actionTarget.value) return
  // 文件夹直接删，文件需要确认
  if (!isFileTarget.value) {
    doDeleteFolder(actionTarget.value)
    return
  }
  deleteTarget.value = actionTarget.value
  nextTick(() => {
    showDeleteModal.value = true
  })
}

async function doDeleteFile() {
  if (!deleteTarget.value) return
  try {
    await deleteFile(deleteTarget.value.id)
    uni.showToast({ title: '已移入回收站', icon: 'success' })
    showDeleteModal.value = false
    deleteTarget.value = null
    loadAll()
  } catch {
    uni.showToast({ title: '删除失败', icon: 'none' })
  }
}

async function doDeleteFolder(folder: any) {
  try {
    await deleteFolder(folder.id)
    uni.showToast({ title: '已删除', icon: 'success' })
    if (actionTarget.value === folder) closeActionSheet()
    await loadFolders()
  } catch (e: any) {
    uni.showToast({ title: e.message || '删除失败（文件夹不为空）', icon: 'none' })
    if (actionTarget.value === folder) closeActionSheet()
  }
}

// --- 文件夹重命名 ---
const showRenameFolderModal = ref(false)
const renameFolderName = ref('')
const renameFolderId = ref<string>('')

function handleRenameFolder() {
  showActionSheet.value = false
  if (!actionTarget.value) return
  renameFolderId.value = actionTarget.value.id
  renameFolderName.value = actionTarget.value.name
  nextTick(() => {
    showRenameFolderModal.value = true
  })
}

async function doRenameFolder() {
  const name = renameFolderName.value.trim()
  if (!name) return
  try {
    await updateFolder(renameFolderId.value, name)
    uni.showToast({ title: '重命名成功', icon: 'success' })
    showRenameFolderModal.value = false
    await loadFolders()
  } catch {
    uni.showToast({ title: '重命名失败', icon: 'none' })
  }
}

// --- 分享 ---
const showShareModal = ref(false)
const shareKeyword = ref('')
const shareCandidates = ref<any[]>([])
const shareSelectedIds = ref<string[]>([])
const sharePermission = ref('View')
let shareSearchTimer: ReturnType<typeof setTimeout> | null = null

async function handleShare() {
  showActionSheet.value = false
  shareKeyword.value = ''
  shareSelectedIds.value = []
  sharePermission.value = 'View'
  shareCandidates.value = []
  // 先加载好友列表
  try {
    const friends: any = await getFriends()
    shareCandidates.value = Array.isArray(friends) ? friends : []
  } catch {
    shareCandidates.value = []
  }
  nextTick(() => {
    showShareModal.value = true
  })
}

function onShareSearch() {
  if (shareSearchTimer) clearTimeout(shareSearchTimer)
  shareSearchTimer = setTimeout(async () => {
    const kw = shareKeyword.value.trim()
    if (!kw) {
      // 恢复显示好友
      try {
        const friends: any = await getFriends()
        shareCandidates.value = Array.isArray(friends) ? friends : []
      } catch {
        shareCandidates.value = []
      }
      return
    }
    try {
      const res: any = await discoverUsers(kw)
      const list = Array.isArray(res) ? res : res?.items || res?.list || []
      shareCandidates.value = list
    } catch {
      shareCandidates.value = []
    }
  }, 300)
}

function toggleShareUser(user: any) {
  const idx = shareSelectedIds.value.indexOf(user.id)
  if (idx >= 0) {
    shareSelectedIds.value.splice(idx, 1)
  } else {
    shareSelectedIds.value.push(user.id)
  }
}

async function doShare() {
  if (!shareSelectedIds.value.length || !actionTarget.value) return
  try {
    await setFileShares(actionTarget.value.id, shareSelectedIds.value, sharePermission.value)
    uni.showToast({ title: '分享成功', icon: 'success' })
    showShareModal.value = false
  } catch {
    uni.showToast({ title: '分享失败', icon: 'none' })
  }
}

function closeShare() {
  showShareModal.value = false
}

// --- 移动到 ---
const showMoveModal = ref(false)
const moveFolderList = ref<any[]>([])
const moveTargetId = ref<string | null>(null)

async function handleMove() {
  showActionSheet.value = false
  moveTargetId.value = null
  try {
    const res: any = await getFolders()
    moveFolderList.value = Array.isArray(res) ? res : res?.items || res?.list || []
  } catch {
    moveFolderList.value = []
  }
  nextTick(() => {
    showMoveModal.value = true
  })
}

async function doMove() {
  if (!actionTarget.value) return
  try {
    if (isFileTarget.value) {
      await moveFile(actionTarget.value.id, moveTargetId.value)
    } else {
      // 文件夹移动到：保留原名，只改 parentId
      await updateFolder(actionTarget.value.id, actionTarget.value.name, moveTargetId.value ?? undefined)
    }
    uni.showToast({ title: '移动成功', icon: 'success' })
    showMoveModal.value = false
    loadAll()
  } catch {
    uni.showToast({ title: '移动失败', icon: 'none' })
  }
}

// ==================== 回收站操作 ====================

async function handleRestore(file: any) {
  try {
    await restoreFile(file.id)
    uni.showToast({ title: '已恢复', icon: 'success' })
    await loadTrash()
  } catch {
    uni.showToast({ title: '恢复失败', icon: 'none' })
  }
}

const showPermDeleteModal = ref(false)
const permDeleteTarget = ref<any>(null)

function confirmPermanentDelete(file: any) {
  permDeleteTarget.value = file
  showPermDeleteModal.value = true
}

async function doPermanentDelete() {
  if (!permDeleteTarget.value) return
  try {
    await permanentDeleteFile(permDeleteTarget.value.id)
    uni.showToast({ title: '已永久删除', icon: 'success' })
    showPermDeleteModal.value = false
    permDeleteTarget.value = null
    await loadTrash()
  } catch {
    uni.showToast({ title: '删除失败', icon: 'none' })
  }
}

// ==================== 文件夹创建 ====================

const showNewFolderModal = ref(false)
const newFolderName = ref('')

async function doCreateFolder() {
  const name = newFolderName.value.trim()
  if (!name) return
  try {
    await createFolder(name, currentFolderId.value ?? undefined)
    uni.showToast({ title: '创建成功', icon: 'success' })
    showNewFolderModal.value = false
    newFolderName.value = ''
    await loadAll()
  } catch {
    uni.showToast({ title: '创建失败', icon: 'none' })
  }
}

// ==================== 上传 ====================

const showUploadModal = ref(false)
const selectedFile = ref('')
const selectedFilePath = ref('')
const uploadProgress = ref(0)

function handleUpload() {
  selectedFile.value = ''
  selectedFilePath.value = ''
  uploadProgress.value = 0
  showUploadModal.value = true
}

/** 拍照 / 相册（图片） */
function chooseImage() {
  // #ifdef MP-WEIXIN
  uni.chooseImage({
    count: 1,
    sizeType: ['original', 'compressed'],
    sourceType: ['camera', 'album'],
    success: (res) => {
      const path = res.tempFilePaths[0]
      selectedFilePath.value = path
      selectedFile.value = res.tempFiles?.[0]?.name || path.substring(path.lastIndexOf('/') + 1) || '已选择图片'
    },
    fail: () => {
      uni.showToast({ title: '未选择图片', icon: 'none' })
    },
  })
  // #endif
  // #ifndef MP-WEIXIN
  uni.chooseImage({
    count: 1,
    sizeType: ['original', 'compressed'],
    sourceType: ['camera', 'album'],
    success: (res) => {
      const path = res.tempFilePaths[0]
      selectedFilePath.value = path
      selectedFile.value = res.tempFiles?.[0]?.name || path.substring(path.lastIndexOf('/') + 1) || '已选择图片'
    },
    fail: () => {},
  })
  // #endif
}

/** 从聊天记录选择文件（任意类型） */
function chooseChatFile() {
  // #ifdef MP-WEIXIN
  uni.chooseMessageFile({
    count: 1,
    type: 'all',
    success: (res) => {
      selectedFilePath.value = res.tempFiles[0].path
      selectedFile.value = res.tempFiles[0]?.name || '已选择文件'
    },
    fail: () => {
      uni.showToast({ title: '文件选择器不可用', icon: 'none' })
    },
  })
  // #endif
  // #ifdef APP-PLUS
  uni.chooseFile({
    count: 1,
    type: 'all',
    success: (res) => {
      selectedFilePath.value = res.tempFilePaths[0]
      selectedFile.value = res.tempFiles[0]?.name || res.tempFilePaths[0].split('/').pop() || '已选择文件'
    },
    fail: () => {
      uni.showToast({ title: '文件选择器不可用', icon: 'none' })
    },
  })
  // #endif
  // #ifndef MP-WEIXIN
  // #ifndef APP-PLUS
  uni.chooseImage({
    count: 1,
    success: (res) => {
      selectedFilePath.value = res.tempFilePaths[0]
      selectedFile.value = res.tempFiles[0]?.name || res.tempFilePaths[0].split('/').pop() || '已选择文件'
    },
    fail: () => {},
  })
  // #endif
  // #endif
}

/** 重置已选文件，重新选择 */
function resetFilePicker() {
  selectedFile.value = ''
  selectedFilePath.value = ''
}

async function confirmUpload() {
  if (!selectedFilePath.value) return
  uploadProgress.value = 0
  try {
    await uploadFile(selectedFilePath.value, (pct) => {
      uploadProgress.value = pct
    }, currentFolderId.value ?? undefined)
    uni.showToast({ title: '上传成功', icon: 'success' })
    showUploadModal.value = false
    loadAll()
  } catch {
    uni.showToast({ title: '上传失败', icon: 'none' })
  }
}

// ==================== 工具函数 ====================

function getFileIcon(name: string): string {
  if (!name) return '📄'
  const ext = name.split('.').pop()?.toLowerCase() || ''
  if (['jpg', 'jpeg', 'png', 'gif', 'webp', 'svg'].includes(ext)) return '🖼'
  if (['zip', 'rar', '7z', 'tar', 'gz'].includes(ext)) return '📦'
  if (['doc', 'docx'].includes(ext)) return '📄'
  if (['xls', 'xlsx'].includes(ext)) return '📊'
  if (['ppt', 'pptx'].includes(ext)) return '📑'
  if (['pdf'].includes(ext)) return '📕'
  return '📄'
}

function formatSize(bytes: number): string {
  if (!bytes) return '-'
  if (bytes < 1024) return bytes + ' B'
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB'
  return (bytes / 1024 / 1024).toFixed(1) + ' MB'
}

function formatDate(t: string) {
  if (!t) return ''
  const d = new Date(t)
  return `${String(d.getMonth() + 1).padStart(2, '0')}/${String(d.getDate()).padStart(2, '0')}`
}

// ==================== 下载 ====================

/** 获取文件名（含后缀），用于 openDocument 判断 */
function getFileName(file: any): string {
  return file?.fileName || `file_${file.id}`
}

/** 根据文件后缀返回 fileType（uni.openDocument 参数） */
function getFileType(fileName: string): string {
  const ext = fileName.split('.').pop()?.toLowerCase() || ''
  if (['doc', 'docx'].includes(ext)) return 'doc'
  if (['xls', 'xlsx'].includes(ext)) return 'xls'
  if (['ppt', 'pptx'].includes(ext)) return 'ppt'
  if (['pdf'].includes(ext)) return 'pdf'
  if (['png', 'jpg', 'jpeg', 'gif', 'bmp', 'webp'].includes(ext)) return 'png'
  return ''
}

/** 判断是否为图片格式 */
function isImageFile(fileName: string): boolean {
  const ext = fileName.split('.').pop()?.toLowerCase() || ''
  return ['png', 'jpg', 'jpeg', 'gif', 'bmp', 'webp'].includes(ext)
}

/** 打开已下载的临时文件（图片→previewImage，文档→openDocument） */
function openDownloadedFile(tempPath: string, fileName: string) {
  if (isImageFile(fileName)) {
    uni.previewImage({
      urls: [tempPath],
      current: tempPath,
      fail: () => {
        uni.showToast({ title: '图片预览失败', icon: 'none' })
      },
    })
  } else {
    uni.openDocument({
      filePath: tempPath,
      showMenu: true,
      success: () => {
        uni.showToast({ title: '已打开，点右上角 ⋮ 可保存', icon: 'none' })
      },
      fail: () => {
        uni.showToast({ title: '该文件暂不能在手机端预览', icon: 'none' })
      },
    })
  }
}

function handleDownloadFile(file: any) {
  const token = uni.getStorageSync('token') || ''
  uni.showLoading({ title: '下载中...' })

  uni.downloadFile({
    url: `${BASE_URL}/files/${file.id}/download`,
    header: { Authorization: `Bearer ${token}` },
    success: (res) => {
      uni.hideLoading()
      if (res.statusCode !== 200) {
        uni.showToast({ title: '下载失败', icon: 'none' })
        return
      }
      openDownloadedFile(res.tempFilePath, getFileName(file))
    },
    fail: () => {
      uni.hideLoading()
      uni.showToast({ title: '下载失败，请检查网络', icon: 'none' })
    },
  })
}

function handleDownload() {
  showActionSheet.value = false
  const file = actionTarget.value
  if (!file) return

  const token = uni.getStorageSync('token') || ''
  uni.showLoading({ title: '下载中...' })

  uni.downloadFile({
    url: `${BASE_URL}/files/${file.id}/download`,
    header: { Authorization: `Bearer ${token}` },
    success: (res) => {
      uni.hideLoading()
      if (res.statusCode !== 200) {
        uni.showToast({ title: '下载失败，服务器异常', icon: 'none' })
        return
      }
      openDownloadedFile(res.tempFilePath, getFileName(file))
    },
    fail: () => {
      uni.hideLoading()
      uni.showToast({ title: '下载失败，请检查网络', icon: 'none' })
    },
  })
}

// ==================== 生命周期 ====================

onShow(() => {
  currentTab.value = 'all'
  currentFolderId.value = null
  folderPath.value = [{ id: null, name: '全部' }]
  loadAll()
})

onPullDownRefresh(() => {
  loadAll()
  uni.stopPullDownRefresh()
})
</script>

<style scoped>
.drive-container {
  min-height: 100vh;
  background: #f6f8fc;
  padding: 0 24rpx 24rpx;
}

/* ===== Tab 栏 ===== */
.tab-bar {
  display: flex;
  background: #fff;
  border-radius: 0 0 28rpx 28rpx;
  padding: 20rpx 16rpx 0;
  box-shadow: 0 10rpx 28rpx rgba(31, 49, 84, 0.06);
  position: sticky;
  top: 0;
  z-index: 10;
}
.tab-item {
  font-size: 28rpx;
  color: #64748b;
  padding: 16rpx 24rpx;
  border-bottom: 4rpx solid transparent;
  font-weight: 500;
}
.tab-item.active {
  color: #1f6fff;
  border-bottom-color: #1f6fff;
}

/* ===== 工具栏 ===== */
.drive-toolbar {
  display: flex;
  gap: 12rpx;
  margin: 16rpx 0;
}
.search-input {
  flex: 1;
  height: 72rpx;
  background: #fff;
  border-radius: 24rpx;
  padding: 0 24rpx;
  font-size: 26rpx;
  box-shadow: 0 10rpx 28rpx rgba(31, 49, 84, 0.06);
  border: 1rpx solid #edf1f7;
}
.placeholder { color: #a8b0c2; }
.tool-btn {
  height: 72rpx;
  line-height: 72rpx;
  padding: 0 20rpx;
  border-radius: 24rpx;
  border: none;
  flex-shrink: 0;
  font-size: 24rpx;
  font-weight: 600;
}
.folder-btn {
  background: #f0f4ff;
  color: #1f6fff;
  box-shadow: 0 8rpx 20rpx rgba(31, 111, 255, 0.08);
}
.upload-btn {
  background: linear-gradient(135deg, #00b8a9, #1fddc5);
  color: #fff;
  box-shadow: 0 12rpx 28rpx rgba(0, 184, 169, 0.18);
}

/* ===== 面包屑 ===== */
.breadcrumb {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  padding: 12rpx 8rpx;
  margin-bottom: 8rpx;
  font-size: 24rpx;
}
.crumb-item {
  color: #1f6fff;
  padding: 4rpx 0;
}
.crumb-sep {
  color: #a8b0c2;
  margin: 0 8rpx;
  font-size: 28rpx;
}
.crumb-current {
  color: #374151;
  font-weight: 500;
}

/* ===== 分区标题 ===== */
.section-label {
  padding: 12rpx 8rpx 8rpx;
}
.section-title {
  font-size: 24rpx;
  color: #64748b;
  font-weight: 500;
}

/* ===== 文件夹网格（flex 布局，兼容性优于 CSS Grid） ===== */
.folder-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 16rpx;
  margin-bottom: 16rpx;
}
.folder-card {
  width: calc((100% - 48rpx) / 4);
  background: #fff;
  border-radius: 20rpx;
  box-shadow: 0 8rpx 20rpx rgba(31, 49, 84, 0.05);
  position: relative;
  overflow: hidden;
}
.folder-card-body {
  padding: 20rpx 12rpx;
  text-align: center;
  cursor: pointer;
}
.folder-card-body:active { background: #f8fbff; }
.folder-card-icon { font-size: 48rpx; display: block; }
.folder-card-name {
  font-size: 22rpx;
  color: #374151;
  margin-top: 8rpx;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.folder-card-more {
  position: absolute;
  bottom: 0;
  right: 0;
  font-size: 32rpx;
  color: #94a3b8;
  width: 52rpx;
  height: 48rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 20rpx 0 20rpx 0;
}
.folder-card-more:active { background: #f0f2f5; }

/* ===== 文件列表 ===== */
.file-list {
  background: #fff;
  border-radius: 28rpx;
  overflow: hidden;
  box-shadow: 0 14rpx 36rpx rgba(31, 49, 84, 0.07);
}
.file-header,
.file-item {
  display: flex;
  align-items: center;
  padding: 20rpx 24rpx;
}
.file-header {
  background: #f8fbff;
  font-size: 22rpx;
  color: #64748b;
  font-weight: 500;
  border-bottom: 1rpx solid #f0f2f5;
}
.file-item {
  border-bottom: 1rpx solid #f0f2f5;
}
.file-item:last-child { border-bottom: none; }
.file-item:active { background: #f8fbff; }
.col-name { flex: 2; display: flex; align-items: center; gap: 8rpx; min-width: 0; }
.col-size { flex: 1; text-align: center; font-size: 22rpx; color: #4b5563; }
.col-user { flex: 1; text-align: center; font-size: 22rpx; color: #4b5563; }
.col-date { flex: 1; text-align: center; font-size: 22rpx; color: #7b8494; }
.col-actions { flex: 1; text-align: center; display: flex; gap: 12rpx; justify-content: center; }
.file-icon {
  width: 54rpx;
  height: 54rpx;
  border-radius: 16rpx;
  background: #eaf2ff;
  color: #1f6fff;
  font-size: 30rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}
.file-name {
  font-size: 26rpx;
  color: #111827;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.file-name-deleted { color: #9ca3af; text-decoration: line-through; }
.action-btn {
  font-size: 24rpx;
  padding: 6rpx 16rpx;
  border-radius: 14rpx;
}
.more-btn { font-size: 32rpx; color: #64748b; background: #f0f2f5; padding: 2rpx 16rpx; }
.restore-btn { color: #00b8a9; background: #e6faf8; }
.perm-delete-btn { color: #ef4444; background: #fff1f2; }

/* ===== 回收站提示 ===== */
.trash-tip {
  text-align: center;
  padding: 20rpx;
  font-size: 22rpx;
  color: #a8b0c2;
}

/* ===== 空状态 ===== */
.empty-state {
  text-align: center;
  padding: 120rpx 0;
  background: #fff;
  border-radius: 28rpx;
  box-shadow: 0 14rpx 36rpx rgba(31, 49, 84, 0.06);
}
.empty-icon { font-size: 72rpx; }
.empty-text { font-size: 28rpx; color: #64748b; display: block; margin-top: 16rpx; }
.empty-hint { font-size: 24rpx; color: #a8b0c2; display: block; margin-top: 8rpx; }

/* ===== 分页 ===== */
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
  border-radius: 12rpx;
}
.page-btn.disabled { color: #a8b0c2; }
.page-info { font-size: 26rpx; color: #7b8494; }

/* ===== 操作菜单（底部弹出） ===== */
.action-sheet {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  background: #fff;
  border-radius: 28rpx 28rpx 0 0;
  padding: 24rpx;
  z-index: 1000;
}
.action-sheet-title {
  font-size: 26rpx;
  color: #7b8494;
  text-align: center;
  display: block;
  padding-bottom: 16rpx;
  border-bottom: 1rpx solid #f0f2f5;
  margin-bottom: 8rpx;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.action-sheet-item {
  display: flex;
  align-items: center;
  gap: 16rpx;
  padding: 24rpx 16rpx;
  font-size: 28rpx;
  color: #111827;
  border-bottom: 1rpx solid #f8f9fb;
}
.action-sheet-item:active { background: #f8fbff; }
.asi-icon { font-size: 32rpx; width: 40rpx; text-align: center; }
.action-danger { color: #ef4444; }
.action-sheet-cancel {
  text-align: center;
  padding: 24rpx;
  margin-top: 12rpx;
  font-size: 28rpx;
  color: #64748b;
  background: #f6f8fc;
  border-radius: 16rpx;
}

/* ===== 弹窗（通用） ===== */
.modal-overlay {
  position: fixed;
  top: 0; left: 0; right: 0; bottom: 0;
  background: rgba(0, 0, 0, 0.45);
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
.modal-title {
  font-size: 32rpx;
  font-weight: 600;
  display: block;
  text-align: center;
  margin-bottom: 24rpx;
}
.modal-input {
  height: 72rpx;
  background: #f6f8fc;
  border-radius: 16rpx;
  padding: 0 20rpx;
  font-size: 26rpx;
  border: 1rpx solid #edf1f7;
  margin-bottom: 20rpx;
}
.modal-btns { display: flex; gap: 20rpx; }
.modal-cancel,
.modal-confirm,
.modal-danger {
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
.modal-danger { background: #ef4444; color: #fff; }

/* ===== 分享弹窗 ===== */
.share-popup { max-height: 75vh; }
.share-file-name {
  font-size: 24rpx;
  color: #7b8494;
  text-align: center;
  display: block;
  margin-bottom: 16rpx;
}
.share-user-list {
  max-height: 300rpx;
  overflow-y: auto;
  margin-bottom: 16rpx;
  border: 1rpx solid #edf1f7;
  border-radius: 16rpx;
}
.share-user-item {
  display: flex;
  align-items: center;
  gap: 16rpx;
  padding: 16rpx 20rpx;
  border-bottom: 1rpx solid #f8f9fb;
}
.share-user-item:active { background: #f0f4ff; }
.share-user-item.selected { background: #f0f4ff; }
.sui-avatar {
  width: 48rpx;
  height: 48rpx;
  border-radius: 50%;
  background: #1f6fff;
  color: #fff;
  font-size: 24rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}
.sui-name { flex: 1; font-size: 26rpx; color: #111827; }
.sui-check { font-size: 28rpx; color: #1f6fff; font-weight: 700; }
.share-empty {
  display: block;
  text-align: center;
  padding: 24rpx;
  font-size: 24rpx;
  color: #a8b0c2;
}
.share-permission {
  display: flex;
  align-items: center;
  gap: 12rpx;
  margin-bottom: 20rpx;
  padding: 0 4rpx;
}
.sp-label { font-size: 26rpx; color: #374151; }
.sp-options { display: flex; gap: 8rpx; }
.sp-opt {
  padding: 8rpx 24rpx;
  border-radius: 28rpx;
  font-size: 24rpx;
  color: #64748b;
  background: #f0f2f5;
}
.sp-opt.active {
  color: #1f6fff;
  background: #eef4ff;
  font-weight: 600;
}

/* ===== 移动到弹窗 ===== */
.move-popup { max-height: 60vh; }
.move-folder-list {
  max-height: 400rpx;
  overflow-y: auto;
  margin-bottom: 20rpx;
  border: 1rpx solid #edf1f7;
  border-radius: 16rpx;
}
.move-folder-item {
  display: flex;
  align-items: center;
  gap: 12rpx;
  padding: 20rpx;
  border-bottom: 1rpx solid #f8f9fb;
  font-size: 26rpx;
}
.move-folder-item:active { background: #f0f4ff; }
.move-folder-item.active { background: #f0f4ff; color: #1f6fff; font-weight: 600; }
.mfi-icon { font-size: 32rpx; }

/* ===== 上传弹窗：两种选择入口 ===== */
.upload-options {
  display: flex;
  gap: 20rpx;
  margin-bottom: 24rpx;
}
.upload-option {
  flex: 1;
  height: 140rpx;
  border: 2rpx dashed #b9cffd;
  border-radius: 24rpx;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  background: #f8fbff;
  gap: 8rpx;
}
.upload-option:active { background: #eef4ff; }
.upload-option-icon { font-size: 44rpx; }
.upload-option-label { font-size: 24rpx; color: #1f6fff; font-weight: 500; }

/* 已选文件行 */
.selected-file-row {
  display: flex;
  align-items: center;
  gap: 12rpx;
  padding: 20rpx 16rpx;
  background: #f8fbff;
  border-radius: 16rpx;
  margin-bottom: 20rpx;
}
.selected-file-icon { font-size: 36rpx; flex-shrink: 0; }
.selected-file-name { flex: 1; font-size: 26rpx; color: #1d2129; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.selected-file-change { font-size: 22rpx; color: #1f6fff; flex-shrink: 0; padding: 4rpx 8rpx; }

.progress-bar {
  height: 32rpx;
  background: #f0f2f5;
  border-radius: 16rpx;
  overflow: hidden;
  position: relative;
  margin-bottom: 20rpx;
}
.progress-fill {
  height: 100%;
  background: linear-gradient(135deg, #00b8a9, #1fddc5);
  border-radius: 16rpx;
  transition: width 0.3s;
}
.progress-text {
  position: absolute;
  top: 0; left: 0; right: 0;
  text-align: center;
  font-size: 20rpx;
  color: #fff;
  line-height: 32rpx;
}

/* ===== 删除弹窗 ===== */
.delete-popup { text-align: center; }
.delete-title { font-size: 32rpx; font-weight: 600; display: block; margin-bottom: 16rpx; }
.delete-text { font-size: 28rpx; color: #111827; display: block; }
.delete-hint { font-size: 24rpx; color: #a8b0c2; display: block; margin: 8rpx 0 24rpx; }
</style>
