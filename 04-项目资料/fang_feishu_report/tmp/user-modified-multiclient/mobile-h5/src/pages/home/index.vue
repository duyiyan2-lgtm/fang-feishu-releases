<template>
  <view class="home-container">
    <!-- 顶部：欢迎 + 通知入口 -->
    <view class="header">
      <view class="header-top">
        <view class="header-text">
          <text class="welcome">仿飞书协同办公</text>
          <text class="app-name">工作台</text>
        </view>
        <view class="notification-btn" @tap="goTo('notifications')">
          <text class="notification-icon">🔔</text>
          <view v-if="unreadCount > 0" class="badge">
            <text class="badge-text">{{ unreadCount > 99 ? '99+' : unreadCount }}</text>
          </view>
        </view>
      </view>
    </view>

    <!-- 功能菜单 -->
    <view class="section-head">
      <text class="section-title">常用应用</text>
      <text class="section-more">全部</text>
    </view>
    <view class="menu-grid">
      <view class="menu-item" @tap="goTo('contacts')">
        <view class="menu-icon" style="background: #409EFF">
          <text class="icon-text">👥</text>
        </view>
        <text class="menu-label">通讯录</text>
      </view>
      <view class="menu-item" @tap="goTo('im')">
        <view class="menu-icon" style="background: #67C23A">
          <text class="icon-text">💬</text>
        </view>
        <text class="menu-label">消息</text>
      </view>
      <view class="menu-item" @tap="goTo('documents')">
        <view class="menu-icon" style="background: #E6A23C">
          <text class="icon-text">📄</text>
        </view>
        <text class="menu-label">文档</text>
      </view>
      <view class="menu-item" @tap="goTo('drive')">
        <view class="menu-icon" style="background: #909399">
          <text class="icon-text">☁️</text>
        </view>
        <text class="menu-label">云盘</text>
      </view>
      <view class="menu-item" @tap="goTo('calendar')">
        <view class="menu-icon" style="background: #B37FEB">
          <text class="icon-text">📅</text>
        </view>
        <text class="menu-label">日历</text>
      </view>
      <view class="menu-item" @tap="goTo('approvals')">
        <view class="menu-icon" style="background: #F56C6C">
          <text class="icon-text">📋</text>
        </view>
        <text class="menu-label">审批</text>
      </view>
      <view class="menu-item" @tap="goTo('tasks')">
        <view class="menu-icon" style="background: #00BFA5">
          <text class="icon-text">✅</text>
        </view>
        <text class="menu-label">任务</text>
      </view>
      <view class="menu-item" @tap="goTo('meetings')">
        <view class="menu-icon" style="background: #FF7043">
          <text class="icon-text">📹</text>
        </view>
        <text class="menu-label">会议</text>
      </view>
      <view class="menu-item" @tap="goTo('wiki')">
        <view class="menu-icon" style="background: #7C4DFF">
          <text class="icon-text">📚</text>
        </view>
        <text class="menu-label">知识库</text>
      </view>
      <view v-if="isAdmin" class="menu-item" @tap="goToAdmin">
        <view class="menu-icon" style="background: #909399">
          <text class="icon-text">⚙️</text>
        </view>
        <text class="menu-label">管理后台</text>
      </view>
    </view>

    <!-- 用户卡片 -->
    <view class="section-head profile-head">
      <text class="section-title">我的</text>
    </view>
    <view class="user-card" @tap="goToProfile">
      <view class="user-avatar" :style="{ backgroundColor: avatarColor }">
        <text class="avatar-text">{{ displayName[0] || '?' }}</text>
      </view>
      <view class="user-info">
        <text class="user-name">{{ displayName || '未登录' }}</text>
        <text class="user-role">{{ isAdmin ? '管理员' : '普通用户' }}</text>
      </view>
      <text class="arrow">›</text>
    </view>

    <!-- 退出登录 -->
    <view class="logout-section">
      <button class="logout-btn" @tap="handleLogout">退出登录</button>
    </view>

    <!-- ====== 个人信息名片弹窗 ====== -->
    <view v-if="showProfileCard" class="card-overlay" @tap="closeProfileCard">
      <view class="card-popup" @tap.stop>
        <view class="card-head" :style="{ backgroundColor: avatarColor }">
          <view class="card-avatar">
            <text class="card-avatar-text">{{ displayName[0] || '?' }}</text>
          </view>
          <text class="card-name">{{ displayName || '未登录' }}</text>
          <text class="card-role-tag">{{ isAdmin ? '管理员' : '普通用户' }}</text>
        </view>
        <view class="card-body">
          <view class="card-row">
            <text class="card-label">用户名</text>
            <text class="card-value">{{ authStore.userInfo?.username || '-' }}</text>
          </view>
          <view class="card-divider" />
          <view class="card-row">
            <text class="card-label">姓名</text>
            <text class="card-value">{{ authStore.userInfo?.realName || '-' }}</text>
          </view>
          <view class="card-divider" />
          <view class="card-row">
            <text class="card-label">部门</text>
            <text class="card-value">{{ deptName(authStore.userInfo?.departmentName || '') || '-' }}</text>
          </view>
          <view class="card-divider" />
          <view class="card-row">
            <text class="card-label">职位</text>
            <text class="card-value">{{ posName(authStore.userInfo?.position || '') || '-' }}</text>
          </view>
        </view>
        <view class="card-footer">
          <button class="card-edit-btn" @tap="goToFullProfile">✏️ 编辑资料</button>
          <button class="card-close-btn" @tap="closeProfileCard">关闭</button>
        </view>
      </view>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { onShow, onShareAppMessage } from '@dcloudio/uni-app'
