<template>
  <div class="min-h-screen w-full flex relative overflow-hidden bg-[#F0F3FA] dark:bg-[#0B0E14]">
    <!-- 背景装饰 -->
    <div class="absolute inset-0 pointer-events-none overflow-hidden">
      <div class="absolute -top-40 -left-40 w-[520px] h-[520px] rounded-full bg-primary/15 blur-3xl" />
      <div class="absolute -bottom-48 -right-32 w-[560px] h-[560px] rounded-full bg-violet-400/10 blur-3xl" />
      <div class="absolute top-1/3 right-1/4 w-64 h-64 rounded-full bg-sky-300/10 blur-3xl" />
      <!-- 网格 -->
      <div
        class="absolute inset-0 opacity-[0.35] dark:opacity-[0.12]"
        style="background-image: linear-gradient(rgba(51,112,255,0.06) 1px, transparent 1px), linear-gradient(90deg, rgba(51,112,255,0.06) 1px, transparent 1px); background-size: 48px 48px;"
      />
    </div>

    <div class="relative z-10 flex w-full min-h-screen">
      <!-- 左侧品牌区 -->
      <div class="hidden lg:flex flex-1 flex-col justify-between p-12 xl:p-16 text-white
                  bg-gradient-to-br from-[#1A3A8F] via-[#2B5CDE] to-[#3370FF] relative overflow-hidden">
        <div class="absolute inset-0 opacity-20"
             style="background-image: radial-gradient(circle at 20% 80%, white 0%, transparent 40%), radial-gradient(circle at 80% 20%, #a5b4fc 0%, transparent 35%);" />
        <div class="relative">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-xl bg-white/15 backdrop-blur flex items-center justify-center border border-white/20">
              <ChatBubbleLeftRightIcon class="w-5 h-5" />
            </div>
            <span class="text-lg font-semibold tracking-wide">仿飞书工作台</span>
          </div>
        </div>

        <div class="relative max-w-md animate-slide-up">
          <h2 class="text-3xl xl:text-4xl font-bold leading-tight tracking-tight">
            一站式企业协作<br />更高效、更流畅
          </h2>
          <p class="mt-5 text-white/75 text-base leading-relaxed">
            消息、文档、会议、审批、云盘与通讯录统一入口。Web 端丝滑切换，PC 客户端原生窗口体验。
          </p>
          <ul class="mt-8 space-y-3 text-sm text-white/85">
            <li class="flex items-center gap-2.5">
              <span class="w-5 h-5 rounded-full bg-white/20 flex items-center justify-center text-xs">✓</span>
              即时消息 + 群会议（Agora）
            </li>
            <li class="flex items-center gap-2.5">
              <span class="w-5 h-5 rounded-full bg-white/20 flex items-center justify-center text-xs">✓</span>
              在线文档 / 云空间 / 知识库
            </li>
            <li class="flex items-center gap-2.5">
              <span class="w-5 h-5 rounded-full bg-white/20 flex items-center justify-center text-xs">✓</span>
              审批流与任务跟踪
            </li>
          </ul>
        </div>

        <div class="relative text-xs text-white/50">
          Class2 · Group7 · Vue 3 + .NET
        </div>
      </div>

      <!-- 右侧登录卡 -->
      <div class="flex-1 flex items-center justify-center p-6 sm:p-10">
        <div class="w-full max-w-[400px] animate-slide-up">
          <div class="bg-white dark:bg-gray-900 rounded-2xl shadow-dialog border border-white/60 dark:border-gray-700/80 p-8 sm:p-10">
            <div class="flex flex-col items-center mb-8 lg:items-start">
              <div class="lg:hidden w-12 h-12 rounded-xl bg-primary flex items-center justify-center mb-4 shadow-glow">
                <ChatBubbleLeftRightIcon class="w-6 h-6 text-white" />
              </div>
              <h1 class="text-xl font-semibold text-ink dark:text-gray-100">欢迎回来</h1>
              <p class="text-sm text-ink-secondary dark:text-gray-400 mt-1.5">登录你的工作台账号</p>
            </div>

            <form class="space-y-4" @submit.prevent="onSubmit">
              <div>
                <label class="block text-sm font-medium text-ink dark:text-gray-200 mb-1.5">账号</label>
                <input
                  v-model="form.account"
                  type="text"
                  autocomplete="username"
                  placeholder="手机号 / 邮箱 / 用户名"
                  :disabled="loading"
                  class="ff-input-outline"
                />
              </div>

              <div>
                <label class="block text-sm font-medium text-ink dark:text-gray-200 mb-1.5">密码</label>
                <div class="relative">
                  <input
                    v-model="form.password"
                    :type="showPwd ? 'text' : 'password'"
                    autocomplete="current-password"
                    placeholder="请输入密码"
                    :disabled="loading"
                    class="ff-input-outline pr-10"
                  />
                  <button
                    type="button"
                    class="absolute right-2 top-1/2 -translate-y-1/2 ff-icon-btn w-7 h-7"
                    tabindex="-1"
                    @click="showPwd = !showPwd"
                  >
                    <EyeIcon v-if="!showPwd" class="w-4 h-4 text-ink-tertiary" />
                    <EyeSlashIcon v-else class="w-4 h-4 text-ink-tertiary" />
                  </button>
                </div>
              </div>

              <div v-if="errorMsg" class="text-xs text-red-500 bg-red-50 dark:bg-red-500/10 rounded-md px-3 py-2 animate-fade-in">
                {{ errorMsg }}
              </div>

              <label class="flex items-center cursor-pointer text-sm text-ink-secondary dark:text-gray-400 select-none">
                <input
                  v-model="remember"
                  type="checkbox"
                  class="w-4 h-4 mr-2 rounded border-gray-300 text-primary focus:ring-primary"
                />
                <span>记住账号</span>
              </label>

              <button type="submit" :disabled="loading" class="ff-btn-primary w-full h-10 text-[15px]">
                <svg v-if="loading" class="animate-spin w-4 h-4" viewBox="0 0 24 24" fill="none">
                  <circle cx="12" cy="12" r="10" stroke="currentColor" stroke-width="3" stroke-opacity="0.25" />
                  <path d="M4 12a8 8 0 018-8v3a5 5 0 00-5 5H4z" fill="currentColor" />
                </svg>
                {{ loading ? '正在登录…' : '登录' }}
              </button>
            </form>

            <div class="flex justify-between text-sm pt-5 mt-5 border-t border-line-soft dark:border-gray-700">
              <span class="text-ink-tertiary">忘记密码？</span>
              <router-link to="/register" class="text-primary hover:text-primary-hover font-medium transition-colors">
                注册账号
              </router-link>
            </div>

            <div class="mt-5 px-3 py-2.5 rounded-lg bg-primary-50 dark:bg-primary/10 text-xs text-primary dark:text-blue-300 leading-relaxed">
              <div class="font-medium mb-0.5">演示账号</div>
              <div>admin / user_a ~ user_d · 密码 <b>123456</b></div>
            </div>
          </div>

          <p class="text-center text-2xs text-ink-tertiary mt-6">
            Web 与 PC 客户端共用同一套界面 · 流畅体验优化 v0.5
          </p>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useUserStore } from '@/stores/user'
