<template>
  <view class="page-container">
    <view class="header-bar">
      <text class="header-title">部门管理</text>
      <button class="add-btn" @tap="openAddModal">+ 新增</button>
    </view>

    <view class="stats-bar">
      <text>共 {{ allDepts.length }} 个部门 · {{ allUsers.length }} 人</text>
    </view>

    <view v-if="loading" class="loading-state"><text>加载中...</text></view>

    <scroll-view v-else class="tree-scroll" scroll-y>
      <view
        v-for="item in visibleList"
        :key="item.id"
        class="dept-row"
        :style="{ paddingLeft: (item._level * 44 + 24) + 'rpx' }"
        @tap="item._hasChildren && toggleExpand(item.id)"
      >
        <!-- 展开/折叠状态图标 -->
        <text class="dept-toggle">{{ item._hasChildren ? (expandedSet.has(item.id) ? '▼' : '▶') : '　' }}</text>
        <!-- 图标 -->
        <text class="dept-icon">{{ item._hasChildren ? (expandedSet.has(item.id) ? '📂' : '📁') : '📄' }}</text>
        <!-- 信息 -->
        <view class="dept-info">
          <text class="dept-name">{{ item.name }}</text>
          <text class="dept-meta">{{ userCountMap[item.id] || 0 }} 人 · 排序 {{ item.sortOrder || '-' }}</text>
        </view>
        <view class="dept-arrow" @tap.stop="selectDept(item)">
          <text class="arrow-icon">›</text>
          <text class="arrow-label">详情</text>
        </view>
      </view>
      <view v-if="!visibleList.length" class="empty-state"><text>暂无部门</text></view>
    </scroll-view>

    <!-- 详情/编辑弹窗 -->
    <view v-if="selectedDept" class="modal-overlay" @tap="closeDetail">
      <view class="modal-popup" @tap.stop>
        <text class="modal-title">部门详情</text>
        <view class="form-group">
          <text class="form-label">名称</text>
          <input v-model="editForm.name" class="form-input" />
        </view>
        <view class="form-group">
          <text class="form-label">上级部门</text>
          <picker :value="editParentIdx" :range="parentLabels" @change="(e) => editParentIdx = e.detail.value">
            <view class="form-picker">{{ parentLabels[editParentIdx] }}</view>
          </picker>
        </view>
        <view class="form-group">
          <text class="form-label">排序号</text>
          <input v-model="editForm.sortOrder" class="form-input" placeholder="数字越小越靠前" type="number" />
        </view>
        <view class="form-group">
          <text class="form-label">成员</text>
          <text class="form-static">{{ userCountMap[selectedDept.id] || 0 }} 人</text>
        </view>
        <view v-if="deptUserList.length" class="user-list">
          <text class="user-list-title">成员列表：</text>
          <view v-for="u in deptUserList" :key="u.id" class="user-tag">{{ u.realName || u.username }}</view>
        </view>
        <view class="modal-btns">
          <button class="btn-danger" @tap="handleDelete">删除</button>
          <button class="btn-confirm" @tap="handleSave">保存</button>
        </view>
      </view>
    </view>

    <!-- 新增弹窗 -->
    <view v-if="showAddModal" class="modal-overlay" @tap="showAddModal = false">
      <view class="modal-popup" @tap.stop>
        <text class="modal-title">新增部门</text>
        <input v-model="addForm.name" class="form-input" placeholder="部门名称" />
        <view class="form-group">
          <text class="form-label">上级部门</text>
          <picker :value="addParentIdx" :range="parentLabels" @change="(e) => addParentIdx = e.detail.value">
            <view class="form-picker">{{ parentLabels[addParentIdx] }}</view>
          </picker>
        </view>
        <input v-model="addForm.sortOrder" class="form-input" placeholder="排序号（选填）" type="number" />
        <view class="modal-btns">
          <button class="btn-cancel" @tap="showAddModal = false">取消</button>
          <button class="btn-confirm" @tap="handleAdd">保存</button>
        </view>
      </view>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref, computed, reactive } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { getDepartmentTree, createDepartment, updateDepartment, deleteDepartment, getUsers } from '@/api/admin'

// ============ 展开/折叠 ============
const expandedSet = reactive(new Set<string>())

function toggleExpand(id: string) {
  if (expandedSet.has(id)) expandedSet.delete(id)
  else expandedSet.add(id)
}

// ============ 数据 ============
const treeData = ref<any[]>([])
const allDepts = ref<any[]>([])
const allUsers = ref<any[]>([])
const loading = ref(false)

