import * as signalR from '@microsoft/signalr'
import { useUserStore } from '@/stores/user'

const HUB_URL = (import.meta.env.VITE_WS_URL || '/hubs/im')
const FULL_URL = '/api/v1'.replace(/\/api\/v1$/, '') // 不需要 baseURL
// 实际 hub url 拼接：axios baseURL = /api/v1，所以 hub url = /hubs/im 即可

/**
 * SignalR Hub 客户端封装
 * 后端 Hub 路径：/hubs/im
 * 已知事件（约定）：
 *   ReceiveMessage           收到新消息
 *   MessageRecalled          消息被撤回
 *   ConversationUpdated      会话列表变化（新会话等）
 *   GroupMemberAdded         群成员加入
 *   GroupMemberRemoved       群成员被踢/退
 *   GroupUpdated             群资料更新
 *   GroupDissolved           群被解散
 *   MessageRead              消息已读回执
 *   IncomingCall             来电（视频通话）
 *   CallAccepted             对方接受
 *   CallRejected             对方拒绝
 *   CallEnded                通话结束
 *   OfferReceived / AnswerReceived / IceCandidateReceived  WebRTC 信令
 */
class IMHub {
  constructor() {
    this.connection = null
    this.handlers = {
      onMessage: null,
      onRecalled: null,
      onConversation: null,
      // ===== 群管理 =====
      onGroupMemberAdded: null,
      onGroupMemberRemoved: null,
      onGroupUpdated: null,
      onGroupDissolved: null,
      onMessageRead: null,
      onFriendRequestReceived: null,
      onFriendRequestAccepted: null,
      onFriendRequestRejected: null,
      onFriendRemoved: null,
      // ===== 通知中心 =====
      onReceiveNotification: null,
      onNotificationRead: null,
      onNotificationsReadAll: null,
      // ===== 会议 =====
      onMeetingInvited: null,
      onMeetingEnded: null,
      // ===== IM 4 个新事件 =====
      onConversationAnnouncementUpdated: null,
      onConversationRemoved: null,
      onConversationDissolved: null,
      onMessageReactionUpdated: null,
      // ===== 视频通话 =====
      onIncomingCall: null,
      onCallAccepted: null,
      onCallRejected: null,
      onCallEnded: null,
      onOfferReceived: null,
      onAnswerReceived: null,
      onIceCandidateReceived: null
    }
    this._started = false
  }

