<template>
  <div class="flex flex-col h-full">
    <div class="h-14 px-6 flex items-center justify-between border-b border-gray-200 dark:border-gray-700 flex-shrink-0">
      <h2 class="text-base font-medium text-gray-900 dark:text-gray-100">数据字典</h2>
      <button @click="addCategory" class="h-8 px-3 text-sm bg-primary hover:bg-primary-hover text-white rounded-md flex items-center">
        <PlusIcon class="w-4 h-4 mr-1" />新增分类
      </button>
    </div>

    <div class="flex-1 overflow-y-auto">
      <div class="p-6 space-y-4">
        <div v-for="cat in dicts" :key="cat.code" class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden">
          <div class="flex items-center justify-between px-5 py-3 bg-gray-50 dark:bg-gray-900/50 border-b border-gray-200 dark:border-gray-700">
            <div>
              <h3 class="text-sm font-medium text-gray-900 dark:text-gray-100">{{ cat.category }}</h3>
              <span class="text-xs text-gray-400 font-mono">code: {{ cat.code }} · {{ cat.items.length }} 项</span>
            </div>
            <div class="flex items-center space-x-3 text-xs">
              <button @click="addItem(cat)" class="text-primary hover:underline">新增项</button>
              <button @click="editCategory(cat)" class="text-primary hover:underline">编辑</button>
              <button @click="removeCategory(cat)" class="text-red-500 hover:underline">删除</button>
            </div>
          </div>
          <table class="w-full text-sm">
            <thead class="text-xs text-gray-500 dark:text-gray-400 bg-white dark:bg-gray-800">
              <tr>
                <th class="text-left py-2 px-5 font-medium w-16">ID</th>
                <th class="text-left py-2 px-3 font-medium">显示名称</th>
                <th class="text-left py-2 px-3 font-medium">字典值</th>
                <th class="text-left py-2 px-3 font-medium w-20">排序</th>
                <th class="text-left py-2 px-3 font-medium w-32">操作</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="it in cat.items" :key="it.id" class="border-t border-gray-100 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-800/50 transition">
                <td class="py-2.5 px-5 text-xs text-gray-500 font-mono">{{ it.id }}</td>
                <td class="py-2.5 px-3 text-gray-900 dark:text-gray-100">{{ it.label }}</td>
                <td class="py-2.5 px-3"><code class="px-1.5 py-0.5 bg-gray-100 dark:bg-gray-700 rounded text-xs">{{ it.value }}</code></td>
                <td class="py-2.5 px-3 text-gray-500 text-sm">{{ it.sort }}</td>
                <td class="py-2.5 px-3">
                  <button @click="editItem(cat, it)" class="text-xs text-primary hover:underline mr-3">编辑</button>
                  <button @click="removeItem(cat, it)" class="text-xs text-red-500 hover:underline">删除</button>
                </td>
              </tr>
              <tr v-if="cat.items.length === 0"><td colspan="5" class="py-6 text-center text-gray-400 text-sm">暂无字典项</td></tr>
            </tbody>
          </table>
        </div>
        <div v-if="dicts.length === 0" class="text-center py-12 text-sm text-gray-400">暂无分类</div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, watch } from 'vue'
import { mockAdminDict } from '@/api/mock'
import { ElMessage, ElMessageBox } from '@/api/toast'
import { PlusIcon } from '@heroicons/vue/24/outline'

// 后端目前没有数据字典接口，这里只做本地持久化（localStorage），不会同步到服务端
const STORAGE_KEY = 'feishu-admin-dict'

function loadDicts() {
  try {
    const stored = JSON.parse(localStorage.getItem(STORAGE_KEY) || 'null')
    if (Array.isArray(stored)) return stored
  } catch {}
  return JSON.parse(JSON.stringify(mockAdminDict))
}

const dicts = ref(loadDicts())

watch(dicts, (v) => {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(v))
}, { deep: true })

function nextItemId(cat) {
  return cat.items.length ? Math.max(...cat.items.map(i => i.id)) + 1 : 1
}

async function addCategory() {
  try {
    const { value: category } = await ElMessageBox.prompt('请输入分类名称', '新增分类')
    const name = (category || '').trim()
    if (!name) return
    const { value: code } = await ElMessageBox.prompt('请输入分类 code（英文/下划线）', '新增分类')
    const c = (code || '').trim()
    if (!c) return
    if (dicts.value.some(d => d.code === c)) {
      ElMessage({ message: 'code 已存在', type: 'warning' })
      return
    }
    dicts.value.push({ category: name, code: c, items: [] })
    ElMessage({ message: '已新增分类', type: 'success' })
  } catch (e) {
    if (e === 'cancel') return
  }
}

async function editCategory(cat) {
  try {
    const { value } = await ElMessageBox.prompt('请输入新的分类名称', '编辑分类', { inputValue: cat.category })
    const name = (value || '').trim()
    if (!name) return
    cat.category = name
    ElMessage({ message: '已保存', type: 'success' })
  } catch (e) {
    if (e === 'cancel') return
  }
}

async function removeCategory(cat) {
  try {
    await ElMessageBox.confirm(`确定删除分类「${cat.category}」及其所有字典项？`, '删除分类', { type: 'warning' })
  } catch { return }
  dicts.value = dicts.value.filter(d => d.code !== cat.code)
  ElMessage({ message: '已删除', type: 'success' })
}

async function addItem(cat) {
  try {
    const { value: label } = await ElMessageBox.prompt('请输入显示名称', '新增字典项')
    const l = (label || '').trim()
    if (!l) return
    const { value: val } = await ElMessageBox.prompt('请输入字典值', '新增字典项')
    const v = (val || '').trim()
    if (!v) return
    cat.items.push({ id: nextItemId(cat), label: l, value: v, sort: cat.items.length + 1 })
    ElMessage({ message: '已新增', type: 'success' })
  } catch (e) {
    if (e === 'cancel') return
  }
}

async function editItem(cat, it) {
  try {
    const { value: label } = await ElMessageBox.prompt('请输入显示名称', '编辑字典项', { inputValue: it.label })
    const l = (label || '').trim()
    if (!l) return
    const { value: val } = await ElMessageBox.prompt('请输入字典值', '编辑字典项', { inputValue: it.value })
    const v = (val || '').trim()
    if (!v) return
    it.label = l
    it.value = v
    ElMessage({ message: '已保存', type: 'success' })
  } catch (e) {
    if (e === 'cancel') return
  }
}

async function removeItem(cat, it) {
  try {
    await ElMessageBox.confirm(`确定删除「${it.label}」？`, '删除字典项', { type: 'warning' })
  } catch { return }
  cat.items = cat.items.filter(i => i.id !== it.id)
  ElMessage({ message: '已删除', type: 'success' })
}
</script>
