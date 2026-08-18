import { useUserStore } from '@/stores/user'

class WSClient {
  constructor() {
    this.ws = null
    this.url = ''
    this.listeners = new Set()
    this.reconnectTimer = null
    this.heartbeatTimer = null
    this.shouldReconnect = true
  }

  connect(url) {
    this.url = url
    this.shouldReconnect = true
    const userStore = useUserStore()
    const token = userStore.token || ''
    const fullUrl = url + (url.includes('?') ? '&' : '?') + 'token=' + encodeURIComponent(token)
    if (this.ws) this.ws.close()
    this.ws = new WebSocket(fullUrl)

    this.ws.onopen = () => {
      console.info('[WS] connected:', this.url)
      this.startHeartbeat()
      this.listeners.forEach((cb) => cb({ type: 'open' }))
    }
    this.ws.onmessage = (e) => {
      try {
        const data = JSON.parse(e.data)
        this.listeners.forEach((cb) => cb(data))
      } catch (err) {
        console.error('[WS] parse error', err)
      }
    }
    this.ws.onclose = () => {
      this.stopHeartbeat()
      console.info('[WS] closed')
      if (this.shouldReconnect) this.scheduleReconnect()
    }
    this.ws.onerror = (err) => {
      console.error('[WS] error', err)
    }
  }

  send(data) {
    if (this.ws?.readyState === WebSocket.OPEN) {
      this.ws.send(typeof data === 'string' ? data : JSON.stringify(data))
    }
  }

  onMessage(cb) {
    this.listeners.add(cb)
    return () => this.listeners.delete(cb)
  }

  startHeartbeat() {
    this.stopHeartbeat()
    this.heartbeatTimer = setInterval(() => this.send({ type: 'ping', ts: Date.now() }), 30000)
  }

  stopHeartbeat() {
    if (this.heartbeatTimer) clearInterval(this.heartbeatTimer)
    this.heartbeatTimer = null
  }

  scheduleReconnect() {
    if (this.reconnectTimer) return
    this.reconnectTimer = setTimeout(() => {
      this.reconnectTimer = null
      this.connect(this.url)
    }, 3000)
  }

  disconnect() {
    this.shouldReconnect = false
    this.stopHeartbeat()
    if (this.reconnectTimer) clearTimeout(this.reconnectTimer)
    this.ws?.close()
  }
}

export default new WSClient()
