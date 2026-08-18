<template>
  <div class="flex h-full bg-white dark:bg-gray-900 transition-colors">
    <div class="w-56 border-r border-gray-200 dark:border-gray-700 p-4 overflow-y-auto flex-shrink-0">
      <h3 class="text-xs text-gray-500 dark:text-gray-400 font-medium px-3 mb-2">分类</h3>
      <div class="space-y-0.5">
        <button v-for="cat in categories" :key="cat.id" @click="activeCat = cat.id"
                :class="['w-full flex items-center px-3 py-2 rounded-md text-sm transition-colors',
                         activeCat === cat.id
                           ? 'bg-primary-50 dark:bg-primary/20 text-primary dark:text-primary-100'
                           : 'text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800']">
          <component :is="cat.icon" class="w-4 h-4 mr-2" />
          <span class="flex-1 text-left">{{ cat.label }}</span>
          <span class="text-xs text-gray-400">{{ cat.count }}</span>
        </button>
      </div>
    </div>

    <div class="flex-1 flex flex-col overflow-hidden">
      <div class="h-14 px-6 flex items-center justify-between border-b border-gray-200 dark:border-gray-700">
        <h2 class="text-base font-medium text-gray-900 dark:text-gray-100">{{ activeCatLabel }}（{{ filteredDocs.length }}）</h2>
        <div class="flex items-center space-x-2">
          <div class="relative">
            <MagnifyingGlassIcon class="absolute left-2.5 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
            <input v-model="search" placeholder="搜索文档"
                   class="h-8 pl-9 pr-3 text-sm bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 rounded-md outline-none focus:ring-2 focus:ring-primary/30 w-48 dark:text-gray-100" />
          </div>
          <button @click="createDoc" class="h-8 px-3 text-sm bg-primary hover:bg-primary-hover text-white rounded-md flex items-center transition">
            <PlusIcon class="w-4 h-4 mr-1" />新建
          </button>
        </div>
      </div>

      <div class="flex-1 overflow-y-auto px-6 py-4">
        <div v-if="loading" class="text-center py-12 text-sm text-gray-400">
          <svg class="animate-spin w-5 h-5 mx-auto mb-2" viewBox="0 0 24 24" fill="none">
            <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="3" stroke-opacity="0.25" />
            <path d="M4 12a8 8 0 018-8v3a5 5 0 00-5 5H4z" fill="currentColor" />
          </svg>
          加载中…
        </div>
        <div v-else-if="filteredDocs.length === 0" class="text-center py-12 text-sm text-gray-400">暂无文档</div>

        <div v-else class="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 gap-4">
          <div v-for="doc in filteredDocs" :key="doc.id"
               :class="['group relative bg-white dark:bg-gray-800 border rounded-lg p-4 transition-all',
                        activeCat === 'trash' ? 'opacity-70 border-gray-200 dark:border-gray-700' : 'cursor-pointer hover:shadow-md hover:border-primary/30 border-gray-200 dark:border-gray-700']"
               @click="activeCat === 'trash' ? null : $router.push(`/documents/${doc.id}`)">
            <div class="absolute top-2 right-2 flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity z-10"
                 @click.stop>
              <button v-if="activeCat !== 'trash'" @click.stop="openShare(doc)" title="分享/可见性" class="w-6 h-6 rounded bg-white dark:bg-gray-700 shadow hover:text-primary flex items-center justify-center">
                <ShareIcon class="w-3.5 h-3.5" />
              </button>
              <button v-if="activeCat === 'trash'" @click.stop="onRestore(doc)" title="恢复" class="w-6 h-6 rounded bg-white dark:bg-gray-700 shadow hover:text-green-500 flex items-center justify-center">
                <ArrowPathIcon class="w-3.5 h-3.5" />
              </button>
              <button @click.stop="activeCat === 'trash' ? onPermanentDelete(doc) : onDelete(doc)" :title="activeCat === 'trash' ? '彻底删除' : '删除'" class="w-6 h-6 rounded bg-white dark:bg-gray-700 shadow hover:text-red-500 flex items-center justify-center">
                <TrashIcon class="w-3.5 h-3.5" />
              </button>
            </div>
            <div class="aspect-[4/3] rounded-md flex items-center justify-center mb-3 relative"
                 :style="{ background: doc.color + '15' }">
              <DocumentTextIcon class="w-10 h-10" :style="{ color: doc.color }" />
            </div>
            <h4 class="text-sm font-medium text-gray-900 dark:text-gray-100 truncate" :title="doc.title">{{ doc.title }}</h4>
            <div class="flex items-center justify-between mt-2 text-xs text-gray-500">
              <span class="truncate">{{ doc.author }}</span>
              <span class="flex-shrink-0">{{ doc.updated }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 分享/可见性弹窗 -->
    <transition
      enter-active-class="transition duration-150"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100">
      <div v-if="sharingDoc" class="fixed inset-0 z-50 bg-black/40 flex items-center justify-center" @click.self="sharingDoc = null">
        <div class="bg-white dark:bg-gray-900 rounded-lg shadow-xl w-[480px] p-6">
          <h3 class="text-base font-medium mb-4 dark:text-gray-100">分享 · {{ sharingDoc.title }}</h3>
          <div class="mb-4">
            <label class="block text-xs text-gray-500 mb-1.5">可见性</label>
            <div class="flex items-center gap-2">
              <button @click="changeVisibility('Private')"
                      :class="['flex-1 h-9 rounded-md text-sm border transition',
                               (sharingVisibility || 'Private') === 'Private'
                                 ? 'border-primary bg-primary/5 text-primary' : 'border-gray-200 dark:border-gray-700 text-gray-700 dark:text-gray-300']">
                🔒 仅自己
              </button>
              <button @click="changeVisibility('Organization')"
                      :class="['flex-1 h-9 rounded-md text-sm border transition',
                               sharingVisibility === 'Organization'
                                 ? 'border-primary bg-primary/5 text-primary' : 'border-gray-200 dark:border-gray-700 text-gray-700 dark:text-gray-300']">
                🏢 全组织
              </button>
            </div>
          </div>
          <div class="mb-4">
            <label class="block text-xs text-gray-500 mb-1.5">协作者（当前 {{ sharingCollab.length }} 人）</label>
            <textarea v-model="sharingCollabInput" rows="2" placeholder="输入用户 UUID（多个用逗号分隔）"
                      class="w-full px-3 py-2 text-xs font-mono border border-gray-200 dark:border-gray-700 rounded-md bg-white dark:bg-gray-800 dark:text-gray-100 outline-none focus:border-primary"></textarea>
            <div class="mt-2 flex items-center gap-2 text-xs">
              <span class="text-gray-500">权限:</span>
              <button @click="sharingPerm = 'View'"
                      :class="['px-2 h-6 rounded border',
                               sharingPerm === 'View' ? 'border-primary bg-primary/5 text-primary' : 'border-gray-200 dark:border-gray-700']">查看</button>
              <button @click="sharingPerm = 'Edit'"
                      :class="['px-2 h-6 rounded border',
                               sharingPerm === 'Edit' ? 'border-primary bg-primary/5 text-primary' : 'border-gray-200 dark:border-gray-700']">编辑</button>
              <button @click="saveCollabs" class="ml-auto h-6 px-3 bg-primary text-white rounded">保存协作者</button>
            </div>
          </div>
          <div class="flex justify-end gap-2 pt-2 border-t border-gray-100 dark:border-gray-800">
            <button @click="sharingDoc = null" class="h-8 px-4 text-sm border border-gray-200 dark:border-gray-700 rounded">关闭</button>
          </div>
        </div>
      </div>
    </transition>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import {
  listDocuments, adaptDocList, createDocument, deleteDocument,
  getCollaborators, setCollaborators, setVisibility
} from '@/api/documents'
import { ElMessage, ElMessageBox } from '@/api/toast'
import {
  StarIcon, ClockIcon, UserGroupIcon, TrashIcon, ShareIcon,
  DocumentTextIcon, PlusIcon, MagnifyingGlassIcon
} from '@heroicons/vue/24/outline'
import { useUserStore } from '@/stores/user'

