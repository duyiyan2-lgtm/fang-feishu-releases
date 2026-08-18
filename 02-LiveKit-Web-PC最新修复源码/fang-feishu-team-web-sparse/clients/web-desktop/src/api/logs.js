import http from './http'

export function listOperationLogs(params = {}) {
  return http.get('/operation-logs', { params: { page: 1, pageSize: 100, ...params } }).then((d) => {
    // 后端返回 { items, total, page, pageSize }
    if (Array.isArray(d?.items)) return d
    if (Array.isArray(d)) return { items: d, total: d.length }
    return { items: [], total: 0 }
  })
}

export function adaptLog(l) {
  return {
    id: l.id,
    module: l.module || '',
    action: l.action || '',
    target: l.targetId || l.target || '',
    user: l.userName || l.user || '',
    userId: l.userId || '',
    ip: l.ip || '',
    result: l.result || (l.success === false ? 'failed' : 'success'),
    time: l.createdAt || l.time || '',
    raw: l
  }
}
