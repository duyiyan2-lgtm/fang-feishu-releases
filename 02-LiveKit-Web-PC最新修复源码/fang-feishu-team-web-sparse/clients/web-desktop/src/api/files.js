import http from './http'

/** 文件列表 */
export function listFiles() {
  return http.get('/files')
}

/** 上传 */
export function uploadFile(file, onProgress) {
  const form = new FormData()
  form.append('file', file)
  return http.post('/files/upload', form, {
    headers: { 'Content-Type': 'multipart/form-data' },
    onUploadProgress: (e) => {
      if (onProgress && e.total) onProgress(Math.round((e.loaded / e.total) * 100))
    }
  })
}

/** 下载（返回 blob URL，调用方负责 a.download 触发） */
export async function downloadFile(id) {
  const res = await http.get(`/files/${id}/download`, { responseType: 'blob' })
  return res
}

/** 删除 */
export function deleteFile(id) {
  return http.delete(`/files/${id}`)
}

/** 还原（从回收站） */
export function restoreFile(id) {
  return http.post(`/files/${id}/restore`, {})
}

/** 彻底删除（从回收站） */
export function permanentDeleteFile(id) {
  return http.delete(`/files/${id}/permanent`)
}

/** 移动文件 */
export function moveFile(id, targetFolderId) {
  return http.patch(`/files/${id}/move`, { targetFolderId })
}

/** 文件夹列表 / 创建 / 改名 / 删除 */
export function listFolders() {
  return http.get('/files/folders').then((d) => Array.isArray(d) ? d : (d?.items ?? []))
}
export function createFolder(payload) {
  return http.post('/files/folders', payload)
}
export function renameFolder(id, name) {
  return http.put(`/files/folders/${id}`, { name })
}
export function deleteFolder(id) {
  return http.delete(`/files/folders/${id}`)
}

/** 回收站 */
export function listTrash() {
  return http.get('/files/trash').then((d) => Array.isArray(d) ? d : (d?.items ?? []))
}

/** 文件预览（返回原始 content + content-type） */
export async function previewFile(id) {
  const res = await http.get(`/files/${id}/preview`, { responseType: 'blob' })
  return res
}

/** 分享：GET 列表 / PUT 替换 */
export function getShares(id) {
  return http.get(`/files/${id}/shares`).then((d) => Array.isArray(d) ? d : (d?.items ?? []))
}
export function setShares(id, { userIds, permission }) {
  return http.put(`/files/${id}/shares`, { UserIds: userIds, Permission: permission })
}

/**
 * 适配：后端 FileItem -> 前端
 */
export function adaptFile(f) {
  return {
    id: f.id,
    name: f.fileName || '未命名',
    size: formatSize(f.fileSize),
    sizeBytes: f.fileSize,
    type: typeFromMime(f.contentType || '', f.fileName || ''),
    color: pickColor(f.fileName),
    uploaderId: f.uploaderId,
    uploaderName: f.uploaderName,
    createdAt: f.createdAt,
    updated: formatRelative(f.createdAt),
    raw: f
  }
}

function formatSize(bytes) {
  if (!bytes && bytes !== 0) return '—'
  if (bytes < 1024) return bytes + ' B'
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB'
  if (bytes < 1024 * 1024 * 1024) return (bytes / 1024 / 1024).toFixed(1) + ' MB'
  return (bytes / 1024 / 1024 / 1024).toFixed(1) + ' GB'
}

function typeFromMime(mime, name) {
  const ext = (name.split('.').pop() || '').toLowerCase()
  if (ext) {
    if (['png','jpg','jpeg','gif','webp','svg','bmp'].includes(ext)) return 'image'
    if (['mp4','mov','avi','webm','mkv'].includes(ext)) return 'video'
    if (['mp3','wav','ogg','flac','m4a'].includes(ext)) return 'audio'
    if (ext === 'pdf') return 'pdf'
    if (['doc','docx','md','rtf'].includes(ext)) return 'doc'
    if (['xls','xlsx','csv'].includes(ext)) return 'sheet'
    if (['ppt','pptx'].includes(ext)) return 'slide'
    if (['zip','rar','7z','tar','gz'].includes(ext)) return 'zip'
  }
  if (!mime) return 'doc'
  if (mime.startsWith('image/')) return 'image'
  if (mime.startsWith('video/')) return 'video'
  if (mime.startsWith('audio/')) return 'audio'
  if (mime === 'application/pdf') return 'pdf'
  if (mime.includes('zip') || mime.includes('compressed')) return 'zip'
  return 'doc'
}

function pickColor(name) {
  const palette = ['#FFB800', '#FF7A45', '#00B96B', '#3370FF', '#EB2F96', '#9F7AEA', '#11CDEF', '#F59E0B']
  let h = 0
  for (let i = 0; i < name.length; i++) h = (h * 31 + name.charCodeAt(i)) >>> 0
  return palette[h % palette.length]
}

function formatRelative(iso) {
  if (!iso) return ''
  const d = new Date(iso)
  const now = Date.now()
  const diff = Math.floor((now - d.getTime()) / 1000)
  if (diff < 60) return '刚刚'
  if (diff < 3600) return `${Math.floor(diff / 60)} 分钟前`
  if (diff < 86400) return `${Math.floor(diff / 3600)} 小时前`
  if (diff < 7 * 86400) return `${Math.floor(diff / 86400)} 天前`
  return d.toISOString().slice(0, 10)
}