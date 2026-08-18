<template>
  <view class="admin-container">
    <text class="admin-title">管理后台</text>
    <view class="admin-grid">
      <view class="admin-card" @tap="goTo('/pages/admin/users')">
        <text class="card-icon">👥</text>
        <text class="card-label">用户管理</text>
      </view>
      <view class="admin-card" @tap="goTo('/pages/admin/departments')">
        <text class="card-icon">🏢</text>
        <text class="card-label">部门管理</text>
      </view>
      <view class="admin-card" @tap="goTo('/pages/admin/roles')">
        <text class="card-icon">🔐</text>
        <text class="card-label">角色管理</text>
      </view>
      <view class="admin-card" @tap="goTo('/pages/admin/logs')">
        <text class="card-icon">📋</text>
        <text class="card-label">操作日志</text>
      </view>
    </view>
  </view>
</template>

<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()

if (!authStore.isAdmin) {
  uni.reLaunch({ url: '/pages/home/index' })
}

function goTo(url: string) {
  uni.navigateTo({ url })
}
</script>

<style scoped>
.admin-container {
  min-height: 100vh;
  background: #f6f8fc;
  padding: 24rpx;
}
.admin-title {
  font-size: 38rpx;
  font-weight: 800;
  color: #111827;
  display: block;
  margin-bottom: 30rpx;
  padding: 34rpx 30rpx;
  border-radius: 32rpx;
  background: linear-gradient(135deg, #111827, #1f6fff);
  color: #fff;
  box-shadow: 0 22rpx 54rpx rgba(31, 49, 84, 0.18);
}
.admin-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 20rpx;
}
.admin-card {
  background: #fff;
  border-radius: 28rpx;
  padding: 46rpx 24rpx;
  text-align: center;
  box-shadow: 0 14rpx 36rpx rgba(31, 49, 84, 0.07);
  border: 1rpx solid #edf1f7;
}
.admin-card:active {
  background: #f8fbff;
  transform: scale(0.98);
}
.card-icon {
  width: 82rpx;
  height: 82rpx;
  margin: 0 auto 18rpx;
  border-radius: 24rpx;
  background: #eef4ff;
  font-size: 46rpx;
  display: flex;
  align-items: center;
  justify-content: center;
}
.card-label {
  font-size: 28rpx;
  font-weight: 700;
  color: #111827;
}
</style>
