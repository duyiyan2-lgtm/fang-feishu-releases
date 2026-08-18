<template>
  <div class="flex h-full">
    <!-- 左：部门树 -->
    <div class="w-80 border-r border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-[#1A1D23] flex flex-col flex-shrink-0">
      <div class="px-4 py-3 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between">
        <h3 class="text-sm font-medium text-gray-700 dark:text-gray-300">组织架构</h3>
        <button @click="createRoot" class="w-6 h-6 rounded bg-primary text-white hover:bg-primary-hover flex items-center justify-center" title="新增根部门">
          <PlusIcon class="w-3.5 h-3.5" />
        </button>
      </div>
      <div class="flex-1 overflow-y-auto p-2">
        <div v-if="loading" class="text-center py-8 text-xs text-gray-400">加载中…</div>
        <DeptTreeNode v-for="d in deptTree" :key="d.id" :node="d" :level="0"
                      :selected-id="activeId"
                      @select="onSelectDept" />
        <div v-if="!deptTree.length && !loading" class="text-center py-8 text-xs text-gray-400">暂无部门</div>
      </div>
    </div>

    <!-- 右：部门详情 -->
    <div class="flex-1 overflow-y-auto">
      <div class="p-6 max-w-4xl" v-if="active">
        <div class="flex items-center justify-between mb-6">
          <div>
            <h2 class="text-xl font-semibold text-gray-900 dark:text-gray-100">
              <span class="text-2xl mr-2">🏢</span>{{ active.name }}
            </h2>
            <p class="text-sm text-gray-500 mt-1">ID: {{ active.id }} · 排序: {{ active.sortOrder }} · 父级: {{ active.parentId || '顶级' }}</p>
          </div>
          <div class="flex items-center space-x-2">
            <button @click="createChild" class="h-8 px-3 text-sm border border-gray-200 dark:border-gray-700 rounded-md hover:bg-gray-50 dark:hover:bg-gray-800 dark:text-gray-200">新增子部门</button>
            <button @click="editActive" class="h-8 px-3 text-sm border border-gray-200 dark:border-gray-700 rounded-md hover:bg-gray-50 dark:hover:bg-gray-800 dark:text-gray-200">编辑</button>
            <button @click="removeActive" class="h-8 px-3 text-sm border border-red-200 dark:border-red-900 text-red-500 rounded-md hover:bg-red-50">删除</button>
          </div>
        </div>

        <div class="grid grid-cols-3 gap-4 mb-6">
          <div class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-4 flex items-center">
            <div class="w-10 h-10 rounded-lg flex items-center justify-center text-lg mr-3" style="background:#3370FF20">👥</div>
            <div>
              <div class="text-xs text-gray-500">部门成员</div>
              <div class="text-lg font-semibold text-gray-900 dark:text-gray-100">{{ deptMembers.length }}</div>
            </div>
          </div>
          <div class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-4 flex items-center">
            <div class="w-10 h-10 rounded-lg flex items-center justify-center text-lg mr-3" style="background:#00B96B20">🎯</div>
            <div>
              <div class="text-xs text-gray-500">子部门</div>
              <div class="text-lg font-semibold text-gray-900 dark:text-gray-100">{{ (active.children || []).length }}</div>
            </div>
          </div>
          <div class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-4 flex items-center">
            <div class="w-10 h-10 rounded-lg flex items-center justify-center text-lg mr-3" style="background:#F59E0B20">🧑‍💼</div>
            <div>
              <div class="text-xs text-gray-500">层级</div>
              <div class="text-lg font-semibold text-gray-900 dark:text-gray-100">第 {{ depth(active.id) + 1 }} 层</div>
            </div>
          </div>
        </div>

        <div class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-5 mb-4">
          <h3 class="text-sm font-medium text-gray-900 dark:text-gray-100 mb-3">基本信息</h3>
          <dl class="grid grid-cols-2 gap-y-3 text-sm">
            <div><dt class="text-gray-500 inline">部门名称：</dt><dd class="inline text-gray-900 dark:text-gray-100">{{ active.name }}</dd></div>
            <div><dt class="text-gray-500 inline">部门 ID：</dt><dd class="inline font-mono text-xs text-gray-700 dark:text-gray-300">{{ active.id }}</dd></div>
            <div><dt class="text-gray-500 inline">父级：</dt><dd class="inline text-gray-900 dark:text-gray-100">{{ active.parentId || '顶级' }}</dd></div>
            <div><dt class="text-gray-500 inline">排序：</dt><dd class="inline text-gray-900 dark:text-gray-100">{{ active.sortOrder }}</dd></div>
          </dl>
        </div>

        <div class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden">
          <div class="px-5 py-3 bg-gray-50 dark:bg-gray-900/50 border-b border-gray-200 dark:border-gray-700">
            <h3 class="text-sm font-medium text-gray-900 dark:text-gray-100">部门成员（{{ deptMembers.length }}）</h3>
          </div>
          <table class="w-full text-sm">
            <thead class="text-xs text-gray-500"><tr><th class="text-left py-2 px-5 font-medium">姓名</th><th class="text-left py-2 px-3 font-medium">职位</th><th class="text-left py-2 px-3 font-medium">邮箱</th></tr></thead>
            <tbody>
              <tr v-for="u in deptMembers" :key="u.id" class="border-t border-gray-100 dark:border-gray-700">
                <td class="py-2.5 px-5">
                  <div class="flex items-center">
                    <div class="w-7 h-7 rounded-full text-white text-xs flex items-center justify-center mr-2" :style="{ background: u.color }">{{ u.name[0] }}</div>
                    <span class="text-gray-900 dark:text-gray-100">{{ u.name }}</span>
                  </div>
                </td>
                <td class="py-2.5 px-3 text-gray-600 dark:text-gray-300">{{ u.title }}</td>
                <td class="py-2.5 px-3 text-gray-500">{{ u.email }}</td>
              </tr>
              <tr v-if="deptMembers.length === 0"><td colspan="3" class="py-8 text-center text-gray-400 text-sm">暂无成员</td></tr>
            </tbody>
          </table>
        </div>

        <div v-if="(active.children || []).length" class="mt-4 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden">
          <div class="px-5 py-3 bg-gray-50 dark:bg-gray-900/50 border-b border-gray-200 dark:border-gray-700">
            <h3 class="text-sm font-medium text-gray-900 dark:text-gray-100">子部门（{{ active.children.length }}）</h3>
          </div>
          <div class="p-3 grid grid-cols-2 md:grid-cols-3 gap-3">
            <div v-for="c in active.children" :key="c.id" @click="onSelectDept(c.id)"
                 class="p-3 border border-gray-200 dark:border-gray-700 rounded-md hover:border-primary cursor-pointer transition">
              <div class="flex items-center">
                <span class="text-xl mr-2">🏢</span>
                <span class="font-medium text-sm text-gray-900 dark:text-gray-100">{{ c.name }}</span>
              </div>
              <div class="mt-1.5 text-xs text-gray-500">{{ (c.children || []).length }} 子部门</div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, h } from 'vue'
