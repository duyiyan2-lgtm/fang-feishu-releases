import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { loginApi, logoutApi, getProfileApi } from '@/api/auth'

/**
 * 适配后端 UserInfo -> 前端使用的形状
 */
function adaptUser(u) {
  if (!u) return null
  return {
    id: u.id,
    username: u.username,
    name: u.realName || u.username,
    realName: u.realName,
    email: u.email,
    phone: u.phone,
    departmentId: u.departmentId,
    department: u.departmentName,
    roles: Array.isArray(u.roles) ? u.roles : [],
    avatarColor: '#3370FF',
    title: ''
  }
}

export const useUserStore = defineStore('user', () => {
  const token = ref('')
  const userInfo = ref(null)
  const expiresAt = ref(null)
  // 登出重入守卫（非响应式，仅用于防止并发重复调用）
  let loggingOut = false

  const isLoggedIn = computed(() => !!token.value)
  const displayName = computed(() => userInfo.value?.name || userInfo.value?.username || '未登录')
  const isAdmin = computed(() => Array.isArray(userInfo.value?.roles) && userInfo.value.roles.includes('Admin'))

  /** 暴露给组件的 setToken（正确方式，避免直接赋值内部 ref） */
  function setToken(t, info, exp) {
    token.value = t
    if (info !== undefined) userInfo.value = adaptUser(info)
    if (exp !== undefined) expiresAt.value = exp
  }

  async function login(account, password) {
    const data = await loginApi({ account, password })
    // 后端响应: { token, expiresAt, user }
    setToken(data.token, data.user, data.expiresAt)
    return data
  }

  async function fetchProfile() {
    if (!token.value) return null
    const u = await getProfileApi()
    userInfo.value = adaptUser(u)
    return userInfo.value
  }

  function logout() {
    // 重入守卫：拦截器的 401 处理可能与用户主动登出并发触发，
    // 若不加守卫会重复发 /auth/logout 请求（配合 http.js 的 isSilent401 双保险）
    if (loggingOut) {
      token.value = ''
      userInfo.value = null
      expiresAt.value = null
      return
    }
    loggingOut = true
    try { logoutApi() } catch { /* best effort */ }
    token.value = ''
    userInfo.value = null
    expiresAt.value = null
    // 微任务后释放，允许下次真正的登出
    Promise.resolve().then(() => { loggingOut = false })
  }

  return { token, userInfo, expiresAt, isLoggedIn, displayName, isAdmin, setToken, login, logout, fetchProfile }
}, {
  persist: {
    key: 'feishu-user',
    storage: localStorage,
    // 只持久化 state（不持久化函数/computed，避免反序列化失败）
    paths: ['token', 'userInfo', 'expiresAt']
  }
})
