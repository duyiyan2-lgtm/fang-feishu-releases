<template>
  <view class="page-container">
    <view class="toolbar">
      <input v-model="keyword" class="search-input" placeholder="搜索姓名/账号..." @confirm="handleSearch" />
      <button class="add-btn" @tap="openForm(null)">+ 新增</button>
    </view>

    <view v-if="loading" class="loading-state"><text>加载中...</text></view>

    <view v-else-if="list.length" class="list">
      <view v-for="item in list" :key="item.id" class="list-item">
        <view class="item-avatar" :style="{ background: getColor(item.id) }">
          <text class="avatar-text">{{ (item.realName || item.username)[0] }}</text>
        </view>
        <view class="item-info">
          <text class="item-name">{{ item.realName || item.username }}</text>
          <text class="item-meta">{{ item.username }} · {{ item.departmentName || '未分配' }}</text>
          <view class="item-tags">
            <text class="tag-role" :class="item.roles?.includes('Admin') ? 'tag-admin' : 'tag-user'">
              {{ item.roles?.join(', ') || 'User' }}
            </text>
            <text class="tag-dept" v-if="item.position">{{ item.position }}</text>
          </view>
        </view>
        <view class="item-right">
          <text class="status-tag" :class="item.status === 'Active' ? 'active' : 'disabled'">
            {{ item.status === 'Active' ? '启用' : '禁用' }}
          </text>
          <view class="item-actions">
            <text class="action-btn" @tap="openForm(item)">编辑</text>
            <text class="action-btn" @tap="toggleStatus(item)">
              {{ item.status === 'Active' ? '禁用' : '启用' }}
            </text>
          </view>
        </view>
      </view>
      <view class="pagination">
        <text class="page-btn" :class="{ disabled: page <= 1 }" @tap="changePage(page - 1)">‹ 上一页</text>
        <text class="page-info">{{ page }}</text>
        <text class="page-btn" :class="{ disabled: !hasMore }" @tap="changePage(page + 1)">下一页 ›</text>
      </view>
    </view>

    <view v-else class="empty-state"><text>暂无用户</text></view>

    <!-- 新增/编辑弹窗 -->
    <view v-if="formVisible" class="modal-overlay" @tap="formVisible = false">
      <view class="modal-popup" @tap.stop>
        <text class="modal-title">{{ editingUser ? '编辑用户' : '新增用户' }}</text>
        <input v-model="form.username" class="form-input" placeholder="账号" :disabled="!!editingUser" />
        <input v-model="form.realName" class="form-input" placeholder="姓名" />
        <input v-model="form.email" class="form-input" placeholder="邮箱" type="email" />
        <input v-model="form.phone" class="form-input" placeholder="手机" type="number" />
        <input v-if="!editingUser" v-model="form.password" class="form-input" placeholder="初始密码" type="text" />
        <input v-model="form.position" class="form-input" placeholder="职位" />
        <view class="form-group" v-if="deptOptions.length">
          <text class="form-label">部门</text>
          <picker :value="deptIdx" :range="deptOptions" @change="onDeptChange">
            <view class="form-picker">{{ deptOptions[deptIdx] }}</view>
          </picker>
        </view>
        <view class="form-group" v-if="roleOptions.length">
          <text class="form-label">角色（多选）</text>
          <view class="role-checkboxes">
            <view
              v-for="(r, i) in allRoles"
              :key="r.id || i"
              class="role-check-item"
              :class="{ checked: selectedRoleIndices.includes(i) }"
              @tap="toggleRole(i)"
            >
              <text class="role-check-mark">{{ selectedRoleIndices.includes(i) ? '✓' : '' }}</text>
              <text class="role-check-name">{{ r.roleName }}</text>
            </view>
          </view>
        </view>
        <view class="modal-btns">
          <button class="btn-cancel" @tap="formVisible = false">取消</button>
          <button class="btn-confirm" @tap="handleSave">保存</button>
        </view>
      </view>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { getUsers, createUser, updateUser, setUserStatus, getDepartmentTree, getRoles } from '@/api/admin'

const list = ref<any[]>([])
const keyword = ref('')
const page = ref(1)
const pageSize = 20
const hasMore = ref(true)
const loading = ref(false)
const formVisible = ref(false)
const editingUser = ref<any>(null)
const form = ref({ username: '', realName: '', email: '', phone: '', password: '', departmentId: '', position: '', roleCodes: [] as string[] })

// 部门选项
const deptOptions = ref<string[]>(['未分配'])
const deptIds = ref<string[]>([''])
const deptIdx = ref(0)

// 角色选项
const allRoles = ref<any[]>([])
const roleOptions = computed(() => allRoles.value.map(r => r.roleName))
const selectedRoleIndices = ref<number[]>([])

