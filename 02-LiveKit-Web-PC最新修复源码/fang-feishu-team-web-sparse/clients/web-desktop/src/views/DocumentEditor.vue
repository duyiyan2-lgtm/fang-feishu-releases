<template>
  <div class="flex flex-col h-full bg-white dark:bg-gray-900 transition-colors">
    <!-- 错误兜底：黑屏时能看到具体报错 -->
    <div v-if="loadError" class="flex-1 flex items-center justify-center p-8">
      <div class="max-w-lg text-center">
        <div class="w-16 h-16 mx-auto mb-4 rounded-full bg-red-100 dark:bg-red-900/30 flex items-center justify-center text-red-500 text-2xl">!</div>
        <h2 class="text-lg font-medium text-gray-800 dark:text-gray-100 mb-2">文档加载失败</h2>
        <p class="text-sm text-gray-500 dark:text-gray-400 mb-4">{{ loadError }}</p>
        <button @click="reload" class="px-4 py-2 bg-primary text-white rounded-md text-sm">重新加载</button>
      </div>
    </div>

    <template v-else>
    <div class="h-14 px-6 flex items-center justify-between border-b border-gray-200 dark:border-gray-700 flex-shrink-0">
      <div class="flex items-center space-x-3 flex-1 min-w-0">
        <button @click="$router.back()" class="w-8 h-8 rounded flex items-center justify-center text-gray-500 hover:bg-gray-100 dark:hover:bg-gray-800 dark:text-gray-300 transition">
          <ArrowLeftIcon class="w-4 h-4" />
        </button>
        <DocumentTextIcon v-if="doc" class="w-5 h-5 flex-shrink-0" :style="{ color: doc.color || '#3370FF' }" />
        <DocumentTextIcon v-else class="w-5 h-5 flex-shrink-0" style="color: #3370FF" />
        <input v-model="title" placeholder="无标题文档"
               class="text-base font-medium bg-transparent outline-none border-none flex-1 min-w-0 dark:text-gray-100" />
      </div>
      <div class="flex items-center space-x-2 flex-shrink-0">
        <button @click="rightPanelOpen = !rightPanelOpen"
                :class="['w-8 h-8 rounded flex items-center justify-center transition',
                         rightPanelOpen ? 'bg-primary text-white' : 'text-gray-500 hover:bg-gray-100 dark:hover:bg-gray-800']"
                title="评论与版本">
          <ChatBubbleLeftRightIcon class="w-4 h-4" />
          <span v-if="comments.length" class="ml-1 text-xs">{{ comments.length }}</span>
        </button>
        <span class="text-xs text-gray-500 dark:text-gray-400">{{ savedText }}</span>
        <button class="h-8 px-3 text-sm border border-gray-200 dark:border-gray-700 rounded-md hover:bg-gray-50 dark:hover:bg-gray-800 dark:text-gray-200 transition flex items-center">
          <ShareIcon class="w-3.5 h-3.5 mr-1.5" />分享
        </button>
      </div>
    </div>

    <div class="flex-1 flex overflow-hidden">
      <!-- 编辑器 -->
      <div class="flex-1 overflow-y-auto">
        <div class="max-w-3xl mx-auto px-8 py-10">
          <div v-if="!doc" class="text-center py-20 text-gray-400 text-sm">
            <svg class="animate-spin w-6 h-6 mx-auto mb-2" viewBox="0 0 24 24" fill="none">
              <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="3" stroke-opacity="0.25" />
              <path d="M4 12a8 8 0 018-8v3a5 5 0 00-5 5H4z" fill="currentColor" />
            </svg>
            正在加载文档...
          </div>
          <RichEditor v-else v-model="content" :editable="!!doc" />
        </div>
      </div>

      <!-- 右侧栏 -->
      <transition
        enter-active-class="transition duration-200"
        enter-from-class="opacity-0 translate-x-full"
        enter-to-class="opacity-100 translate-x-0"
        leave-active-class="transition duration-150"
        leave-from-class="opacity-100"
        leave-to-class="opacity-0 translate-x-full">
        <aside v-if="rightPanelOpen"
               class="w-96 border-l border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 flex flex-col flex-shrink-0">
          <div class="flex items-center border-b border-gray-200 dark:border-gray-700 px-2">
            <button @click="rightTab = 'comment'" :class="['flex-1 h-11 text-sm font-medium border-b-2 transition',
                                                            rightTab === 'comment' ? 'border-primary text-primary' : 'border-transparent text-gray-500 hover:text-gray-700']">
              评论 ({{ comments.length }})
            </button>
            <button @click="rightTab = 'version'" :class="['flex-1 h-11 text-sm font-medium border-b-2 transition',
                                                            rightTab === 'version' ? 'border-primary text-primary' : 'border-transparent text-gray-500 hover:text-gray-700']">
              版本 ({{ versions.length }})
            </button>
            <button @click="rightPanelOpen = false" class="w-8 h-8 rounded hover:bg-gray-100 dark:hover:bg-gray-800 flex items-center justify-center text-gray-500">
              <XMarkIcon class="w-4 h-4" />
            </button>
          </div>

          <div v-if="rightTab === 'comment'" class="flex-1 flex flex-col">
            <div class="flex-1 overflow-y-auto p-4 space-y-3">
              <div v-for="c in comments" :key="c.id" class="flex">
                <div class="w-8 h-8 rounded-full text-white text-xs flex items-center justify-center flex-shrink-0" :style="{ background: c.userColor }">{{ c.avatar }}</div>
                <div class="ml-2.5 flex-1 min-w-0">
                  <div class="bg-gray-50 dark:bg-gray-800 rounded-lg px-3 py-2">
                    <div class="text-xs font-medium text-gray-900 dark:text-gray-100">{{ c.user }}</div>
                    <p class="text-sm text-gray-700 dark:text-gray-200 mt-0.5">{{ c.content }}</p>
                  </div>
                  <div class="flex items-center text-xs text-gray-400 mt-1 ml-1 space-x-3">
                    <span>{{ c.time }}</span>
                  </div>
                </div>
              </div>
              <div v-if="!comments.length" class="text-center py-12">
                <ChatBubbleLeftRightIcon class="w-10 h-10 mx-auto text-gray-300 dark:text-gray-700 mb-2" />
                <p class="text-xs text-gray-400">暂无评论</p>
              </div>
            </div>
            <div class="border-t border-gray-200 dark:border-gray-700 p-3 bg-gray-50 dark:bg-gray-800/50">
              <textarea v-model="newComment" rows="2" placeholder="写评论..."
                        class="w-full px-3 py-2 text-sm bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-md outline-none focus:border-primary focus:ring-2 focus:ring-primary/20 dark:text-gray-100 resize-none" />
              <div class="flex justify-end mt-2">
                <button @click="postNewComment" :disabled="!newComment.trim() || sendingComment"
                        class="h-7 px-3 text-xs bg-primary text-white rounded hover:bg-primary-hover disabled:opacity-50 disabled:cursor-not-allowed transition">
                  {{ sendingComment ? '发送中…' : '发送' }}
                </button>
              </div>
            </div>
          </div>

          <div v-else class="flex-1 overflow-y-auto p-3">
            <div class="text-xs text-gray-500 mb-3 px-1">共 {{ versions.length }} 个版本</div>
            <div class="relative pl-6 space-y-3">
              <div class="absolute left-3 top-2 bottom-2 w-px bg-gray-200 dark:bg-gray-700"></div>
              <div v-for="(v, idx) in versions" :key="v.id" class="relative">
                <div :class="['absolute -left-3 w-3 h-3 rounded-full border-2',
                              idx === 0 ? 'bg-primary border-primary' : 'bg-white dark:bg-gray-900 border-gray-300 dark:border-gray-600']"></div>
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg px-3 py-2">
                  <div class="flex items-center justify-between">
                    <span :class="['text-xs font-medium', idx === 0 ? 'text-primary' : 'text-gray-700 dark:text-gray-200']">
                      {{ idx === 0 ? '当前版本' : '历史版本' }}
                    </span>
                    <span class="text-xs text-gray-400">{{ v.time }}</span>
                  </div>
                  <div class="text-xs text-gray-600 dark:text-gray-300 mt-1">{{ v.desc }}</div>
                  <div class="text-xs text-gray-400 mt-0.5">{{ v.time }}</div>
                  <button v-if="idx > 0" @click.stop="onRestoreVersion(v)" :disabled="busyVersionId === v.id"
                          class="mt-1.5 text-xs text-primary hover:underline disabled:opacity-50">
                    {{ busyVersionId === v.id ? '回滚中…' : '回滚到此版本' }}
                  </button>
                </div>
              </div>
              <div v-if="!versions.length" class="text-center py-10">
                <ClockIcon class="w-10 h-10 mx-auto text-gray-300 dark:text-gray-700 mb-2" />
                <p class="text-xs text-gray-400">暂无版本历史</p>
              </div>
            </div>
          </div>
        </aside>
      </transition>
    </div>
    </template>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted, onBeforeUnmount } from 'vue'