import { getDepartmentTree, listContacts, adaptContact, createDepartment, updateDepartment, deleteDepartment } from '@/api/contacts'
import { ElMessage, ElMessageBox } from '@/api/toast'
import { PlusIcon, ChevronRightIcon } from '@heroicons/vue/24/outline'

const loading = ref(true)
const deptTree = ref([])
const contacts = ref([])

const activeId = ref(null)
const active = computed(() => findNode(deptTree.value, activeId.value))
const deptMembers = computed(() => contacts.value.filter(c => c.departmentId === activeId.value))

function findNode(tree, id) {
  if (!tree || !id) return null
  for (const n of tree) {
    if (n.id === id) return n
    const inner = findNode(n.children, id)
    if (inner) return inner
  }
  return null
}

function depth(id) {
  function dfs(tree, target, d) {
    for (const n of tree) {
      if (n.id === target) return d
      const r = dfs(n.children || [], target, d + 1)
      if (r !== -1) return r
    }
    return -1
  }
  return dfs(deptTree.value, id, 0)
}

function onSelectDept(id) { activeId.value = id }

async function loadTree(selectId) {
  loading.value = true
  try {
    const [tree, list] = await Promise.all([getDepartmentTree(), listContacts()])
    deptTree.value = tree || []
    contacts.value = (list || []).map(adaptContact)
    if (selectId) {
      activeId.value = selectId
    } else if (!findNode(deptTree.value, activeId.value) && deptTree.value.length) {
      activeId.value = findFirstLeafId(deptTree.value)
    }
  } catch (e) {
    ElMessage({ message: '加载部门失败', type: 'error' })
  } finally {
    loading.value = false
  }
}

