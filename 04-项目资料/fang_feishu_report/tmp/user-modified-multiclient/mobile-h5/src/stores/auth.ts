/**
 * 认证状态管理
 * 管理登录 Token、用户信息、登录/退出
 */
import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { post, get } from '@/api/request'
import { signalR } from '@/api/signalr'

export interface UserInfo {
  id: string
  username: string
  realName: string
  email: string
  phone: string
  departmentId: string
  departmentName: string
  position: string
  roles: string[]
}

export const useAuthStore = defineStore('auth', () => {
  // ===== 状态 =====
  const token = ref('')
  const userInfo = ref<UserInfo | null>(null)

  // ===== 计算属性 =====
  const isLoggedIn = computed(() => !!token.value)
  const isAdmin = computed(() => userInfo.value?.roles?.includes('Admin') ?? false)
  const displayName = computed(() => userInfo.value?.realName || userInfo.value?.username || '')

  // ===== 初始化：从本地存储恢复登录态 =====
  function init() {
    try {
      const savedToken = uni.getStorageSync('token')
      const savedUser = uni.getStorageSync('userInfo')
      if (savedToken) {
        token.value = savedToken
      }
      if (savedUser) {
        userInfo.value = JSON.parse(savedUser)
      }
    } catch {
      // 存储读取失败，忽略
    }
  }

  // ===== 登录 =====
  async function login(username: string, password: string) {
    const res: any = await post('/auth/login', { username, password })
    token.value = res.token
    userInfo.value = res.user

    // 保存到本地存储
    uni.setStorageSync('token', res.token)
    uni.setStorageSync('userInfo', JSON.stringify(res.user))

    // 登录成功后建立 SignalR 实时连接
    signalR.connect(res.token).catch(() => {})

    return res
  }

  // ===== 获取当前用户 =====
  async function fetchUserInfo() {
    const res: any = await get('/auth/me')
    userInfo.value = res as UserInfo
    uni.setStorageSync('userInfo', JSON.stringify(res))
    return userInfo.value
  }

  // ===== 退出登录 =====
  function logout() {
    signalR.disconnect()
    token.value = ''
    userInfo.value = null
    uni.removeStorageSync('token')
    uni.removeStorageSync('userInfo')
    uni.reLaunch({ url: '/pages/login/index' })
  }

  return {
    token,
    userInfo,
    isLoggedIn,
    isAdmin,
    displayName,
    init,
    login,
    fetchUserInfo,
    logout,
  }
})
