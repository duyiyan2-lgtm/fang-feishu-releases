/**
 * 移动端请求封装（ uni.request ）
 * 自动带 Token，统一错误处理
 * 后端不可用时自动降级到 Mock 数据
 */

import { mockRoutes, mockUser, mockToken } from './mock'

/**
 * H5 开发模式用 Vite 代理避免浏览器跨域；
 * 小程序模式（无跨域限制）及生产环境用直连 URL。
 */
const isH5Dev = typeof window !== 'undefined' && import.meta.env.DEV
const BASE_URL = isH5Dev ? '/api/v1' : 'https://alxy.fun/api/v1'
export { BASE_URL }
const USE_MOCK = false // 设为 true 可启用 Mock

interface RequestOptions {
  url: string
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'
  data?: any
  header?: Record<string, string>
}

/** 获取 Token */
function getToken(): string {
  try {
    return uni.getStorageSync('token') || ''
  } catch {
    return ''
  }
}

/** Mock 匹配 */
function handleMock<T>(options: RequestOptions): Promise<T> {
  return new Promise((resolve, reject) => {
    const route = mockRoutes.find(
      (r) => r.method === (options.method || 'GET') && r.url === options.url,
    )
    if (!route) {
      reject(new Error(`Mock: 未匹配的路由 ${options.method} ${options.url}`))
      return
    }
    // 模拟网络延迟
    setTimeout(() => {
      try {
        const result = route.handler(options.data)
        resolve(result as T)
      } catch (err: any) {
        reject(new Error(err.message || 'Mock error'))
      }
    }, 300)
  })
}

/** 统一请求 */
let lastAuthRedirect = 0 // 防止 401 风暴：5 秒内只重定向一次
function request<T = any>(options: RequestOptions): Promise<T> {
  // Mock 模式直接返回模拟数据
  if (USE_MOCK) {
    return handleMock<T>(options)
  }

  return new Promise((resolve, reject) => {
    const token = getToken()
    const header: Record<string, string> = {
      'Content-Type': 'application/json',
      ...(options.header || {}),
    }
    if (token) {
      header['Authorization'] = `Bearer ${token}`
    }

    // 登录页不触发自动跳转
    const isLoginPage = options.url === '/auth/login'

    uni.request({
      url: BASE_URL + options.url,
      method: options.method || 'GET',
      // 微信小程序 uni.request 对 PUT/PATCH/DELETE 的 data 序列化不稳定
      // 手动 JSON.stringify 确保 body 格式正确
      data: options.method && options.method !== 'GET' ? JSON.stringify(options.data) : options.data,
      header,
      dataType: 'json',
      timeout: 15000,
      success: (res) => {
        const data = res.data as any
        if ((res.statusCode === 200 || res.statusCode === 201) && data?.code === 0) {
          resolve(data.data as T)
        } else if ((res.statusCode === 401 || data?.code === 401) && !isLoginPage) {
          // Token 过期，跳转登录（5 秒内不重复跳转，防止多个请求同时触发）
          const now = Date.now()
          if (now - lastAuthRedirect > 5000) {
            lastAuthRedirect = now
            uni.removeStorageSync('token')
            uni.removeStorageSync('userInfo')
            uni.showToast({ title: '登录已过期，请重新登录', icon: 'none' })
            uni.reLaunch({ url: '/pages/login/index' })
          }
          reject(new Error(data?.message || '登录已过期'))
        } else {
          // 显示具体错误信息 + HTTP 状态码，方便调试
          const bodyPreview = typeof data === 'string' ? data.substring(0, 80) : ''
          const detail = data?.message || bodyPreview || `HTTP ${res.statusCode}`
          reject(new Error(detail))
        }
      },
      fail: () => {
        // 网络错误，不降级 Mock，让调用方 catch 处理
        reject(new Error('网络连接失败，请检查后端服务是否可用'))
      },
    })
  })
}

// ===== 登录拦截：Mock 模式下直接保存 token =====
// 如果使用 Mock 登录，手动保存 token 到 storage
if (USE_MOCK) {
  try {
    if (!uni.getStorageSync('token') && !uni.getStorageSync('mock_initialized')) {
      uni.setStorageSync('mock_initialized', 'true')
    }
    // 如果已有 mock token，自动补充 userInfo
    if (uni.getStorageSync('token') === mockToken && !uni.getStorageSync('userInfo')) {
      uni.setStorageSync('userInfo', JSON.stringify(mockUser))
    }
  } catch {}
}

/** GET 请求 */
export function get<T = any>(url: string, data?: any): Promise<T> {
  return request<T>({ url, method: 'GET', data })
}

/** POST 请求 */
export function post<T = any>(url: string, data?: any): Promise<T> {
  return request<T>({ url, method: 'POST', data })
}

/** PUT 请求 */
export function put<T = any>(url: string, data?: any): Promise<T> {
  return request<T>({ url, method: 'PUT', data })
}

/** PATCH 请求 */
export function patch<T = any>(url: string, data?: any): Promise<T> {
  return request<T>({ url, method: 'PATCH', data })
}

/** DELETE 请求 */
export function del<T = any>(url: string): Promise<T> {
  return request<T>({ url, method: 'DELETE' })
}
