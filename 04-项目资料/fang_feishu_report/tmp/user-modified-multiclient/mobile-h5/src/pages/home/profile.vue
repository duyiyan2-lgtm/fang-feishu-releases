<template>
  <view class="profile-container">
    <!-- 顶部头像区 -->
    <view class="profile-head">
      <view class="avatar-wrap" @tap="handleChangeAvatar">
        <view class="profile-avatar" :style="{ backgroundColor: avatarColor }">
          <text class="avatar-text">{{ displayName[0] || '?' }}</text>
        </view>
        <view class="avatar-overlay">
          <text class="avatar-overlay-text">更换</text>
        </view>
      </view>
      <text class="profile-name">{{ displayName || '未登录' }}</text>
      <text class="profile-role-tag">{{ isAdmin ? '管理员' : '普通用户' }}</text>
    </view>

    <!-- 信息卡片 -->
    <view class="info-card">
      <view class="info-row">
        <text class="info-label">用户名</text>
        <text class="info-value readonly">{{ userInfo?.username || '-' }}</text>
      </view>
      <view class="info-divider" />
      <view class="info-row" @tap="editMode ? null : toggleEdit()">
        <text class="info-label">姓名</text>
        <input v-if="editMode" v-model="editForm.realName" class="info-input" placeholder="输入姓名" />
        <text v-else class="info-value">{{ userInfo?.realName || '-' }}</text>
        <text v-if="!editMode" class="info-arrow">›</text>
      </view>
      <view class="info-divider" />
      <view class="info-row" @tap="editMode ? null : toggleEdit()">
        <text class="info-label">邮箱</text>
        <input v-if="editMode" v-model="editForm.email" class="info-input" placeholder="输入邮箱" />
        <text v-else class="info-value">{{ userInfo?.email || '-' }}</text>
        <text v-if="!editMode" class="info-arrow">›</text>
      </view>
      <view class="info-divider" />
      <view class="info-row" @tap="editMode ? null : toggleEdit()">
        <text class="info-label">手机</text>
        <input v-if="editMode" v-model="editForm.phone" class="info-input" placeholder="输入手机号" />
        <text v-else class="info-value">{{ userInfo?.phone || '-' }}</text>
        <text v-if="!editMode" class="info-arrow">›</text>
      </view>
      <view class="info-divider" />
      <view class="info-row">
        <text class="info-label">部门</text>
        <text class="info-value readonly">{{ deptName(userInfo?.departmentName) || '-' }}</text>
      </view>
      <view class="info-divider" />
      <view class="info-row">
        <text class="info-label">职位</text>
        <text class="info-value readonly">{{ userInfo?.position || '-' }}</text>
      </view>
    </view>

    <!-- 编辑模式按钮 -->
    <view v-if="editMode" class="action-section">
      <button class="save-btn" :disabled="saving" @tap="handleSave">保存修改</button>
      <button class="cancel-btn" @tap="cancelEdit">取消</button>
    </view>

    <!-- 菜单列表 -->
    <view class="menu-card">
      <view class="menu-row" @tap="handleChangePassword">
        <text class="menu-icon">🔑</text>
        <text class="menu-label">修改密码</text>
        <text class="menu-arrow">›</text>
      </view>
      <view class="menu-divider" />
      <view class="menu-row" @tap="handleClearCache">
        <text class="menu-icon">🗑️</text>
        <text class="menu-label">清除缓存</text>
        <text class="menu-arrow">›</text>
      </view>
    </view>

    <!-- 修改密码弹窗 -->
    <view v-if="pwdFormVisible" class="modal-overlay" @tap="pwdFormVisible = false">
      <view class="modal-popup" @tap.stop>
        <text class="modal-title">修改密码</text>
        <input v-model="pwdForm.oldPassword" class="form-input" type="password" password placeholder="当前密码" />
        <input v-model="pwdForm.newPassword" class="form-input" type="password" password placeholder="新密码（至少6位）" />
        <input v-model="pwdForm.confirmPassword" class="form-input" type="password" password placeholder="确认新密码" />
        <view class="modal-btns">
          <button class="btn-cancel" @tap="pwdFormVisible = false">取消</button>
          <button class="btn-confirm" :disabled="pwdSubmitting" @tap="submitChangePassword">
            {{ pwdSubmitting ? '提交中...' : '确认修改' }}
          </button>
        </view>
      </view>
    </view>

    <!-- 退出登录 -->
    <view class="action-section">
      <button class="logout-btn" @tap="handleLogout">退出登录</button>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { useAuthStore } from '@/stores/auth'