async function loadRoles() {
  try {
    const res: any = await getRoles()
    allRoles.value = Array.isArray(res) ? res : []
  } catch { allRoles.value = [] }
}

function onDeptChange(e: any) {
  deptIdx.value = e.detail.value
  form.value.departmentId = deptIds.value[deptIdx.value] || ''
}

async function loadDepartments() {
  try {
    const tree: any = await getDepartmentTree()
    const treeList = Array.isArray(tree) ? tree : []
    const list = treeList.length === 1 && treeList[0].children?.length ? treeList[0].children : treeList
    const flat: any[] = []
    function walk(nodes: any[]) {
      nodes.forEach((n: any) => { flat.push(n); if (n.children?.length) walk(n.children) })
    }
    walk(list)
    deptOptions.value = ['未分配', ...flat.map((d: any) => d.name)]
    deptIds.value = ['', ...flat.map((d: any) => d.id)]
  } catch (e) { console.warn('[Admin] load departments failed', e) }
}

async function loadData() {
  loading.value = true
  try {
    const params: any = { page: page.value, pageSize }
    if (keyword.value) params.keyword = keyword.value
    const res: any = await getUsers(params)
    const items = Array.isArray(res) ? res : res?.items || res?.list || []
    list.value = items
    hasMore.value = items.length >= pageSize
  } catch { list.value = [] } finally { loading.value = false }
}

function handleSearch() { page.value = 1; loadData() }
function changePage(p: number) { if (p < 1) return; page.value = p; loadData() }

function openForm(user: any) {
  editingUser.value = user
  form.value = {
    username: user?.username || '',
    realName: user?.realName || '',
    email: user?.email || '',
    phone: user?.phone || '',
    password: '',
    departmentId: user?.departmentId || '',
    position: user?.position || '',
    roleCodes: [],
  }
  deptIdx.value = deptIds.value.indexOf(user?.departmentId || '')
  if (deptIdx.value < 0) deptIdx.value = 0

  // 加载用户已有角色
  if (user?.roles) {
    selectedRoleIndices.value = allRoles.value
      .map((r, i) => user.roles.includes(r.roleCode) ? i : -1)
      .filter(i => i >= 0)
  } else {
    selectedRoleIndices.value = []
  }

  formVisible.value = true
}

function toggleRole(index: number) {
  const idx = selectedRoleIndices.value.indexOf(index)
  if (idx >= 0) {
    selectedRoleIndices.value.splice(idx, 1)
  } else {
    selectedRoleIndices.value.push(index)
  }
}

const avatarColors = ['#409EFF', '#67C23A', '#E6A23C', '#F56C6C', '#909399', '#B37FEB', '#00BFA5', '#FF7043']
function getColor(id: string): string {
  let hash = 0
  for (let i = 0; i < id.length; i++) hash = ((hash << 5) - hash) + id.charCodeAt(i)
  return avatarColors[Math.abs(hash) % avatarColors.length]
}

async function handleSave() {
  try {
    if (editingUser.value) {
      // 只发送有值的字段，避免空字符串和 roleCodes 触发后端问题
      const payload: Record<string, any> = {}
      if (form.value.realName) payload.realName = form.value.realName
      if (form.value.email) payload.email = form.value.email
      if (form.value.phone) payload.phone = form.value.phone
      if (form.value.position) payload.position = form.value.position
      if (form.value.departmentId) payload.departmentId = form.value.departmentId
      // 不发送 roleCodes，避免后端 Role 导航属性加载异常导致 500
      await updateUser(editingUser.value.id, payload)
    } else {
      const roleCodes = selectedRoleIndices.value.map(i => allRoles.value[i]?.roleCode).filter(Boolean)
      await createUser({
        username: form.value.username,
        password: form.value.password,
        realName: form.value.realName,
        email: form.value.email,
        phone: form.value.phone,
        departmentId: form.value.departmentId || undefined,
        roleCodes: roleCodes.length ? roleCodes : undefined,
      })
    }
    formVisible.value = false
    loadData()
    uni.showToast({ title: '保存成功', icon: 'success' })
  } catch (e: any) {
    uni.showToast({ title: e.message || '保存失败', icon: 'none' })
  }
}

async function toggleStatus(user: any) {
  const newStatus = user.status === 'Active' ? 'Disabled' : 'Active'
  uni.showModal({
    title: '提示',
    content: `确定要${newStatus === 'Disabled' ? '禁用' : '启用'}用户「${user.realName || user.username}」吗？`,
    success: async (res) => {
      if (res.confirm) {
        try {
          await setUserStatus(user.id, newStatus)
          user.status = newStatus === 'Active' ? 'Active' : 'Disabled'
        } catch { uni.showToast({ title: '操作失败', icon: 'none' }) }
      }
    },
  })
}

onShow(() => {
  loadDepartments()
  loadRoles()
  loadData()
})
</script>