import { useAuthStore } from '@/stores/auth'
import { getUnreadCount } from '@/api/notifications'
import { updateBadges } from '@/utils/badge'
import { signalR } from '@/api/signalr'

const authStore = useAuthStore()
const displayName = computed(() => authStore.displayName)
const isAdmin = computed(() => authStore.isAdmin)
const unreadCount = ref(0)

const avatarColor = computed(() => {
  const id = authStore.userInfo?.id || '0'
  const colors = ['#409EFF', '#67C23A', '#E6A23C', '#F56C6C', '#909399', '#B37FEB']
  return colors[id.charCodeAt(0) % colors.length]
})

/** 获取未读通知数 */
async function loadUnreadCount() {
  try {
    const res: any = await getUnreadCount()
    unreadCount.value = res?.unreadCount || 0
  } catch {
    unreadCount.value = 0
  }
}

function goTo(page: string) {
  switch (page) {
    case 'contacts':
      uni.switchTab({ url: '/pages/contacts/index' })
      break
    case 'im':
      uni.switchTab({ url: '/pages/im/index' })
      break
    case 'documents':
      uni.switchTab({ url: '/pages/documents/index' })
      break
    case 'drive':
      uni.navigateTo({ url: '/pages/drive/index' })
      break
    case 'calendar':
      uni.navigateTo({ url: '/pages/calendar/index' })
      break
    case 'approvals':
      uni.navigateTo({ url: '/pages/approvals/index' })
      break
    case 'notifications':
      uni.navigateTo({ url: '/pages/notifications/index' })
      break
    case 'tasks':
      uni.navigateTo({ url: '/pages/tasks/index' })
      break
    case 'meetings':
      uni.navigateTo({ url: '/pages/meetings/index' })
      break
    case 'wiki':
      uni.navigateTo({ url: '/pages/wiki/index' })
      break
    default:
      uni.showToast({ title: '功能开发中', icon: 'none' })
  }
}

function goToAdmin() {
  if (!authStore.isAdmin) {
    uni.showToast({ title: '仅管理员可访问', icon: 'none' })
    return
  }
  uni.navigateTo({ url: '/pages/admin/index' })
}

/** 分享小程序给好友 */
function handleShare() {
  // onShareAppMessage 由微信原生触发，这里只是留一个入口
}

/** 右上角分享（微信小程序 onShareAppMessage） */
onShareAppMessage(() => {
  return {
    title: '仿飞书协同办公 - 高效工作平台',
    path: '/pages/home/index',
    imageUrl: '/static/logo.png',
  }
})

