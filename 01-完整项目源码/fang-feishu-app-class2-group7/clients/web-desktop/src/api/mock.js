// 各页面使用的模拟数据

export const mockConversations = [
  {
    id: 1, name: '产品评审组', color: '#3370FF', type: 'group',
    lastMessage: '今晚的需求评审准时开始', lastTime: '10:32', unread: 3,
    messages: [
      { id: 'm1', sender: 'other', content: '各位晚上 7 点准时开始需求评审', time: '09:50' },
      { id: 'm2', sender: 'me', content: '收到，我会准时参加', time: '09:55' },
      { id: 'm3', sender: 'other', content: '今晚的需求评审准时开始', time: '10:32' }
    ]
  },
  {
    id: 2, name: '张三', color: '#FF7A45', type: 'single',
    lastMessage: '好的，明天见！', lastTime: '09:18', unread: 0,
    messages: [
      { id: 'm1', sender: 'other', content: '明天上午 10 点的会议', time: '09:00' },
      { id: 'm2', sender: 'me', content: '收到，几点结束？', time: '09:10' },
      { id: 'm3', sender: 'other', content: '大概 11 点半', time: '09:15' },
      { id: 'm4', sender: 'me', content: '好的，明天见！', time: '09:18' }
    ]
  },
  {
    id: 3, name: '李四', color: '#00B96B', type: 'single',
    lastMessage: '代码已经合并到主干', lastTime: '昨天', unread: 1,
    messages: [{ id: 'm1', sender: 'other', content: '代码已经合并到主干', time: '昨天 18:22' }]
  },
  {
    id: 4, name: '前端小分队', color: '#9F7AEA', type: 'group',
    lastMessage: '[打卡] 王晓明 完成今日工作', lastTime: '昨天', unread: 0,
    messages: [
      { id: 'm1', sender: 'sys', content: '王晓明 加入了群聊', time: '周一 10:00' },
      { id: 'm2', sender: 'sys', content: '[打卡] 王晓明 完成今日工作', time: '昨天 17:30' }
    ]
  },
  {
    id: 5, name: 'HR 小姐姐', color: '#EB2F96', type: 'single',
    lastMessage: '请提交本周的周报', lastTime: '星期三', unread: 0,
    messages: [{ id: 'm1', sender: 'other', content: '请提交本周的周报', time: '星期三 14:00' }]
  }
]

export const mockContacts = [
  { id: 'c1', name: '张三', title: '产品经理', dept: '产品部', color: '#FF7A45', online: true, phone: '138-0000-1111', email: 'zhangsan@example.com' },
  { id: 'c2', name: '李四', title: '前端工程师', dept: '研发部', color: '#00B96B', online: true, phone: '138-0000-2222', email: 'lisi@example.com' },
  { id: 'c3', name: '王五', title: 'UI 设计师', dept: '设计部', color: '#9F7AEA', online: false, phone: '138-0000-3333', email: 'wangwu@example.com' },
  { id: 'c4', name: '赵六', title: '测试工程师', dept: '测试部', color: '#EB2F96', online: true, phone: '138-0000-4444', email: 'zhaoliu@example.com' },
  { id: 'c5', name: '钱七', title: '后端工程师', dept: '研发部', color: '#3370FF', online: false, phone: '138-0000-5555', email: 'qianqi@example.com' },
  { id: 'c6', name: '孙八', title: '运维工程师', dept: '运维部', color: '#F59E0B', online: true, phone: '138-0000-6666', email: 'sunba@example.com' },
  { id: 'c7', name: 'Alice', title: '前端工程师', dept: '研发部', color: '#5E72E4', online: true, phone: '138-0000-7777', email: 'alice@example.com' },
  { id: 'c8', name: 'Bob', title: '架构师', dept: '研发部', color: '#11CDEF', online: false, phone: '138-0000-8888', email: 'bob@example.com' }
]