import { useRoute } from 'vue-router'
import RichEditor from '@/components/RichEditor.vue'
import { ArrowLeftIcon, DocumentTextIcon, ShareIcon, ChatBubbleLeftRightIcon, ClockIcon, XMarkIcon } from '@heroicons/vue/24/outline'
import dayjs from '@/utils/dayjs'
import { ElMessage } from '@/api/toast'
import { getDocument, updateDocument, postComment, listComments, adaptDocDetail, adaptComment, adaptVersion, restoreVersion } from '@/api/documents'

const route = useRoute()
const docId = route.params.id

const doc = ref(null)
const title = ref('')
const content = ref('<p></p>')
const savedAt = ref(null)
const saving = ref(false)
const rightPanelOpen = ref(true)
const rightTab = ref('comment')
const newComment = ref('')
const sendingComment = ref(false)
const versions = ref([])
const busyVersionId = ref(null)
const comments = ref([])
const loadError = ref('')

let saveTimer

const savedText = computed(() => {
  if (saving.value) return '保存中…'
  if (savedAt.value) return `已自动保存 · ${dayjs(savedAt.value).format('HH:mm')}`
  return '未保存'
})

watch([title, content], () => {
  if (!doc.value) return
  clearTimeout(saveTimer)
  saveTimer = setTimeout(saveDocument, 1500)
}, { deep: true })

