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
    avatarUrl: u.avatarUrl || u.avatar || null,
    avatarColor: '#3370FF',
    title: ''
  }
}

export const useUserStore = defineStore('user', () => {
  const token = ref('')
  const userInfo = ref(null)
  const expiresAt = ref(null)
  // 登出重入守卫：并发调用共享同一个清理 Promise。
  let logoutPromise = null

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

  function logout({ notifyServer = true } = {}) {
    if (logoutPromise) return logoutPromise

    const sessionToken = token.value
    // 请求显式携带退出前的 token，因此可以立即清理 UI 登录态，不会再次出现默认 admin。
    const serverLogout = notifyServer && sessionToken
      ? logoutApi(sessionToken)
      : Promise.resolve()
    token.value = ''
    userInfo.value = null
    expiresAt.value = null
    try {
      localStorage.removeItem('feishu-user')
      localStorage.removeItem('feishu-messages')
      sessionStorage.clear()
    } catch {}

    logoutPromise = Promise.allSettled([
      serverLogout,
      // 退出时同步停止旧 token 的实时连接并清除跨账号消息缓存。
      import('@/stores/messages')
        .then(({ useMessagesStore }) => useMessagesStore().resetSession())
        .catch(() => {}),
      // 防止退出后旧账号的通知红点和列表带到下一个账号。
      import('@/stores/notifications')
        .then(({ useNotificationsStore }) => useNotificationsStore().resetSession())
        .catch(() => {})
    ]).finally(() => { logoutPromise = null })
    return logoutPromise
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