<style scoped>
.page-container { min-height: 100vh; background: #f0f2f5; padding: 24rpx; }
.toolbar { display: flex; gap: 16rpx; margin-bottom: 24rpx; }
.search-input { flex: 1; height: 68rpx; background: #fff; border-radius: 34rpx; padding: 0 28rpx; font-size: 26rpx; }
.add-btn { height: 68rpx; line-height: 68rpx; padding: 0 24rpx; background: #409EFF; color: #fff; font-size: 24rpx; border-radius: 34rpx; border: none; }
.list { background: #fff; border-radius: 16rpx; }
.list-item { display: flex; align-items: center; padding: 24rpx; border-bottom: 1rpx solid #f0f2f5; }
.list-item:last-child { border-bottom: none; }
.item-info { flex: 1; min-width: 0; }
.item-avatar { width: 64rpx; height: 64rpx; border-radius: 50%; display: flex; align-items: center; justify-content: center; margin-right: 16rpx; flex-shrink: 0; }
.avatar-text { color: #fff; font-size: 28rpx; font-weight: 600; }
.item-name { font-size: 28rpx; font-weight: 500; color: #1d2129; display: block; }
.item-meta { font-size: 22rpx; color: #86909c; }
.item-tags { display: flex; flex-wrap: wrap; gap: 6rpx; margin-top: 6rpx; }
.tag-role { font-size: 18rpx; padding: 2rpx 12rpx; border-radius: 6rpx; }
.tag-admin { background: #e8f3ff; color: #409EFF; }
.tag-user { background: #f0f9eb; color: #67C23A; }
.tag-dept { font-size: 18rpx; padding: 2rpx 12rpx; border-radius: 6rpx; background: #f5f6f7; color: #86909c; }
.item-right { display: flex; flex-direction: column; align-items: flex-end; gap: 8rpx; flex-shrink: 0; margin-left: 12rpx; }
.status-tag { font-size: 20rpx; padding: 4rpx 12rpx; border-radius: 8rpx; }
.status-tag.active { background: #f0f9eb; color: #67C23A; }
.status-tag.disabled { background: #fef0f0; color: #f56c6c; }
.item-actions { display: flex; gap: 12rpx; }
.action-btn { font-size: 24rpx; color: #409EFF; }
.pagination { display: flex; justify-content: center; align-items: center; padding: 24rpx; gap: 24rpx; }
.page-btn { font-size: 24rpx; color: #409EFF; padding: 8rpx 20rpx; background: #f0f2f5; border-radius: 8rpx; }
.page-btn.disabled { color: #c9cdd4; }
.page-info { font-size: 24rpx; color: #86909c; }
.loading-state, .empty-state { padding: 120rpx 0; text-align: center; font-size: 28rpx; color: #86909c; }

/* 弹窗 */
.modal-overlay { position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0,0,0,0.45); z-index: 999; display: flex; align-items: center; justify-content: center; padding: 60rpx; }
.modal-popup { width: 100%; max-width: 560rpx; background: #fff; border-radius: 20rpx; padding: 32rpx; }
.modal-title { font-size: 32rpx; font-weight: 600; text-align: center; display: block; margin-bottom: 24rpx; }
.form-input { width: 100%; height: 72rpx; border: 1rpx solid #e8eaed; border-radius: 12rpx; padding: 0 20rpx; font-size: 26rpx; margin-bottom: 16rpx; box-sizing: border-box; }
.modal-btns { display: flex; gap: 16rpx; margin-top: 8rpx; }
.btn-cancel, .btn-confirm { flex: 1; height: 72rpx; line-height: 72rpx; border-radius: 36rpx; font-size: 26rpx; border: none; }
.btn-cancel { background: #f5f6f7; color: #4e5969; }
.btn-confirm { background: #409EFF; color: #fff; }
.form-group { margin-bottom: 16rpx; }
.form-label { font-size: 24rpx; color: #86909c; display: block; margin-bottom: 8rpx; }
.form-picker { height: 72rpx; line-height: 72rpx; border: 1rpx solid #e8eaed; border-radius: 12rpx; padding: 0 20rpx; font-size: 26rpx; color: #4e5969; margin-bottom: 16rpx; }
.form-hint { font-size: 22rpx; color: #c9cdd4; display: block; margin-bottom: 12rpx; }
.role-checkboxes { display: flex; flex-wrap: wrap; gap: 12rpx; margin-bottom: 16rpx; }
.role-check-item { display: flex; align-items: center; gap: 6rpx; padding: 8rpx 16rpx; border: 1rpx solid #e8eaed; border-radius: 8rpx; font-size: 24rpx; color: #4e5969; }
.role-check-item.checked { background: #e8f3ff; border-color: #409EFF; color: #409EFF; }
.role-check-mark { font-weight: 700; font-size: 20rpx; min-width: 20rpx; }
.role-check-name { }
</style>