import { uploadFile } from '@/api/drive'
import { updateUser } from '@/api/admin'
import { get, post } from '@/api/request'

const authStore = useAuthStore()
const userInfo = computed(() => authStore.userInfo)
const displayName = computed(() => authStore.displayName)
const isAdmin = computed(() => authStore.isAdmin)

const editMode = ref(false)
const saving = ref(false)

/** 编辑表单 */
const editForm = ref({
  realName: '',
  email: '',
  phone: '',
})

function toggleEdit() {
  // 非管理员只能看不能改
  if (!isAdmin.value) {
    uni.showToast({ title: '仅管理员可编辑', icon: 'none' })
    return
  }
  editForm.value = {
    realName: userInfo.value?.realName || '',
    email: userInfo.value?.email || '',
    phone: userInfo.value?.phone || '',
  }
  editMode.value = true
}

function cancelEdit() {
  editMode.value = false
}

async function handleSave() {
  if (!isAdmin.value || !userInfo.value?.id) return
  saving.value = true
  try {
    await updateUser(userInfo.value.id, {
      realName: editForm.value.realName || undefined,
      email: editForm.value.email || undefined,
      phone: editForm.value.phone || undefined,
    })
    // 更新本地 userInfo
    authStore.userInfo = {
      ...authStore.userInfo!,
      realName: editForm.value.realName || authStore.userInfo!.realName,
      email: editForm.value.email || authStore.userInfo!.email,
      phone: editForm.value.phone || authStore.userInfo!.phone,
    }
    uni.setStorageSync('userInfo', JSON.stringify(authStore.userInfo))
    uni.showToast({ title: '保存成功', icon: 'success' })
    editMode.value = false
  } catch (e: any) {
    uni.showToast({ title: e?.message || '保存失败', icon: 'none' })
  } finally {
    saving.value = false
  }
}

/** 更换头像 → 上传文件后存入本地缓存（后端暂无保存头像API） */
async function handleChangeAvatar() {
  try {
    const res = await new Promise<any>((resolve, reject) => {
      uni.chooseImage({
        count: 1,
        sourceType: ['album', 'camera'],
        success: (r) => resolve(r),
        fail: () => reject(new Error('取消选择')),
      })
    })
    uni.showLoading({ title: '上传头像...' })
    const uploaded: any = await uploadFile(res.tempFilePaths[0])
    uni.hideLoading()
    if (uploaded?.id) {
      // 本地保存头像 fileId（页面关闭后下一次启动会用 authStore 的信息）
      uni.setStorageSync('avatar_file_id', uploaded.id)
      uni.showToast({ title: '头像已更新（本地）', icon: 'success' })
    }
  } catch {
    uni.hideLoading()
  }
}

const pwdFormVisible = ref(false)
const pwdForm = ref({ oldPassword: '', newPassword: '', confirmPassword: '' })
const pwdSubmitting = ref(false)

function handleChangePassword() {
  pwdForm.value = { oldPassword: '', newPassword: '', confirmPassword: '' }
  pwdFormVisible.value = true
}

async function submitChangePassword() {
  const { oldPassword, newPassword, confirmPassword } = pwdForm.value
  if (!oldPassword || !newPassword) {
    uni.showToast({ title: '请填写完整', icon: 'none' })
    return
  }
  if (newPassword.length < 6) {
    uni.showToast({ title: '新密码至少6位', icon: 'none' })
    return
  }
  if (newPassword !== confirmPassword) {
    uni.showToast({ title: '两次密码不一致', icon: 'none' })
    return
  }
  pwdSubmitting.value = true
  try {
    await post('/auth/change-password', { oldPassword, newPassword })
    pwdFormVisible.value = false
    uni.showToast({ title: '密码修改成功', icon: 'success' })
  } catch {
    uni.showToast({ title: '密码修改失败，请检查原密码', icon: 'none' })
  } finally {
    pwdSubmitting.value = false
  }
}

