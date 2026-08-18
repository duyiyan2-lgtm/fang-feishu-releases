import { defineStore } from 'pinia'
import { ref, watch } from 'vue'

export const useThemeStore = defineStore('theme', () => {
  const stored = localStorage.getItem('theme')
  const isDark = ref(
    stored === 'dark' || (!stored && window.matchMedia('(prefers-color-scheme: dark)').matches)
  )

  function toggle() { isDark.value = !isDark.value }
  function setDark(val) { isDark.value = !!val }

  watch(isDark, (val) => {
    document.documentElement.classList.toggle('dark', val)
    localStorage.setItem('theme', val ? 'dark' : 'light')
  }, { immediate: true })

  return { isDark, toggle, setDark }
}, {
  persist: { key: 'feishu-theme', storage: localStorage }
})