/** 部门中文映射 */
function deptName(name: string): string {
  const map: Record<string, string> = {
    'Technology': '技术部',
    'Product': '产品部',
    'Operations': '运营部',
    'Design': '设计部',
    'Marketing': '市场部',
    'Human Resources': '人力资源部',
    'Finance': '财务部',
    'Administration': '行政部',
    'FangFeishu Demo Company': '仿飞书 Demo 公司',
  }
  return map[name] || name
}

/** 职位中文映射 */
function posName(name: string): string {
  if (!name) return ''
  const map: Record<string, string> = {
    'Project Lead': '项目主管',
    'Senior Developer': '高级开发',
    'Frontend Developer': '前端开发',
    'Backend Developer': '后端开发',
    'DevOps Engineer': '运维工程师',
    'Product Manager': '产品经理',
    'Designer': '设计师',
  }
  return map[name] || name
}

/** 退出登录 */
function handleLogout() {
  uni.showModal({
    title: '提示',
    content: '确定退出登录吗？',
    success: (res) => {
      if (res.confirm) {
        signalR.disconnect()
        authStore.logout()
      }
    },
  })
}

/** 名片弹窗 */
const showProfileCard = ref(false)

function goToProfile() {
  showProfileCard.value = true
}

function closeProfileCard() {
  showProfileCard.value = false
}

function goToFullProfile() {
  showProfileCard.value = false
  uni.navigateTo({ url: '/pages/home/profile' })
}

// 每次页面显示时加载未读通知数
onShow(() => {
  loadUnreadCount()
  updateBadges()
})

// 监听来自其他页面的未读变更事件（如通知中心标记已读）
onMounted(() => {
  uni.$on('unread-changed', loadUnreadCount)
})

onUnmounted(() => {
  uni.$off('unread-changed', loadUnreadCount)
})
</script>

<style scoped>
.home-container {
  min-height: 100vh;
  background: #f6f8fc;
  padding: 28rpx;
  box-sizing: border-box;
}

.header {
  margin-bottom: 28rpx;
  padding: 34rpx 30rpx;
  border-radius: 32rpx;
  background: linear-gradient(135deg, #1f6fff 0%, #18b7ff 100%);
  box-shadow: 0 24rpx 60rpx rgba(31, 111, 255, 0.2);
}

.header-top {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
}

.header-text {
  flex: 1;
}

.welcome {
  font-size: 28rpx;
  color: rgba(255, 255, 255, 0.82);
  display: block;
}

.app-name {
  font-size: 42rpx;
  font-weight: 800;
  color: #ffffff;
  display: block;
  margin-top: 8rpx;
}

/* 通知入口 */
.notification-btn {
  position: relative;
  width: 80rpx;
  height: 80rpx;
  background: rgba(255, 255, 255, 0.18);
  border-radius: 24rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  border: 1rpx solid rgba(255, 255, 255, 0.28);
}

.notification-icon {
  font-size: 36rpx;
}

.badge {
  position: absolute;
  top: 4rpx;
  right: 4rpx;
  min-width: 32rpx;
  height: 32rpx;
  background: #ff4d4f;
  border-radius: 16rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0 6rpx;
}

.badge-text {
  color: #fff;
  font-size: 18rpx;
  font-weight: 600;
}

/* 功能菜单网格 */
.menu-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 18rpx;
  margin-bottom: 28rpx;
}

.section-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin: 4rpx 4rpx 18rpx;
}

.section-title {
  font-size: 30rpx;
  font-weight: 800;
  color: #111827;
}

.section-more {
  font-size: 24rpx;
  color: #7b8494;
}

.profile-head {
  margin-top: 4rpx;
}

.menu-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  background: #fff;
  border-radius: 26rpx;
  padding: 28rpx 12rpx 24rpx;
  position: relative;
  box-shadow: 0 12rpx 32rpx rgba(31, 49, 84, 0.06);
}

.menu-item:active {
  transform: scale(0.98);
  background: #f9fbff;
}

.menu-icon {
  width: 84rpx;
  height: 84rpx;
  border-radius: 24rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 12rpx;
}

.icon-text {
  font-size: 38rpx;
}

.menu-label {
  font-size: 24rpx;
  color: #374151;
  font-weight: 600;
}