function handleClearCache() {
  uni.showModal({
    title: '提示',
    content: '确定清除所有本地缓存吗？（不会影响服务器数据）',
    success: (res) => {
      if (res.confirm) {
        try {
          const token = uni.getStorageSync('token')
          const user = uni.getStorageSync('userInfo')
          uni.clearStorageSync()
          // 保留登录信息
          if (token) uni.setStorageSync('token', token)
          if (user) uni.setStorageSync('userInfo', user)
          uni.showToast({ title: '缓存已清除', icon: 'success' })
        } catch {
          uni.showToast({ title: '清除失败', icon: 'none' })
        }
      }
    },
  })
}

function handleLogout() {
  uni.showModal({
    title: '提示',
    content: '确定退出登录吗？',
    success: (res) => {
      if (res.confirm) {
        authStore.logout()
      }
    },
  })
}

/** 头像颜色 */
const avatarColor = computed(() => {
  const id = userInfo.value?.id || '0'
  const colors = ['#409EFF', '#67C23A', '#E6A23C', '#F56C6C', '#909399', '#B37FEB']
  return colors[id.charCodeAt(0) % colors.length]
})

function deptName(name: string): string {
  const map: Record<string, string> = {
    Technology: '技术部',
    Product: '产品部',
    Operations: '运营部',
    Design: '设计部',
    Marketing: '市场部',
    'Human Resources': '人力资源部',
    Finance: '财务部',
    Administration: '行政部',
    'FangFeishu Demo Company': '仿飞书 Demo 公司',
  }
  return map[name] || name || '-'
}

// 每次显示刷新用户信息
onShow(async () => {
  try {
    const res: any = await get('/auth/me')
    if (res) {
      authStore.userInfo = { ...authStore.userInfo, ...res }
      uni.setStorageSync('userInfo', JSON.stringify(authStore.userInfo))
    }
  } catch (e) { console.warn('[Profile] refresh failed', e) }
})
</script>

<style scoped>
.profile-container {
  min-height: 100vh;
  background: #f6f8fc;
  padding-bottom: 40rpx;
}

