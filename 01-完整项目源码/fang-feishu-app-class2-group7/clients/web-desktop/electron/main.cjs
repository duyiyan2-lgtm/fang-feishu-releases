const {
  app, BrowserWindow, shell, ipcMain, Menu, dialog, nativeTheme, Tray, nativeImage
} = require('electron')
const path = require('path')
const net = require('net')
const fs = require('fs')

const isDev = process.env.ELECTRON_DEV === '0'
  ? false
  : (process.env.ELECTRON_DEV === '1' || !app.isPackaged)

/** @type {BrowserWindow | null} */
let mainWindow = null
/** @type {BrowserWindow | null} */
let splashWindow = null
/** @type {Tray | null} */
let tray = null
let isQuitting = false

const APP_NAME = '仿飞书工作台'
const APP_TITLE = '仿飞书工作台 · PC'
const SPLASH_MS = 1600

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

async function waitForVite(maxWait = 60000) {
  const ports = [5182, 5173, 5180, 5181, 5183, 5184, 5185, 4173]
  const start = Date.now()
  while (Date.now() - start < maxWait) {
    for (const port of ports) {
      if (await isPortOpen(port)) return port
    }
    await new Promise((r) => setTimeout(r, 600))
  }
  return null
}

function getIconPath() {
  const candidates = [
    path.join(__dirname, '../build/icon.ico'),
    path.join(__dirname, '../build/icon.png'),
    path.join(__dirname, '../build/icon-256.png'),
    path.join(__dirname, '../public/icon.png'),
    path.join(__dirname, '../public/favicon.ico')
  ]
  for (const p of candidates) {
    try {
      if (fs.existsSync(p)) return p
    } catch { /* ignore */ }
  }
  return undefined
}

function getTrayImage() {
  const candidates = [
    path.join(__dirname, '../build/icon-32.png'),
    path.join(__dirname, '../build/icon-16.png'),
    path.join(__dirname, '../build/icon.png'),
    path.join(__dirname, '../build/icon.ico')
  ]
  for (const p of candidates) {
    try {
      if (fs.existsSync(p)) {
        const img = nativeImage.createFromPath(p)
        if (!img.isEmpty()) return process.platform === 'win32' ? img.resize({ width: 16, height: 16 }) : img
      }
    } catch { /* ignore */ }
  }
  return nativeImage.createEmpty()
}

function createSplash() {
  const icon = getIconPath()
  splashWindow = new BrowserWindow({
    width: 420,
    height: 320,
    frame: false,
    transparent: false,
    resizable: false,
    movable: true,
    center: true,
    alwaysOnTop: true,
    skipTaskbar: true,
    show: false,
    backgroundColor: '#0B1220',
    icon,
    webPreferences: {
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: true
    }
  })
  splashWindow.loadFile(path.join(__dirname, 'splash.html'))
  splashWindow.once('ready-to-show', () => {
    if (splashWindow && !splashWindow.isDestroyed()) splashWindow.show()
  })
  splashWindow.on('closed', () => { splashWindow = null })
}

function closeSplash() {
  if (splashWindow && !splashWindow.isDestroyed()) {
    splashWindow.close()
  }
  splashWindow = null
}

function showMain() {
  if (!mainWindow || mainWindow.isDestroyed()) return
  mainWindow.show()
  mainWindow.focus()
  closeSplash()
}

function createWindow() {
  const icon = getIconPath()
  mainWindow = new BrowserWindow({
    width: 1440,
    height: 900,
    minWidth: 1100,
    minHeight: 700,
    show: false,
    title: APP_TITLE,
    backgroundColor: nativeTheme.shouldUseDarkColors ? '#0E1116' : '#F2F3F5',
    frame: false,
    titleBarStyle: 'hidden',
    autoHideMenuBar: true,
    icon,
    webPreferences: {
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: false,
      spellcheck: false,
      backgroundThrottling: false,
      preload: path.join(__dirname, 'preload.cjs')
    }
  })

  const emitMaximize = () => {
    if (mainWindow && !mainWindow.isDestroyed()) {
      mainWindow.webContents.send('window:maximize-change', mainWindow.isMaximized())
    }
  }
  mainWindow.on('maximize', emitMaximize)
  mainWindow.on('unmaximize', emitMaximize)

  // 关闭时最小化到托盘
  mainWindow.on('close', (e) => {
    if (!isQuitting) {
      e.preventDefault()
      mainWindow.hide()
      if (tray && process.platform === 'win32') {
        tray.displayBalloon?.({
          title: APP_NAME,
          content: '应用仍在后台运行，可从托盘图标打开'
        })
      }
    }
  })

  mainWindow.on('closed', () => {
    mainWindow = null
  })

  // 外部链接
  mainWindow.webContents.setWindowOpenHandler(({ url }) => {
    shell.openExternal(url)
    return { action: 'deny' }
  })
  mainWindow.webContents.on('will-navigate', (event, url) => {
    const isLocal = url.startsWith('file://') || url.startsWith('http://localhost') || url.startsWith('http://127.0.0.1')
    if (!isLocal && (url.startsWith('http://') || url.startsWith('https://'))) {
      event.preventDefault()
      shell.openExternal(url)
    }
  })

  const splashStarted = Date.now()
  const reveal = () => {
    const wait = Math.max(0, SPLASH_MS - (Date.now() - splashStarted))
    setTimeout(showMain, wait)
  }

  if (isDev) {
    waitForVite().then((port) => {
      if (port) {
        mainWindow.loadURL(`http://localhost:${port}`)
        if (process.env.ELECTRON_OPEN_DEVTOOLS === '1') {
          mainWindow.webContents.openDevTools({ mode: 'detach' })
        }
      } else {
        closeSplash()
        dialog.showErrorBox(
          '启动失败',
          '找不到 Vite 开发服务器（端口 5182 / 5173 / 5180-5185）。\n请先运行：npm run dev'
        )
      }
    })
  } else {
    mainWindow.loadFile(path.join(__dirname, '../dist/index.html'))
  }

  mainWindow.webContents.once('did-finish-load', reveal)
  // 兜底：加载失败也关闭 splash
  mainWindow.webContents.once('did-fail-load', () => {
    setTimeout(showMain, 400)
  })
}

