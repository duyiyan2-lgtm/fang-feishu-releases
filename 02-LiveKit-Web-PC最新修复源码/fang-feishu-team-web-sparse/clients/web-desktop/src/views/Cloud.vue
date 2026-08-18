<template>
  <div class="flex flex-col h-full bg-white dark:bg-gray-900 transition-colors">
    <!-- 工具栏 -->
    <div class="h-14 px-6 flex items-center justify-between border-b border-gray-200 dark:border-gray-700">
      <div class="flex items-center space-x-1.5 text-sm text-gray-600 dark:text-gray-300">
        <FolderIcon class="w-4 h-4" />
        <span class="hover:text-primary cursor-pointer transition-colors">我的空间</span>
        <ChevronRightIcon class="w-3 h-3 text-gray-400" />
        <span class="text-gray-900 dark:text-gray-100">根目录</span>
      </div>
      <div class="flex items-center space-x-2">
        <input ref="fileInput" type="file" multiple class="hidden" @change="onFileSelected" />
        <button @click="view = view === 'trash' ? 'list' : 'trash'; if (view === 'trash') loadTrash()"
                :class="['h-8 px-3 text-sm border rounded-md flex items-center transition',
                         view === 'trash'
                           ? 'border-primary text-primary bg-primary/5'
                           : 'border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-800 dark:text-gray-200']">
          <TrashIcon class="w-4 h-4 mr-1.5" />回收站
        </button>
        <button v-if="view === 'list'" @click="pickFile" :disabled="uploading"
                class="h-8 px-3 text-sm border border-gray-200 dark:border-gray-700 rounded-md hover:bg-gray-50 dark:hover:bg-gray-800 dark:text-gray-200 flex items-center transition disabled:opacity-60">
          <ArrowUpTrayIcon class="w-4 h-4 mr-1.5" />
          {{ uploading ? `上传中 ${uploadProgress}%` : '上传' }}
        </button>
        <button @click="openCreate" class="h-8 px-3 text-sm bg-primary hover:bg-primary-hover text-white rounded-md flex items-center transition">
          <PlusIcon class="w-4 h-4 mr-1.5" />新建
        </button>
      </div>
    </div>

    <!-- 文件网格 -->
    <div class="flex-1 overflow-y-auto px-6 py-4">
      <div v-if="loading" class="text-center py-12 text-sm text-gray-400">
        <svg class="animate-spin w-5 h-5 mx-auto mb-2" viewBox="0 0 24 24" fill="none">
          <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="3" stroke-opacity="0.25" />
          <path d="M4 12a8 8 0 018-8v3a5 5 0 00-5 5H4z" fill="currentColor" />
        </svg>
        加载中…
      </div>

      <!-- 列表视图 -->
      <template v-else-if="view === 'list'">
        <div v-if="files.length === 0" class="text-center py-12">
          <CloudIcon class="w-12 h-12 mx-auto text-gray-300 dark:text-gray-700 mb-2" />
          <p class="text-sm text-gray-400">还没有文件，点击「上传」开始</p>
        </div>
        <div v-else class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-3">
        <div v-for="file in files" :key="file.id"
             class="group relative bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-3 hover:shadow-md hover:border-primary/30 transition-all">
          <div class="aspect-square rounded-md flex items-center justify-center mb-2 relative cursor-pointer"
               :style="{ background: file.color + '15' }"
               @click="preview(file)">
            <component :is="fileTypeIcon(file.type)" class="w-14 h-14" :style="{ color: file.color }" />
            <span class="absolute bottom-1.5 right-1.5 text-[10px] uppercase font-medium px-1.5 py-0.5 rounded text-white"
                  :style="{ background: file.color }">{{ file.type }}</span>
          </div>
          <h4 class="text-sm font-medium text-gray-900 dark:text-gray-100 truncate text-center" :title="file.name">{{ file.name }}</h4>
          <div class="text-center text-xs text-gray-400 mt-1">{{ file.size }}</div>

          <!-- hover 操作 -->
          <div class="absolute top-2 left-2 right-2 flex justify-between opacity-0 group-hover:opacity-100 transition-opacity">
            <button class="w-6 h-6 rounded bg-white dark:bg-gray-700 shadow hover:text-primary flex items-center justify-center"
                    @click.stop="preview(file)" title="预览">
              <EyeIcon class="w-3.5 h-3.5" />
            </button>
            <button class="w-6 h-6 rounded bg-white dark:bg-gray-700 shadow hover:text-primary flex items-center justify-center"
                    @click.stop="openShare(file)" title="分享">
              <ShareIcon class="w-3.5 h-3.5" />
            </button>
            <button class="w-6 h-6 rounded bg-white dark:bg-gray-700 shadow hover:text-primary flex items-center justify-center"
                    @click.stop="download(file)" title="下载">
              <ArrowDownTrayIcon class="w-3.5 h-3.5" />
            </button>
            <button class="w-6 h-6 rounded bg-white dark:bg-gray-700 shadow hover:text-red-500 flex items-center justify-center"
                    @click.stop="del(file)" title="删除">
              <TrashIcon class="w-3.5 h-3.5" />
            </button>
          </div>
        </div>
      </div>
      </template>

      <!-- 回收站视图 -->
      <template v-else>
        <div v-if="trashFiles.length === 0" class="text-center py-12">
          <TrashIcon class="w-12 h-12 mx-auto text-gray-300 dark:text-gray-700 mb-2" />
          <p class="text-sm text-gray-400">回收站是空的</p>
        </div>
        <div v-else class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-3">
          <div v-for="file in trashFiles" :key="file.id"
               class="group relative bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-3 opacity-75 hover:opacity-100 transition-all">
            <div class="aspect-square rounded-md flex items-center justify-center mb-2 relative"
                 :style="{ background: file.color + '15' }">
              <component :is="fileTypeIcon(file.type)" class="w-14 h-14" :style="{ color: file.color }" />
            </div>
            <h4 class="text-sm font-medium text-gray-900 dark:text-gray-100 truncate text-center" :title="file.name">{{ file.name }}</h4>
            <div class="text-center text-xs text-gray-400 mt-1">{{ file.size }}</div>
            <div class="absolute top-2 left-2 right-2 flex justify-between opacity-0 group-hover:opacity-100 transition-opacity">
              <button class="w-6 h-6 rounded bg-white dark:bg-gray-700 shadow hover:text-primary flex items-center justify-center"
                      @click.stop="onRestore(file)" title="还原">
                <ArrowPathIcon class="w-3.5 h-3.5" />
              </button>
              <button class="w-6 h-6 rounded bg-white dark:bg-gray-700 shadow hover:text-red-500 flex items-center justify-center"
                      @click.stop="onPermanentDelete(file)" title="彻底删除">
                <XMarkIcon class="w-3.5 h-3.5" />
              </button>
            </div>
          </div>
        </div>
      </template>
    </div>

    <!-- 存储条（前端统计，因为后端未暴露） -->
    <div class="border-t border-gray-200 dark:border-gray-700 px-6 py-3 bg-gray-50 dark:bg-[#1A1D23]">
      <div class="flex items-center justify-between mb-1.5">
        <span class="text-xs text-gray-500 dark:text-gray-400">
          已使用 <span class="font-medium text-gray-700 dark:text-gray-200">{{ totalSizeText }}</span> · 共 {{ files.length }} 个文件
        </span>
        <span class="text-xs text-primary cursor-pointer hover:underline">升级容量</span>
      </div>
      <div class="w-full h-1.5 bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden">
        <div class="h-full bg-primary rounded-full transition-all" :style="{ width: usagePercent + '%' }"></div>
      </div>
    </div>

    <!-- 预览弹窗 -->
    <transition
      enter-active-class="transition duration-150"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100">
      <div v-if="previewFile" class="fixed inset-0 z-50 bg-black/70 flex items-center justify-center p-8" @click.self="previewFile = null">
        <div class="bg-white dark:bg-gray-900 rounded-lg shadow-2xl w-[800px] max-h-[80vh] flex flex-col">
          <div class="px-5 py-3 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between">
            <h3 class="text-sm font-medium dark:text-gray-100 truncate">{{ previewFile.name }}</h3>
            <button @click="previewFile = null" class="text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 text-lg">✕</button>
          </div>
          <div class="flex-1 overflow-auto p-4 bg-gray-50 dark:bg-gray-800/50">
            <pre v-if="previewContent" class="text-xs font-mono whitespace-pre-wrap break-all dark:text-gray-200">{{ previewContent }}</pre>
            <div v-else-if="previewUrl" class="flex items-center justify-center min-h-[200px]">
              <img v-if="isImage(previewFile.type)" :src="previewUrl" class="max-w-full max-h-[60vh] object-contain" />
              <video v-else-if="previewFile.type === 'video'" :src="previewUrl" controls class="max-w-full max-h-[60vh]"></video>
              <audio v-else-if="previewFile.type === 'audio'" :src="previewUrl" controls class="w-full"></audio>
              <div v-else class="text-center text-sm text-gray-500">该文件类型不支持在线预览，可下载查看</div>
            </div>
            <div v-if="previewLoading" class="text-center py-8 text-sm text-gray-400">加载中…</div>
            <div v-if="previewError" class="text-center py-8 text-sm text-red-500">{{ previewError }}</div>
          </div>
        </div>
      </div>
    </transition>

    <!-- 分享弹窗 -->
    <transition
      enter-active-class="transition duration-150"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100">
      <div v-if="sharingFile" class="fixed inset-0 z-50 bg-black/40 flex items-center justify-center" @click.self="sharingFile = null">
        <div class="bg-white dark:bg-gray-900 rounded-lg shadow-xl w-[480px] p-6">
          <h3 class="text-base font-medium mb-4 dark:text-gray-100">分享 · {{ sharingFile.name }}</h3>
          <div class="mb-4">
            <label class="block text-xs text-gray-500 mb-1.5">当前分享（后端返回）</label>
            <div class="text-xs px-3 py-2 bg-gray-50 dark:bg-gray-800 rounded border border-gray-200 dark:border-gray-700 min-h-[36px]">
              {{ sharingList.length ? sharingList.map(s => s.userId || s.userName || JSON.stringify(s)).join(', ') : '（暂无）' }}
            </div>
          </div>
          <div class="mb-4">
            <label class="block text-xs text-gray-500 mb-1.5">分享给（用户 UUID，多个用逗号）</label>
            <textarea v-model="sharingUserIds" rows="2" placeholder="例：2a77e08f-2da9-413e-a5c4-59cc5295d7ee"
                      class="w-full px-3 py-2 text-xs font-mono border border-gray-200 dark:border-gray-700 rounded-md bg-white dark:bg-gray-800 dark:text-gray-100 outline-none focus:border-primary"></textarea>
            <div class="mt-2 flex items-center gap-2 text-xs">
              <span class="text-gray-500">权限:</span>
              <button @click="sharePerm = 'View'"
                      :class="['px-2 h-6 rounded border',
                               sharePerm === 'View' ? 'border-primary bg-primary/5 text-primary' : 'border-gray-200 dark:border-gray-700']">查看</button>
              <button @click="sharePerm = 'Edit'"
                      :class="['px-2 h-6 rounded border',
                               sharePerm === 'Edit' ? 'border-primary bg-primary/5 text-primary' : 'border-gray-200 dark:border-gray-700']">编辑</button>
              <button @click="saveShares" class="ml-auto h-6 px-3 bg-primary text-white rounded">保存分享</button>
            </div>
          </div>
          <div class="flex justify-end gap-2 pt-2 border-t border-gray-100 dark:border-gray-800">
            <button @click="sharingFile = null" class="h-8 px-4 text-sm border border-gray-200 dark:border-gray-700 rounded">关闭</button>
          </div>
        </div>
      </div>
    </transition>

    <!-- 新建文件夹弹窗 -->
    <transition
      enter-active-class="transition duration-150"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100">
      <div v-if="creating" class="fixed inset-0 z-50 bg-black/30 flex items-center justify-center" @click.self="creating = false">
        <div class="bg-white dark:bg-gray-900 rounded-lg shadow-xl w-[400px] p-6">
          <h3 class="text-base font-medium text-gray-900 dark:text-gray-100 mb-4">新建文件夹</h3>
          <div>
            <label class="block text-xs text-gray-600 dark:text-gray-400 mb-1">文件夹名称 *</label>
            <input v-model="newName" placeholder="如：项目资料"
                   class="w-full h-9 px-3 text-sm border border-gray-200 dark:border-gray-700 rounded-md bg-white dark:bg-gray-800/50 outline-none focus:border-primary focus:ring-2 focus:ring-primary/20 dark:text-gray-100" />
          </div>
          <div class="mt-3 text-xs text-gray-400">
            <FolderIcon class="w-3.5 h-3.5 inline mr-1" />
            将在根目录下创建
          </div>
          <div class="mt-5 flex justify-end space-x-2">
            <button @click="creating = false" class="h-8 px-4 text-sm border border-gray-200 dark:border-gray-700 rounded hover:bg-gray-50 dark:hover:bg-gray-800 dark:text-gray-200">取消</button>
            <button @click="confirmCreate" :disabled="!newName.trim()" class="h-8 px-4 text-sm bg-primary text-white rounded hover:bg-primary-hover disabled:opacity-50 transition">创建</button>
          </div>
        </div>
      </div>
    </transition>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { listFiles, uploadFile, downloadFile, deleteFile, restoreFile, permanentDeleteFile, listTrash, createFolder, previewFile as apiPreviewFile, getShares, setShares, adaptFile } from '@/api/files'
