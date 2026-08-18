import { computed, ref, watch, onMounted, onBeforeUnmount, nextTick } from 'vue'

/**
 * 轻量虚拟列表：固定行高 + overscan
 * @param {import('vue').Ref | import('vue').ComputedRef} source 源数组 ref/computed
 * @param {{ itemHeight?: number, overscan?: number }} options
 */
export function useVirtualList(source, options = {}) {
  const itemHeight = options.itemHeight ?? 64
  const overscan = options.overscan ?? 6

  const scrollTop = ref(0)
  const viewportHeight = ref(400)
  const containerRef = ref(null)

  const total = computed(() => (source.value?.length || 0))
  const totalHeight = computed(() => total.value * itemHeight)

  const range = computed(() => {
    const start = Math.max(0, Math.floor(scrollTop.value / itemHeight) - overscan)
    const visible = Math.ceil(viewportHeight.value / itemHeight) + overscan * 2
    const end = Math.min(total.value, start + visible)
    return { start, end }
  })

  const offsetY = computed(() => range.value.start * itemHeight)

  const visibleItems = computed(() => {
    const list = source.value || []
    const { start, end } = range.value
    const out = []
    for (let i = start; i < end; i++) {
      out.push({ index: i, data: list[i], key: list[i]?.id ?? i })
    }
    return out
  })

  function onScroll(e) {
    scrollTop.value = e.target.scrollTop
  }

  function measure() {
    if (containerRef.value) {
      viewportHeight.value = containerRef.value.clientHeight || 400
    }
  }

  function scrollToBottom(behavior = 'auto') {
    nextTick(() => {
      const el = containerRef.value
      if (!el) return
      const top = el.scrollHeight
      if (behavior === 'smooth' && typeof el.scrollTo === 'function') {
        el.scrollTo({ top, behavior: 'smooth' })
      } else {
        el.scrollTop = top
      }
      scrollTop.value = el.scrollTop
    })
  }

  function scrollToIndex(index, align = 'start') {
    nextTick(() => {
      const el = containerRef.value
      if (!el) return
      let top = index * itemHeight
      if (align === 'center') top = top - viewportHeight.value / 2 + itemHeight / 2
      if (align === 'end') top = top - viewportHeight.value + itemHeight
      el.scrollTop = Math.max(0, top)
      scrollTop.value = el.scrollTop
    })
  }

  let ro = null
  onMounted(() => {
    measure()
    if (typeof ResizeObserver !== 'undefined' && containerRef.value) {
      ro = new ResizeObserver(() => measure())
      ro.observe(containerRef.value)
    }
  })

  onBeforeUnmount(() => {
    if (ro) ro.disconnect()
  })

  watch(total, () => measure())

  return {
    containerRef,
    totalHeight,
    offsetY,
    visibleItems,
    onScroll,
    measure,
    scrollToBottom,
    scrollToIndex,
    itemHeight,
    scrollTop
  }
}