const router = useRouter()
const userStore = useUserStore()
const loading = ref(true)
const docs = ref([])
const search = ref('')
const activeCat = ref('all')

// 后端没有回收站/软删除概念，删除即物理删除。
// 这里用 localStorage 做一层前端软删除：先「移入回收站」（仅本地隐藏，不调用删除接口），
// 真正调后端 DELETE 的时机是「彻底删除」按钮。
const TRASH_KEY = 'feishu-doc-trash'
const trashedIds = ref(loadTrashedIds())

function loadTrashedIds() {
  try {
    const ids = JSON.parse(localStorage.getItem(TRASH_KEY) || '[]')
    return Array.isArray(ids) ? ids : []
  } catch {
    return []
  }
}

function saveTrashedIds() {
  localStorage.setItem(TRASH_KEY, JSON.stringify(trashedIds.value))
}

// 分享/可见性弹窗
const sharingDoc = ref(null)
const sharingVisibility = ref(null)
const sharingCollab = ref([])
const sharingCollabInput = ref('')
const sharingPerm = ref('View')

const liveDocs = computed(() => docs.value.filter(d => !trashedIds.value.includes(d.id)))
const trashedDocs = computed(() => docs.value.filter(d => trashedIds.value.includes(d.id)))

const categories = computed(() => [
  { id: 'all',    label: '全部文档', icon: DocumentTextIcon, count: liveDocs.value.length },
  { id: 'recent', label: '最近访问', icon: ClockIcon,        count: Math.min(liveDocs.value.length, 10) },
  { id: 'mine',   label: '我创建的', icon: StarIcon,         count: liveDocs.value.filter(d => d.ownerId === userStore.userInfo?.id).length },
  { id: 'shared', label: '与我共享', icon: UserGroupIcon,    count: 0 },
  { id: 'trash',  label: '回收站',   icon: TrashIcon,        count: trashedDocs.value.length }
])

