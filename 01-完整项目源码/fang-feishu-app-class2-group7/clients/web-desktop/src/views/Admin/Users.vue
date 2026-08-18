<template>
  <div class="flex flex-col h-full">
    <div class="h-14 px-6 flex items-center justify-between border-b border-gray-200 dark:border-gray-700 flex-shrink-0">
      <h2 class="text-base font-medium text-gray-900 dark:text-gray-100">用户管理 ({{ users.length }})</h2>
      <div class="flex items-center space-x-2">
        <input v-model="search" placeholder="搜索用户"
               class="h-8 px-3 text-sm bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 rounded-md outline-none focus:ring-2 focus:ring-primary/30 w-48 dark:text-gray-100" />
        <button @click="openCreate" class="h-8 px-3 text-sm bg-primary hover:bg-primary-hover text-white rounded-md flex items-center">
          <PlusIcon class="w-4 h-4 mr-1" />新建用户
        </button>
      </div>
    </div>

    <div class="flex-1 overflow-y-auto">
      <div v-if="loading" class="text-center py-12 text-sm text-gray-400">
        <svg class="animate-spin w-5 h-5 mx-auto mb-2" viewBox="0 0 24 24" fill="none">
          <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="3" stroke-opacity="0.25" />
          <path d="M4 12a8 8 0 018-8v3a5 5 0 00-5 5H4z" fill="currentColor" />
        </svg>
        加载中…
      </div>
      <table v-else class="w-full text-sm">
        <thead class="text-xs text-gray-500 dark:text-gray-400 bg-gray-50 dark:bg-gray-800/50 sticky top-0">
          <tr>
            <th class="text-left py-3 px-6 font-medium w-12"><input type="checkbox" /></th>
            <th class="text-left py-3 px-3 font-medium">姓名</th>
            <th class="text-left py-3 px-3 font-medium">邮箱</th>
            <th class="text-left py-3 px-3 font-medium w-32">部门</th>
            <th class="text-left py-3 px-3 font-medium w-32">角色</th>
            <th class="text-left py-3 px-3 font-medium w-28">状态</th>
            <th class="text-left py-3 px-3 font-medium w-32">最近登录</th>
            <th class="text-left py-3 px-6 font-medium w-32">操作</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="u in filteredUsers" :key="u.id" class="border-b border-gray-100 dark:border-gray-800 hover:bg-gray-50 dark:hover:bg-gray-800/50 transition">
            <td class="py-3 px-6"><input type="checkbox" /></td>
            <td class="py-3 px-3">
              <div class="flex items-center">
                <div class="w-8 h-8 rounded-full bg-gradient-to-br from-primary to-purple-500 text-white text-sm flex items-center justify-center mr-2.5">{{ u.name[0] }}</div>
                <div>
                  <div class="font-medium text-gray-900 dark:text-gray-100">{{ u.name }}</div>
                  <div class="text-xs text-gray-400">@{{ u.username }}</div>
                </div>
              </div>
            </td>
            <td class="py-3 px-3 text-gray-600 dark:text-gray-300">{{ u.email }}</td>
            <td class="py-3 px-3 text-gray-600 dark:text-gray-300">{{ u.dept || '—' }}</td>
            <td class="py-3 px-3">
              <span class="px-2 py-0.5 rounded text-xs bg-blue-50 dark:bg-blue-500/20 text-blue-700 dark:text-blue-300">{{ u.role }}</span>
            </td>
            <td class="py-3 px-3">
              <span :class="['px-2 py-0.5 rounded-full text-xs font-medium',
                            u.status === 'active' ? 'bg-green-50 text-green-600 dark:bg-green-500/20 dark:text-green-300' : 'bg-red-50 text-red-600']">
                {{ u.status === 'active' ? '启用' : '禁用' }}
              </span>
            </td>
            <td class="py-3 px-3 text-gray-500">{{ u.lastLogin }}</td>
            <td class="py-3 px-6">
              <button @click="toggleStatus(u)" :class="['text-xs hover:underline mr-3', u.status === 'active' ? 'text-red-500' : 'text-green-500']">
                {{ u.status === 'active' ? '禁用' : '启用' }}
              </button>
            </td>
          </tr>
        </tbody>
      </table>
      <div v-if="!loading && filteredUsers.length === 0" class="text-center py-12 text-sm text-gray-400">暂无用户</div>
    </div>

    <!-- 新建用户弹窗 -->
    <transition
      enter-active-class="transition duration-150"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100">
      <div v-if="creating" class="fixed inset-0 z-50 bg-black/30 flex items-center justify-center" @click.self="creating = false">
        <div class="bg-white dark:bg-gray-900 rounded-lg shadow-xl w-[480px] p-6">
          <h3 class="text-base font-medium text-gray-900 dark:text-gray-100 mb-4">新建用户</h3>
          <div class="space-y-3">
            <div>
              <label class="block text-xs text-gray-600 dark:text-gray-400 mb-1">用户名 *</label>
              <input v-model="form.username" placeholder="登录用户名（英文）"
                     class="w-full h-9 px-3 text-sm border border-gray-200 dark:border-gray-700 rounded-md bg-white dark:bg-gray-800/50 outline-none focus:border-primary focus:ring-2 focus:ring-primary/20 dark:text-gray-100" />
            </div>
            <div>
              <label class="block text-xs text-gray-600 dark:text-gray-400 mb-1">密码 *</label>
              <input v-model="form.password" type="password" placeholder="≥ 4 位"
                     class="w-full h-9 px-3 text-sm border border-gray-200 dark:border-gray-700 rounded-md bg-white dark:bg-gray-800/50 outline-none focus:border-primary focus:ring-2 focus:ring-primary/20 dark:text-gray-100" />
            </div>
            <div>
              <label class="block text-xs text-gray-600 dark:text-gray-400 mb-1">真实姓名 *</label>
              <input v-model="form.realName" placeholder="如：张三"
                     class="w-full h-9 px-3 text-sm border border-gray-200 dark:border-gray-700 rounded-md bg-white dark:bg-gray-800/50 outline-none focus:border-primary focus:ring-2 focus:ring-primary/20 dark:text-gray-100" />
            </div>
            <div>
              <label class="block text-xs text-gray-600 dark:text-gray-400 mb-1">邮箱</label>
              <input v-model="form.email" type="email" placeholder="可选"
                     class="w-full h-9 px-3 text-sm border border-gray-200 dark:border-gray-700 rounded-md bg-white dark:bg-gray-800/50 outline-none focus:border-primary focus:ring-2 focus:ring-primary/20 dark:text-gray-100" />
            </div>
            <div>
              <label class="block text-xs text-gray-600 dark:text-gray-400 mb-1">职位</label>
              <input v-model="form.position" placeholder="如：前端工程师"
                     class="w-full h-9 px-3 text-sm border border-gray-200 dark:border-gray-700 rounded-md bg-white dark:bg-gray-800/50 outline-none focus:border-primary focus:ring-2 focus:ring-primary/20 dark:text-gray-100" />
            </div>
            <div>
              <label class="block text-xs text-gray-600 dark:text-gray-400 mb-1">角色（逗号分隔）</label>
              <input v-model="form.roleCodes" placeholder="User, Admin"
                     class="w-full h-9 px-3 text-sm border border-gray-200 dark:border-gray-700 rounded-md bg-white dark:bg-gray-800/50 outline-none focus:border-primary focus:ring-2 focus:ring-primary/20 dark:text-gray-100" />
            </div>
          </div>
          <div class="mt-5 flex justify-end space-x-2">
            <button @click="creating = false" class="h-8 px-4 text-sm border border-gray-200 dark:border-gray-700 rounded hover:bg-gray-50 dark:hover:bg-gray-800 dark:text-gray-200">取消</button>
            <button @click="submitCreate" :disabled="saving" class="h-8 px-4 text-sm bg-primary text-white rounded hover:bg-primary-hover disabled:opacity-60 transition">
              {{ saving ? '创建中…' : '创建' }}
            </button>
          </div>
        </div>
      </div>
    </transition>
  </div>
