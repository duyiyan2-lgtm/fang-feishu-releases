/**
 * SignalR 实时通信客户端（基于 WebSocket，兼容 uni-app）
 *
 * 用法：
 *   import { signalR } from '@/api/signalr'
 *   signalR.connect(token)
 *   signalR.onMessage((msg) => { ... })
 *   signalR.invoke('SendMessage', { conversationId, content })
 *   signalR.disconnect()
 */

const SIGNALR_URL = 'https://alxy.fun/hubs/im'
const WS_BASE = 'wss://alxy.fun/hubs/im'

type MessageHandler = (msg: any) => void

let socket: UniApp.SocketTask | null = null
let reconnectTimer: number | null = null
const messageHandlers = new Set<MessageHandler>()
let connected = false
let connId = ''
let connToken = ''

/** 发起 negotiate 获取连接参数 */
function negotiate(token: string): Promise<{ connectionId: string; connectionToken: string }> {
  return new Promise((resolve, reject) => {
    uni.request({
      url: `${SIGNALR_URL}/negotiate?negotiateVersion=1`,
      method: 'POST',
      header: { Authorization: `Bearer ${token}` },
      success: (res) => {
        const data = res.data as any
        if (data.connectionId) {
          resolve({ connectionId: data.connectionId, connectionToken: data.connectionToken })
        } else {
          reject(new Error('negotiate 失败'))
        }
      },
      fail: () => reject(new Error('network error')),
    })
  })
}

/** 建立 WebSocket 连接并完成 SignalR 握手 */
function connectWs(token: string, cId: string, cToken: string): Promise<void> {
  return new Promise((resolve, reject) => {
    const url = `${WS_BASE}?id=${cToken}&access_token=${encodeURIComponent(token)}`
    const task = uni.connectSocket({ url, complete: () => {} })
    socket = task

    let handshakeDone = false

    task.onOpen(() => {
      // 发送 SignalR 握手包
      const handshake = '{"protocol":"json","version":1}'
      task.send({ data: handshake })
    })

    task.onMessage((res) => {
      const data = res.data as string
      // SignalR 用 \x1e 分隔多个消息
      const parts = data.split('').filter(Boolean)
      for (const part of parts) {
        try {
          const obj = JSON.parse(part)
          // 握手响应：{} 表示成功
          if (!handshakeDone && obj && Object.keys(obj).length === 0) {
            handshakeDone = true
            connected = true
            resolve()
            return
          }
          // 通知所有已注册的 handler
          messageHandlers.forEach((h) => h(obj))
        } catch {}
      }
    })

    task.onError(() => {
      connected = false
      if (!handshakeDone) reject(new Error('WebSocket error'))
    })

    task.onClose(() => {
      connected = false
      // 自动重连
      scheduleReconnect(token)
    })

    // 10 秒超时
    setTimeout(() => {
      if (!handshakeDone) reject(new Error('handshake timeout'))
    }, 10000)
  })
}

/** 定时重连 */
function scheduleReconnect(token: string) {
  if (reconnectTimer) return
  reconnectTimer = setTimeout(async () => {
    reconnectTimer = null
    try {
      await connect(token)
    } catch {
      scheduleReconnect(token)
    }
  }, 5000)
}

/** 发送 invocation 消息 */
function sendInvocation(target: string, args: any[]) {
  if (!socket || !connected) return
  const msg = JSON.stringify({ type: 1, target, arguments: args }) + ''
  socket.send({ data: msg })
}

// ====== 公开 API ======

export const signalR = {
  /** 连接 SignalR */
  async connect(token: string) {
    try {
      const { connectionId, connectionToken } = await negotiate(token)
      connId = connectionId
      connToken = connectionToken
      await connectWs(token, connectionId, connectionToken)
    } catch (err) {
      console.warn('[SignalR] 连接失败，将继续使用 HTTP', err)
      // 连接失败不阻塞，仍可用 HTTP 发送
    }
  },

  /** 断开连接 */
  disconnect() {
    if (reconnectTimer) {
      clearTimeout(reconnectTimer)
      reconnectTimer = null
    }
    if (socket) {
      socket.close()
      socket = null
    }
    connected = false
    messageHandlers.clear()
  },

  /** 注册消息接收回调 */
  onMessage(handler: MessageHandler) {
    messageHandlers.add(handler)
  },

  /** 取消消息接收回调 — 只移除自己的 handler，不影响其他页面 */
  offMessage(handler?: MessageHandler) {
    if (handler) {
      messageHandlers.delete(handler)
    } else {
      messageHandlers.clear()
    }
  },

  /** 调用 Hub 方法 */
  invoke(method: string, ...args: any[]) {
    sendInvocation(method, args)
  },

  /** 是否已连接 */
  get isConnected() {
    return connected
  },
}
