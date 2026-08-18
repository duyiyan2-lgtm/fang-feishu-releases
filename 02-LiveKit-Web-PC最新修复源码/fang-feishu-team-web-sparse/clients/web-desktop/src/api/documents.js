import http from './http'

/** 列表 */
export function listDocuments() {
  return http.get('/documents')
}

/** 详情 */
export function getDocument(id) {
  return http.get(`/documents/${id}`)
}

/** 创建 */
export function createDocument(payload) {
  return http.post('/documents', payload)
}

/** 更新（title + content） */
export function updateDocument(id, payload) {
  return http.put(`/documents/${id}`, payload)
}

/** 发评论 */
export function postComment(docId, content) {
  return http.post(`/documents/${docId}/comments`, { content })
}

/** 评论列表 GET（之前只有 POST，是写一条丢一条） */
export function listComments(docId) {
  return http.get(`/documents/${docId}/comments`).then((d) => {
    if (Array.isArray(d)) return d
    if (Array.isArray(d?.items)) return d.items
    return []
  })
}

/** 删评论 */
export function deleteComment(docId, commentId) {
  return http.delete(`/documents/${docId}/comments/${commentId}`)
}

/** 删文档 */
export function deleteDocument(id) {
  return http.delete(`/documents/${id}`)
}

/** 协作者列表 GET / 替换 PUT */
/** 后端：{ UserIds: [uuid, ...], Permission: "View" | "Edit" } */
export function getCollaborators(docId) {
  return http.get(`/documents/${docId}/collaborators`).then((d) => Array.isArray(d) ? d : (d?.items ?? []))
}
export function setCollaborators(docId, { userIds, permission }) {
  return http.put(`/documents/${docId}/collaborators`, { UserIds: userIds, Permission: permission })
}

/** 可见性 PATCH：Organization | Private */
export function setVisibility(docId, visibility) {
  return http.patch(`/documents/${docId}/visibility`, { visibility })
}

/** 版本回滚 */
export function restoreVersion(docId, versionId) {
  return http.post(`/documents/${docId}/versions/${versionId}/restore`, {})
}

/** 版本列表（详情接口也带，这里再暴露一个） */
export function listVersions(docId) {
  return http.get(`/documents/${docId}/versions`)
}

/**
 * 适配：列表项 -> 前端
 */
export function adaptDocList(d) {
  return {
    id: d.id,
    title: d.title || '无标题',
    type: 'doc',
    updated: formatRelative(d.updatedAt),
    size: '—',
    author: d.ownerName || '未知',
    ownerId: d.ownerId,
    color: pickColor(d.title || d.id),
    raw: d
  }
}

/**
 * 适配：详情 -> 前端
 */
export function adaptDocDetail(d) {
  return {
    id: d.id,
    title: d.title || '无标题',
    content: d.content || '<p></p>',
    author: d.ownerName || '未知',
    createdAt: d.createdAt,
    updatedAt: d.updatedAt,
    updatedBy: d.updatedBy,
    comments: (d.comments || []).map(adaptComment),
    versions: (d.versions || []).map(adaptVersion),
    raw: d
  }
}

export function adaptComment(c) {
  return {
    id: c.id,
    user: c.userName || c.authorName || '用户',
    userColor: pickColor(c.userName || c.authorName || c.userId || ''),
    avatar: (c.userName || c.authorName || '用')[0],
    content: c.content,
    time: formatRelative(c.createdAt),
    raw: c
  }
}

export function adaptVersion(v) {
  return {
    id: v.id,
    time: formatRelative(v.createdAt),
    desc: `版本 ${v.id.slice(0, 8)}`,
    author: '用户',
    raw: v
  }
}

function pickColor(name) {
  const palette = ['#3370FF', '#FF7A45', '#00B96B', '#9F7AEA', '#EB2F96', '#F59E0B', '#11CDEF']
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
  if (diff < 30 * 86400) return `${Math.floor(diff / 7 / 86400)} 周前`
  return d.toISOString().slice(0, 10)
}