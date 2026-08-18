<template>
  <div class="flex flex-col h-full bg-white dark:bg-gray-900 transition-colors">
    <div class="h-14 px-6 flex items-center justify-between border-b border-gray-200 dark:border-gray-700 flex-shrink-0">
      <h1 class="text-base font-medium text-gray-900 dark:text-gray-100">知识库</h1>
      <div class="flex items-center space-x-2">
        <input v-model="search" @keydown.enter="doSearch" placeholder="搜索知识库"
               class="h-8 px-3 text-sm bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 rounded-md outline-none focus:ring-2 focus:ring-primary/30 w-56 dark:text-gray-100" />
        <button @click="openCreateSpace" class="h-8 px-3 text-sm bg-primary hover:bg-primary-hover text-white rounded-md flex items-center transition">
          <PlusIcon class="w-4 h-4 mr-1" />新建空间
        </button>
      </div>
    </div>

    <div class="flex-1 overflow-y-auto">
      <div v-if="loading" class="p-12 text-center text-sm text-gray-400">加载中…</div>

      <!-- 搜索结果 -->
      <div v-else-if="searchMode" class="max-w-4xl mx-auto px-6 py-4">
        <div class="flex items-center justify-between mb-3">
          <h2 class="text-sm font-medium text-gray-700 dark:text-gray-300">搜索 "{{ lastKw }}" · {{ searchResults.length }} 条结果</h2>
          <button @click="exitSearch" class="text-xs text-primary hover:underline">返回空间列表</button>
        </div>
        <div v-if="searchResults.length === 0" class="text-center py-10 text-gray-400 text-sm">无匹配结果</div>
        <div v-else class="space-y-2">
          <div v-for="r in searchResults" :key="r.id" class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-4">
            <h3 class="text-sm font-medium text-gray-900 dark:text-gray-100">{{ r.title || r.name || '未命名' }}</h3>
            <p class="mt-1 text-sm text-gray-600 dark:text-gray-300 line-clamp-2">{{ r.content || r.description || '—' }}</p>
          </div>
        </div>
      </div>

      <!-- 空间列表 -->
      <div v-else class="max-w-5xl mx-auto px-6 py-4">
        <div v-if="spaces.length === 0" class="text-center py-20">
          <BookOpenIcon class="w-12 h-12 mx-auto text-gray-300 dark:text-gray-700 mb-3" />
          <p class="text-sm text-gray-500">还没有知识空间</p>
        </div>
        <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
          <div v-for="s in spaces" :key="s.id"
               class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-4 hover:shadow-md hover:border-primary/30 transition-all cursor-pointer"
               @click="openSpace(s)">
            <div class="flex items-start justify-between">
              <div class="flex items-center gap-2">
                <BookOpenIcon class="w-5 h-5 text-primary" />
                <h3 class="text-sm font-medium text-gray-900 dark:text-gray-100">{{ s.name }}</h3>
              </div>
              <button @click.stop="delSpace(s)" class="text-xs text-red-500 hover:underline">删除</button>
            </div>
            <p class="mt-2 text-sm text-gray-600 dark:text-gray-300 line-clamp-2 min-h-[2.5em]">
              {{ s.description || '（无描述）' }}
            </p>
            <div class="mt-2 text-xs text-gray-400">
              {{ s.nodeCount ?? 0 }} 个文档 · 创建于 {{ formatDate(s.createdAt) }}
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 新建空间弹窗 -->
    <transition
      enter-active-class="transition duration-150"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100">
      <div v-if="creating" class="fixed inset-0 z-50 bg-black/30 flex items-center justify-center" @click.self="creating = false">
        <div class="bg-white dark:bg-gray-900 rounded-lg shadow-xl w-[440px] p-6">
          <h3 class="text-base font-medium text-gray-900 dark:text-gray-100 mb-4">新建知识空间</h3>
          <div class="space-y-3">
            <div>
              <label class="block text-xs text-gray-600 dark:text-gray-400 mb-1">空间名称 *</label>
              <input v-model="newName" placeholder="如：产品文档"
                     class="w-full h-9 px-3 text-sm border border-gray-200 dark:border-gray-700 rounded-md bg-white dark:bg-gray-800/50 outline-none focus:border-primary focus:ring-2 focus:ring-primary/20 dark:text-gray-100" />
            </div>
            <div>
              <label class="block text-xs text-gray-600 dark:text-gray-400 mb-1">描述</label>
              <textarea v-model="newDesc" rows="3" placeholder="简短描述这个空间..."
                        class="w-full px-3 py-2 text-sm border border-gray-200 dark:border-gray-700 rounded-md bg-white dark:bg-gray-800/50 outline-none focus:border-primary focus:ring-2 focus:ring-primary/20 dark:text-gray-100"></textarea>
            </div>
          </div>
          <div class="mt-5 flex justify-end space-x-2">
            <button @click="creating = false" class="h-8 px-4 text-sm border border-gray-200 dark:border-gray-700 rounded hover:bg-gray-50 dark:hover:bg-gray-800 dark:text-gray-200">取消</button>
            <button @click="confirmCreate" :disabled="!newName.trim()" class="h-8 px-4 text-sm bg-primary text-white rounded hover:bg-primary-hover disabled:opacity-50 transition">创建</button>
          </div>
        </div>
      </div>
    </transition>

    <!-- 空间详情弹窗（节点列表） -->
    <transition
      enter-active-class="transition duration-150"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100">
      <div v-if="activeSpace" class="fixed inset-0 z-50 bg-black/30 flex items-center justify-center" @click.self="activeSpace = null">
        <div class="bg-white dark:bg-gray-900 rounded-lg shadow-xl w-[600px] max-h-[80vh] flex flex-col">
          <div class="px-6 py-4 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between">
            <div>
              <h3 class="text-base font-medium text-gray-900 dark:text-gray-100">{{ activeSpace.name }}</h3>
              <p class="text-xs text-gray-500 mt-0.5">{{ activeSpace.description || '（无描述）' }}</p>
            </div>
            <button @click="activeSpace = null" class="text-gray-400 hover:text-gray-600">✕</button>
          </div>
          <div class="px-6 py-3 border-b border-gray-100 dark:border-gray-800 flex items-center space-x-2">
            <input v-model="newNodeTitle" @keydown.enter="addNode" placeholder="新建文档标题"
                   class="flex-1 h-8 px-3 text-sm bg-gray-100 dark:bg-gray-800 rounded-md outline-none dark:text-gray-100" />
            <button @click="addNode" :disabled="!newNodeTitle.trim()" class="h-8 px-3 text-sm bg-primary text-white rounded disabled:opacity-50">添加</button>
          </div>
          <div class="flex-1 overflow-y-auto px-6 py-3">
            <div v-if="loadingNodes" class="text-center py-6 text-sm text-gray-400">加载中…</div>
            <div v-else-if="nodes.length === 0" class="text-center py-6 text-sm text-gray-400">还没有文档</div>
            <div v-else class="space-y-1">
              <div v-for="n in nodes" :key="n.id" @click="openNode(n)"
                   class="flex items-center justify-between p-2 hover:bg-gray-50 dark:hover:bg-gray-800 rounded cursor-pointer group">
                <span class="text-sm text-gray-700 dark:text-gray-200 truncate">{{ n.title }}</span>
                <button @click.stop="delNode(n)" class="text-xs text-red-500 hover:underline opacity-0 group-hover:opacity-100">删除</button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </transition>
  </div>