/* 用户卡片 */
.user-card {
  display: flex;
  align-items: center;
  background: #fff;
  border-radius: 28rpx;
  padding: 30rpx;
  box-shadow: 0 12rpx 32rpx rgba(31, 49, 84, 0.06);
}

.user-avatar {
  width: 88rpx;
  height: 88rpx;
  border-radius: 26rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-right: 20rpx;
}

.avatar-text {
  color: #fff;
  font-size: 32rpx;
  font-weight: 500;
}

.user-info {
  flex: 1;
}

.user-name {
  font-size: 31rpx;
  font-weight: 600;
  color: #111827;
  display: block;
}

.user-role {
  font-size: 24rpx;
  color: #7b8494;
  margin-top: 4rpx;
  display: block;
}

.arrow {
  font-size: 38rpx;
  color: #a8b0c2;
}

/* 退出登录 */
.logout-section {
  margin-top: 34rpx;
  padding-bottom: 40rpx;
}
.logout-btn {
  width: 100%;
  height: 88rpx;
  line-height: 88rpx;
  background: #fff;
  color: #ef4444;
  font-size: 28rpx;
  border-radius: 24rpx;
  text-align: center;
  border: none;
  box-shadow: 0 10rpx 28rpx rgba(31, 49, 84, 0.05);
}

/* ===== 名片弹窗（同通讯录风格） ===== */
.card-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.45);
  z-index: 999;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 60rpx;
  animation: fadeIn 0.2s ease;
}
@keyframes fadeIn {
  from { opacity: 0; }
  to { opacity: 1; }
}
.card-popup {
  width: 100%;
  max-width: 560rpx;
  background: #fff;
  border-radius: 32rpx;
  overflow: hidden;
  animation: slideUp 0.25s ease;
  box-shadow: 0 16rpx 48rpx rgba(0, 0, 0, 0.15);
}
@keyframes slideUp {
  from { transform: translateY(60rpx); opacity: 0; }
  to { transform: translateY(0); opacity: 1; }
}
.card-head {
  padding: 52rpx 0 38rpx;
  text-align: center;
}
.card-avatar {
  width: 100rpx;
  height: 100rpx;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.25);
  display: flex;
  align-items: center;
  justify-content: center;
  margin: 0 auto 16rpx;
  border: 4rpx solid rgba(255, 255, 255, 0.4);
}
.card-avatar-text {
  color: #fff;
  font-size: 40rpx;
  font-weight: 700;
}
.card-name {
  color: #fff;
  font-size: 36rpx;
  font-weight: 600;
}
.card-body {
  padding: 28rpx 36rpx 20rpx;
}
.card-row {
  display: flex;
  align-items: center;
  padding: 16rpx 0;
}
.card-label {
  font-size: 26rpx;
  color: #86909c;
  width: 80rpx;
  flex-shrink: 0;
}
.card-value {
  font-size: 28rpx;
  color: #1d2129;
  flex: 1;
}
.card-divider {
  height: 1rpx;
  background: #f0f2f5;
}
.card-footer {
  padding: 0 36rpx 36rpx;
}
.card-close-btn {
  width: 100%;
  height: 80rpx;
  line-height: 80rpx;
  background: #f6f8fc;
  color: #374151;
  font-size: 28rpx;
  border-radius: 40rpx;
  text-align: center;
  border: none;
  font-weight: 500;
}
.card-close-btn:active {
  background: #edf2fb;
}
.card-edit-btn {
  width: 100%;
  height: 80rpx;
  line-height: 80rpx;
  background: linear-gradient(135deg, #1f6fff, #18b7ff);
  color: #fff;
  font-size: 28rpx;
  border-radius: 40rpx;
  text-align: center;
  border: none;
  font-weight: 500;
  margin-bottom: 12rpx;
  box-shadow: 0 8rpx 24rpx rgba(31, 111, 255, 0.2);
}
.card-edit-btn:active {
  opacity: 0.85;
}
.card-role-tag {
  color: rgba(255, 255, 255, 0.8);
  font-size: 22rpx;
  background: rgba(255, 255, 255, 0.18);
  padding: 4rpx 18rpx;
  border-radius: 999rpx;
  margin-top: 8rpx;
  display: inline-block;
}
</style>