async function saveDocument() {
  if (!doc.value) return
  saving.value = true
  try {
    await updateDocument(docId, { title: title.value, content: content.value })
    savedAt.value = Date.now()
    // 重新拉详情，更新版本列表
    await refreshDoc()
  } catch (e) {
    console.error('[doc] save failed', e)
  } finally {
    saving.value = false
  }
}

async function refreshDoc() {
  try {
    const detail = await getDocument(docId)
    const d = adaptDocDetail(detail)
    doc.value = d
    versions.value = d.versions || []
    // 详情里 comments 可能没带，独立拉一次
    await loadComments()
  } catch (e) {
    console.error('[doc] refresh failed', e)
  }
}

async function loadComments() {
  try {
    const list = await listComments(docId)
    comments.value = list.map(adaptComment)
  } catch (e) {
    console.error('[doc] load comments failed', e)
  }
}

async function postNewComment() {
  const text = newComment.value.trim()
  if (!text) return
  sendingComment.value = true
  try {
    const c = await postComment(docId, text)
    // 后端返回 comment 对象，自适应加入
    const newC = {
      id: c.id,
      user: c.userName || c.authorName || '我',
      userColor: pickColor(c.userName || c.authorName || ''),
      avatar: (c.userName || c.authorName || '我')[0],
      content: c.content,
      time: '刚刚'
    }
    comments.value.push(newC)
    newComment.value = ''
  } catch (e) {
    ElMessage({ message: '评论失败', type: 'error' })
  } finally {
    sendingComment.value = false
  }
}

function pickColor(name) {
  const palette = ['#3370FF', '#FF7A45', '#00B96B', '#9F7AEA', '#EB2F96', '#F59E0B', '#11CDEF']
  let h = 0
  for (let i = 0; i < name.length; i++) h = (h * 31 + name.charCodeAt(i)) >>> 0
  return palette[h % palette.length]
}

onMounted(async () => {
  try {
    const detail = await getDocument(docId)
    const d = adaptDocDetail(detail)
    doc.value = d
    title.value = d.title || '无标题文档'
    content.value = d.content || '<p></p>'
    versions.value = d.versions || []
    comments.value = d.comments || []
    // 详情里 comments 可能没带，独立拉一次真后端
    await loadComments()
  } catch (e) {
    const msg = e?.response?.data?.message || e?.message || '未知错误'
    loadError.value = `无法加载文档（id: ${docId}）：${msg}`
    console.error('[doc] load failed:', e)
  }
})

function reload() {
  loadError.value = ''
  window.location.reload()
}

async function onRestoreVersion(v) {
  if (!confirm(`确定回滚到版本「${v.time}」？当前内容会被覆盖。`)) return
  busyVersionId.value = v.id
  try {
    await restoreVersion(docId, v.id)
    ElMessage({ message: '已回滚', type: 'success' })
    await refreshDoc()
  } catch (e) {
    ElMessage({ message: '回滚失败：' + (e?.message || ''), type: 'error' })
  } finally {
    busyVersionId.value = null
  }
}

onBeforeUnmount(() => clearTimeout(saveTimer))
</script>