const activeCatLabel = computed(() => categories.value.find(c => c.id === activeCat.value)?.label || '全部文档')

const filteredDocs = computed(() => {
  let list = liveDocs.value
  // 分类过滤
  if (activeCat.value === 'recent') {
    list = [...list].slice(0, 10)  // 按创建时间倒序（mock：取前 10）
  } else if (activeCat.value === 'mine') {
    list = list.filter(d => d.ownerId === userStore.userInfo?.id)
  } else if (activeCat.value === 'shared') {
    list = []  // 后端没「共享」概念
  } else if (activeCat.value === 'trash') {
    list = trashedDocs.value
  }
  // 搜索
  const kw = (search.value || '').trim().toLowerCase()
  if (kw) list = list.filter(d => d.title.toLowerCase().includes(kw) || d.author.toLowerCase().includes(kw))
  return list
})

onMounted(async () => {
  loading.value = true
  try {
    const list = await listDocuments()
    docs.value = (list || []).map(adaptDocList)
  } catch (e) {
    ElMessage({ message: '加载文档失败', type: 'error' })
  } finally {
    loading.value = false
  }
})

async function createDoc() {
  try {
    const doc = await createDocument({ title: '无标题文档', content: '<p></p>' })
    ElMessage({ message: '已创建文档', type: 'success' })
    router.push(`/documents/${doc.id}`)
  } catch (e) {
    ElMessage({ message: '创建失败', type: 'error' })
  }
}

async function onDelete(doc) {
  try {
    await ElMessageBox.confirm(`确定将「${doc.title}」移入回收站？`, '删除文档', { type: 'warning' })
  } catch { return }
  if (!trashedIds.value.includes(doc.id)) {
    trashedIds.value = [...trashedIds.value, doc.id]
    saveTrashedIds()
  }
  ElMessage({ message: '已移入回收站', type: 'success' })
}

function onRestore(doc) {
  trashedIds.value = trashedIds.value.filter(id => id !== doc.id)
  saveTrashedIds()
  ElMessage({ message: '已恢复', type: 'success' })
}

async function onPermanentDelete(doc) {
  try {
    await ElMessageBox.confirm(`确定彻底删除「${doc.title}」？此操作不可恢复。`, '彻底删除', { type: 'warning' })
  } catch { return }
  try {
    await deleteDocument(doc.id)
    docs.value = docs.value.filter(d => d.id !== doc.id)
    trashedIds.value = trashedIds.value.filter(id => id !== doc.id)
    saveTrashedIds()
    ElMessage({ message: '已彻底删除', type: 'success' })
  } catch (e) {
    ElMessage({ message: '删除失败：' + (e?.message || ''), type: 'error' })
  }
}

async function openShare(doc) {
  sharingDoc.value = doc
  sharingVisibility.value = 'Private'
  sharingCollabInput.value = ''
  sharingPerm.value = 'View'
  sharingCollab.value = []
  try {
    const list = await getCollaborators(doc.id)
    sharingCollab.value = list
    if (list.length) sharingCollabInput.value = list.map(c => c.userId || c.id).join(', ')
  } catch (e) {
    // ignore - 后端可能没数据
  }
}

async function changeVisibility(v) {
  if (!sharingDoc.value) return
  try {
    await setVisibility(sharingDoc.value.id, v)
    sharingVisibility.value = v
    ElMessage({ message: v === 'Private' ? '已设为仅自己可见' : '已设为全组织可见', type: 'success' })
  } catch (e) {
    ElMessage({ message: '设置失败：' + (e?.message || ''), type: 'error' })
  }
}

async function saveCollabs() {
  if (!sharingDoc.value) return
  const userIds = sharingCollabInput.value
    .split(/[,\s]+/).map(s => s.trim()).filter(Boolean)
  try {
    await setCollaborators(sharingDoc.value.id, { userIds, permission: sharingPerm.value })
    sharingCollab.value = userIds.map(id => ({ userId: id, permission: sharingPerm.value }))
    ElMessage({ message: '协作者已保存', type: 'success' })
  } catch (e) {
    ElMessage({ message: '保存失败：' + (e?.message || ''), type: 'error' })
  }
}
</script>