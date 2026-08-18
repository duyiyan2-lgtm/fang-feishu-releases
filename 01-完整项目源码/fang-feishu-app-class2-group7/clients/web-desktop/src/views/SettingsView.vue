<template>
  <div class="flex h-full bg-white dark:bg-gray-900 transition-colors overflow-y-auto">
    <div class="flex-1 max-w-3xl mx-auto p-8">
      <h1 class="text-xl font-semibold dark:text-gray-100 mb-1">账号设置</h1>
      <p class="text-sm text-gray-500 mb-6">管理你的个人资料</p>

      <div v-if="loading && !loaded" class="text-center py-12 text-gray-400 text-sm">
        <svg class="animate-spin w-5 h-5 mx-auto mb-2" viewBox="0 0 24 24" fill="none">
          <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="3" stroke-opacity="0.25" />
          <path d="M4 12a8 8 0 018-8v3a5 5 0 00-5 5H4z" fill="currentColor" />
        </svg>
        加载中…
      </div>

      <form v-else @submit.prevent="onSave" class="space-y-5">
        <!-- 头像 -->
        <div class="flex items-center space-x-4 pb-5 border-b border-gray-100 dark:border-gray-800">
          <div class="w-20 h-20 rounded-full bg-primary flex items-center justify-center text-white text-2xl font-medium overflow-hidden flex-shrink-0">
            <img v-if="avatarPreviewUrl" :src="avatarPreviewUrl" class="w-full h-full object-cover" />
            <span v-else>{{ form.realName?.[0] || '?' }}</span>
          </div>
          <div class="flex flex-col">
            <label class="h-9 px-4 text-sm bg-primary text-white rounded-md flex items-center cursor-pointer hover:bg-primary-hover transition">
              <CameraIcon class="w-4 h-4 mr-1.5" />
              更换头像
              <input type="file" accept="image/*" class="hidden" @change="onAvatarChange" />
            </label>
            <p class="mt-2 text-xs text-gray-500">支持 JPG / PNG，最大 2MB</p>
          </div>
        </div>

        <!-- 用户名 + 部门（只读） -->
        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm text-gray-600 dark:text-gray-400 mb-1.5">用户名</label>
            <input :value="form.username" disabled
                   class="w-full h-9 px-3 text-sm border border-gray-200 dark:border-gray-700 dark:bg-gray-800 dark:text-gray-400 rounded-md bg-gray-50 cursor-not-allowed" />
          </div>
          <div>
            <label class="block text-sm text-gray-600 dark:text-gray-400 mb-1.5">部门</label>
            <input :value="form.departmentName || '—'" disabled
                   class="w-full h-9 px-3 text-sm border border-gray-200 dark:border-gray-700 dark:bg-gray-800 dark:text-gray-400 rounded-md bg-gray-50 cursor-not-allowed" />
          </div>
        </div>

        <!-- 真实姓名 -->
        <div>
          <label class="block text-sm text-gray-600 dark:text-gray-400 mb-1.5">
            姓名 <span class="text-red-500">*</span>
          </label>
          <input v-model="form.realName" required maxlength="64" placeholder="您的姓名"
                 class="w-full h-9 px-3 text-sm border border-gray-200 dark:border-gray-700 dark:bg-gray-800 dark:text-gray-100 rounded-md focus:border-primary focus:ring-2 focus:ring-primary/20 outline-none" />
          <p class="mt-1 text-xs text-gray-400">1-64 字符</p>
        </div>

        <!-- 邮箱 + 电话 -->
        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm text-gray-600 dark:text-gray-400 mb-1.5">邮箱</label>
            <input v-model="form.email" type="email" maxlength="256" placeholder="example@company.com"
                   class="w-full h-9 px-3 text-sm border border-gray-200 dark:border-gray-700 dark:bg-gray-800 dark:text-gray-100 rounded-md focus:border-primary focus:ring-2 focus:ring-primary/20 outline-none" />
          </div>
          <div>
            <label class="block text-sm text-gray-600 dark:text-gray-400 mb-1.5">手机号</label>
            <input v-model="form.phone" maxlength="64" placeholder="13800000000"
                   class="w-full h-9 px-3 text-sm border border-gray-200 dark:border-gray-700 dark:bg-gray-800 dark:text-gray-100 rounded-md focus:border-primary focus:ring-2 focus:ring-primary/20 outline-none" />
          </div>
        </div>

        <!-- 职位 + 工作地 -->
        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm text-gray-600 dark:text-gray-400 mb-1.5">职位</label>
            <input v-model="form.position" maxlength="160" placeholder="如：前端工程师"
                   class="w-full h-9 px-3 text-sm border border-gray-200 dark:border-gray-700 dark:bg-gray-800 dark:text-gray-100 rounded-md focus:border-primary focus:ring-2 focus:ring-primary/20 outline-none" />
          </div>
          <div>
            <label class="block text-sm text-gray-600 dark:text-gray-400 mb-1.5">工作地</label>
            <input v-model="form.workPlace" maxlength="160" placeholder="如：北京"
                   class="w-full h-9 px-3 text-sm border border-gray-200 dark:border-gray-700 dark:bg-gray-800 dark:text-gray-100 rounded-md focus:border-primary focus:ring-2 focus:ring-primary/20 outline-none" />
          </div>
        </div>

        <!-- 简介 -->
        <div>
          <label class="block text-sm text-gray-600 dark:text-gray-400 mb-1.5">个人简介</label>
          <textarea v-model="form.bio" rows="3" maxlength="1000" placeholder="介绍一下你自己…"
                    class="w-full px-3 py-2 text-sm border border-gray-200 dark:border-gray-700 dark:bg-gray-800 dark:text-gray-100 rounded-md focus:border-primary focus:ring-2 focus:ring-primary/20 outline-none resize-none" />
          <p class="mt-1 text-xs text-gray-400 text-right">{{ form.bio?.length || 0 }} / 1000</p>
        </div>

        <!-- 操作按钮 -->
        <div class="pt-4 border-t border-gray-100 dark:border-gray-800 flex items-center justify-end space-x-3">
          <button type="button" @click="reset"
                  class="h-9 px-5 text-sm text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-md">
            重置
          </button>
          <button type="submit" :disabled="saving"
                  class="h-9 px-6 text-sm bg-primary hover:bg-primary-hover text-white rounded-md disabled:opacity-50 flex items-center">
            <svg v-if="saving" class="animate-spin w-4 h-4 mr-1.5" viewBox="0 0 24 24" fill="none">
              <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="3" stroke-opacity="0.25" />
              <path d="M4 12a8 8 0 018-8v3a5 5 0 00-5 5H4z" fill="currentColor" />
            </svg>
            {{ saving ? '保存中…' : '保存修改' }}
          </button>
        </div>
      </form>
    </div>
  </div>
