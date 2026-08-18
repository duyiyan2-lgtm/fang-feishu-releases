/**
 * Mock 数据 —— 后端不可用时前端降级使用
 * 数据结构与真实 API 一致，字段直接返回中文
 */

// ==================== 用户 ====================

export const mockUser = {
  id: 'mock-user-id',
  username: 'admin',
  realName: '管理员',
  email: 'admin@example.com',
  phone: '13800000001',
  departmentId: 'mock-dept-tech',
  departmentName: '技术部',
  position: '项目主管',
  roles: ['Admin'],
}

export const mockToken = 'mock-token-for-development'

// ==================== 部门树 ====================

export const mockDepartments = [
  {
    id: 'root',
    name: 'FangFeishu Demo Company',
    children: [
      {
        id: 'dept-tech',
        name: 'Technology',
        sortOrder: 1,
        children: [],
      },
      {
        id: 'dept-product',
        name: 'Product',
        sortOrder: 2,
        children: [],
      },
      {
        id: 'dept-ops',
        name: 'Operations',
        sortOrder: 3,
        children: [],
      },
      {
        id: 'dept-design',
        name: 'Design',
        sortOrder: 4,
        children: [],
      },
      {
        id: 'dept-marketing',
        name: 'Marketing',
        sortOrder: 5,
        children: [],
      },
      {
        id: 'dept-hr',
        name: 'Human Resources',
        sortOrder: 6,
        children: [],
      },
      {
        id: 'dept-finance',
        name: 'Finance',
        sortOrder: 7,
        children: [],
      },
    ],
  },
]

// ==================== 成员 ====================

export const mockMembers = [
  { id: 'u1', realName: '管理员', username: 'admin', email: 'admin@example.com', phone: '13800000001', departmentId: 'dept-tech', departmentName: 'Technology', position: 'Project Lead', avatarUrl: null, workPlace: 'Demo Office', bio: '系统管理员' },
  { id: 'u2', realName: '张三', username: 'user_a', email: 'zhangsan@example.com', phone: '13800000002', departmentId: 'dept-tech', departmentName: 'Technology', position: 'Frontend Developer', avatarUrl: null, workPlace: 'Demo Office' },
  { id: 'u3', realName: '李四', username: 'user_b', email: 'lisi@example.com', phone: '13800000003', departmentId: 'dept-product', departmentName: 'Product', position: 'Product Manager', avatarUrl: null, workPlace: 'Demo Office' },
  { id: 'u4', realName: '王五', username: 'user_c', email: 'wangwu@example.com', phone: '13800000004', departmentId: 'dept-ops', departmentName: 'Operations', position: 'DevOps Engineer', avatarUrl: null, workPlace: 'Demo Office' },
  { id: 'u5', realName: '赵六', username: 'user_d', email: 'zhaoliu@example.com', phone: '13800000005', departmentId: 'dept-tech', departmentName: 'Technology', position: 'Backend Developer', avatarUrl: null, workPlace: 'Demo Office' },
  { id: 'u6', realName: '孙七', username: 'user_e', email: 'sunqi@example.com', phone: '13800000006', departmentId: 'dept-design', departmentName: 'Design', position: 'Designer', avatarUrl: null, workPlace: 'Demo Office' },
  { id: 'u7', realName: '周八', username: 'user_f', email: 'zhouba@example.com', phone: '13800000007', departmentId: 'dept-marketing', departmentName: 'Marketing', position: 'Marketing Specialist', avatarUrl: null, workPlace: 'Demo Office' },
  { id: 'u8', realName: '吴九', username: 'user_g', email: 'wujiu@example.com', phone: '13800000008', departmentId: 'dept-hr', departmentName: 'Human Resources', position: 'HR Specialist', avatarUrl: null, workPlace: 'Demo Office' },
  { id: 'u9', realName: '郑十', username: 'user_h', email: 'zhengshi@example.com', phone: '13800000009', departmentId: 'dept-finance', departmentName: 'Finance', position: 'Finance Specialist', avatarUrl: null, workPlace: 'Demo Office' },
]

// ==================== Mock 路由表 ====================

interface MockEntry {
  method: string
  url: string
  handler: (data?: any) => any
}

export const mockRoutes: MockEntry[] = [
  {
    method: 'POST',
    url: '/auth/login',
    handler: (data) => {
      if (data?.username === 'admin' && data?.password === '123456') {
        return {
          token: mockToken,
          expiresAt: '2026-12-31T23:59:59Z',
          user: { ...mockUser },
        }
      }
      throw { code: 1001, message: '账号或密码错误' }
    },
  },
  {
    method: 'GET',
    url: '/auth/me',
    handler: () => {
      return { ...mockUser }
    },
  },
  {
    method: 'GET',
    url: '/departments/tree',
    handler: () => {
      return JSON.parse(JSON.stringify(mockDepartments))
    },
  },
  {
    method: 'GET',
    url: '/contacts',
    handler: (data) => {
      let list = [...mockMembers]
      if (data?.departmentId) {
        list = list.filter((m) => m.departmentId === data.departmentId)
      }
      if (data?.keyword) {
        const kw = data.keyword.toLowerCase()
        list = list.filter(
          (m) =>
            m.realName.includes(kw) ||
            m.phone.includes(kw) ||
            m.email?.toLowerCase().includes(kw),
        )
      }
      const page = data?.page || 1
      const pageSize = data?.pageSize || 20
      const start = (page - 1) * pageSize
      return {
        items: list.slice(start, start + pageSize),
        total: list.length,
      }
    },
  },
  {
    method: 'GET',
    url: '/notifications/unread-count',
    handler: () => ({ count: 0 }),
  },
]