import { ElMessage } from '@/api/toast'
import {
  FolderIcon, ChevronRightIcon, ArrowUpTrayIcon, ArrowDownTrayIcon, ArrowPathIcon, PlusIcon, CloudIcon, TrashIcon,
  PhotoIcon, MusicalNoteIcon, FilmIcon, DocumentTextIcon, ArchiveBoxIcon, DocumentIcon,
  TableCellsIcon, PresentationChartLineIcon, XMarkIcon, ShareIcon, EyeIcon
} from '@heroicons/vue/24/outline'

const fileInput = ref(null)
const loading = ref(true)
const files = ref([])
const uploading = ref(false)
const uploadProgress = ref(0)
const creating = ref(false)
const newName = ref('')

// 预览
const previewFile = ref(null)
const previewUrl = ref(null)
const previewContent = ref('')
const previewLoading = ref(false)
const previewError = ref('')

// 分享
const sharingFile = ref(null)
const sharingList = ref([])
const sharingUserIds = ref('')
const sharePerm = ref('View')

// 视图与回收站
const view = ref('list')  // 'list' | 'trash'
const trashFiles = ref([])

const iconMap = {
  image: PhotoIcon, audio: MusicalNoteIcon, video: FilmIcon,
  doc: DocumentTextIcon, pdf: DocumentIcon, zip: ArchiveBoxIcon,
  sheet: TableCellsIcon, slide: PresentationChartLineIcon
}
function fileTypeIcon(type) { return iconMap[type] || DocumentTextIcon }

