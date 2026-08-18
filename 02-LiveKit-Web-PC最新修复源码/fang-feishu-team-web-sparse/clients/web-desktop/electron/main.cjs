const { app, BrowserWindow, shell, ipcMain, Menu, dialog } = require('electron')
const path = require('path')
const net = require('net')

const isDev = process.env.ELECTRON_DEV === '0'
  ? false
  : (process.env.ELECTRON_DEV === '1' || !app.isPackaged)
let mainWindow = null

function isSafeExternalUrl(rawUrl) {
  try {
    const protocol = new URL(rawUrl).protocol
    return ['https:', 'http:', 'mailto:'].includes(protocol)
  } catch {
    return false
  }
}

function isTrustedAppUrl(rawUrl) {
  try {
    const url = new URL(rawUrl)
    if (url.protocol === 'file:') return app.isPackaged
    return isDev && ['localhost', '127.0.0.1'].includes(url.hostname)
  } catch {
    return false
  }
}

/** 检测端口是否被占用（即 vite 是否在跑） */
function isPortOpen(port) {
  return new Promise((resolve) => {
    const socket = new net.Socket()
    socket.setTimeout(500)
    socket.once('connect', () => { socket.destroy(); resolve(true) })
    socket.once('timeout', () => { socket.destroy(); resolve(false) })
    socket.once('error', () => { socket.destroy(); resolve(false) })
    socket.connect(port, '127.0.0.1')
  })
}

/** 等待 vite dev server 启动，最多等 60 秒 */
async function waitForVite(maxWait = 60000) {
  const ports = [5173, 5180, 5181, 5182, 5183, 5184, 5185]
  const start = Date.now()
  while (Date.now() - start < maxWait) {
    for (const port of ports) {
      if (await isPortOpen(port)) return port
    }
    await new Promise((r) => setTimeout(r, 800))
  }
  return null
}

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1440,
    height: 900,
    minWidth: 1024,
    minHeight: 700,
    show: false,
    title: 'Feishu Workspace',
    backgroundColor: '#F5F6F7',
    autoHideMenuBar: false,
    webPreferences: {
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: true,
      devTools: isDev,
      preload: path.join(__dirname, 'preload.cjs')
    }
  })

  if (isDev) {
    // 开发模式：等 vite ready 后再加载
    waitForVite().then((port) => {
      if (port) {
        mainWindow.loadURL(`http://localhost:${port}`)
        mainWindow.webContents.openDevTools({ mode: 'detach' })
      } else {
        dialog.showErrorBox('启动失败', '找不到 Vite 开发服务器（端口 5173 / 5180-5185）。请先运行 npm run dev。')
      }
    })
  } else {
    // 生产模式：加载打包后的文件
    mainWindow.loadFile(path.join(__dirname, '../dist/index.html'))
  }

  mainWindow.once('ready-to-show', () => {
    mainWindow.show()
  })

  // 外部链接用系统浏览器打开
  mainWindow.webContents.setWindowOpenHandler(({ url }) => {
    if (isSafeExternalUrl(url)) void shell.openExternal(url)
    return { action: 'deny' }
  })

  // 防止远程页面替换整个桌面客户端；合法外链交给系统浏览器。
  mainWindow.webContents.on('will-navigate', (event, url) => {
    if (isTrustedAppUrl(url)) return
    event.preventDefault()
    if (isSafeExternalUrl(url)) void shell.openExternal(url)
  })
  mainWindow.webContents.on('will-attach-webview', (event) => event.preventDefault())

  // 会议仅向本应用来源开放摄像头/麦克风，拒绝第三方页面申请权限。
  mainWindow.webContents.session.setPermissionRequestHandler((webContents, permission, callback, details) => {
    const requestingUrl = details?.requestingUrl || webContents.getURL()
    const allowedPermissions = new Set(['media', 'notifications', 'fullscreen'])
    callback(isTrustedAppUrl(requestingUrl) && allowedPermissions.has(permission))
  })

  mainWindow.on('closed', () => {
    mainWindow = null
  })
}

app.whenReady().then(() => {
  createWindow()

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow()
  })
})

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') app.quit()
})

// 应用菜单
const isMac = process.platform === 'darwin'
const template = [
  ...(isMac ? [{
    label: app.name,
    submenu: [
      { role: 'about' },
      { type: 'separator' },
      { role: 'services' },
      { type: 'separator' },
      { role: 'hide' },
      { role: 'hideOthers' },
      { role: 'unhide' },
      { type: 'separator' },
      { role: 'quit' }
    ]
  }] : []),
  {
    label: '文件',
    submenu: [
      {
        label: '新建窗口',
        accelerator: 'CmdOrCtrl+N',
        click: () => createWindow()
      },
      { type: 'separator' },
      isMac ? { role: 'close' } : { role: 'quit' }
    ]
  },
  {
    label: '编辑',
    submenu: [
      { role: 'undo' },
      { role: 'redo' },
      { type: 'separator' },
      { role: 'cut' },
      { role: 'copy' },
      { role: 'paste' },
      { role: 'selectAll' }
    ]
  },
  {
    label: '视图',
    submenu: [
      { role: 'reload' },
      { role: 'forceReload' },
      { role: 'toggleDevTools' },
      { type: 'separator' },
      { role: 'resetZoom' },
      { role: 'zoomIn' },
      { role: 'zoomOut' },
      { type: 'separator' },
      { role: 'togglefullscreen' }
    ]
  },
  {
    label: '帮助',
    submenu: [
      {
        label: '关于 Feishu Workspace',
        click: () => {
          dialog.showMessageBox(mainWindow, {
            type: 'info',
            title: '关于',
            message: 'Feishu Workspace',
            detail: `版本: ${app.getVersion()}\nElectron: ${process.versions.electron}\nNode: ${process.versions.node}\n平台: ${process.platform}`
          })
        }
      }
    ]
  }
]
Menu.setApplicationMenu(Menu.buildFromTemplate(template))

// IPC handlers
ipcMain.handle('app:get-version', () => app.getVersion())
ipcMain.handle('app:open-external', (_, url) => {
  if (!isSafeExternalUrl(url)) return false
  void shell.openExternal(url)
  return true
})
ipcMain.handle('app:get-platform', () => process.platform)
ipcMain.handle('app:show-message', (_, { title, message }) => {
  dialog.showMessageBox(mainWindow, { type: 'info', title, message })
})
