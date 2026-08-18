import http from './http'

const palette = ['#3370FF', '#00B96B', '#EB2F96', '#F59E0B', '#9F7AEA', '#11CDEF', '#FF7A45', '#52C41A']

/** 列表 */
export function listEvents(from, to) {
  return http.get('/calendar/events', { params: { from, to } })
}

/** 创建 */
export function createEvent(payload) {
  return http.post('/calendar/events', payload)
}

/** 更新 */
export function updateEvent(id, payload) {
  return http.put(`/calendar/events/${id}`, payload)
}

/** 删除 */
export function deleteEvent(id) {
  return http.delete(`/calendar/events/${id}`)
}

/** 忙闲查询 GET /calendar/events/free-busy?from=&to= */
export function getFreeBusy(from, to) {
  return http.get('/calendar/events/free-busy', { params: { from, to } }).then((d) => {
    if (Array.isArray(d)) return d
    if (Array.isArray(d?.items)) return d.items
    return []
  })
}

/**
 * 适配：后端 event -> 前端
 */
export function adaptEvent(e) {
  const start = new Date(e.startTime)
  const end = new Date(e.endTime)
  return {
    id: e.id,
    title: e.title || '未命名',
    date: isoDate(start),
    start: formatHM(start),
    end: formatHM(end),
    startFull: e.startTime,
    endFull: e.endTime,
    location: e.location || '',
    description: e.description || '',
    color: palette[Math.abs(hash(e.userId || e.id)) % palette.length],
    raw: e
  }
}

function isoDate(d) {
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}
function formatHM(d) {
  return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`
}
function hash(s) {
  let h = 0
  for (let i = 0; i < s.length; i++) h = (h * 31 + s.charCodeAt(i)) | 0
  return h
}