// 部门人数统计
const userCountMap = computed(() => {
  const map: Record<string, number> = {}
  for (const u of allUsers.value) {
    if (u.departmentId) map[u.departmentId] = (map[u.departmentId] || 0) + 1
  }
  return map
})

// 所有节点的扁平列表（含根节点、层级、父ID）
const allFlat = computed(() => {
  const result: any[] = []
  function walk(nodes: any[], level = 0) {
    for (const n of nodes) {
      result.push({
        id: n.id,
        name: n.name,
        parentId: n.parentId,
        sortOrder: n.sortOrder,
        children: n.children,
        _level: level,
        _hasChildren: n.children?.length > 0,
      })
      if (n.children?.length) walk(n.children, level + 1)
    }
  }
  walk(treeData.value)
  return result
})

// 可见列表（展开状态的节点可见）
const visibleList = computed(() => {
  return allFlat.value.filter((item: any) => {
    if (item._level === 0) return true
    return item.parentId && expandedSet.has(item.parentId)
  })
})

// 上级部门选择器选项
const parentLabels = computed(() => {
  return ['（顶级部门）', ...allFlat.value.map((d: any) => '　'.repeat(d._level) + d.name)]
})

async function loadData() {
  loading.value = true
  try {
    const [treeRes, userRes] = await Promise.all([getDepartmentTree(), getUsers()])
    allUsers.value = Array.isArray(userRes) ? userRes : []
    const treeList = Array.isArray(treeRes) ? treeRes : []
    treeData.value = treeList
    // 所有部门扁平
    const flat: any[] = []
    function walk(nodes: any[]) {
      for (const n of nodes) {
        flat.push(n)
        if (n.children?.length) walk(n.children)
      }
    }
    walk(treeList)
    allDepts.value = flat
    // 默认展开第一级
    treeList.forEach((n: any) => { expandedSet.add(n.id) })
    // 展开第二级
    treeList.forEach((n: any) => {
      n.children?.forEach((c: any) => { expandedSet.add(c.id) })
    })
  } catch {
    treeData.value = []
    allDepts.value = []
  } finally { loading.value = false }
}

// ============ 选中部门 ============
const selectedDept = ref<any>(null)
const editForm = ref({ name: '', sortOrder: '' })
const editParentIdx = ref(0)

const deptUserList = computed(() => {
  if (!selectedDept.value) return []
  return allUsers.value.filter((u: any) => u.departmentId === selectedDept.value.id)
})

function selectDept(dept: any) {
  selectedDept.value = dept
  editForm.value = { name: dept.name || '', sortOrder: String(dept.sortOrder || '') }
  const parentId = dept.parentId
  editParentIdx.value = parentId
    ? allFlat.value.findIndex((d: any) => d.id === parentId) + 1
    : 0
}

function closeDetail() { selectedDept.value = null }

async function handleSave() {
  if (!selectedDept.value) return
  try {
    const parentId = editParentIdx.value > 0 ? allFlat.value[editParentIdx.value - 1]?.id : undefined
    await updateDepartment(selectedDept.value.id, {
      name: editForm.value.name,
      parentId,
      sortOrder: editForm.value.sortOrder ? Number(editForm.value.sortOrder) : undefined,
    })
    selectedDept.value = null
    uni.showToast({ title: '保存成功', icon: 'success' })
    loadData()
  } catch { uni.showToast({ title: '保存失败', icon: 'none' }) }
}

const deptDeleting = ref(false)

function handleDelete() {
  if (!selectedDept.value || deptDeleting.value) return
  const count = userCountMap.value[selectedDept.value.id] || 0
  uni.showModal({
    title: '确认删除',
    content: `确定要删除「${selectedDept.value.name}」吗？\n该部门下有 ${count} 名成员。此操作不可恢复。`,
    success: async (res) => {
      if (res.confirm) {
        deptDeleting.value = true
        try {
          await deleteDepartment(selectedDept.value.id)
          selectedDept.value = null
          uni.showToast({ title: '已删除', icon: 'success' })
          loadData()
        } catch {
          uni.showToast({ title: '删除失败', icon: 'none' })
        } finally {
          deptDeleting.value = false
        }
      }
    },
  })
}

// ============ 新增 ============
const showAddModal = ref(false)
const addForm = ref({ name: '', sortOrder: '' })
const addParentIdx = ref(0)

function openAddModal() {
  addForm.value = { name: '', sortOrder: '' }
  addParentIdx.value = 0
  showAddModal.value = true
}

