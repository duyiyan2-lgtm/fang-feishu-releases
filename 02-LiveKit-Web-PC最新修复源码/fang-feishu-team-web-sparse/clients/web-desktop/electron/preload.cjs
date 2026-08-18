const { contextBridge, ipcRenderer } = require('electron')

/**
 * 通过 contextBridge 暴露安全 API 给渲染进程
 * 渲染进程只能访问这里定义的方法，不能直接访问 Node.js
 */
contextBridge.exposeInMainWorld('electronAPI', {
  getVersion: () => ipcRenderer.invoke('app:get-version'),
  openExternal: (url) => ipcRenderer.invoke('app:open-external', url),
  getPlatform: () => ipcRenderer.invoke('app:get-platform'),
  showMessage: (title, message) => ipcRenderer.invoke('app:show-message', { title, message }),
  isElectron: true
})