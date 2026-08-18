<template>
  <view class="login-container">
    <view class="login-card">
      <view class="login-header">
        <view class="brand-mark">
          <text class="brand-mark-text">方</text>
        </view>
        <text class="login-title">仿飞书协同办公</text>
        <text class="login-subtitle">登录您的账号</text>
      </view>

      <!-- 登录表单 -->
      <view class="login-form">
        <view class="form-item">
          <text class="form-label">账号</text>
          <input
            v-model="username"
            class="form-input"
            placeholder="请输入用户名"
            placeholder-class="placeholder"
            @input="clearError"
          />
        </view>
        <view class="form-item">
          <text class="form-label">密码</text>
          <view class="password-wrapper">
            <input
              v-model="password"
              class="form-input password-input"
              :password="!showPassword"
              placeholder="请输入密码"
              placeholder-class="placeholder"
              @input="clearError"
            />
            <text class="eye-icon" @tap="showPassword = !showPassword">
              {{ showPassword ? '🙈' : '👁' }}
            </text>
          </view>
        </view>

        <!-- 记住账号密码 -->
        <view class="form-item-remember">
          <label class="remember-label" @tap="toggleRemember">
            <view class="checkbox" :class="{ checked: rememberAccount }">
              <text v-if="rememberAccount" class="checkmark">✓</text>
            </view>
            <text class="remember-text">记住账号密码</text>
          </label>
        </view>

        <!-- 错误提示 -->
        <view v-if="errorMsg" class="error-tip">
          <text class="error-text">{{ errorMsg }}</text>
        </view>

        <!-- 登录按钮 -->
        <button
          class="login-btn"
          :disabled="loading"
          :loading="loading"
          @tap="handleLogin"
        >
          {{ loading ? '登录中...' : '登录' }}
        </button>
      </view>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'

const authStore = useAuthStore()

const username = ref('')
const password = ref('')
const loading = ref(false)
const errorMsg = ref('')
const rememberAccount = ref(false)
const showPassword = ref(false)

// 初始化：从本地存储恢复记住的账号密码
onMounted(() => {
  // 清理旧格式
  uni.removeStorageSync('rememberedUsername')

  const saved = uni.getStorageSync('rememberedLogin')
  if (saved) {
    try {
      const data = JSON.parse(saved)
      username.value = data.username || ''
      password.value = data.password || ''
      rememberAccount.value = true
    } catch {
      // 兼容旧格式（纯用户名）
      username.value = saved
      rememberAccount.value = true
    }
  }
})

function toggleRemember() {
  rememberAccount.value = !rememberAccount.value
}

function clearError() {
  errorMsg.value = ''
}

async function handleLogin() {
  // 表单校验
  if (!username.value.trim()) {
    errorMsg.value = '请输入账号'
    return
  }
  if (!password.value) {
    errorMsg.value = '请输入密码'
    return
  }

  loading.value = true
  errorMsg.value = ''

  try {
    await authStore.login(username.value.trim(), password.value)
    // 记住账号密码
    if (rememberAccount.value) {
      uni.setStorageSync('rememberedLogin', JSON.stringify({
        username: username.value.trim(),
        password: password.value,
      }))
    } else {
      uni.removeStorageSync('rememberedLogin')
    }
    uni.showToast({ title: '登录成功', icon: 'success' })
    // 飞书式入口：登录后进入消息页
    uni.reLaunch({ url: '/pages/im/index' })
  } catch (err: any) {
    errorMsg.value = err.message || '账号或密码错误'
    password.value = '' // 密码清空，账号保留
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.login-container {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100vh;
  background: linear-gradient(180deg, #eef5ff 0%, #f7f9fd 44%, #ffffff 100%);
  padding: 48rpx 36rpx;
  box-sizing: border-box;
  position: relative;
  overflow: hidden;
}
.login-container::before {
  content: '';
  position: absolute;
  top: -180rpx;
  right: -140rpx;
  width: 420rpx;
  height: 420rpx;
  border-radius: 50%;
  background: rgba(31, 111, 255, 0.12);
}
.login-container::after {
  content: '';
  position: absolute;
  left: -120rpx;
  bottom: 120rpx;
  width: 280rpx;
  height: 280rpx;
  border-radius: 50%;
  background: rgba(0, 190, 180, 0.12);
}
.login-card {
  position: relative;
  z-index: 1;
  width: 100%;
  max-width: 600rpx;
  background: rgba(255, 255, 255, 0.96);
  border-radius: 36rpx;
  padding: 64rpx 42rpx 46rpx;
  box-shadow: 0 30rpx 80rpx rgba(31, 72, 132, 0.14);
}
.login-header {
  text-align: center;
  margin-bottom: 52rpx;
}
.brand-mark {
  width: 96rpx;
  height: 96rpx;
  margin: 0 auto 24rpx;
  border-radius: 28rpx;
  background: linear-gradient(135deg, #1f6fff 0%, #18b7ff 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 16rpx 34rpx rgba(31, 111, 255, 0.26);
}
.brand-mark-text {
  color: #fff;
  font-size: 42rpx;
  font-weight: 800;
}
.login-title {
  font-size: 42rpx;
  font-weight: 800;
  color: #111827;
  display: block;
  letter-spacing: 1rpx;
}
.login-subtitle {
  font-size: 26rpx;
  color: #7b8494;
  margin-top: 12rpx;
  display: block;
}
.login-form {
  width: 100%;
}
.form-item {
  margin-bottom: 30rpx;
}
.form-label {
  font-size: 26rpx;
  color: #4b5563;
  margin-bottom: 12rpx;
  display: block;
  font-weight: 600;
}
.form-input {
  height: 92rpx;
  background: #f6f8fc;
  border-radius: 22rpx;
  padding: 0 28rpx;
  font-size: 28rpx;
  color: #111827;
  box-sizing: border-box;
  border: 2rpx solid #edf1f7;
}
.password-wrapper {
  position: relative;
  display: flex;
  align-items: center;
}
.password-input {
  flex: 1;
  padding-right: 92rpx;
}
.eye-icon {
  position: absolute;
  right: 18rpx;
  font-size: 34rpx;
  padding: 14rpx;
}
.placeholder {
  color: #a8b0c2;
  font-size: 28rpx;
}
.error-tip {
  background: #fff1f2;
  border-radius: 18rpx;
  padding: 18rpx 22rpx;
  margin-bottom: 24rpx;
  border: 1rpx solid #ffe1e5;
}
.error-text {
  color: #ef4444;
  font-size: 24rpx;
}
.login-btn {
  width: 100%;
  height: 92rpx;
  line-height: 92rpx;
  background: linear-gradient(135deg, #1f6fff 0%, #18b7ff 100%);
  color: #fff;
  font-size: 32rpx;
  border-radius: 24rpx;
  text-align: center;
  margin-top: 20rpx;
  border: none;
  font-weight: 700;
  box-shadow: 0 16rpx 34rpx rgba(31, 111, 255, 0.26);
}
.login-btn[disabled] {
  opacity: 0.6;
}

/* 记住账号 */
.form-item-remember {
  margin-bottom: 24rpx;
}
.remember-label {
  display: flex;
  align-items: center;
}
.checkbox {
  width: 34rpx;
  height: 34rpx;
  border: 2rpx solid #cfd6e3;
  border-radius: 10rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-right: 12rpx;
}
.checkbox.checked {
  background: #1f6fff;
  border-color: #1f6fff;
}
.checkmark {
  color: #fff;
  font-size: 20rpx;
  font-weight: bold;
}
.remember-text {
  font-size: 24rpx;
  color: #7b8494;
}
</style>
