<template>
  <div class="flex h-full">
    <!-- 角色列表 -->
    <div class="w-72 border-r border-gray-200 dark:border-gray-700 flex flex-col bg-gray-50 dark:bg-[#1A1D23] flex-shrink-0">
      <div class="px-4 py-3 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between">
        <h3 class="text-sm font-medium text-gray-700 dark:text-gray-300">角色列表 ({{ roles.length }})</h3>
        <button @click="createRole_" class="w-6 h-6 rounded bg-primary text-white hover:bg-primary-hover flex items-center justify-center">
          <PlusIcon class="w-3.5 h-3.5" />
        </button>
      </div>
      <div class="flex-1 overflow-y-auto p-2 space-y-1">
        <div v-for="r in roles" :key="r.id" @click="activeId = r.id"
             :class="['p-3 rounded-md cursor-pointer transition border',
                      activeId === r.id
                        ? 'bg-primary-50 dark:bg-primary/20 border-primary'
                        : 'bg-white dark:bg-gray-800 border-transparent hover:border-gray-200 dark:hover:border-gray-700']">
          <div class="flex items-center justify-between mb-1">
            <span class="font-medium text-sm text-gray-900 dark:text-gray-100">{{ r.name }}</span>
            <span class="text-xs text-gray-500">{{ r.userCount }} 人</span>
          </div>
          <div class="text-xs text-gray-500 truncate">{{ r.desc }}</div>
          <div class="mt-2 text-xs font-mono text-gray-400">code: {{ r.code }}</div>
        </div>
      </div>
    </div>

    <!-- 权限配置 -->
    <div class="flex-1 overflow-y-auto">
      <div class="p-6 max-w-3xl">
        <h2 class="text-base font-medium text-gray-900 dark:text-gray-100 mb-1">{{ activeRole?.name }}</h2>
        <p class="text-sm text-gray-500 mb-5">{{ activeRole?.desc }}</p>

        <h3 class="text-sm font-medium text-gray-700 dark:text-gray-300 mb-3 pb-2 border-b border-gray-200 dark:border-gray-700">权限配置</h3>
        <div class="space-y-4">
          <div v-for="(group, idx) in permissionTree" :key="idx" class="bg-gray-50 dark:bg-gray-800/50 rounded-lg p-4">
            <div class="flex items-center justify-between mb-3">
              <h4 class="text-sm font-medium text-gray-800 dark:text-gray-200">{{ group.name }}</h4>
              <label class="flex items-center text-xs text-gray-500 cursor-pointer">
                <input type="checkbox" :checked="isAllChecked(group.children)" @change="toggleGroup(group.children, $event.target.checked)" class="mr-1.5 rounded" />
                全选
              </label>
            </div>
            <div class="grid grid-cols-2 md:grid-cols-3 gap-2">
              <label v-for="p in group.children" :key="p.code"
                     class="flex items-center text-sm text-gray-700 dark:text-gray-300 p-2 bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded cursor-pointer hover:border-primary transition">
                <input type="checkbox" :checked="isChecked(p.code)"
                       @change="togglePermission(p.code, $event.target.checked)"
                       :disabled="activeRole?.code === 'admin'"
                       class="w-4 h-4 mr-2 rounded text-primary" />
                <span class="flex-1">{{ p.name }}</span>
                <span class="text-xs text-gray-400 font-mono">{{ p.code }}</span>
              </label>
            </div>
          </div>
        </div>

        <div class="mt-6 flex justify-end space-x-2">
          <button @click="cancelEdit" class="h-9 px-4 border border-gray-200 dark:border-gray-700 rounded-md hover:bg-gray-50 dark:hover:bg-gray-800 text-sm">取消</button>
          <button @click="savePermissions" class="h-9 px-5 bg-primary hover:bg-primary-hover text-white rounded-md text-sm">保存</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { mockPermissionTree } from '@/api/mock'  // 权限分组树：后端无现成接口，保留 mock
import { listRoles, adaptRole, createRole, updateRole } from '@/api/roles'
import { ElMessage, ElMessageBox } from '@/api/toast'
import { PlusIcon } from '@heroicons/vue/24/outline'

// 后端 RoleRequest 只有 roleName/roleCode/description，没有权限字段，
// 权限勾选状态只能存本地（无法持久化到后端）
const PERM_STORAGE_KEY = 'feishu-role-permissions'

const roles = ref([])
const permissionTree = ref(mockPermissionTree)
const activeId = ref(null)
const checkedCodes = ref(new Set())
const loading = ref(false)

const activeRole = computed(() => roles.value.find(r => r.id === activeId.value))
const isChecked = (code) => activeRole.value?.code === 'admin' || checkedCodes.value.has(code)
function isAllChecked(children) { return children.every(c => isChecked(c.code)) }
function togglePermission(code, val) { val ? checkedCodes.value.add(code) : checkedCodes.value.delete(code) }
function toggleGroup(children, val) { children.forEach(c => val ? checkedCodes.value.add(c.code) : checkedCodes.value.delete(c.code)) }

function loadStoredPermissions() {
  try {
    return JSON.parse(localStorage.getItem(PERM_STORAGE_KEY) || '{}')
  } catch {
    return {}
  }
}

function saveStoredPermissions(map) {
  localStorage.setItem(PERM_STORAGE_KEY, JSON.stringify(map))
}

watch(activeId, (id) => {
  if (!id) return
  const stored = loadStoredPermissions()
  checkedCodes.value = new Set(stored[id] || [])
})

async function load() {
  loading.value = true
  try {
    const list = await listRoles()
    roles.value = list.map(adaptRole)
    if (!activeId.value && roles.value.length) activeId.value = roles.value[0].id
  } catch (e) {
    ElMessage({ message: '加载角色失败：' + (e?.message || ''), type: 'error' })
  } finally {
    loading.value = false
  }
}

async function createRole_() {
  try {
    const { value } = await ElMessageBox.prompt('请输入角色名称', '新增角色', {
      confirmButtonText: '创建', cancelButtonText: '取消'
    })
    const name = (value || '').trim()
    if (!name) return
    const code = 'role_' + Date.now()
    const created = await createRole({ roleName: name, roleCode: code, description: '' })
    await load()
    if (created && created.id) activeId.value = created.id
    ElMessage({ message: '已创建角色', type: 'success' })
  } catch (e) {
    if (e === 'cancel' || e?.message === 'cancel') return
    ElMessage({ message: '创建失败', type: 'error' })
  }
}

async function savePermissions() {
  if (!activeRole.value) return
  try {
    await updateRole(activeRole.value.id, {
      roleName: activeRole.value.name,
      roleCode: activeRole.value.code,
      description: activeRole.value.description
    })
    const stored = loadStoredPermissions()
    stored[activeRole.value.id] = Array.from(checkedCodes.value)
    saveStoredPermissions(stored)
    ElMessage({ message: '已保存（权限配置仅保存在本地，后端暂不支持持久化）', type: 'success' })
  } catch (e) {
    ElMessage({ message: '保存失败', type: 'error' })
  }
}

function cancelEdit() {
  if (!activeId.value) return
  const stored = loadStoredPermissions()
  checkedCodes.value = new Set(stored[activeId.value] || [])
  ElMessage({ message: '已取消更改', type: 'info' })
}

onMounted(load)
</script>
