import { createRouter, createWebHistory } from 'vue-router'
import { useUserStore } from '@/stores/user'

const routes = [
  { path: '/login', name: 'Login', component: () => import('@/views/Login.vue'), meta: { title: '登录', public: true } },
  { path: '/register', name: 'Register', component: () => import('@/views/RegisterView.vue'), meta: { title: '注册', public: true } },
  {
    path: '/',
    component: () => import('@/views/MainLayout.vue'),
    redirect: '/home',
    children: [
      { path: 'home',          name: 'Home',          component: () => import('@/views/HomeView.vue'),     meta: { title: '首页' } },
      { path: 'messages',      name: 'Messages',      component: () => import('@/views/Messages.vue'),      meta: { title: '消息' } },
      { path: 'notifications', name: 'Notifications', component: () => import('@/views/Notifications.vue'), meta: { title: '消息通知' } },
      { path: 'calendar',      name: 'Calendar',      component: () => import('@/views/Calendar.vue'),      meta: { title: '日历' } },
      { path: 'documents',     name: 'Documents',     component: () => import('@/views/Documents.vue'),     meta: { title: '文档' } },
      { path: 'documents/:id', name: 'DocumentEditor',component: () => import('@/views/DocumentEditor.vue'),meta: { title: '文档编辑' } },
      { path: 'cloud',         name: 'Cloud',         component: () => import('@/views/Cloud.vue'),         meta: { title: '云空间' } },
      { path: 'contacts',      name: 'Contacts',      component: () => import('@/views/Contacts.vue'),      meta: { title: '联系人' } },
      { path: 'friends',       name: 'Friends',       component: () => import('@/views/FriendsListView.vue'), meta: { title: '好友' } },
      { path: 'settings',      name: 'Settings',      component: () => import('@/views/SettingsView.vue'),     meta: { title: '账号设置' } },
      { path: 'tasks',         name: 'Tasks',         component: () => import('@/views/TasksView.vue'),        meta: { title: '任务' } },
      { path: 'wiki',          name: 'Wiki',          component: () => import('@/views/WikiView.vue'),         meta: { title: '知识库' } },

      // 审批（OA）
      { path: 'approvals',                  name: 'ApprovalList',   component: () => import('@/views/Approval/ApprovalList.vue'),   meta: { title: '审批' } },
      { path: 'approvals/new',              name: 'ApprovalCreate', component: () => import('@/views/Approval/ApprovalCreate.vue'), meta: { title: '发起审批' } },
      { path: 'approvals/:id',              name: 'ApprovalDetail', component: () => import('@/views/Approval/ApprovalDetail.vue'), meta: { title: '审批详情' } },

      // 应用中心 / 开放平台：后端无对应接口，暂隐藏（2026-07-17）
      // 启用前需要后端先加 app-center / platform 相关接口

      // 管理后台
      { path: 'admin',            name: 'AdminLayout', component: () => import('@/views/Admin/AdminLayout.vue'), meta: { title: '管理后台' }, redirect: '/admin/users',
        children: [
          { path: 'users',        name: 'AdminUsers',  component: () => import('@/views/Admin/Users.vue'),       meta: { title: '用户管理' } },
          { path: 'depts',        name: 'AdminDepts',  component: () => import('@/views/Admin/Departments.vue'), meta: { title: '部门管理' } },
          { path: 'roles',        name: 'AdminRoles',  component: () => import('@/views/Admin/Roles.vue'),       meta: { title: '角色权限' } },
          { path: 'dict',         name: 'AdminDict',   component: () => import('@/views/Admin/Dict.vue'),        meta: { title: '数据字典' } },
          { path: 'logs',         name: 'AdminLogs',   component: () => import('@/views/Admin/Logs.vue'),        meta: { title: '操作日志' } }
        ]
      }
    ]
  },
  { path: '/:pathMatch(.*)*', redirect: '/home' }
]

const router = createRouter({ history: createWebHistory(), routes })

router.beforeEach((to) => {
  const userStore = useUserStore()
  if (to.meta.public) return userStore.isLoggedIn ? '/home' : true
  return userStore.isLoggedIn ? true : '/login'
})

router.afterEach((to) => {
  if (to.meta?.title) document.title = `${to.meta.title} · 仿飞书工作台`
})

export default router
