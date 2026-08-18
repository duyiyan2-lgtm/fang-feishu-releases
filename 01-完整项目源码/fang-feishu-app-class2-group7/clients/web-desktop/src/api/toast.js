import { reactive } from 'vue'

const state = reactive({ items: [] })
let id = 0

export function ElMessage({ message, type = 'info', duration = 2500 }) {
  const item = { id: ++id, message, type }
  state.items.push(item)
  setTimeout(() => {
    const idx = state.items.findIndex((i) => i.id === item.id)
    if (idx > -1) state.items.splice(idx, 1)
  }, duration)
}

/**
 * ElMessageBox.prompt — 输入框
 * 用浏览器原生 prompt 包装
 * @returns Promise<{ value: string }> or rejected with 'cancel'
 */
export const ElMessageBox = {
  prompt: (message, title, opts = {}) => {
    return new Promise((resolve, reject) => {
      const result = window.prompt(`${title ? title + '\n\n' : ''}${message}`, opts.inputValue || '')
      if (result === null) {
        reject('cancel')
      } else {
        resolve({ value: result })
      }
    })
  },
  /**
   * ElMessageBox.confirm — 确认框
   * @returns Promise<void> or rejected with 'cancel'
   */
  confirm: (message, title, opts = {}) => {
    return new Promise((resolve, reject) => {
      const label = title ? `${title}\n\n${message}` : message
      if (window.confirm(label)) {
        resolve()
      } else {
        reject('cancel')
      }
    })
  },
  alert: (message, title, opts = {}) => {
    return new Promise((resolve) => {
      window.alert(title ? `${title}\n\n${message}` : message)
      resolve()
    })
  }
}

export function useToast() {
  return state
}