const { contextBridge, ipcRenderer } = require('electron')

/**
 * 安全暴露桌面能力给渲染进程（Vue）
 * 仅 contextBridge 白名单方法，禁止 nodeIntegration
 */
contextBridge.exposeInMainWorld('electronAPI', {
  isElectron: true,
  getVersion: () => ipcRenderer.invoke('app:get-version'),
  openExternal: (url) => ipcRenderer.invoke('app:open-external', url),
  getPlatform: () => ipcRenderer.invoke('app:get-platform'),
  showMessage: (title, message) => ipcRenderer.invoke('app:show-message', { title, message }),
  isMaximized: () => ipcRenderer.invoke('app:is-maximized'),
  windowControl: (action) => ipcRenderer.invoke('window:control', action),
  onMaximizeChange: (callback) => {
    if (typeof callback !== 'function') return () => {}
    const handler = (_event, value) => callback(value)
    ipcRenderer.on('window:maximize-change', handler)
    return () => ipcRenderer.removeListener('window:maximize-change', handler)
  }
})
