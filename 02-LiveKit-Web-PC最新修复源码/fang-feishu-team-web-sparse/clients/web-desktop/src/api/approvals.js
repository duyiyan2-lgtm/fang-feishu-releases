import http from './http'

/** 列表 */
export function listApprovals() {
  return http.get('/approvals')
}

/** 创建 */
export function createApproval(payload) {
  // 后端 ApprovalRequest = { type, title, content }
  return http.post('/approvals', payload)
}

/** 通过 */
export function approveApproval(id, comment) {
  return http.patch(`/approvals/${id}/approve`, { comment: comment || '' })
}

/** 驳回 */
export function rejectApproval(id, comment) {
  return http.patch(`/approvals/${id}/reject`, { comment: comment || '' })
}

/** 催办 */
export function remindApproval(id) {
  return http.post(`/approvals/${id}/remind`, {})
}

/** 撤回（仅申请人，且状态为 Pending） */
export function withdrawApproval(id) {
  return http.post(`/approvals/${id}/withdraw`, {})
}

/** 模板列表 / 详情 / 创建 / 编辑 / 删除 */
export function listTemplates() {
  return http.get('/approvals/templates').then((d) => Array.isArray(d) ? d : (d?.items ?? []))
}
export function getTemplate(id) {
  return http.get(`/approvals/templates/${id}`)
}
export function createTemplate(payload) {
  return http.post('/approvals/templates', payload)
}
export function updateTemplate(id, payload) {
  return http.put(`/approvals/templates/${id}`, payload)
}
export function deleteTemplate(id) {
  return http.delete(`/approvals/templates/${id}`)
}

/**
 * 适配：列表项 -> 前端
 */
export function adaptApproval(a) {
  // 解析 content，可能形如 "【2026-07-07 09:00 至 2026-07-07 18:00】请假"
  const fields = parseContent(a.content)

  // 适配 status
  const status = (a.status || 'Pending').toLowerCase()

  // flow 从 records 映射
  const flow = (a.records || []).map((r, idx) => {
    const isApprove = (r.action || '').toLowerCase() === 'approve'
    return {
      node: idx === 0 ? '审批人' : `审批人 ${idx + 1}`,
      person: r.approverName || '审批人',
      status: isApprove ? 'approved' : 'rejected',
      time: formatDateTime(r.createdAt),
      comment: r.comment || '',
      raw: r
    }
  })

  return {
    id: a.id,
    type: a.type || '其他',
    typeKey: typeToKey(a.type),
    title: a.title || '审批申请',
    applicant: a.applicantName || '申请人',
    applicantId: a.applicantId,
    applicantColor: pickColor(a.applicantName || ''),
    department: '',
    createdAt: formatDateTime(a.createdAt),
    createdAtRaw: a.createdAt,
    status,
    priority: 'normal',
    fields,
    flow,
    raw: a
  }
}

function parseContent(content) {
  if (!content) return []
  // 匹配 "【A 至 B】备注"
  const m = content.match(/^【(.+?)至(.+?)】\s*(.*)$/s)
  if (m) {
    return [
      { key: 'period', label: '时间段', value: `${m[1].trim()} 至 ${m[2].trim()}` },
      { key: 'reason', label: '事由', value: m[3].trim() || '—' }
    ]
  }
  return [{ key: 'content', label: '内容', value: content }]
}

function typeToKey(type) {
  if (!type) return 'other'
  const t = type.toLowerCase()
  if (t.includes('leave') || t === '请假' || t.includes('年假') || t.includes('病假')) return 'leave'
  if (t.includes('expense') || t === '报销') return 'expense'
  if (t.includes('trip') || t === '出差') return 'trip'
  if (t.includes('overtime') || t === '加班') return 'overtime'
  if (t.includes('seal') || t === '用印') return 'seal'
  return 'other'
}

function pickColor(name) {
  const palette = ['#3370FF', '#FF7A45', '#00B96B', '#9F7AEA', '#EB2F96', '#F59E0B', '#11CDEF']
  let h = 0
  for (let i = 0; i < name.length; i++) h = (h * 31 + name.charCodeAt(i)) >>> 0
  return palette[h % palette.length]
}

function formatDateTime(iso) {
  if (!iso) return ''
  const d = new Date(iso)
  const pad = (n) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}`
}