const totalBytes = computed(() => files.value.reduce((s, f) => s + (f.sizeBytes || 0), 0))
const totalSizeText = computed(() => {
  const b = totalBytes.value
  if (b < 1024 * 1024) return (b / 1024).toFixed(1) + ' KB'
  if (b < 1024 * 1024 * 1024) return (b / 1024 / 1024).toFixed(2) + ' MB'
  return (b / 1024 / 1024 / 1024).toFixed(2) + ' GB'
})
const usagePercent = computed(() => Math.min(100, (totalBytes.value / (50 * 1024 * 1024 * 1024)) * 100))

onMounted(async () => {
  loading.value = true
  try {
    const list = await listFiles()
    files.value = (list || []).map(adaptFile)
  } catch (e) {
    ElMessage({ message: '加载文件失败', type: 'error' })
  } finally {
    loading.value = false
  }
})

function pickFile() { fileInput.value?.click() }

function openCreate() {
  newName.value = ''
  creating.value = true
}

async function confirmCreate() {
  const name = newName.value.trim()
  if (!name) return
  try {
    await createFolder({ name })
    ElMessage({ message: `文件夹「${name}」已创建`, type: 'success' })
    creating.value = false
    newName.value = ''
    await load()
  } catch (e) {
    ElMessage({ message: '创建失败：' + (e?.message || ''), type: 'error' })
  }
}