function createTray() {
  if (tray) return
  const image = getTrayImage()
  tray = new Tray(image)
  tray.setToolTip(APP_NAME)
  const contextMenu = Menu.buildFromTemplate([
    {
      label: '打开工作台',
      click: () => {
        if (!mainWindow) createWindow()
        else {
          mainWindow.show()
          mainWindow.focus()
        }
      }
    },
    {
      label: '重新加载',
      click: () => {
        if (mainWindow && !mainWindow.isDestroyed()) mainWindow.reload()
      }
    },
    { type: 'separator' },
    {
      label: '退出',
      click: () => {
        isQuitting = true
        app.quit()
      }
    }
  ])
  tray.setContextMenu(contextMenu)
  tray.on('double-click', () => {
    if (!mainWindow) createWindow()
    else {
      mainWindow.show()
      mainWindow.focus()
    }
  })
  tray.on('click', () => {
    if (process.platform === 'win32') {
      if (!mainWindow) createWindow()
      else if (mainWindow.isVisible()) mainWindow.focus()
      else {
        mainWindow.show()
        mainWindow.focus()
      }
    }
  })
}

function setupMenu() {
  const isMac = process.platform === 'darwin'
  const template = [
    ...(isMac
      ? [{
          label: APP_NAME,
          submenu: [
            { role: 'about' },
            { type: 'separator' },
            { role: 'quit' }
          ]
        }]
      : []),
    {
      label: '文件',
      submenu: [
        {
          label: '新建窗口',
          accelerator: 'CmdOrCtrl+N',
          click: () => createWindow()
        },
        { type: 'separator' },
        {
          label: '退出',
          accelerator: isMac ? 'Cmd+Q' : 'Alt+F4',
          click: () => {
            isQuitting = true
            app.quit()
          }
        }
      ]
    },
    {
      label: '编辑',
      submenu: [
        { role: 'undo', label: '撤销' },
        { role: 'redo', label: '重做' },
        { type: 'separator' },
        { role: 'cut', label: '剪切' },
        { role: 'copy', label: '复制' },
        { role: 'paste', label: '粘贴' },
        { role: 'selectAll', label: '全选' }
      ]
    },
    {
      label: '视图',
      submenu: [
        { role: 'reload', label: '重新加载' },
        { role: 'toggleDevTools', label: '开发者工具' },
        { type: 'separator' },
        { role: 'resetZoom', label: '实际大小' },
        { role: 'zoomIn', label: '放大' },
        { role: 'zoomOut', label: '缩小' },
        { type: 'separator' },
        { role: 'togglefullscreen', label: '全屏' }
      ]
    },
    {
      label: '帮助',
      submenu: [
        {
          label: `关于 ${APP_NAME}`,
          click: () => {
            dialog.showMessageBox(mainWindow || undefined, {
              type: 'info',
              title: '关于',
              message: APP_NAME,
              detail: [
                `版本: ${app.getVersion()}`,
                `Electron: ${process.versions.electron}`,
                `Chrome: ${process.versions.chrome}`,
                `Node: ${process.versions.node}`,
                `平台: ${process.platform} ${process.arch}`
              ].join('\n')
            })
          }
        }
      ]
    }
  ]
  Menu.setApplicationMenu(Menu.buildFromTemplate(template))
}

// ─── IPC ───
ipcMain.handle('app:get-version', () => app.getVersion())
ipcMain.handle('app:open-external', (_, url) => shell.openExternal(url))
ipcMain.handle('app:get-platform', () => process.platform)
ipcMain.handle('app:show-message', (_, { title, message }) => {
  dialog.showMessageBox(mainWindow || undefined, { type: 'info', title, message })
})
ipcMain.handle('app:is-maximized', () => !!(mainWindow && mainWindow.isMaximized()))
ipcMain.handle('window:control', (_, action) => {
  if (!mainWindow) return
  switch (action) {
    case 'minimize':
      mainWindow.minimize()
      break
    case 'maximize':
      if (mainWindow.isMaximized()) mainWindow.unmaximize()
      else mainWindow.maximize()
      break
    case 'close':
      // 与托盘策略一致：关到托盘
      mainWindow.hide()
      break
    default:
      break
  }
})
ipcMain.handle('app:quit', () => {
  isQuitting = true
  app.quit()
})

const gotLock = app.requestSingleInstanceLock()
if (!gotLock) {
  app.quit()
} else {
  app.on('second-instance', () => {
    if (mainWindow) {
      if (mainWindow.isMinimized()) mainWindow.restore()
      mainWindow.show()
      mainWindow.focus()
    }
  })

  app.whenReady().then(() => {
    app.setName(APP_NAME)
    createSplash()
    createWindow()
    createTray()
    setupMenu()

    app.on('activate', () => {
      if (BrowserWindow.getAllWindows().length === 0) {
        createSplash()
        createWindow()
      } else if (mainWindow) {
        mainWindow.show()
      }
    })
  })
}

app.on('before-quit', () => {
  isQuitting = true
})

app.on('window-all-closed', () => {
  // 有托盘时不因关窗退出
  if (process.platform !== 'darwin' && isQuitting) {
    app.quit()
  }
})