async function handleAdd() {
  if (!addForm.value.name.trim()) {
    uni.showToast({ title: '请输入部门名称', icon: 'none' })
    return
  }
  try {
    const parentId = addParentIdx.value > 0 ? allFlat.value[addParentIdx.value - 1]?.id : undefined
    await createDepartment({
      name: addForm.value.name.trim(),
      parentId,
      sortOrder: addForm.value.sortOrder ? Number(addForm.value.sortOrder) : undefined,
    })
    showAddModal.value = false
    uni.showToast({ title: '新增成功', icon: 'success' })
    loadData()
  } catch { uni.showToast({ title: '新增失败', icon: 'none' }) }
}

onShow(() => loadData())
</script>

<style scoped>
.page-container { min-height: 100vh; background: #f0f2f5; }
.header-bar { display: flex; justify-content: space-between; align-items: center; padding: 20rpx 24rpx; background: #fff; border-bottom: 1rpx solid #f0f2f5; }
.header-title { font-size: 28rpx; font-weight: 600; color: #1d2129; }
.add-btn { height: 56rpx; line-height: 56rpx; padding: 0 20rpx; background: #409EFF; color: #fff; font-size: 22rpx; border-radius: 28rpx; border: none; }
.stats-bar { padding: 12rpx 24rpx; background: #fff; border-bottom: 1rpx solid #f0f2f5; font-size: 22rpx; color: #86909c; }

.tree-scroll { padding-bottom: 40rpx; }
.dept-row { display: flex; align-items: center; padding: 22rpx 24rpx; background: #fff; border-bottom: 1rpx solid #f5f6f7; }
.dept-row:active { background: #f0f2f5; }
.dept-toggle { width: 28rpx; font-size: 18rpx; color: #c9cdd4; flex-shrink: 0; text-align: center; }
.dept-toggle-placeholder { width: 28rpx; flex-shrink: 0; }
.dept-icon { font-size: 28rpx; margin: 0 12rpx; flex-shrink: 0; }
.dept-info { flex: 1; min-width: 0; }
.dept-name { font-size: 26rpx; font-weight: 500; color: #1d2129; display: block; }
.dept-meta { font-size: 20rpx; color: #c9cdd4; }
.dept-arrow { display: flex; flex-direction: column; align-items: center; justify-content: center; padding: 16rpx 20rpx; margin: -16rpx -20rpx; flex-shrink: 0; }
.arrow-icon { font-size: 36rpx; color: #409EFF; font-weight: 700; line-height: 1; }
.arrow-label { font-size: 18rpx; color: #409EFF; margin-top: 2rpx; }
.loading-state, .empty-state { padding: 120rpx 0; text-align: center; font-size: 28rpx; color: #86909c; }

/* 弹窗 */
.modal-overlay { position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0,0,0,0.45); z-index: 999; display: flex; align-items: center; justify-content: center; padding: 60rpx; }
.modal-popup { width: 100%; max-width: 580rpx; background: #fff; border-radius: 20rpx; padding: 32rpx; max-height: 75vh; overflow-y: auto; }
.modal-title { font-size: 32rpx; font-weight: 600; text-align: center; display: block; margin-bottom: 20rpx; }
.form-group { margin-bottom: 16rpx; }
.form-label { font-size: 24rpx; color: #86909c; display: block; margin-bottom: 6rpx; }
.form-input { width: 100%; height: 64rpx; border: 1rpx solid #e8eaed; border-radius: 12rpx; padding: 0 16rpx; font-size: 26rpx; box-sizing: border-box; }
.form-picker { width: 100%; height: 64rpx; line-height: 64rpx; border: 1rpx solid #e8eaed; border-radius: 12rpx; padding: 0 16rpx; font-size: 24rpx; color: #4e5969; box-sizing: border-box; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.form-static { font-size: 26rpx; color: #1d2129; }
.user-list { margin: 8rpx 0; }
.user-list-title { font-size: 22rpx; color: #86909c; display: block; margin-bottom: 6rpx; }
.user-tag { display: inline-block; font-size: 22rpx; padding: 4rpx 14rpx; background: #f0f9eb; color: #67C23A; border-radius: 8rpx; margin: 4rpx 4rpx 4rpx 0; }
.modal-btns { display: flex; gap: 12rpx; margin-top: 20rpx; }
.btn-cancel, .btn-confirm, .btn-danger { flex: 1; height: 64rpx; line-height: 64rpx; border-radius: 32rpx; font-size: 24rpx; border: none; }
.btn-cancel { background: #f5f6f7; color: #4e5969; }
.btn-confirm { background: #409EFF; color: #fff; }
.btn-danger { background: #f56c6c; color: #fff; }
</style>
