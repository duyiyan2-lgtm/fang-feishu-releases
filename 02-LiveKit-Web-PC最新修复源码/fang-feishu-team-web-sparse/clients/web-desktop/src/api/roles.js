import http from './http'

export function listRoles() {
  return http.get('/roles').then((d) => Array.isArray(d) ? d : (d?.items ?? []))
}

export function createRole(payload) {
  return http.post('/roles', payload)
}

export function updateRole(id, payload) {
  return http.put(`/roles/${id}`, payload)
}

export function adaptRole(r) {
  return {
    id: r.id,
    name: r.roleName || r.name || '',
    code: r.roleCode || r.code || r.roleName || '',
    description: r.description || '',
    permissions: Array.isArray(r.permissions) ? r.permissions : [],
    userCount: r.userCount ?? 0,
    raw: r
  }
}
