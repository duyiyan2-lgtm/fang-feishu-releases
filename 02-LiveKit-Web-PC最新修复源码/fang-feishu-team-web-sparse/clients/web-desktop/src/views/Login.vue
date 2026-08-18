<template>
  <div class="auth-page min-h-screen w-full flex items-center justify-center relative overflow-hidden"
       style="background: linear-gradient(135deg, #E8EFFC 0%, #F5F6F7 50%, #FFFFFF 100%);">
    <div class="absolute inset-0 overflow-hidden pointer-events-none">
      <div class="absolute -top-32 -left-32 w-96 h-96 rounded-full bg-primary/10 blur-3xl"></div>
      <div class="absolute -bottom-32 -right-32 w-96 h-96 rounded-full bg-purple-400/10 blur-3xl"></div>
    </div>

    <div class="relative z-10 w-[400px] bg-white rounded-xl shadow-xl p-10 border border-gray-100">
      <div class="flex flex-col items-center mb-8">
        <div class="w-14 h-14 rounded-xl bg-primary flex items-center justify-center mb-4 shadow-lg shadow-primary/20">
          <ChatBubbleLeftRightIcon class="w-7 h-7 text-white" />
        </div>
        <h1 class="text-xl font-semibold text-gray-800">Feishu-like Workspace</h1>
        <p class="text-sm text-gray-500 mt-1">欢迎回来，请登录你的账号</p>
      </div>

      <form class="space-y-4" @submit.prevent="onSubmit">
        <div>
          <label class="block text-sm text-gray-700 mb-1.5">手机号 / 邮箱</label>
          <input v-model="form.account" name="account" type="text" autocomplete="username"
                 placeholder="请输入手机号或邮箱" :disabled="loading"
                 class="w-full h-10 px-3 text-sm bg-white text-gray-800 placeholder:text-gray-400 border border-gray-300 rounded-md outline-none focus:border-primary focus:ring-2 focus:ring-primary/20 transition disabled:bg-gray-50" />
        </div>

        <div>
          <label class="block text-sm text-gray-700 mb-1.5">密码</label>
          <input v-model="form.password" name="password" type="password" autocomplete="current-password"
                 placeholder="请输入密码" :disabled="loading"
                 class="w-full h-10 px-3 text-sm bg-white text-gray-800 placeholder:text-gray-400 border border-gray-300 rounded-md outline-none focus:border-primary focus:ring-2 focus:ring-primary/20 transition disabled:bg-gray-50" />
        </div>

        <div v-if="errorMsg" class="text-xs text-red-500 px-1">{{ errorMsg }}</div>

        <label class="flex items-center cursor-pointer text-sm text-gray-600">
          <input type="checkbox" class="w-4 h-4 mr-1.5 rounded bg-white border-gray-300 text-primary focus:ring-primary" />
          <span>记住我</span>
        </label>

        <button type="submit" :disabled="loading"
                class="w-full h-10 bg-primary hover:bg-primary-hover active:bg-primary-active text-white text-sm font-medium rounded-md transition-colors shadow-sm disabled:opacity-60 disabled:cursor-not-allowed flex items-center justify-center">
          <svg v-if="loading" class="animate-spin w-4 h-4 mr-2" viewBox="0 0 24 24" fill="none">
            <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="3" stroke-opacity="0.25" />
            <path d="M4 12a8 8 0 018-8v3a5 5 0 00-5 5H4z" fill="currentColor" />
          </svg>
          {{ loading ? '正在登录...' : '登录' }}
        </button>
      </form>

      <div class="my-6 flex items-center text-xs text-gray-400">
        <div class="flex-1 h-px bg-gray-200"></div>
        <span class="px-3">第三方登录</span>
        <div class="flex-1 h-px bg-gray-200"></div>
      </div>

      <div class="flex justify-center space-x-4 mb-6">
        <button v-for="i in 3" :key="i"
                class="w-9 h-9 rounded-full border border-gray-200 hover:border-primary hover:text-primary text-gray-500 flex items-center justify-center transition">
          <span class="text-xs">{{ ['Wx','Qr','Gh'][i-1] }}</span>
        </button>
      </div>

      <div class="flex justify-between text-sm pt-4 border-t border-gray-100">
        <a class="text-primary hover:underline cursor-pointer">忘记密码？</a>
        <router-link to="/register" class="text-primary hover:underline ml-1">注册账号</router-link>
      </div>

      <div v-if="isDev" class="mt-4 px-3 py-2 bg-blue-50 rounded text-xs text-blue-600 text-center leading-relaxed">
        💡 测试账号：<b>admin / user_a / user_b / user_c / user_d</b><br/>
        🔑 密码统一：<b>123456</b>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { ChatBubbleLeftRightIcon } from '@heroicons/vue/24/outline'

const router = useRouter()
const userStore = useUserStore()
const isDev = import.meta.env.DEV

const form = reactive({ account: '', password: '' })
const loading = ref(false)
const errorMsg = ref('')

async function onSubmit() {
  errorMsg.value = ''
  loading.value = true
  try {
    await userStore.login(form.account, form.password)
    await router.replace('/messages')
  } catch (e) {
    console.error('[Login] failed:', e)
    // 401 / 密码错的友好提示（不要再让 axios 字面 "Request failed with status code 401" 显示给用户）
    if (e.code === 1001 || e.code === 401 || e.message?.includes('Invalid') || e.message?.includes('401') || e.response?.status === 401) {
      errorMsg.value = '账号或密码错误，请检查后重试'
    } else if (e.message?.includes('Network') || e.message?.includes('timeout') || e.message?.includes('Failed to fetch')) {
      errorMsg.value = '网络异常，请检查后端连接后重试'
    } else {
      errorMsg.value = '登录失败，请重试'
    }
  } finally {
    loading.value = false
  }
}
</script>
