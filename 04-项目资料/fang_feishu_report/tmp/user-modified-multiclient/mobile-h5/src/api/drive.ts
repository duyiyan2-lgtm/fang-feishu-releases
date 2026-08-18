/**
 * 云盘模块 API
 */

import { get, post, put, patch, del, BASE_URL } from './request'

// ==================== 文件 ====================

/** 文件列表（支持关键词搜索和文件夹过滤） */
export function getFiles(params?: {
  page?: number
  pageSize?: number
  keyword?: string
  folderId?: string
}) {
  return get('/files', params)
}

/** 上传文件（multipart/form-data，支持进度回调） */
export function uploadFile(
  tempFilePath: string,
  onProgress?: (pct: number) => void,
  folderId?: string,
) {
  return new Promise<any>((resolve, reject) => {
    const token = uni.getStorageSync('token') || ''
    const task = uni.uploadFile({
      url: `${BASE_URL}/files/upload`,
      filePath: tempFilePath,
      name: 'file',
      formData: folderId ? { folderId } : undefined,
      header: {
        Authorization: `Bearer ${token}`,
      },
      success: (res) => {
        try {
          const data = JSON.parse(res.data as string)
          if ((res.statusCode === 200 || res.statusCode === 201) && data.code === 0) {
            resolve(data.data)
          } else {
            reject(new Error(data.message || '上传失败'))
          }
        } catch {
          reject(new Error('上传返回格式异常'))
        }
      },
      fail: () => reject(new Error('网络错误')),
    })
    if (onProgress) {
      task.onProgressUpdate((e) => {
        onProgress(e.progress)
      })
    }
  })
}

/** 下载文件 */
export function getDownloadUrl(id: string): string {
  const token = uni.getStorageSync('token') || ''
  return `${BASE_URL}/files/${id}/download?token=${encodeURIComponent(token)}`
}

/** 删除文件（移到回收站） */
export function deleteFile(id: string) {
  return del(`/files/${id}`)
}

/** 恢复文件 */
export function restoreFile(id: string) {
  return post(`/files/${id}/restore`)
}

/** 永久删除 */
export function permanentDeleteFile(id: string) {
  return del(`/files/${id}/permanent`)
}

/** 移动文件到文件夹 */
export function moveFile(id: string, folderId?: string | null) {
  return patch(`/files/${id}/move`, { folderId })
}

/** 获取回收站列表 */
export function getTrash() {
  return get('/files/trash')
}

// ==================== 分享 ====================

/** 获取文件分享列表 */
export function getFileShares(id: string) {
  return get(`/files/${id}/shares`)
}

/** 设置文件分享 */
export function setFileShares(
  id: string,
  userIds: string[],
  permission: string,
) {
  return put(`/files/${id}/shares`, { userIds, permission })
}

// ==================== 文件夹 ====================

/** 获取文件夹列表 */
export function getFolders(parentId?: string) {
  return get('/files/folders', parentId ? { parentId } : undefined)
}

/** 创建文件夹 */
export function createFolder(name: string, parentId?: string) {
  return post('/files/folders', { name, parentId })
}

/** 更新文件夹（重命名/移动） */
export function updateFolder(id: string, name: string, parentId?: string) {
  return put(`/files/folders/${id}`, { name, parentId })
}

/** 删除文件夹（必须为空） */
export function deleteFolder(id: string) {
  return del(`/files/folders/${id}`)
}