</template>

<script setup>
defineOptions({ name: 'Wiki' })

import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from '@/api/toast'
import {
  listSpaces, createSpace, deleteSpace,
  listNodes, createNode, deleteNode,
  searchWiki
} from '@/api/wiki'
import { createDocument } from '@/api/documents'
import { PlusIcon, BookOpenIcon } from '@heroicons/vue/24/outline'

const router = useRouter()

const spaces = ref([])
const loading = ref(false)
const search = ref('')
const searchMode = ref(false)
const searchResults = ref([])
const lastKw = ref('')

const creating = ref(false)
const newName = ref('')
const newDesc = ref('')

const activeSpace = ref(null)
const nodes = ref([])
const loadingNodes = ref(false)
const newNodeTitle = ref('')

async function load() {
  loading.value = true
  try {
    spaces.value = await listSpaces()
  } catch (e) {
    ElMessage({ message: '加载知识库失败：' + (e?.message || ''), type: 'error' })
  } finally {
    loading.value = false
  }
}

function openCreateSpace() {
  newName.value = ''
  newDesc.value = ''
  creating.value = true
}

async function confirmCreate() {
  if (!newName.value.trim()) return
  try {
    await createSpace({ name: newName.value.trim(), description: newDesc.value.trim() })
    creating.value = false
    ElMessage({ message: '已创建', type: 'success' })
    await load()
  } catch (e) {
    ElMessage({ message: '创建失败：' + (e?.message || ''), type: 'error' })
  }
}