async function onFileSelected(e) {
  const fs = Array.from(e.target.files || [])
  if (!fs.length) return
  uploading.value = true
  uploadProgress.value = 0
  for (const f of fs) {
    try {
      const data = await uploadFile(f, (p) => { uploadProgress.value = p })
      files.value.unshift(adaptFile(data))
      ElMessage({ message: `已上传 ${f.name}`, type: 'success' })
    } catch (err) {
      ElMessage({ message: `上传 ${f.name} 失败：${err.message || ''}`, type: 'error' })
    }
  }
  uploading.value = false
  uploadProgress.value = 0
  // 清空 input，允许重复上传同名
  if (fileInput.value) fileInput.value.value = ''
}

async function download(file) {
  try {
    const blob = await downloadFile(file.id)
    const url = window.URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = file.name
    document.body.appendChild(a)
    a.click()
    a.remove()
    window.URL.revokeObjectURL(url)
  } catch (e) {
    ElMessage({ message: '下载失败', type: 'error' })
  }
}

async function del(file) {
  if (!confirm(`确认删除「${file.name}」？`)) return
  try {
    await deleteFile(file.id)
    files.value = files.value.filter(x => x.id !== file.id)
    ElMessage({ message: '已移到回收站', type: 'success' })
  } catch (e) {
    ElMessage({ message: '删除失败：' + (e?.message || ''), type: 'error' })
  }
}

