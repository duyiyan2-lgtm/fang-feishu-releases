// 当前用户 API
import http from './http'

/** 获取当前用户信息 */
export function getMyProfile() {
  return http.get('/auth/me')
}

/**
 * 更新当前用户信息
 * ⚠️ 后端要求 body 必须是 { request: { RealName, Email, ... } } 包裹 + PascalCase
 */
export function updateMyProfile(payload) {
  return http.patch('/auth/me', { request: payload })
}

/** 上传头像 — 复用 Files API */
export function uploadAvatar(file) {
  const form = new FormData()
  form.append('file', file)
  return http.post('/files/upload', form, {
    headers: { 'Content-Type': 'multipart/form-data' }
  })
}