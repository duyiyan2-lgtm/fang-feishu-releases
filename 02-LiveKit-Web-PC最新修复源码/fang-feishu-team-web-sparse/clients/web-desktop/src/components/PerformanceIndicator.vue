<template>
  <div class="performance-indicator" :class="[`is-${level}`, { 'is-compact': compact }]">
    <button type="button" class="performance-trigger" @click="open = !open" :title="`页面流畅度：${label}`">
      <span class="performance-bars" aria-hidden="true">
        <i></i><i></i><i></i>
      </span>
      <span v-if="!compact" class="performance-copy">
        <span class="performance-caption">页面流畅度</span>
        <strong>{{ label }}</strong>
      </span>
      <strong v-else>{{ label }}</strong>
    </button>

    <transition name="quality-popover">
      <div v-if="open" v-click-outside="() => open = false" class="performance-popover">
        <div class="performance-popover__head">
          <div>
            <span>当前体验</span>
            <strong>{{ label }}</strong>
          </div>
          <b>{{ score }}</b>
        </div>
        <div class="quality-meter"><span :style="{ width: `${score}%` }"></span></div>
        <dl>
          <div><dt>页面帧率</dt><dd>{{ fps }} FPS</dd></div>
          <div><dt>网络延迟</dt><dd>{{ rtt === null ? '浏览器未提供' : `${rtt} ms` }}</dd></div>
          <div><dt>网络类型</dt><dd>{{ networkText }}</dd></div>
          <div><dt>带宽参考</dt><dd>{{ downlink === null ? '—' : `${downlink} Mbps` }}</dd></div>
        </dl>
        <p>{{ suggestion }}</p>
      </div>
    </transition>
  </div>
</template>

<script setup>
import { computed, ref } from 'vue'
import { usePerformanceQuality } from '@/composables/usePerformanceQuality'

defineProps({ compact: { type: Boolean, default: false } })
const open = ref(false)
const { fps, rtt, effectiveType, downlink, score, level, label } = usePerformanceQuality()

const networkText = computed(() => {
  if (level.value === 'offline') return '已离线'
  if (effectiveType.value === 'unknown') return '正常'
  return effectiveType.value.toUpperCase()
})
const suggestion = computed(() => ({
  excellent: '当前页面与网络状态稳定，适合视频会议和实时协作。',
  good: '当前使用较顺畅，可正常进行消息和视频协作。',
  fair: '体验可能有轻微延迟，视频会议时建议关闭其他高带宽任务。',
  poor: '检测到卡顿风险，建议切换网络或关闭不必要的页面。',
  offline: '网络已断开，消息与会议将在恢复连接后继续。'
})[level.value])

const vClickOutside = {
  mounted(el, binding) {
    el._outside = (event) => { if (!el.contains(event.target)) binding.value(event) }
    window.setTimeout(() => document.addEventListener('click', el._outside), 0)
  },
  unmounted(el) { document.removeEventListener('click', el._outside) }
}
</script>

<style scoped>
.performance-indicator { position: relative; }
.performance-trigger { display: flex; align-items: center; gap: 9px; height: 38px; padding: 0 12px; border: 1px solid var(--border-subtle); border-radius: 12px; color: var(--text-primary); background: var(--surface-soft); transition: .2s ease; }
.performance-trigger:hover { transform: translateY(-1px); border-color: var(--quality-color); box-shadow: 0 8px 22px rgba(15, 23, 42, .08); }
.performance-copy { display: flex; flex-direction: column; align-items: flex-start; line-height: 1.05; }
.performance-caption { color: var(--text-tertiary); font-size: 10px; }
.performance-copy strong, .performance-trigger > strong { margin-top: 3px; color: var(--quality-color); font-size: 12px; }
.performance-bars { display: flex; align-items: flex-end; gap: 2px; width: 15px; height: 14px; }
.performance-bars i { width: 3px; border-radius: 3px; background: var(--quality-color); }
.performance-bars i:nth-child(1) { height: 5px; }.performance-bars i:nth-child(2) { height: 9px; }.performance-bars i:nth-child(3) { height: 14px; }
.is-excellent { --quality-color: #16a879; }.is-good { --quality-color: #3b82f6; }.is-fair { --quality-color: #f59e0b; }.is-poor,.is-offline { --quality-color: #ef4444; }
.is-compact .performance-trigger { height: 34px; padding: 0 10px; background: rgba(15, 23, 42, .55); border-color: rgba(255,255,255,.12); color: white; }
.performance-popover { position: absolute; top: calc(100% + 10px); right: 0; z-index: 100; width: 290px; padding: 16px; border: 1px solid var(--border-subtle); border-radius: 16px; background: var(--surface-elevated); box-shadow: 0 18px 60px rgba(15, 23, 42, .18); color: var(--text-primary); }
.performance-popover__head { display: flex; justify-content: space-between; align-items: center; }
.performance-popover__head div { display: flex; flex-direction: column; gap: 3px; }.performance-popover__head span { font-size: 11px; color: var(--text-tertiary); }.performance-popover__head strong { font-size: 16px; color: var(--quality-color); }.performance-popover__head b { font-size: 24px; color: var(--quality-color); }
.quality-meter { height: 6px; margin: 12px 0 14px; overflow: hidden; border-radius: 99px; background: var(--surface-muted); }.quality-meter span { display: block; height: 100%; border-radius: inherit; background: var(--quality-color); transition: width .4s ease; }
dl { display: grid; grid-template-columns: 1fr 1fr; gap: 8px; } dl div { padding: 9px 10px; border-radius: 10px; background: var(--surface-soft); } dt { color: var(--text-tertiary); font-size: 10px; } dd { margin: 3px 0 0; font-size: 12px; font-weight: 650; }
p { margin: 12px 0 0; color: var(--text-secondary); font-size: 11px; line-height: 1.6; }
.quality-popover-enter-active,.quality-popover-leave-active { transition: .16s ease; }.quality-popover-enter-from,.quality-popover-leave-to { opacity: 0; transform: translateY(-5px) scale(.98); }
</style>