const _today = new Date()
const _fmt = (offset) => {
  const d = new Date(_today)
  d.setDate(d.getDate() + offset)
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${d.getFullYear()}-${m}-${day}`
}

export const mockEvents = [
  { id: 'e1', title: '需求评审会', date: _fmt(0), start: '10:00', end: '11:30', color: '#3370FF' },
  { id: 'e2', title: '产品周会', date: _fmt(1), start: '14:00', end: '15:00', color: '#00B96B' },
  { id: 'e3', title: '客户演示', date: _fmt(3), start: '15:30', end: '16:30', color: '#EB2F96' },
  { id: 'e4', title: '代码评审', date: _fmt(5), start: '11:00', end: '12:00', color: '#9F7AEA' },
  { id: 'e5', title: '产品 Roadmap', date: _fmt(7), start: '16:00', end: '17:30', color: '#F59E0B' },
  { id: 'e6', title: '前端分享', date: _fmt(10), start: '20:00', end: '21:00', color: '#FF7A45' },
  { id: 'e7', title: '午饭', date: _fmt(2), start: '12:00', end: '13:00', color: '#737373' },
  { id: 'e8', title: '技术分享会', date: _fmt(-2), start: '15:00', end: '16:30', color: '#3370FF' },
  { id: 'e9', title: '一对一沟通', date: _fmt(8), start: '11:00', end: '11:30', color: '#00B96B' },
  { id: 'e10', title: '生日会', date: _fmt(15), start: '18:30', end: '20:00', color: '#EB2F96' },
  { id: 'e11', title: '架构评审', date: _fmt(4), start: '09:30', end: '11:00', color: '#11CDEF' }
]

export const mockDocuments = [
  { id: 'd1', title: '产品需求文档 V2.1', type: 'doc', updated: '2 小时前', size: '256 KB', author: '张三', color: '#3370FF', content: '' },
  { id: 'd2', title: '技术方案 - 消息系统重构', type: 'doc', updated: '昨天', size: '1.2 MB', author: '李四', color: '#00B96B', content: '' },
  { id: 'd3', title: '设计稿总览', type: 'figma', updated: '3 天前', size: '—', author: '王五', color: '#9F7AEA', content: '' },
  { id: 'd4', title: 'Q3 OKR 汇总', type: 'sheet', updated: '5 天前', size: '512 KB', author: '钱七', color: '#00B96B', content: '' },
  { id: 'd5', title: '项目启动说明', type: 'slide', updated: '1 周前', size: '3.4 MB', author: '孙八', color: '#F59E0B', content: '' },
  { id: 'd6', title: '接口文档', type: 'doc', updated: '2 周前', size: '420 KB', author: '李四', color: '#3370FF', content: '' },
  { id: 'd7', title: '会议纪要 - 8/15', type: 'doc', updated: '3 周前', size: '64 KB', author: '张三', color: '#3370FF', content: '' },
  { id: 'd8', title: '竞品分析报告', type: 'sheet', updated: '1 个月前', size: '890 KB', author: '赵六', color: '#00B96B', content: '' }
]

export const mockFiles = [
  { id: 'f1', name: '项目资料', type: 'folder', count: 28, color: '#FFB800' },
  { id: 'f2', name: '设计稿', type: 'folder', count: 12, color: '#FFB800' },
  { id: 'f3', name: '会议录音', type: 'audio', size: '14.2 MB', color: '#EB2F96' },
  { id: 'f4', name: '产品截图.png', type: 'image', size: '2.1 MB', color: '#00B96B' },
  { id: 'f5', name: '架构图.pdf', type: 'pdf', size: '880 KB', color: '#FF4D4F' },
  { id: 'f6', name: '活动视频', type: 'video', size: '128 MB', color: '#3370FF' },
  { id: 'f7', name: '年度报告.docx', type: 'doc', size: '3.5 MB', color: '#3370FF' },
  { id: 'f8', name: '代码片段.zip', type: 'zip', size: '5.6 MB', color: '#737373' },
  { id: 'f9', name: '员工手册', type: 'folder', count: 6, color: '#FFB800' },
  { id: 'f10', name: '截图 2024.png', type: 'image', size: '760 KB', color: '#00B96B' },
  { id: 'f11', name: '客户合同.pdf', type: 'pdf', size: '1.2 MB', color: '#FF4D4F' },
  { id: 'f12', name: '活动照片', type: 'folder', count: 18, color: '#FFB800' }
]

export function mockUserInfo(account = '') {
  return {
    id: 'u-self',
    name: '王晓明',
    email: account.includes('@') ? account : `${account}@example.com`,
    phone: /^\d+$/.test(account) ? account : '',
    department: '前端研发组',
    title: '前端工程师',
    avatarColor: '#3370FF'
  }
}

// ============ 审批（OA） ============
export const mockApprovalTypes = [
  { id: 'leave',   name: '请假申请',   icon: '🌴', color: '#3370FF' },
  { id: 'expense', name: '报销申请',   icon: '💰', color: '#00B96B' },
  { id: 'trip',    name: '出差申请',   icon: '✈️', color: '#EB2F96' },
  { id: 'overtime',name: '加班申请',   icon: '⏰', color: '#F59E0B' },
  { id: 'seal',    name: '用印申请',   icon: '🔖', color: '#9F7AEA' },
  { id: 'goods',   name: '物品领用',   icon: '📦', color: '#11CDEF' }
]

export const mockApprovals = [
  {
    id: 'A20240701001', type: 'leave', title: '请假申请 - 病假 2 天',
    applicant: '王晓明', applicantColor: '#3370FF', department: '前端研发组',
    createdAt: '2024-07-01 09:32', status: 'pending', priority: 'normal',
    fields: [
      { key: 'leaveType', label: '请假类型', value: '病假' },
      { key: 'startDate', label: '开始日期', value: '2024-07-02' },
      { key: 'endDate',   label: '结束日期', value: '2024-07-03' },
      { key: 'duration',  label: '共计',     value: '2 天' },
      { key: 'reason',    label: '请假事由', value: '感冒发烧，需要休息两天' }
    ],
    flow: [
      { node: '直属主管', person: '李经理', status: 'approved', time: '2024-07-01 10:15', comment: '注意休息' },
      { node: '部门负责人', person: '张总监', status: 'current', time: null, comment: null }
    ]
  },
  {
    id: 'A20240628008', type: 'expense', title: '报销 - 客户拜访差旅',
    applicant: '张三', applicantColor: '#FF7A45', department: '产品部',
    createdAt: '2024-06-28 14:20', status: 'approved', priority: 'high',
    fields: [
      { key: 'amount',  label: '报销金额', value: '¥2,360.00' },
      { key: 'category',label: '费用类别', value: '交通 + 住宿' },
      { key: 'project', label: '关联项目', value: '新产品发布' },
      { key: 'remark',  label: '备注',     value: '6/25 客户拜访' }
    ],
    flow: [
      { node: '直属主管', person: '李经理', status: 'approved', time: '2024-06-28 15:00', comment: '同意' },
      { node: '财务审批', person: '王财务', status: 'approved', time: '2024-06-29 09:30', comment: '审核通过' },
      { node: 'CEO',      person: '陈总',   status: 'approved', time: '2024-06-29 11:00', comment: 'OK' }
    ]
  },
  {
    id: 'A20240630023', type: 'trip', title: '出差 - 上海客户拜访',
    applicant: '李四', applicantColor: '#00B96B', department: '研发部',
    createdAt: '2024-06-30 11:00', status: 'rejected', priority: 'normal',
    fields: [
      { key: 'destination',label: '目的地',     value: '上海' },
      { key: 'startDate',  label: '出发日期',   value: '2024-07-10' },
      { key: 'endDate',    label: '返回日期',   value: '2024-07-12' },
      { key: 'duration',   label: '共计',       value: '3 天' },
      { key: 'estimated',  label: '预算',       value: '¥5,000' }
    ],
    flow: [
      { node: '直属主管', person: '李经理', status: 'rejected', time: '2024-06-30 14:00', comment: '当前项目紧急，建议改期' }
    ]
  },
  {
    id: 'A20240702001', type: 'overtime', title: '加班 - 周末赶项目',
    applicant: '钱七', applicantColor: '#3370FF', department: '研发部',
    createdAt: '2024-07-02 18:30', status: 'pending', priority: 'normal',
    fields: [
      { key: 'date',     label: '加班日期', value: '2024-07-06 至 2024-07-07' },
      { key: 'duration', label: '加班时长', value: '16 小时' },
      { key: 'reason',   label: '加班原因', value: '新产品上线前最后冲刺' }
    ],
    flow: [
      { node: '直属主管', person: '孙经理', status: 'current', time: null, comment: null }
    ]
  },
  {
    id: 'A20240702002', type: 'seal', title: '用印 - 客户合同',
    applicant: '孙八', applicantColor: '#F59E0B', department: '销售部',
    createdAt: '2024-07-02 16:00', status: 'pending', priority: 'high',
    fields: [
      { key: 'sealType', label: '印章类型', value: '合同章' },
      { key: 'document', label: '文件名称', value: '客户合作协议 v2.1' },
      { key: 'count',    label: '盖章份数', value: '3 份' }
    ],
    flow: [
      { node: '部门主管', person: '张总监', status: 'current', time: null, comment: null },
      { node: '法务审核', person: '陈律师', status: 'wait',     time: null, comment: null }
    ]
  },
  {
    id: 'A20240629009', type: 'leave', title: '请假 - 年假 5 天',
    applicant: '我', applicantColor: '#3370FF', department: '前端研发组',
    createdAt: '2024-06-29 09:00', status: 'pending', priority: 'normal',
    fields: [
      { key: 'leaveType', label: '请假类型', value: '年假' },
      { key: 'startDate', label: '开始日期', value: '2024-08-01' },
      { key: 'endDate',   label: '结束日期', value: '2024-08-05' },
      { key: 'duration',  label: '共计',     value: '5 天' },
      { key: 'reason',    label: '请假事由', value: '家庭旅行' }
    ],
    flow: [
      { node: '直属主管', person: '李经理', status: 'current', time: null, comment: null }
    ]
  }
]

// ============ 应用中心 ============
export const mockAppStore = [
  { id: 'im',       name: '即时通讯',     desc: '团队消息、群聊、文件传输',          category: 'communication', installed: true,  downloads: 128000, rating: 4.9, color: '#3370FF', icon: '💬', author: '官方' },
  { id: 'mail',     name: '企业邮箱',     desc: '专业企业邮箱服务',                category: 'communication', installed: true,  downloads: 89000,  rating: 4.7, color: '#EB2F96', icon: '📧', author: '官方' },
  { id: 'video',    name: '视频会议',     desc: '高清流畅的远程视频会议',            category: 'meeting',      installed: true,  downloads: 56000,  rating: 4.8, color: '#F59E0B', icon: '📹', author: '官方' },
  { id: 'doc',      name: '在线文档',     desc: '多人实时协作的云端文档',            category: 'productivity', installed: true,  downloads: 95000,  rating: 4.9, color: '#00B96B', icon: '📝', author: '官方' },
  { id: 'oa',       name: '审批 OA',      desc: '自定义审批流程与表单',              category: 'workflow',     installed: true,  downloads: 45000,  rating: 4.6, color: '#9F7AEA', icon: '✅', author: '官方' },
  { id: 'calendar', name: '智能日历',     desc: '团队日程协同',                    category: 'productivity', installed: true,  downloads: 38000,  rating: 4.5, color: '#11CDEF', icon: '📅', author: '官方' },
  { id: 'drive',    name: '云盘',         desc: '企业级文件存储与共享',              category: 'productivity', installed: true,  downloads: 67000,  rating: 4.7, color: '#FFB800', icon: '☁️', author: '官方' },
  { id: 'crm',      name: 'CRM 客户管理', desc: '客户关系管理与销售漏斗',            category: 'business',     installed: false, downloads: 33000,  rating: 4.4, color: '#FF4D4F', icon: '👥', author: '销售易' },
  { id: 'hr',       name: 'HR 人事管理',  desc: '员工档案、考勤、薪酬一站式管理',     category: 'business',     installed: false, downloads: 28000,  rating: 4.3, color: '#52C41A', icon: '🧑‍💼', author: '薪人薪事' },
  { id: 'report',   name: '数据报表',     desc: '可视化 BI 分析报表',               category: 'data',         installed: false, downloads: 19000,  rating: 4.5, color: '#722ED1', icon: '📊', author: '帆软' },
  { id: 'project',  name: '项目管理',     desc: '敏捷迭代、任务追踪、燃尽图',        category: 'workflow',     installed: false, downloads: 41000,  rating: 4.6, color: '#13C2C2', icon: '🎯', author: 'PingCode' },
  { id: 'wiki',     name: '知识库',       desc: '团队知识沉淀与文档分享',            category: 'productivity', installed: false, downloads: 24000,  rating: 4.4, color: '#FA8C16', icon: '📚', author: '官方' }
]

// ============ 通知中心 ============
export const mockNotifications = [
  { id: 'n1', type: 'mention', title: '@你 看一下项目方案', content: '张三 在「2024 项目方案」中 @了你', source: '张三', createdAt: '5 分钟前', read: false, color: '#3370FF' },
  { id: 'n2', type: 'system',  title: '系统升级通知',         content: '系统将于今晚 22:00 - 23:00 进行例行升级', source: '系统消息', createdAt: '1 小时前', read: false, color: '#EB2F96' },
  { id: 'n3', type: 'approve', title: '请假申请待审批',       content: '钱七 提交了 加班申请，期望你审批', source: '钱七', createdAt: '2 小时前', read: false, color: '#F59E0B' },
  { id: 'n4', type: 'comment', title: '新评论',              content: '张三 评论了你的文档《产品需求文档》', source: '张三', createdAt: '3 小时前', read: false, color: '#00B96B' },
  { id: 'n5', type: 'like',    title: '有人点赞了',           content: '李四 点赞了你的分享《前端最佳实践》', source: '李四', createdAt: '昨天', read: false, color: '#9F7AEA' },
  { id: 'n6', type: 'mention', title: '@你 看一下需求',       content: '王五 在评论中 @了你', source: '王五', createdAt: '昨天', read: true, color: '#3370FF' },
  { id: 'n7', type: 'system',  title: '密码即将过期',         content: '您的账号密码将在 7 天后过期，请及时修改', source: '系统消息', createdAt: '2 天前', read: true, color: '#EB2F96' },
  { id: 'n8', type: 'approve', title: '审批已通过',           content: '你发起的报销申请已通过', source: '审批系统', createdAt: '3 天前', read: true, color: '#52C41A' }
]

// ============ 管理后台 ============
export const mockAdminUsers = [
  { id: 'u1', name: '王晓明', email: 'wangxm@example.com', dept: '研发部', role: '管理员',    status: 'active',   lastLogin: '刚刚' },
  { id: 'u2', name: '张三',   email: 'zhangs@example.com', dept: '产品部', role: '普通用户', status: 'active',   lastLogin: '10 分钟前' },
  { id: 'u3', name: '李四',   email: 'lisi@example.com',  dept: '研发部', role: '部门主管', status: 'active',   lastLogin: '1 小时前' },
  { id: 'u4', name: '王五',   email: 'wangw@example.com',  dept: '设计部', role: '普通用户', status: 'active',   lastLogin: '昨天' },
  { id: 'u5', name: '赵六',   email: 'zhaol@example.com', dept: '测试部', role: '普通用户', status: 'disabled', lastLogin: '3 天前' },
  { id: 'u6', name: '钱七',   email: 'qianq@example.com', dept: '研发部', role: '普通用户', status: 'active',   lastLogin: '2 小时前' },
  { id: 'u7', name: '孙八',   email: 'sunb@example.com',  dept: '销售部', role: '部门主管', status: 'active',   lastLogin: '5 分钟前' },
  { id: 'u8', name: 'Alice',  email: 'alice@example.com', dept: '研发部', role: '普通用户', status: 'active',   lastLogin: '昨天' }
]

export const mockAdminRoles = [
  { id: 'r1', name: '管理员',  code: 'admin',   desc: '拥有所有权限',     userCount: 2,  permissions: ['*'] },
  { id: 'r2', name: '部门主管',code: 'manager', desc: '管理部门所有资源', userCount: 5,  permissions: ['user.read', 'user.write', 'doc.read', 'doc.write', 'approval.read', 'approval.write'] },
  { id: 'r3', name: '普通用户',code: 'user',    desc: '基础使用权限',     userCount: 86, permissions: ['doc.read', 'doc.write', 'im.send'] },
  { id: 'r4', name: '访客',    code: 'guest',   desc: '只读权限',         userCount: 12, permissions: ['doc.read'] }
]

export const mockPermissionTree = [
  {
    name: '系统管理', children: [
      { name: '用户管理',    code: 'user.manage' },
      { name: '角色权限',    code: 'role.manage' },
      { name: '操作日志',    code: 'log.read' }
    ]
  },
  {
    name: '文档协作', children: [
      { name: '查看文档',    code: 'doc.read' },
      { name: '编辑文档',    code: 'doc.write' },
      { name: '删除文档',    code: 'doc.delete' }
    ]
  },
  {
    name: '审批流程', children: [
      { name: '查看审批',    code: 'approval.read' },
      { name: '发起审批',    code: 'approval.write' },
      { name: '审批操作',    code: 'approval.act' }
    ]
  },
  {
    name: '应用中心', children: [
      { name: '安装应用',    code: 'app.install' },
      { name: '配置应用',    code: 'app.config' }
    ]
  }
]

export const mockAdminDict = [
  {
    category: '请假类型', code: 'leave_type', items: [
      { id: 1, label: '事假',   value: 'personal',  sort: 1 },
      { id: 2, label: '病假',   value: 'sick',      sort: 2 },
      { id: 3, label: '年假',   value: 'annual',    sort: 3 },
      { id: 4, label: '调休',   value: 'compensatory', sort: 4 },
      { id: 5, label: '婚假',   value: 'marriage',  sort: 5 }
    ]
  },
  {
    category: '报销类别', code: 'expense_type', items: [
      { id: 1, label: '交通',   value: 'transport', sort: 1 },
      { id: 2, label: '住宿',   value: 'lodging',   sort: 2 },
      { id: 3, label: '餐饮',   value: 'meal',      sort: 3 },
      { id: 4, label: '办公',   value: 'office',    sort: 4 }
    ]
  },
  {
    category: '用户状态', code: 'user_status', items: [
      { id: 1, label: '正常',   value: 'active',    sort: 1 },
      { id: 2, label: '禁用',   value: 'disabled',  sort: 2 },
      { id: 3, label: '离职',   value: 'resigned',  sort: 3 }
    ]
  }
]

export const mockAdminLogs = [
  { id: 'L001', module: '用户管理',   action: '创建用户', user: '王晓明', target: '新建用户 Bob',     ip: '10.71.2.235', time: '2 分钟前',  result: 'success' },
  { id: 'L002', module: '角色权限',   action: '修改权限', user: '王晓明', target: '调整「部门主管」权限', ip: '10.71.2.235', time: '10 分钟前', result: 'success' },
  { id: 'L003', module: '审批管理',   action: '审批通过', user: '张三',   target: 'A20240701001 请假', ip: '10.71.2.100', time: '1 小时前',  result: 'success' },
  { id: 'L004', module: '登录认证',   action: '登录失败', user: 'unknown',target: '账号 admin 密码错误', ip: '203.0.113.5',  time: '2 小时前',  result: 'failure' },
  { id: 'L005', module: '文档管理',   action: '删除文档', user: '李四',   target: '删除「过期方案 v1.0」', ip: '10.71.2.88',  time: '昨天',      result: 'success' },
  { id: 'L006', module: '应用中心',   action: '安装应用', user: '孙八',   target: '安装「CRM 客户管理」',  ip: '10.71.2.55',  time: '昨天',      result: 'success' },
  { id: 'L007', module: '数据字典',   action: '新增字典', user: '王晓明', target: '新增「项目状态」字典', ip: '10.71.2.235', time: '2 天前',    result: 'success' },
  { id: 'L008', module: '登录认证',   action: '退出登录', user: 'Alice',  target: '用户主动退出',     ip: '10.71.2.18',  time: '2 天前',    result: 'success' },
  { id: 'L009', module: 'Webhook',    action: '触发通知', user: '系统',   target: '推送消息到 https://api.example.com', ip: '127.0.0.1', time: '3 天前', result: 'success' },
  { id: 'L010', module: '开放平台',   action: '创建应用', user: '王晓明', target: '创建第三方应用「测试APP」', ip: '10.71.2.235', time: '3 天前', result: 'success' }
]

// ============ 开放平台 ============
export const mockApiDocs = [
  { method: 'GET',    path: '/api/v1/users',           desc: '获取用户列表', group: '用户' },
  { method: 'GET',    path: '/api/v1/users/:id',       desc: '获取单个用户信息', group: '用户' },
  { method: 'POST',   path: '/api/v1/users',           desc: '创建用户',     group: '用户' },
  { method: 'PUT',    path: '/api/v1/users/:id',       desc: '更新用户信息', group: '用户' },
  { method: 'DELETE', path: '/api/v1/users/:id',       desc: '删除用户',     group: '用户' },
  { method: 'GET',    path: '/api/v1/messages',        desc: '获取消息列表', group: '消息' },
  { method: 'POST',   path: '/api/v1/messages',        desc: '发送消息',     group: '消息' },
  { method: 'GET',    path: '/api/v1/approvals',       desc: '获取审批列表', group: '审批' },
  { method: 'POST',   path: '/api/v1/approvals',       desc: '发起审批',     group: '审批' },
  { method: 'POST',   path: '/api/v1/approvals/:id/approve', desc: '审批通过', group: '审批' },
  { method: 'GET',    path: '/api/v1/documents',       desc: '获取文档列表', group: '文档' },
  { method: 'POST',   path: '/api/v1/calendar/events', desc: '创建日历事件', group: '日历' }
]

export const mockWebhooks = [
  { id: 'w1', name: '订单通知',   url: 'https://api.example.com/webhook/order', event: ['order.created', 'order.paid'], status: 'active',   secret: 'whsec_****b8a2', createdAt: '2024-06-15' },
  { id: 'w2', name: '审批回调',   url: 'https://crm.example.com/hook/approval', event: ['approval.done'], status: 'active',   secret: 'whsec_****91ce', createdAt: '2024-06-20' },
  { id: 'w3', name: '消息推送',   url: 'https://bot.example.com/incoming',     event: ['message.received'], status: 'paused', secret: 'whsec_****3f4d', createdAt: '2024-07-01' },
  { id: 'w4', name: '用户同步',   url: 'https://hr.example.com/sync',          event: ['user.created', 'user.updated', 'user.deleted'], status: 'active', secret: 'whsec_****a7e8', createdAt: '2024-05-30' }
]

export const mockPlatformApps = [
  { id: 'a1', name: '测试APP',     appId: 'cli_test1234', secret: 'sk_****b1c2', scope: ['user.read', 'im.send'], status: 'active',  createdAt: '2024-06-01' },
  { id: 'a2', name: '小程序后端',   appId: 'cli_mini5678', secret: 'sk_****d3e4', scope: ['doc.read', 'im.send', 'calendar.read'], status: 'active', createdAt: '2024-06-15' },
  { id: 'a3', name: '数据看板',    appId: 'cli_dash9012', secret: 'sk_****f5g6', scope: ['doc.read', 'approval.read'],           status: 'active', createdAt: '2024-07-01' },
  { id: 'a4', name: '外部 CRM 集成', appId: 'cli_crm3456', secret: 'sk_****h7i8', scope: ['user.read', 'approval.read', 'approval.write'], status: 'disabled', createdAt: '2024-05-20' }
]


// ============ 部门树（组织架构） ============
export const mockDepts = [
  { id: 'd1', name: '飞书科技',     parentId: null, leader: '陈总',   count: 256, sort: 1, icon: '🏢' },
  { id: 'd2', name: '研发中心',     parentId: 'd1', leader: '李副总', count: 120, sort: 1, icon: '👨‍💻' },
  { id: 'd3', name: '前端研发组',   parentId: 'd2', leader: '王晓明', count: 18,  sort: 1, icon: '🎨' },
  { id: 'd4', name: '后端研发组',   parentId: 'd2', leader: '钱七',   count: 45,  sort: 2, icon: '⚙️' },
  { id: 'd5', name: '测试部',       parentId: 'd2', leader: '赵六',   count: 22,  sort: 3, icon: '🧪' },
  { id: 'd6', name: '产品部',       parentId: 'd1', leader: '张三',   count: 35,  sort: 2, icon: '📊' },
  { id: 'd7', name: '产品设计组',   parentId: 'd6', leader: '王五',   count: 8,   sort: 1, icon: '🎯' },
  { id: 'd8', name: '产品运营组',   parentId: 'd6', leader: 'Tom',    count: 12,  sort: 2, icon: '📈' },
  { id: 'd9', name: '销售部',       parentId: 'd1', leader: '孙八',   count: 60,  sort: 3, icon: '💼' },
  { id: 'd10',name: '人事行政部',   parentId: 'd1', leader: '陈姐',   count: 14,  sort: 4, icon: '🧑‍💼' },
  { id: 'd11',name: '财务部',       parentId: 'd1', leader: '汪会计', count: 8,   sort: 5, icon: '💰' }
]

// ============ 文档评论 ============
export const mockDocComments = {
  d1: [
    { id: 'c1', user: '张三',   userColor: '#FF7A45', content: '这里需求描述可以再具体一些吗？例如异常场景。', time: '2 小时前', avatar: '张' },
    { id: 'c2', user: '李四',   userColor: '#00B96B', content: '已补充，请看第三节的「异常流」。', time: '1 小时前', avatar: '李' },
    { id: 'c3', user: '王晓明', userColor: '#3370FF', content: '我建议把性能指标写到顶部。', time: '30 分钟前', avatar: '王' }
  ],
  d2: [
    { id: 'c4', user: '钱七',   userColor: '#3370FF', content: '消息系统的 QPS 上限能详细说明吗？', time: '昨天', avatar: '钱' }
  ],
  d3: [{ id: 'c5', user: '王五', userColor: '#9F7AEA', content: '设计稿入口已附在末尾。', time: '3 天前', avatar: '王' }],
  d4: [], d5: [], d6: [], d7: [], d8: []
}

// ============ 文档版本历史 ============
export const mockDocVersions = {
  d1: [
    { id: 'v5', time: '12:45', author: '我',   desc: '更新 - 编辑「产品需求文档 V2.1」' },
    { id: 'v4', time: '11:30', author: '张三', desc: '编辑 - 增加了异常流说明' },
    { id: 'v3', time: '昨天 17:00', author: '我', desc: '更新 - 添加了性能指标' },
    { id: 'v2', time: '昨天 14:00', author: '我', desc: '创建 - 初始化文档' },
    { id: 'v1', time: '2 天前',   author: '张三', desc: '创建 - 初始模板' }
  ],
  d2: [
    { id: 'v3', time: '18:00', author: '李四', desc: '更新 - 修订架构图' },
    { id: 'v2', time: '昨天',   author: '李四', desc: '更新 - 加入消息撤回方案' },
    { id: 'v1', time: '2 周前', author: '李四', desc: '创建' }
  ],
  d3: [{ id: 'v1', time: '3 天前', author: '王五', desc: '创建' }],
  d4: [], d5: [], d6: [], d7: [], d8: []
}