  start() {
    if (this._started) return
    // 先解除旧 listener（防止多次 start() 导致重复 add）
    if (this.connection) {
      this.connection.off('ReceiveMessage')
      this.connection.off('MessageRecalled')
      this.connection.off('ConversationUpdated')
      this.connection.off('GroupMemberAdded')
      this.connection.off('GroupMemberRemoved')
      this.connection.off('GroupUpdated')
      this.connection.off('GroupDissolved')
      this.connection.off('MessageRead')
      this.connection.off('FriendRequestReceived')
      this.connection.off('FriendRequestAccepted')
      this.connection.off('FriendRequestRejected')
      this.connection.off('FriendRemoved')
      this.connection.off('ReceiveNotification')
      this.connection.off('NotificationRead')
      this.connection.off('NotificationsReadAll')
      this.connection.off('MeetingInvited')
      this.connection.off('MeetingEnded')
      this.connection.off('ConversationAnnouncementUpdated')
      this.connection.off('ConversationRemoved')
      this.connection.off('ConversationDissolved')
      this.connection.off('MessageReactionUpdated')
      this.connection.off('IncomingCall')
      this.connection.off('CallAccepted')
      this.connection.off('CallRejected')
      this.connection.off('CallEnded')
      this.connection.off('OfferReceived')
      this.connection.off('AnswerReceived')
      this.connection.off('IceCandidateReceived')
    }

    const userStore = useUserStore()
    const token = userStore.token || ''

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${HUB_URL}?access_token=${encodeURIComponent(token)}`, {
        skipNegotiation: false,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    // ===== 现有事件 =====
    this.connection.on('ReceiveMessage', (msg) => {
      this.handlers.onMessage?.(msg)
    })
    this.connection.on('MessageRecalled', (payload) => {
      this.handlers.onRecalled?.(payload)
    })
    this.connection.on('ConversationUpdated', (payload) => {
      this.handlers.onConversation?.(payload)
    })

    // ===== 群管理事件 =====
    this.connection.on('GroupMemberAdded', (p) => this.handlers.onGroupMemberAdded?.(p))
    this.connection.on('GroupMemberRemoved', (p) => this.handlers.onGroupMemberRemoved?.(p))
    this.connection.on('GroupUpdated', (p) => this.handlers.onGroupUpdated?.(p))
    this.connection.on('GroupDissolved', (p) => this.handlers.onGroupDissolved?.(p))
    this.connection.on('MessageRead', (p) => this.handlers.onMessageRead?.(p))

    // ===== 视频通话事件 =====
    this.connection.on('IncomingCall', (p) => this.handlers.onIncomingCall?.(p))
    this.connection.on('CallAccepted', (p) => this.handlers.onCallAccepted?.(p))
    this.connection.on('CallRejected', (p) => this.handlers.onCallRejected?.(p))
    this.connection.on('CallEnded', (p) => this.handlers.onCallEnded?.(p))
    this.connection.on('OfferReceived', (p) => this.handlers.onOfferReceived?.(p))
    this.connection.on('AnswerReceived', (p) => this.handlers.onAnswerReceived?.(p))
    this.connection.on('IceCandidateReceived', (p) => this.handlers.onIceCandidateReceived?.(p))

    // ===== 通知中心 =====
    this.connection.on('ReceiveNotification', (p) => this.handlers.onReceiveNotification?.(p))
    this.connection.on('NotificationRead', (p) => this.handlers.onNotificationRead?.(p))
    this.connection.on('NotificationsReadAll', (p) => this.handlers.onNotificationsReadAll?.(p))
    // ===== 好友 =====
    this.connection.on('FriendRemoved', (p) => this.handlers.onFriendRemoved?.(p))
    // ===== IM 4 个新事件 =====
    this.connection.on('ConversationAnnouncementUpdated', (p) => this.handlers.onConversationAnnouncementUpdated?.(p))
    this.connection.on('ConversationRemoved', (p) => this.handlers.onConversationRemoved?.(p))
    this.connection.on('ConversationDissolved', (p) => this.handlers.onConversationDissolved?.(p))
    this.connection.on('MessageReactionUpdated', (p) => this.handlers.onMessageReactionUpdated?.(p))
    // ===== 会议 =====
    this.connection.on('MeetingInvited', (p) => this.handlers.onMeetingInvited?.(p))
    this.connection.on('MeetingEnded', (p) => this.handlers.onMeetingEnded?.(p))

    this.connection.onreconnected(() => {
      console.info('[SignalR] reconnected')
      this._whenConnectedResolve?.()
      this._whenConnectedResolve = null
      this._whenConnectedReject = null
    })
    this.connection.onclose(() => {
      this._started = false
      // 重置 promise 以便下次 start() 能再次被 await
      this._whenConnectedPromise = null
      this._whenConnectedResolve = null
      this._whenConnectedReject = null
    })

    this.connection.start()
      .then(() => {
        this._started = true
        console.info('[SignalR] connected:', HUB_URL)
        this._whenConnectedResolve?.()
        this._whenConnectedResolve = null
        this._whenConnectedReject = null
      })
      .catch((err) => {
        console.error('[SignalR] start error', err)
        this._whenConnectedReject?.(err)
        this._whenConnectedPromise = null
        this._whenConnectedResolve = null
      })
  }

  /**
   * 返回一个 Promise，resolve 当连接已建立（或重新连接成功）。
   * 每次 start() 重置；外部 await 即可拿到「真实已连接」的通知。
   */
  whenConnected() {
    if (this._started) return Promise.resolve()
    if (!this._whenConnectedPromise) {
      this._whenConnectedPromise = new Promise((resolve, reject) => {
        this._whenConnectedResolve = resolve
        this._whenConnectedReject = reject
      })
    }
    return this._whenConnectedPromise
  }

  stop() {
    if (this.connection) this.connection.stop()
    this._started = false
  }

  // ===== IM 业务方法 =====

  /** 发送消息（SignalR invoke） */
  sendMessage(conversationId, content, messageType = 'Text') {
    if (!this._started) return Promise.reject(new Error('SignalR 未连接'))
    return this.connection.invoke('SendMessage', conversationId, content, messageType)
  }

  /** 加入会话群组 */
  joinConversation(conversationId) {
    if (!this._started) return Promise.resolve()
    return this.connection.invoke('JoinConversation', conversationId).catch(() => {})
  }

  // ===== 视频通话 invoke 方法 =====

  /** 发起通话 */
  callUser(targetUserId, callType) {
    if (!this._started) return Promise.reject(new Error('SignalR 未连接'))
    return this.connection.invoke('CallUser', targetUserId, callType)
  }

  /** 接受通话 */
  acceptCall(callId) {
    if (!this._started) return Promise.reject(new Error('SignalR 未连接'))
    return this.connection.invoke('AcceptCall', callId)
  }

  /** 拒绝通话 */
  rejectCall(callId, reason = 'decline') {
    if (!this._started) return Promise.reject(new Error('SignalR 未连接'))
    return this.connection.invoke('RejectCall', callId, reason)
  }

  /** 挂断 */
  endCall(callId) {
    if (!this._started) return Promise.reject(new Error('SignalR 未连接'))
    return this.connection.invoke('EndCall', callId)
  }

  /** 发送 SDP Offer */
  sendOffer(callId, sdpJson) {
    if (!this._started) return Promise.reject(new Error('SignalR 未连接'))
    return this.connection.invoke('SendOffer', callId, sdpJson)
  }

  /** 发送 SDP Answer */
  sendAnswer(callId, sdpJson) {
    if (!this._started) return Promise.reject(new Error('SignalR 未连接'))
    return this.connection.invoke('SendAnswer', callId, sdpJson)
  }

  /** 发送 ICE 候选 */
  sendIceCandidate(callId, candidateJson) {
    if (!this._started) return Promise.reject(new Error('SignalR 未连接'))
    return this.connection.invoke('SendIceCandidate', callId, candidateJson)
  }
}

const hub = new IMHub()
export default hub