</template>

<script setup>
defineOptions({ name: 'Settings' })

import { ref, reactive, onMounted, watch } from 'vue'
import { CameraIcon } from '@heroicons/vue/24/outline'
import { getMyProfile, updateMyProfile, uploadAvatar } from '@/api/me'
import { downloadFile } from '@/api/files'
import { useUserStore } from '@/stores/user'
import { ElMessage } from '@/api/toast'

const userStore = useUserStore()

const loading = ref(false)
const saving = ref(false)
const loaded = ref(false)

const original = ref({})
const form = reactive({
  username: '',
  realName: '',
  email: '',
  phone: '',
  position: '',
  workPlace: '',
  bio: '',
  avatarUrl: '',
  departmentId: '',
  departmentName: ''
})

// 头像预览：avatarUrl 存的是文件 id（后端下载接口需要鉴权头，<img> 无法直接带 token），
// 所以这里用 blob 拉取再转成本地 object URL 来显示
const avatarPreviewUrl = ref('')
let avatarObjectUrl = null

async function resolveAvatarPreview(value) {
  if (avatarObjectUrl) {
    URL.revokeObjectURL(avatarObjectUrl)
    avatarObjectUrl = null
  }
  if (!value) {
    avatarPreviewUrl.value = ''
    return
  }
  if (/^https?:\/\//i.test(value) || value.startsWith('blob:') || value.startsWith('data:')) {
    avatarPreviewUrl.value = value
    return
  }
  try {
    const blob = await downloadFile(value)
    avatarObjectUrl = URL.createObjectURL(blob)
    avatarPreviewUrl.value = avatarObjectUrl
  } catch {
    avatarPreviewUrl.value = ''
  }
}

watch(() => form.avatarUrl, (v) => resolveAvatarPreview(v))

async function load() {
  loading.value = true
  try {
    const data = await getMyProfile()
    Object.assign(form, {
      username: data.username || userStore.userInfo?.username || '',
      realName: data.realName || '',
      email: data.email || '',
      phone: data.phone || '',
      position: data.position || '',
      workPlace: data.workPlace || '',
      bio: data.bio || '',
      avatarUrl: data.avatarUrl || userStore.userInfo?.avatarUrl || '',
      departmentId: data.departmentId || '',
      departmentName: data.departmentName || ''
    })
    original.value = JSON.parse(JSON.stringify(form))
    loaded.value = true
  } catch (e) {
    ElMessage({ message: '加载个人信息失败：' + e.message, type: 'error' })
  } finally {
    loading.value = false
  }
}

function reset() {
  Object.assign(form, original.value)
  ElMessage({ message: '已重置', type: 'info' })
}

async function onSave() {
  if (!form.realName?.trim()) {
    ElMessage({ message: '姓名不能为空', type: 'warning' })
    return
  }
  saving.value = true
  try {
    // 关键：后端字段 PascalCase
    const payload = {
      RealName: form.realName?.trim(),
      Email: form.email?.trim() || null,
      Phone: form.phone?.trim() || null,
      Position: form.position?.trim() || null,
      WorkPlace: form.workPlace?.trim() || null,
      Bio: form.bio?.trim() || null,
      AvatarUrl: form.avatarUrl?.trim() || null
    }
    const updated = await updateMyProfile(payload)
    // 更新 userStore（头像/名字变化）
    if (userStore.userInfo) {
      userStore.userInfo.realName = updated.realName || form.realName
      userStore.userInfo.avatarUrl = updated.avatarUrl || form.avatarUrl
      userStore.userInfo.position = updated.position || form.position
    }
    ElMessage({ message: '已保存', type: 'success' })
    // 重新拉一次
    await load()
  } catch (e) {
    ElMessage({ message: '保存失败：' + e.message, type: 'error' })
  } finally {
    saving.value = false
  }
}

async function onAvatarChange(e) {
  const file = e.target.files?.[0]
  if (!file) return
  if (file.size > 2 * 1024 * 1024) {
    ElMessage({ message: '图片不能超过 2MB', type: 'warning' })
    return
  }
  try {
    // /files/upload 只返回文件元数据（id 等），没有直接可访问的 url 字段
    // 头像展示走 downloadFile(id) 拉 blob 显示，真正保存的是文件 id
    const r = await uploadAvatar(file)
    const fileId = r?.id || r?.fileId
    if (fileId) {
      form.avatarUrl = fileId
      ElMessage({ message: '头像已上传，点保存生效', type: 'success' })
    } else {
      ElMessage({ message: '上传返回异常', type: 'warning' })
    }
  } catch (e) {
    ElMessage({ message: '上传失败：' + e.message, type: 'error' })
  } finally {
    e.target.value = ''
  }
}

onMounted(load)
</script>