async function createRoot() {
  try {
    const { value } = await ElMessageBox.prompt('请输入部门名称', '新增根部门', {
      confirmButtonText: '创建', cancelButtonText: '取消'
    })
    const name = (value || '').trim()
    if (!name) return
    const created = await createDepartment({ parentId: null, name, sortOrder: 0 })
    await loadTree(created && created.id)
    ElMessage({ message: '已创建根部门', type: 'success' })
  } catch (e) {
    if (e === 'cancel' || e?.message === 'cancel') return
    ElMessage({ message: '创建失败', type: 'error' })
  }
}

async function createChild() {
  if (!active.value) return
  try {
    const { value } = await ElMessageBox.prompt('请输入部门名称', '新增子部门', {
      confirmButtonText: '创建', cancelButtonText: '取消'
    })
    const name = (value || '').trim()
    if (!name) return
    const created = await createDepartment({ parentId: active.value.id, name, sortOrder: 0 })
    await loadTree(created && created.id)
    ElMessage({ message: '已创建子部门', type: 'success' })
  } catch (e) {
    if (e === 'cancel' || e?.message === 'cancel') return
    ElMessage({ message: '创建失败', type: 'error' })
  }
}

async function editActive() {
  if (!active.value) return
  try {
    const { value } = await ElMessageBox.prompt('请输入新的部门名称', '编辑部门', {
      confirmButtonText: '保存', cancelButtonText: '取消',
      inputValue: active.value.name
    })
    const name = (value || '').trim()
    if (!name) return
    await updateDepartment(active.value.id, {
      parentId: active.value.parentId || null,
      name,
      sortOrder: active.value.sortOrder || 0
    })
    await loadTree(active.value.id)
    ElMessage({ message: '已保存', type: 'success' })
  } catch (e) {
    if (e === 'cancel' || e?.message === 'cancel') return
    ElMessage({ message: '保存失败', type: 'error' })
  }
}

async function removeActive() {
  if (!active.value) return
  try {
    await ElMessageBox.confirm(`确定删除部门「${active.value.name}」吗？此操作不可撤销。`, '删除部门', {
      confirmButtonText: '删除', cancelButtonText: '取消', type: 'warning'
    })
    await deleteDepartment(active.value.id)
    activeId.value = null
    await loadTree()
    ElMessage({ message: '已删除', type: 'success' })
  } catch (e) {
    if (e === 'cancel' || e?.message === 'cancel') return
    ElMessage({ message: '删除失败', type: 'error' })
  }
}

onMounted(() => loadTree())

function findFirstLeafId(tree) {
  if (!tree.length) return null
  const first = tree[0]
  if (first.children && first.children.length) return findFirstLeafId(first.children)
  return first.id
}

const DeptTreeNode = {
  props: ['node', 'level', 'selectedId'],
  emits: ['select'],
  setup(props, { emit }) {
    const expanded = ref(props.level < 1)
    function toggle() { expanded.value = !expanded.value }
    return () => {
      const hasChild = props.node.children && props.node.children.length > 0
      const selected = props.selectedId === props.node.id
      return h('div', {}, [
        h('div', {
          class: ['flex items-center py-1.5 px-2 rounded cursor-pointer text-sm transition min-w-0',
                  selected ? 'bg-primary-50 dark:bg-primary/20 text-primary' : 'hover:bg-gray-100 dark:hover:bg-gray-800 text-gray-700 dark:text-gray-300'],
          style: { paddingLeft: (props.level * 16 + 8) + 'px' },
          onClick: () => emit('select', props.node.id)
        }, [
          hasChild
            ? h(ChevronRightIcon, { class: ['w-3 h-3 mr-1 flex-shrink-0 transition', expanded.value ? 'rotate-90' : ''], onClick: (e) => { e.stopPropagation(); toggle() } })
            : h('span', { class: 'w-3 h-3 mr-1 flex-shrink-0' }),
          h('span', { class: 'text-base mr-2' }, '🏢'),
          h('span', { class: 'flex-1 truncate' }, props.node.name),
          hasChild ? h('span', { class: 'text-xs text-gray-400 ml-2' }, props.node.children.length) : null
        ]),
        expanded.value && hasChild
          ? h('div', {}, props.node.children.map(c => h(DeptTreeNode, { node: c, level: props.level + 1, selectedId: props.selectedId, onSelect: (id) => emit('select', id) })))
          : null
      ])
    }
  }
}
</script>
