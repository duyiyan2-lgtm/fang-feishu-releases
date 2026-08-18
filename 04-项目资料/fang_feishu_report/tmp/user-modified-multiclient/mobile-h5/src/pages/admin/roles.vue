<template>
  <view class="page-container">
    <view class="header-bar">
      <text class="header-title">角色管理 ({{ roles.length }})</text>
      <button class="add-btn" @tap="openAddModal">+ 新增角色</button>
    </view>

    <view v-if="loading" class="loading-state"><text>加载中...</text></view>

    <view v-else-if="roles.length" class="role-list">
      <view
        v-for="role in roles"
        :key="role.id"
        class="role-card"
        @tap="showDetail(role)"
      >
        <view class="role-head" :style="{ background: role.roleCode === 'Admin' ? '#e8f3ff' : '#f0f9eb' }">
          <text class="role-name">{{ role.roleName }}</text>
          <text class="role-code">{{ role.roleCode }}</text>
        </view>
        <view class="role-body">
          <text class="role-desc">{{ role.description || '暂无描述' }}</text>
          <text class="role-count">👤 {{ userCountByRole(role.roleCode) }} 人</text>
        </view>
      </view>
    </view>

    <view v-else class="empty-state"><text>暂无角色</text></view>

    <!-- 角色详情弹窗 -->
    <view v-if="detailRole" class="modal-overlay" @tap="detailRole = null">
      <view class="modal-popup" @tap.stop>
        <text class="modal-title">{{ detailRole.roleName }}</text>
        <view class="detail-row">
          <text class="detail-label">编码</text>
          <text class="detail-value">{{ detailRole.roleCode }}</text>
        </view>
        <view class="detail-row">
          <text class="detail-label">描述</text>
          <text class="detail-value">{{ detailRole.description || '-' }}</text>
        </view>
        <view class="detail-row">
          <text class="detail-label">成员</text>
          <text class="detail-value">{{ userCountByRole(detailRole.roleCode) }} 人</text>
        </view>
        <view v-if="usersInRole(detailRole.roleCode).length" class="user-list">
          <text class="user-list-title">拥有此角色的用户：</text>
          <view v-for="u in usersInRole(detailRole.roleCode)" :key="u.id" class="user-chip">
            <text class="user-chip-name">{{ u.realName || u.username }}</text>
            <text class="user-chip-acct">{{ u.username }}</text>
          </view>
        </view>
        <button class="modal-close-btn" @tap="detailRole = null">关闭</button>
      </view>
    </view>

    <!-- 新增角色弹窗 -->
    <view v-if="showAddModal" class="modal-overlay" @tap="showAddModal = false">
      <view class="modal-popup" @tap.stop>
        <text class="modal-title">新增角色</text>
        <input v-model="addForm.RoleName" class="form-input" placeholder="角色名称（如 管理员）" />
        <input v-model="addForm.RoleCode" class="form-input" placeholder="角色编码（如 Admin）" />
        <input v-model="addForm.Description" class="form-input" placeholder="角色描述（选填）" />
        <view class="modal-btns">
          <button class="btn-cancel" @tap="showAddModal = false">取消</button>
          <button class="btn-confirm" @tap="handleAddRole">保存</button>
        </view>
      </view>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { getRoles, createRole, getUsers } from '@/api/admin'

const roles = ref<any[]>([])
const allUsers = ref<any[]>([])
const loading = ref(false)
const detailRole = ref<any>(null)
const showAddModal = ref(false)
const addForm = ref({ RoleName: '', RoleCode: '', Description: '' })

function userCountByRole(roleCode: string): number {
  return allUsers.value.filter((u: any) => u.roles?.includes(roleCode)).length
}

function usersInRole(roleCode: string): any[] {
  return allUsers.value.filter((u: any) => u.roles?.includes(roleCode))
}

async function loadData() {
  loading.value = true
  try {
    const [roleRes, userRes] = await Promise.all([getRoles(), getUsers()])
    roles.value = Array.isArray(roleRes) ? roleRes : []
    allUsers.value = Array.isArray(userRes) ? userRes : []
  } catch {
    roles.value = []
    allUsers.value = []
  } finally { loading.value = false }
}

function showDetail(role: any) {
  detailRole.value = role
}

function openAddModal() {
  addForm.value = { RoleName: '', RoleCode: '', Description: '' }
  showAddModal.value = true
}