</template>

<script setup>
import { ref, computed, reactive, onMounted } from 'vue'
import { listUsers, createUser, setUserStatus, adaptUser } from '@/api/users'
import { ElMessage } from '@/api/toast'
import { PlusIcon } from '@heroicons/vue/24/outline'

const users = ref([])
const loading = ref(true)
const search = ref('')
const creating = ref(false)
const saving = ref(false)
const form = reactive({
  username: '',
  password: '',
  realName: '',
  email: '',
  position: '',
  roleCodes: 'User'
})

const filteredUsers = computed(() => {
  const kw = search.value.trim().toLowerCase()
  if (!kw) return users.value
  return users.value.filter(u =>
    (u.name || '').toLowerCase().includes(kw) ||
    (u.email || '').toLowerCase().includes(kw) ||
    (u.username || '').toLowerCase().includes(kw)
  )
})

onMounted(async () => {
  loading.value = true
  try {
    const list = await listUsers()
    users.value = (list || []).map(adaptUser)
  } catch (e) {
    console.error('[users] load failed', e)
    ElMessage({ message: '加载用户失败', type: 'error' })
  } finally {
    loading.value = false
  }
})

function openCreate() {
  Object.assign(form, {
    username: '', password: '', realName: '', email: '', position: '', roleCodes: 'User'
  })
  creating.value = true
}

async function submitCreate() {
  if (!form.username) return ElMessage({ message: '请输入用户名', type: 'warning' })
  if (!form.password || form.password.length < 4) return ElMessage({ message: '密码至少 4 位', type: 'warning' })
  if (!form.realName) return ElMessage({ message: '请输入真实姓名', type: 'warning' })
  saving.value = true
  try {
    const payload = {
      username: form.username,
      password: form.password,
      realName: form.realName,
      email: form.email || null,
      position: form.position || null,
      roleCodes: form.roleCodes.split(/[,，\s]+/).filter(Boolean)
    }
    const created = await createUser(payload)
    users.value.unshift(adaptUser(created))
    ElMessage({ message: `用户 ${created.username} 已创建`, type: 'success' })
    creating.value = false
  } catch (e) {
    ElMessage({ message: '创建失败：' + (e?.response?.data?.message || e?.message || ''), type: 'error' })
  } finally {
    saving.value = false
  }
}

async function toggleStatus(u) {
  const next = u.status === 'active' ? 'disabled' : 'active'
  try {
    await setUserStatus(u.id, next)
    u.status = next
    ElMessage({ message: next === 'active' ? '已启用' : '已禁用', type: 'success' })
  } catch (e) {
    ElMessage({ message: '操作失败', type: 'error' })
  }
}
</script>