async function loadTrash() {
  loading.value = true
  try {
    const list = await listTrash()
    trashFiles.value = (list || []).map(adaptFile)
  } catch (e) {
    ElMessage({ message: '加载回收站失败：' + (e?.message || ''), type: 'error' })
  } finally {
    loading.value = false
  }
}

async function onRestore(file) {
  try {
    await restoreFile(file.id)
    trashFiles.value = trashFiles.value.filter(x => x.id !== file.id)
    ElMessage({ message: '已还原', type: 'success' })
    await load()
  } catch (e) {
    ElMessage({ message: '还原失败：' + (e?.message || ''), type: 'error' })
  }
}

async function onPermanentDelete(file) {
  if (!confirm(`彻底删除「${file.name}」？此操作不可恢复。`)) return
  try {
    await permanentDeleteFile(file.id)
    trashFiles.value = trashFiles.value.filter(x => x.id !== file.id)
    ElMessage({ message: '已彻底删除', type: 'success' })
  } catch (e) {
    ElMessage({ message: '删除失败：' + (e?.message || ''), type: 'error' })
  }
}

async function preview(file) {
  previewFile.value = file
  previewLoading.value = true
  previewError.value = ''
  previewUrl.value = null
  previewContent.value = ''
  try {
    const blob = await apiPreviewFile(file.id)
    if (blob instanceof Blob) {
      const url = URL.createObjectURL(blob)
      previewUrl.value = url
      // 文本类型：显示内容
      if (file.type === 'doc' || file.type === 'sheet' || /\.(txt|md|csv|json|log)$/i.test(file.name)) {
        try { previewContent.value = await blob.text() } catch {}
      }
    } else if (typeof blob === 'string') {
      previewContent.value = blob
    }
  } catch (e) {
    previewError.value = '预览失败：' + (e?.message || '')
  } finally {
    previewLoading.value = false
  }
}

function isImage(type) { return type === 'image' }

async function openShare(file) {
  sharingFile.value = file
  sharingUserIds.value = ''
  sharePerm.value = 'View'
  sharingList.value = []
  try {
    sharingList.value = await getShares(file.id)
  } catch (e) {
    // 后端可能返回空数组
  }
}

async function saveShares() {
  if (!sharingFile.value) return
  const userIds = sharingUserIds.value.split(/[,\s]+/).map(s => s.trim()).filter(Boolean)
  try {
    await setShares(sharingFile.value.id, { userIds, permission: sharePerm.value })
    ElMessage({ message: '分享已保存', type: 'success' })
    sharingList.value = userIds.map(id => ({ userId: id, permission: sharePerm.value }))
  } catch (e) {
    ElMessage({ message: '保存失败：' + (e?.message || ''), type: 'error' })
  }
}
</script>