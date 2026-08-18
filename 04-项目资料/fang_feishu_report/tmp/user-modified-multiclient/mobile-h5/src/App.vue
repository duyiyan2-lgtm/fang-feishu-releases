<script setup lang="ts">
import { onLaunch, onShow } from '@dcloudio/uni-app'
import { useAuthStore } from '@/stores/auth'

onLaunch(() => {
  // 初始化认证状态（从本地存储恢复 Token 和用户信息）
  const authStore = useAuthStore()
  authStore.init()
})

// 全局 token 守卫：手动清除 token 后自动跳回登录页
onShow(() => {
  // 排除登录页自身，避免死循环
  const pages = getCurrentPages()
  const currentRoute = pages[pages.length - 1]?.route || ''
  if (currentRoute === 'pages/login/index') return

  // 直接读 storage，比 store 更可靠（store 可能缓存了旧值）
  const token = uni.getStorageSync('token')
  if (!token) {
    uni.showToast({ title: '请重新登录', icon: 'none' })
    uni.reLaunch({ url: '/pages/login/index' })
  }
})
</script>

<style>
/* 全局视觉基线 */
page {
  background-color: #f6f8fc;
  font-size: 14px;
  color: #1f2937;
  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", "PingFang SC", "Helvetica Neue", Arial, sans-serif;
}

view,
text,
input,
textarea,
button {
  box-sizing: border-box;
}

button {
  border: none;
  overflow: visible;
}

button::after {
  border: none;
}

input,
textarea {
  caret-color: #1f6fff;
}

.placeholder {
  color: #a8b0c2;
}

.empty-state,
.loading-state,
.error-state {
  color: #7b8494;
}

.modal-overlay {
  backdrop-filter: blur(8rpx);
}

.modal-popup,
.card-popup {
  box-shadow: 0 30rpx 80rpx rgba(29, 57, 112, 0.18);
}

/* 管理子页面统一美化 */
.page-container {
  background: #f6f8fc !important;
  padding: 24rpx !important;
}

.page-container .header-bar,
.page-container .toolbar,
.page-container .filter-bar {
  background: #ffffff !important;
  border-radius: 28rpx !important;
  border-bottom: none !important;
  padding: 20rpx 22rpx !important;
  margin-bottom: 18rpx !important;
  box-shadow: 0 12rpx 32rpx rgba(31, 49, 84, 0.06) !important;
}

.page-container .header-title {
  color: #111827 !important;
  font-size: 30rpx !important;
  font-weight: 800 !important;
}

.page-container .stats-bar {
  background: #eef4ff !important;
  border-radius: 22rpx !important;
  border-bottom: none !important;
  color: #1f6fff !important;
  margin-bottom: 18rpx !important;
}

.page-container .list,
.page-container .role-card,
.page-container .tree-scroll {
  background: #ffffff !important;
  border-radius: 28rpx !important;
  overflow: hidden !important;
  box-shadow: 0 14rpx 36rpx rgba(31, 49, 84, 0.07) !important;
}

.page-container .list-item,
.page-container .dept-row {
  background: #ffffff !important;
  border-bottom-color: #edf1f7 !important;
}

.page-container .list-item:active,
.page-container .dept-row:active,
.page-container .role-card:active {
  background: #f8fbff !important;
}

.page-container .search-input,
.page-container .filter-input,
.page-container .filter-picker,
.page-container .form-input,
.page-container .form-picker {
  background: #f6f8fc !important;
  border: 1rpx solid #edf1f7 !important;
  border-radius: 18rpx !important;
  color: #111827 !important;
}

.page-container .add-btn,
.page-container .search-btn,
.page-container .btn-confirm {
  background: #1f6fff !important;
  color: #ffffff !important;
  border-radius: 999rpx !important;
  font-weight: 700 !important;
}

.page-container .btn-cancel,
.page-container .modal-close-btn {
  background: #f6f8fc !important;
  color: #374151 !important;
  border-radius: 999rpx !important;
}

.page-container .btn-danger {
  background: #ef4444 !important;
  color: #ffffff !important;
  border-radius: 999rpx !important;
}

.page-container .modal-popup {
  border-radius: 28rpx !important;
}

.page-container .item-name,
.page-container .dept-name,
.page-container .role-name,
.page-container .log-info {
  color: #111827 !important;
  font-weight: 700 !important;
}

.page-container .item-meta,
.page-container .dept-meta,
.page-container .role-desc,
.page-container .log-detail,
.page-container .form-label,
.page-container .detail-label {
  color: #7b8494 !important;
}

.page-container .action-btn,
.page-container .arrow-icon,
.page-container .arrow-label,
.page-container .page-btn {
  color: #1f6fff !important;
}
</style>
