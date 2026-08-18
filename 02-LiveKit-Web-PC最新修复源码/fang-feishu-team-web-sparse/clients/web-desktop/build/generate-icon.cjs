const fs = require('node:fs')
const path = require('node:path')
const { app, BrowserWindow } = require('electron')

app.whenReady().then(async () => {
  const projectRoot = path.resolve(__dirname, '..')
  const svg = fs.readFileSync(path.join(projectRoot, 'public', 'app-icon.svg'), 'utf8')
  const html = `<!doctype html><style>*{margin:0}body{width:512px;height:512px;overflow:hidden}svg{width:512px;height:512px;display:block}</style>${svg}`
  const window = new BrowserWindow({ width: 512, height: 512, show: false, frame: false })

  await window.loadURL(`data:text/html;base64,${Buffer.from(html).toString('base64')}`)
  const image = await window.webContents.capturePage()
  fs.writeFileSync(path.join(__dirname, 'icon.png'), image.toPNG())
  window.destroy()
  app.quit()
}).catch((error) => {
  console.error(error)
  app.exit(1)
})
