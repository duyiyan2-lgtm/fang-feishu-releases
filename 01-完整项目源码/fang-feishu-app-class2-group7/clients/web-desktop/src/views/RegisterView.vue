<template>
  <div class="min-h-screen w-full flex items-center justify-center relative overflow-hidden bg-[#F0F3FA] dark:bg-[#0B0E14]">
    <div class="absolute inset-0 pointer-events-none overflow-hidden">
      <div class="absolute -top-32 -left-32 w-96 h-96 rounded-full bg-primary/15 blur-3xl" />
      <div class="absolute -bottom-32 -right-32 w-96 h-96 rounded-full bg-violet-400/10 blur-3xl" />
    </div>

    <div class="relative z-10 w-full max-w-[460px] mx-4 animate-slide-up">
      <div class="bg-white dark:bg-gray-900 rounded-2xl shadow-dialog border border-white/60 dark:border-gray-700/80 p-8 sm:p-10">
        <div class="flex flex-col items-center mb-8">
          <div class="w-12 h-12 rounded-xl bg-primary flex items-center justify-center mb-4 shadow-glow">
            <UserPlusIcon class="w-6 h-6 text-white" />
          </div>
          <h1 class="text-xl font-semibold text-ink dark:text-gray-100">创建账号</h1>
          <p class="text-sm text-ink-secondary dark:text-gray-400 mt-1.5">加入仿飞书工作台</p>
        </div>

        <form class="space-y-4" @submit.prevent="onSubmit">
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-sm font-medium text-ink dark:text-gray-200 mb-1.5">
                用户名 <span class="text-red-500">*</span>
              </label>
              <input
                v-model="form.username"
                required
                minlength="2"
                maxlength="64"
                placeholder="字母数字"
                class="ff-input-outline"
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-ink dark:text-gray-200 mb-1.5">
                姓名 <span class="text-red-500">*</span>
              </label>
              <input
                v-model="form.realName"
                required
                minlength="1"
                maxlength="64"
                placeholder="真实姓名"
                class="ff-input-outline"
              />
            </div>
          </div>

          <div>
            <label class="block text-sm font-medium text-ink dark:text-gray-200 mb-1.5">
              密码 <span class="text-red-500">*</span>
            </label>
            <input
              v-model="form.password"
              type="password"
              required
              minlength="4"
              maxlength="64"
              placeholder="至少 4 位"
              class="ff-input-outline"
            />
          </div>

          <div>
            <label class="block text-sm font-medium text-ink dark:text-gray-200 mb-1.5">手机号</label>
            <input v-model="form.phone" maxlength="64" placeholder="13800000000" class="ff-input-outline" />
          </div>

          <div>
            <label class="block text-sm font-medium text-ink dark:text-gray-200 mb-1.5">邮箱</label>
            <input
              v-model="form.email"
              type="email"
              maxlength="256"
              placeholder="example@company.com"
              class="ff-input-outline"
            />
          </div>

          <div v-if="errorMsg" class="text-xs text-red-500 bg-red-50 dark:bg-red-500/10 rounded-md px-3 py-2">
            {{ errorMsg }}
          </div>

          <button type="submit" :disabled="loading" class="ff-btn-primary w-full h-10">
            <svg v-if="loading" class="animate-spin w-4 h-4" viewBox="0 0 24 24" fill="none">
              <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="3" stroke-opacity="0.25" />
              <path d="M4 12a8 8 0 018-8v3a5 5 0 00-5 5H4z" fill="currentColor" />
            </svg>
            {{ loading ? '注册中…' : '创建账号' }}
          </button>
        </form>

        <div class="mt-6 text-center text-sm text-ink-secondary dark:text-gray-400">
          已有账号？
          <router-link to="/login" class="text-primary hover:text-primary-hover font-medium ml-1 transition-colors">
            直接登录
          </router-link>
        </div>
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

const form = reactive({
  username: '',
  realName: '',
  password: '',
  phone: '',
  email: ''
})
const loading = ref(false)
const errorMsg = ref('')

async function onSubmit() {
  errorMsg.value = ''
  loading.value = true
  try {
    await registerApi({ ...form })
    ElMessage({ message: '注册成功，正在登录…', type: 'success' })
    await userStore.login(form.username, form.password)
    await router.push('/home')
  } catch (e) {
    errorMsg.value = e?.message || e?.response?.data?.message || '注册失败，请重试'
  } finally {
    loading.value = false
  }
}
</script>
