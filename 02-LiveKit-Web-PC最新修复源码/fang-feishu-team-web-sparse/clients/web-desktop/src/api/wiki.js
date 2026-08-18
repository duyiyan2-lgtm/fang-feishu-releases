import http from './http'

export function listSpaces() {
  return http.get('/wiki/spaces').then((d) => Array.isArray(d) ? d : (d?.items ?? []))
}
export function getSpace(id) {
  return http.get(`/wiki/spaces/${id}`)
}
export function createSpace(payload) {
  return http.post('/wiki/spaces', payload)
}
export function updateSpace(id, payload) {
  return http.put(`/wiki/spaces/${id}`, payload)
}
export function deleteSpace(id) {
  return http.delete(`/wiki/spaces/${id}`)
}

export function listNodes(spaceId) {
  return http.get(`/wiki/spaces/${spaceId}/nodes`).then((d) => Array.isArray(d) ? d : (d?.items ?? []))
}
export function createNode(spaceId, payload) {
  return http.post(`/wiki/spaces/${spaceId}/nodes`, payload)
}
export function updateNode(spaceId, nodeId, payload) {
  return http.put(`/wiki/spaces/${spaceId}/nodes/${nodeId}`, payload)
}
export function deleteNode(spaceId, nodeId) {
  return http.delete(`/wiki/spaces/${spaceId}/nodes/${nodeId}`)
}

export function listMembers(spaceId) {
  return http.get(`/wiki/spaces/${spaceId}/members`).then((d) => Array.isArray(d) ? d : (d?.items ?? []))
}
export function setMembers(spaceId, members) {
  return http.put(`/wiki/spaces/${spaceId}/members`, { members })
}

export function searchWiki(keyword) {
  return http.get('/wiki/search', { params: keyword ? { keyword } : {} }).then((d) => Array.isArray(d) ? d : (d?.items ?? []))
}