async function handleAddRole() {
  if (!addForm.value.RoleName.trim() || !addForm.value.RoleCode.trim()) {
    uni.showToast({ title: '请填写名称和编码', icon: 'none' })
    return
  }
  try {
    await createRole({
      RoleName: addForm.value.RoleName.trim(),
      RoleCode: addForm.value.RoleCode.trim(),
      Description: addForm.value.Description.trim() || undefined,
    })
    showAddModal.value = false
    uni.showToast({ title: '创建成功', icon: 'success' })
    loadData()
  } catch { uni.showToast({ title: '创建失败', icon: 'none' }) }
}

onShow(() => loadData())
</script>

<style scoped>
.page-container { min-height: 100vh; background: #f0f2f5; }
.header-bar { display: flex; justify-content: space-between; align-items: center; padding: 20rpx 24rpx; background: #fff; border-bottom: 1rpx solid #f0f2f5; }
.header-title { font-size: 28rpx; font-weight: 600; color: #1d2129; }
.add-btn { height: 56rpx; line-height: 56rpx; padding: 0 20rpx; background: #409EFF; color: #fff; font-size: 22rpx; border-radius: 28rpx; border: none; }

.role-list { margin: 16rpx 24rpx; }
.role-card { background: #fff; border-radius: 16rpx; margin-bottom: 16rpx; overflow: hidden; box-shadow: 0 2rpx 8rpx rgba(0,0,0,0.04); }
.role-card:active { opacity: 0.85; }
.role-head { padding: 20rpx 24rpx; display: flex; align-items: center; gap: 12rpx; }
.role-name { font-size: 28rpx; font-weight: 600; color: #1d2129; }
.role-code { font-size: 20rpx; color: #86909c; background: rgba(255,255,255,0.7); padding: 2rpx 12rpx; border-radius: 8rpx; }
.role-body { padding: 16rpx 24rpx; display: flex; justify-content: space-between; align-items: center; }
.role-desc { font-size: 24rpx; color: #86909c; flex: 1; }
.role-count { font-size: 22rpx; color: #4e5969; }

.loading-state, .empty-state { padding: 120rpx 0; text-align: center; font-size: 28rpx; color: #86909c; }

/* 详情弹窗 */
.modal-overlay { position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0,0,0,0.45); z-index: 999; display: flex; align-items: center; justify-content: center; padding: 60rpx; }
.modal-popup { width: 100%; max-width: 580rpx; background: #fff; border-radius: 20rpx; padding: 32rpx; max-height: 70vh; overflow-y: auto; }
.modal-title { font-size: 32rpx; font-weight: 600; text-align: center; display: block; margin-bottom: 24rpx; }
.detail-row { display: flex; padding: 12rpx 0; border-bottom: 1rpx solid #f0f2f5; }
.detail-label { font-size: 24rpx; color: #86909c; width: 80rpx; flex-shrink: 0; }
.detail-value { font-size: 26rpx; color: #1d2129; flex: 1; }
.user-list { margin-top: 16rpx; }
.user-list-title { font-size: 24rpx; color: #86909c; display: block; margin-bottom: 8rpx; }
.user-chip { display: inline-flex; align-items: center; background: #f5f6f7; border-radius: 8rpx; padding: 6rpx 12rpx; margin: 4rpx 4rpx 4rpx 0; }
.user-chip-name { font-size: 24rpx; color: #1d2129; margin-right: 8rpx; }
.user-chip-acct { font-size: 20rpx; color: #c9cdd4; }
.modal-close-btn { width: 100%; height: 72rpx; line-height: 72rpx; background: #f5f6f7; color: #4e5969; font-size: 26rpx; border-radius: 36rpx; border: none; margin-top: 24rpx; }
.form-input { width: 100%; height: 72rpx; border: 1rpx solid #e8eaed; border-radius: 12rpx; padding: 0 20rpx; font-size: 26rpx; margin-bottom: 16rpx; box-sizing: border-box; }
.modal-btns { display: flex; gap: 16rpx; margin-top: 8rpx; }
.btn-cancel, .btn-confirm { flex: 1; height: 72rpx; line-height: 72rpx; border-radius: 36rpx; font-size: 26rpx; border: none; }
.btn-cancel { background: #f5f6f7; color: #4e5969; }
.btn-confirm { background: #409EFF; color: #fff; }
</style>