async function delSpace(s) {
  try { await ElMessageBox.confirm('确定删除空间「' + s.name + '」？所有文档也会被删除。', '删除空间', { type: 'warning' }) }
  catch { return }
  try {
    await deleteSpace(s.id)
    spaces.value = spaces.value.filter(x => x.id !== s.id)
    ElMessage({ message: '已删除', type: 'success' })
  } catch (e) {
    ElMessage({ message: '删除失败：' + (e?.message || ''), type: 'error' })
  }
}

async function openSpace(s) {
  activeSpace.value = s
  await loadNodes(s.id)
}

async function loadNodes(spaceId) {
  loadingNodes.value = true
  try {
    nodes.value = await listNodes(spaceId)
  } catch (e) {
    ElMessage({ message: '加载文档失败：' + (e?.message || ''), type: 'error' })
  } finally {
    loadingNodes.value = false
  }
}

async function addNode() {
  const title = newNodeTitle.value.trim()
  if (!title || !activeSpace.value) return
  try {
    // wiki 节点本身没有正文，需要先建一个真正的文档，再把 documentId 挂到节点上
    // 这样才能复用已有的文档编辑器（DocumentEditor）来编辑/查看内容
    const doc = await createDocument({ title, content: '<p></p>' })
    const n = await createNode(activeSpace.value.id, { title, documentId: doc.id, sortOrder: nodes.value.length })
    nodes.value.push({ ...n, documentId: n.documentId || doc.id })
    newNodeTitle.value = ''
  } catch (e) {
    ElMessage({ message: '创建失败：' + (e?.message || ''), type: 'error' })
  }
}

function openNode(n) {
  if (!n.documentId) {
    ElMessage({ message: '该文档没有关联内容', type: 'warning' })
    return
  }
  activeSpace.value = null
  router.push(`/documents/${n.documentId}`)
}

async function delNode(n) {
  if (!activeSpace.value) return
  try {
    await deleteNode(activeSpace.value.id, n.id)
    nodes.value = nodes.value.filter(x => x.id !== n.id)
  } catch (e) {
    ElMessage({ message: '删除失败：' + (e?.message || ''), type: 'error' })
  }
}

async function doSearch() {
  const kw = search.value.trim()
  if (!kw) { exitSearch(); return }
  lastKw.value = kw
  searchMode.value = true
  try {
    searchResults.value = await searchWiki(kw)
  } catch (e) {
    ElMessage({ message: '搜索失败：' + (e?.message || ''), type: 'error' })
    searchResults.value = []
  }
}

function exitSearch() {
  searchMode.value = false
  searchResults.value = []
  search.value = ''
  lastKw.value = ''
}

function formatDate(iso) {
  if (!iso) return ''
  return new Date(iso).toISOString().slice(0, 10)
}

onMounted(load)
</script>