import { ChatBubbleLeftRightIcon, EyeIcon, EyeSlashIcon } from '@heroicons/vue/24/outline'

const router = useRouter()
const userStore = useUserStore()

const form = reactive({ account: '', password: '' })
const loading = ref(false)
const errorMsg = ref('')
const showPwd = ref(false)
const remember = ref(true)

const REMEMBER_KEY = 'ff-login-account'

onMounted(() => {
  const saved = localStorage.getItem(REMEMBER_KEY)
  if (saved) form.account = saved
})

async function onSubmit() {
  errorMsg.value = ''
  if (!form.account.trim() || !form.password) {
    errorMsg.value = '请输入账号和密码'
    return
  }
  loading.value = true
  try {
    await userStore.login(form.account.trim(), form.password)
    if (remember.value) {
      localStorage.setItem(REMEMBER_KEY, form.account.trim())
    } else {
      localStorage.removeItem(REMEMBER_KEY)
    }
    await router.push('/home')
  } catch (e) {
    if (e.code === 1001 || e.code === 401 || e.message?.includes('Invalid') || e.message?.includes('401') || e.response?.status === 401) {
      errorMsg.value = '账号或密码错误（可用 admin / user_a~d，密码 123456）'
    } else if (e.message?.includes('Network') || e.message?.includes('timeout') || e.message?.includes('Failed to fetch')) {
      errorMsg.value = '网络异常，请检查后端连接后重试'
    } else {
      errorMsg.value = e.message || '登录失败，请重试'
    }
  } finally {
    loading.value = false
  }
}
</script>
