import http from './http'

const IS_MOCK = import.meta.env.VITE_USE_MOCK === 'true'

/**
 * 登录接口（真实后端 + Mock 双模式）
 * 真实接口：POST /api/v1/auth/login
 * @param {string} account 手机号 / 邮箱 / 用户名（后端统一字段为 username）
 * @param {string} password
 */
export function loginApi({ account, password }) {
  if (IS_MOCK) {
    return import('./mock').then(({ mockUserInfo }) => {
      return new Promise((resolve, reject) => {
        setTimeout(() => {
          if (!account || !password) return reject(new Error('请输入账号和密码'))
          if (password.length < 4) return reject(new Error('密码至少 4 位'))
          resolve({
            token: 'mock-token-' + Math.random().toString(36).slice(2, 10),
            expiresAt: new Date(Date.now() + 8 * 3600_000).toISOString(),
            user: { id: 'u-self', username: account, realName: '王晓明', email: account, phone: '', departmentId: 'd3', departmentName: '前端研发组', roles: ['User'] }
          })
        }, 400)
      })
    })
  }
  return http.post('/auth/login', { username: account, password })
}

/**
 * 获取当前用户信息
 */
export function getProfileApi() {
  if (IS_MOCK) return import('./mock').then(({ mockUserInfo }) => Promise.resolve(mockUserInfo()))
  return http.get('/auth/me')
}

/**
 * 退出登录
 * 忽略后端错误（即使后端 401，客户端也会清 token，UI 上登录态已清）
 */
export function logoutApi(sessionToken) {
  if (IS_MOCK) return Promise.resolve()
  const config = sessionToken
    ? { headers: { Authorization: `Bearer ${sessionToken}` } }
    : undefined
  return http.post('/auth/logout', {}, config).catch(() => null)
}

/**
 * 注册接口（真实后端 + Mock 双模式）
 * 后端字段：Username / Password / RealName / Email / Phone（PascalCase + 包裹 request）
 */
export function registerApi(payload) {
  if (IS_MOCK) {
    return import('./mock').then(({ mockUserInfo }) => {
      return new Promise((resolve, reject) => {
        setTimeout(() => {
          if (!payload.Username || !payload.Password) return reject(new Error('用户名和密码必填'))
          if (payload.Password.length < 4) return reject(new Error('密码至少 4 位'))
          resolve({
            token: 'mock-token-' + Math.random().toString(36).slice(2, 10),
            expiresAt: new Date(Date.now() + 8 * 3600_000).toISOString(),
            user: { id: 'u-' + Date.now(), username: payload.Username, realName: payload.RealName || payload.Username, email: payload.Email, phone: payload.Phone || '', departmentId: payload.DepartmentId, departmentName: '', roles: ['User'] }
          })
        }, 400)
      })
    })
  }
  return http.post('/auth/register', payload)
}