/* 顶部头像区 */
.profile-head {
  background: linear-gradient(135deg, #1f6fff 0%, #18b7ff 100%);
  padding: 60rpx 0 48rpx;
  display: flex;
  flex-direction: column;
  align-items: center;
}
.avatar-wrap {
  position: relative;
  width: 140rpx;
  height: 140rpx;
  margin-bottom: 20rpx;
}
.profile-avatar {
  width: 140rpx;
  height: 140rpx;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 6rpx solid rgba(255, 255, 255, 0.35);
  box-shadow: 0 8rpx 32rpx rgba(0, 0, 0, 0.12);
}
.avatar-text {
  color: #fff;
  font-size: 52rpx;
  font-weight: 700;
}
.avatar-overlay {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  height: 40rpx;
  background: rgba(0, 0, 0, 0.35);
  border-radius: 0 0 70rpx 70rpx;
  display: flex;
  align-items: center;
  justify-content: center;
}
.avatar-overlay-text {
  color: #fff;
  font-size: 20rpx;
}
.profile-name {
  color: #fff;
  font-size: 36rpx;
  font-weight: 700;
  margin-bottom: 8rpx;
}
.profile-role-tag {
  color: rgba(255, 255, 255, 0.8);
  font-size: 22rpx;
  background: rgba(255, 255, 255, 0.18);
  padding: 4rpx 18rpx;
  border-radius: 999rpx;
}

/* 信息卡片 */
.info-card {
  margin: 28rpx 24rpx 0;
  background: #fff;
  border-radius: 28rpx;
  padding: 8rpx 28rpx;
  box-shadow: 0 14rpx 36rpx rgba(31, 49, 84, 0.06);
}
.info-row {
  display: flex;
  align-items: center;
  padding: 24rpx 0;
  min-height: 44rpx;
}
.info-label {
  font-size: 26rpx;
  color: #86909c;
  width: 100rpx;
  flex-shrink: 0;
}
.info-value {
  font-size: 28rpx;
  color: #1d2129;
  flex: 1;
}
.info-value.readonly {
  color: #7b8494;
}
.info-input {
  flex: 1;
  font-size: 28rpx;
  color: #1d2129;
  height: 56rpx;
  background: #f6f8fc;
  border-radius: 12rpx;
  padding: 0 16rpx;
  border: 1rpx solid #edf1f7;
}
.info-arrow {
  font-size: 32rpx;
  color: #a8b0c2;
  margin-left: 8rpx;
}
.info-divider {
  height: 1rpx;
  background: #f0f2f5;
}

/* 菜单卡片 */
.menu-card {
  margin: 28rpx 24rpx 0;
  background: #fff;
  border-radius: 28rpx;
  box-shadow: 0 14rpx 36rpx rgba(31, 49, 84, 0.06);
}
.menu-row {
  display: flex;
  align-items: center;
  padding: 28rpx;
}
.menu-row:active {
  background: #f8fbff;
}
.menu-icon {
  font-size: 32rpx;
  margin-right: 20rpx;
}
.menu-label {
  flex: 1;
  font-size: 28rpx;
  color: #1d2129;
}
.menu-arrow {
  font-size: 32rpx;
  color: #a8b0c2;
}
.menu-divider {
  height: 1rpx;
  background: #f0f2f5;
  margin: 0 28rpx;
}

/* 操作按钮 */
.action-section {
  margin: 28rpx 24rpx 0;
}
.save-btn {
  width: 100%;
  height: 88rpx;
  line-height: 88rpx;
  background: linear-gradient(135deg, #1f6fff, #18b7ff);
  color: #fff;
  font-size: 30rpx;
  border-radius: 24rpx;
  border: none;
  font-weight: 600;
  box-shadow: 0 12rpx 28rpx rgba(31, 111, 255, 0.2);
}
.save-btn[disabled] {
  opacity: 0.5;
}
.cancel-btn {
  width: 100%;
  height: 76rpx;
  line-height: 76rpx;
  background: #fff;
  color: #374151;
  font-size: 28rpx;
  border-radius: 24rpx;
  border: none;
  margin-top: 16rpx;
  box-shadow: 0 8rpx 20rpx rgba(31, 49, 84, 0.05);
}
.logout-btn {
  width: 100%;
  height: 88rpx;
  line-height: 88rpx;
  background: #fff;
  color: #ef4444;
  font-size: 28rpx;
  border-radius: 24rpx;
  border: none;
  box-shadow: 0 10rpx 28rpx rgba(31, 49, 84, 0.05);
}

/* 弹窗 */
.modal-overlay {
  position: fixed;
  top: 0; left: 0; right: 0; bottom: 0;
  background: rgba(0,0,0,0.45);
  z-index: 999;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 60rpx;
}
.modal-popup {
  width: 100%;
  max-width: 560rpx;
  background: #fff;
  border-radius: 20rpx;
  padding: 32rpx;
}
.modal-title {
  font-size: 32rpx;
  font-weight: 600;
  text-align: center;
  display: block;
  margin-bottom: 24rpx;
}
.form-input {
  width: 100%;
  height: 72rpx;
  border: 1rpx solid #e8eaed;
  border-radius: 12rpx;
  padding: 0 20rpx;
  font-size: 26rpx;
  margin-bottom: 16rpx;
  box-sizing: border-box;
}
.modal-btns {
  display: flex;
  gap: 16rpx;
  margin-top: 8rpx;
}
.btn-cancel, .btn-confirm {
  flex: 1;
  height: 72rpx;
  line-height: 72rpx;
  border-radius: 36rpx;
  font-size: 26rpx;
  border: none;
}
.btn-cancel { background: #f5f6f7; color: #4e5969; }
.btn-confirm { background: #409EFF; color: #fff; }
.btn-confirm[disabled] { opacity: 0.5; }
</style>
