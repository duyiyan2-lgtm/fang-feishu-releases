import { computed, ref } from 'vue'

const fps = ref(60)
const rtt = ref(null)
const effectiveType = ref('unknown')
const downlink = ref(null)
const online = ref(typeof navigator === 'undefined' ? true : navigator.onLine)
const longTasks = ref(0)
const lastUpdatedAt = ref(Date.now())
const pageVisible = ref(typeof document === 'undefined' ? true : !document.hidden)
let started = false

function readConnection() {
  if (typeof navigator === 'undefined') return
  const connection = navigator.connection || navigator.mozConnection || navigator.webkitConnection
  online.value = navigator.onLine
  rtt.value = Number.isFinite(connection?.rtt) ? connection.rtt : null
  downlink.value = Number.isFinite(connection?.downlink) ? connection.downlink : null
  effectiveType.value = connection?.effectiveType || 'unknown'
  lastUpdatedAt.value = Date.now()
}

function startFpsMonitor() {
  let frameCount = 0
  let windowStartedAt = performance.now()

  const tick = (now) => {
    // 浏览器会主动暂停后台标签页的 rAF。这里保留切到后台前的最后一次
    // 有效结果，避免用户回来时被错误提示为“页面卡顿”。
    if (!pageVisible.value) {
      frameCount = 0
      windowStartedAt = now
      requestAnimationFrame(tick)
      return
    }
    frameCount += 1
    const elapsed = now - windowStartedAt
    if (elapsed >= 1000) {
      const current = Math.min(60, Math.round((frameCount * 1000) / elapsed))
      fps.value = Math.round(fps.value * 0.55 + current * 0.45)
      frameCount = 0
      windowStartedAt = now
      lastUpdatedAt.value = Date.now()
    }
    requestAnimationFrame(tick)
  }

  requestAnimationFrame(tick)
}

function startLongTaskMonitor() {
  if (typeof PerformanceObserver === 'undefined') return
  try {
    const observer = new PerformanceObserver((list) => {
      longTasks.value += list.getEntries().length
      window.setTimeout(() => {
        longTasks.value = Math.max(0, longTasks.value - list.getEntries().length)
      }, 10000)
    })
    observer.observe({ type: 'longtask', buffered: true })
  } catch {
    // 部分浏览器不支持 longtask，忽略即可。
  }
}

function start() {
  if (started || typeof window === 'undefined') return
  started = true
  readConnection()
  startFpsMonitor()
  startLongTaskMonitor()

  const connection = navigator.connection || navigator.mozConnection || navigator.webkitConnection
  connection?.addEventListener?.('change', readConnection)
  window.addEventListener('online', readConnection)
  window.addEventListener('offline', readConnection)
  document.addEventListener('visibilitychange', () => {
    pageVisible.value = !document.hidden
    if (pageVisible.value) readConnection()
  })
}

function scoreQuality() {
  if (!online.value) return 0
  let score = 100
  if (fps.value < 55) score -= (55 - fps.value) * 2
  if (rtt.value !== null && rtt.value > 80) score -= Math.min(35, (rtt.value - 80) / 8)
  if (['slow-2g', '2g'].includes(effectiveType.value)) score -= 45
  else if (effectiveType.value === '3g') score -= 18
  score -= Math.min(24, longTasks.value * 4)
  return Math.max(0, Math.round(score))
}

const score = computed(scoreQuality)
const level = computed(() => {
  if (!online.value) return 'offline'
  if (score.value >= 85) return 'excellent'
  if (score.value >= 68) return 'good'
  if (score.value >= 45) return 'fair'
  return 'poor'
})
const label = computed(() => ({
  excellent: '流畅',
  good: '良好',
  fair: '一般',
  poor: '较差',
  offline: '离线'
})[level.value])
const detail = computed(() => {
  const parts = [`${fps.value} FPS`]
  if (rtt.value !== null) parts.push(`${rtt.value} ms`)
  if (effectiveType.value !== 'unknown') parts.push(effectiveType.value.toUpperCase())
  return parts.join(' · ')
})

export function usePerformanceQuality() {
  start()
  return {
    fps,
    rtt,
    effectiveType,
    downlink,
    online,
    pageVisible,
    longTasks,
    lastUpdatedAt,
    score,
    level,
    label,
    detail
  }
}
