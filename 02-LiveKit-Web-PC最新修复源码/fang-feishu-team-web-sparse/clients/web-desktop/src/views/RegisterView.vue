<template>
  <div class="auth-page min-h-screen w-full flex items-center justify-center relative overflow-hidden"
       style="background: linear-gradient(135deg, #E8EFFC 0%, #F5F6F7 50%, #FFFFFF 100%);">
    <div class="absolute inset-0 overflow-hidden pointer-events-none">
      <div class="absolute -top-32 -left-32 w-96 h-96 rounded-full bg-primary/10 blur-3xl"></div>
      <div class="absolute -bottom-32 -right-32 w-96 h-96 rounded-full bg-purple-400/10 blur-3xl"></div>
    </div>

    <div class="relative z-10 w-[460px] bg-white rounded-xl shadow-xl p-10 border border-gray-100">
      <div class="flex flex-col items-center mb-8">
        <div class="w-14 h-14 rounded-xl bg-primary flex items-center justify-center mb-4 shadow-lg shadow-primary/20">
          <UserPlusIcon class="w-7 h-7 text-white" />
        </div>
        <h1 class="text-xl font-semibold text-gray-800">创建账号</h1>
        <p class="text-sm text-gray-500 mt-1">加入 Feishu-like Workspace</p>
      </div>

      <form class="space-y-4" @submit.prevent="onSubmit">
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm text-gray-700 mb-1.5">用户名 <span class="text-red-500">*</span></label>
            <input v-model="form.username" name="username" autocomplete="username" required minlength="2" maxlength="64" placeholder="字母数字"
                   class="w-full h-10 px-3 text-sm bg-white text-gray-800 placeholder:text-gray-400 border border-gray-300 rounded-md outline-none focus:border-primary focus:ring-2 focus:ring-primary/20" />
          </div>
          <div>
            <label class="block text-sm text-gray-700 mb-1.5">姓名 <span class="text-red-500">*</span></label>
            <input v-model="form.realName" name="realName" autocomplete="name" required minlength="1" maxlength="64" placeholder="您的真实姓名"
                   class="w-full h-10 px-3 text-sm bg-white text-gray-800 placeholder:text-gray-400 border border-gray-300 rounded-md outline-none focus:border-primary focus:ring-2 focus:ring-primary/20" />
          </div>
        </div>

        <div>
          <label class="block text-sm text-gray-700 mb-1.5">密码 <span class="text-red-500">*</span></label>
          <input v-model="form.password" name="password" type="password" autocomplete="new-password" required minlength="4" maxlength="64" placeholder="至少 4 位"
                 class="w-full h-10 px-3 text-sm bg-white text-gray-800 placeholder:text-gray-400 border border-gray-300 rounded-md outline-none focus:border-primary focus:ring-2 focus:ring-primary/20" />
        </div>

        <div>
          <label class="block text-sm text-gray-700 mb-1.5">手机号</label>
          <input v-model="form.phone" name="phone" type="tel" autocomplete="tel" maxlength="64" placeholder="13800000000"
                 class="w-full h-10 px-3 text-sm bg-white text-gray-800 placeholder:text-gray-400 border border-gray-300 rounded-md outline-none focus:border-primary focus:ring-2 focus:ring-primary/20" />
        </div>

        <div>
          <label class="block text-sm text-gray-700 mb-1.5">邮箱</label>
          <input v-model="form.email" name="email" type="email" autocomplete="email" maxlength="256" placeholder="example@company.com"
                 class="w-full h-10 px-3 text-sm bg-white text-gray-800 placeholder:text-gray-400 border border-gray-300 rounded-md outline-none focus:border-primary focus:ring-2 focus:ring-primary/20" />
        </div>

        <div v-if="errorMsg" class="text-xs text-red-500 px-1">{{ errorMsg }}</div>

        <button type="submit" :disabled="loading"
                class="w-full h-10 bg-primary hover:bg-primary-hover active:bg-primary-active text-white text-sm font-medium rounded-md transition-colors shadow-sm disabled:opacity-60 disabled:cursor-not-allowed flex items-center justify-center">
          <svg v-if="loading" class="animate-spin w-4 h-4 mr-2" viewBox="0 0 24 24" fill="none">
            <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="3" stroke-opacity="0.25" />
            <path d="M4 12a8 8 0 018-8v3a5 5 0 00-5 5H4z" fill="currentColor" />
          </svg>
          {{ loading ? '注册中…' : '创建账号' }}
        </button>
      </form>

      <div class="mt-6 text-center text-sm text-gray-600">
        已有账号？
        <router-link to="/login" class="text-primary hover:underline ml-1">直接登录</router-link>
      </div>

      <div v-if="isDev" class="mt-4 px-3 py-2 bg-blue-50 rounded text-xs text-blue-600 text-center">
        💡 测试账号：<b>admin / 123456</b>（已有）
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { UserPlusIcon } from '@heroicons/vue/24/outline'
import { registerApi } from '@/api/auth'
import { useUserStore } from '@/stores/user'
import { ElMessage } from '@/api/toast'

const router = useRouter()
const userStore = useUserStore()
const isDev = import.meta.env.DEV

const form = reactive({
  username: '',
  password: '',
  realName: '',
  phone: '',
  email: ''
})
const loading = ref(false)
const errorMsg = ref('')

async function onSubmit() {
  errorMsg.value = ''
  if (form.password.length < 4) {
    errorMsg.value = '密码至少 4 位'
    return
  }
  loading.value = true
  try {
    // 关键：后端字段 PascalCase 直发（swagger 显示直发）
    const payload = {
      Username: form.username.trim(),
      Password: form.password,
      RealName: form.realName.trim(),
      Phone: form.phone.trim() || null,
      Email: form.email.trim() || null
    }
    const data = await registerApi(payload)
    // 直接用注册返回的 token 登录
    if (data?.token) {
      // Pinia setup style 用暴露的 setToken action
      userStore.setToken(data.token, data.user, data.expiresAt)
      ElMessage({ message: '注册成功！欢迎加入', type: 'success' })
      await router.push('/messages')
    } else {
      ElMessage({ message: '注册成功，请登录', type: 'success' })
      await router.push('/login')
    }
  } catch (e) {
    console.error('[Register] failed:', e)
    if (e.code === 409 || e.message?.includes('already') || e.message?.includes('exists')) {
      errorMsg.value = '用户名已被占用'
    } else if (e.message?.includes('validation') || e.message?.includes('required')) {
      errorMsg.value = '请填写完整信息'
    } else {
      errorMsg.value = e.message || '注册失败，请重试'
    }
  } finally {
    loading.value = false
  }
}
</script>
