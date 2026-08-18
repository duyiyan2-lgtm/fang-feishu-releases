import axios from 'axios'
import { useUserStore } from '@/stores/user'
import router from '@/router'
import { ElMessage } from './toast'

const http = axios.create({
  baseURL: import.meta.env.VITE_API_BASE || '/api/v1',
  // 30s：兼容冷启动 / 数据量大场景
  timeout: 30000
})

/** 不需要 Authorization header 的公开端点 */
const PUBLIC_ENDPOINTS = [
  '/auth/login',
  '/auth/register',
  '/auth/refresh'
]

function isPublic(url) {
  if (!url) return false
  return PUBLIC_ENDPOINTS.some(p => url.includes(p))
}

/**
 * 这些端点即使返回 401 也「不」触发强制登出跳转。
 * 关键：/auth/logout 本身就是登出请求，若它的 401 又触发一次登出，
 * 会形成「logout → 401 → logout → 401 …」的无限递归（实测刷屏数百请求）。
 * logout 仍会正常附带 token（用于让服务端销毁会话），只是它的 401 被静默处理。
 */
const SILENT_401_ENDPOINTS = ['/auth/logout']
function isSilent401(url) {
  if (!url) return false
  return SILENT_401_ENDPOINTS.some(p => url.includes(p))
}

http.interceptors.request.use((config) => {
  // 公开端点：不附加 token（避免旧 token 把 login 请求变成 401）
  if (!isPublic(config.url)) {
    const userStore = useUserStore()
    if (userStore.token) config.headers.Authorization = `Bearer ${userStore.token}`
  }
  return config
})

http.interceptors.response.use(
  async (res) => {                                // ← 加 async
    const body = res.data
    if (body && typeof body === 'object' && 'code' in body) {
      // 标准信封 { code, message, data, traceId }
      if (body.code === 0 || body.code === 200 || body.code === '0') {
        return body.data
      }
      // 业务错误
      const err = new Error(body.message || '请求失败')
      err.code = body.code
      err.body = body
      err.traceId = body.traceId
      err.status = res.status

      // 401 未登录 / token 过期（仅在「非公开端点」上才做强制登出）
      // 同时：当前已经在登录页 → 不弹 toast（避免噪声）
      const onLoginPage = router.currentRoute.value.path === '/login'
      const isAuthErr = body.code === 401 || body.code === 10001 || body.code === 1001
      if (isAuthErr && !isPublic(res.config?.url) && !isSilent401(res.config?.url)) {
        const userStore = useUserStore()
        // 彻底清理：清 store + localStorage + 跳登录页（用 forceReload 避免 nextTick 状态问题）
        await userStore.logout({ notifyServer: false })
        try {
          localStorage.removeItem('feishu-user')
          localStorage.removeItem('feishu-messages')
          sessionStorage.clear()
        } catch {}
        if (!onLoginPage) {
          await router.push('/login')
        }
        return Promise.reject(err)
      }
      // 登录页上的错误交由 Login.vue 自行处理（用 form.errorMsg 显示）
      // 其他页面才弹 toast
      if (!onLoginPage) {
        ElMessage({ message: body.message || '请求失败', type: 'error' })
      }
      return Promise.reject(err)
    }
    // 非信封（如 swagger 文档、文件下载直接 blob）
    return body
  },
  async (err) => {                                    // ← 加 async
    const status = err.response?.status
    const url = err.config?.url
    const onLoginPage = router.currentRoute.value.path === '/login'

    // 网络/非信封错误的友好消息
    let msg
    if (status === 401) msg = '登录已过期，请重新登录'
    else if (status === 403) msg = '没有访问权限'
    else if (status === 404) msg = '资源不存在'
    else if (status >= 500) msg = '服务器错误，请稍后再试'
    else if (err.code === 'ECONNABORTED' || err.message?.includes('timeout')) msg = '请求超时，服务器响应较慢，请稍后再试'
    else if (err.message?.includes('Network')) msg = '网络连接失败，请检查网络'
    else msg = err.response?.data?.message || err.message || '请求失败'

    // 仅非公开端点的 401 才强制登出（/auth/logout 自身 401 静默处理，避免死循环）
    if (status === 401 && !isPublic(url) && !isSilent401(url)) {
      const userStore = useUserStore()
      await userStore.logout({ notifyServer: false })
      try {
        localStorage.removeItem('feishu-user')
        localStorage.removeItem('feishu-messages')
        sessionStorage.clear()
      } catch {}
      if (!onLoginPage) {
        await router.push('/login')                   // ← await 现在合法了
      }
      // 在登录页就不弹 toast（避免噪声）
    } else if (!onLoginPage) {
      // 只在非登录页弹 toast
      ElMessage({ message: msg, type: 'error' })
    }
    return Promise.reject(err)
  }
)

export default http
