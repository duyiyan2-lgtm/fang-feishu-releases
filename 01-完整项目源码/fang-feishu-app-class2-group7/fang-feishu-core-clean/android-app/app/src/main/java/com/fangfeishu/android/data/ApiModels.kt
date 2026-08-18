package com.fangfeishu.android.data

data class ApiResponse<T>(
    val code: Int,
    val message: String,
    val data: T?,
    val traceId: String? = null
)

data class LoginRequest(val username: String, val password: String, val clientType: String = "Android")

data class RegisterRequest(
    val username: String,
    val password: String,
    val realName: String,
    val email: String?,
    val phone: String?,
    val clientType: String = "Android"
)

data class LoginData(val token: String, val expiresAt: String?, val user: User)

data class User(
    val id: String,
    val username: String,
    val realName: String,
    val email: String? = null,
    val phone: String? = null,
    val departmentId: String? = null,
    val departmentName: String? = null,
    val position: String? = null,
    val avatarUrl: String? = null,
    val workPlace: String? = null,
    val bio: String? = null,
    val roles: List<String> = emptyList()
)

data class UpdateProfileRequest(
    val realName: String? = null,
    val email: String? = null,
    val phone: String? = null,
    val position: String? = null,
    val avatarUrl: String? = null,
    val workPlace: String? = null,
    val bio: String? = null
)

data class FriendRequest(
    val id: String,
    val status: String,
    val direction: String,
    val greeting: String? = null,
    val createdAt: String? = null,
    val user: User
)

data class CreateFriendRequest(val userId: String, val greeting: String? = null)

data class Conversation(
    val id: String,
    val type: String = "Group",
    val title: String? = null,
    val avatar: String? = null,
    val ownerId: String? = null,
    val adminIds: List<String> = emptyList(),
    val members: List<ConversationMember> = emptyList(),
    val lastMessage: Message? = null,
    val unreadCount: Int = 0,
    val createdAt: String? = null
)

data class ConversationMember(
    val userId: String,
    val realName: String? = null,
    val avatar: String? = null,
    val joinedAt: String? = null
)

data class Message(
    val id: String,
    val conversationId: String,
    val senderId: String? = null,
    val senderName: String? = null,
    val content: String = "",
    val messageType: String = "Text",
    val createdAt: String? = null,
    val isRecalled: Boolean = false,
    val reactions: List<MessageReaction> = emptyList()
)

data class MessageReaction(val reactionType: String, val userId: String? = null, val userName: String? = null)
data class MessageSearchResult(
    val message: Message,
    val conversationTitle: String? = null
)
data class CreateConversationRequest(val type: String, val title: String?, val memberUserIds: List<String>)
data class SendMessageRequest(val conversationId: String, val content: String, val messageType: String = "Text")

data class Document(
    val id: String,
    val title: String,
    val content: String? = null,
    val ownerName: String? = null,
    val updatedAt: String? = null,
    val createdAt: String? = null,
    val visibility: String? = null
)

data class DocumentRequest(val title: String, val content: String?)

data class CalendarEvent(
    val id: String,
    val title: String,
    val startTime: String,
    val endTime: String,
    val location: String? = null,
    val description: String? = null,
    val attendees: List<CalendarAttendee> = emptyList()
)

data class CalendarAttendee(val userId: String, val userName: String? = null, val status: String? = null)
data class CalendarEventRequest(
    val title: String,
    val startTime: String,
    val endTime: String,
    val location: String? = null,
    val description: String? = null
)

data class Approval(
    val id: String,
    val type: String,
    val title: String,
    val content: String,
    val status: String,
    val applicantName: String? = null,
    val createdAt: String? = null,
    val records: List<ApprovalRecord> = emptyList()
)

data class ApprovalRecord(val approverName: String? = null, val action: String, val comment: String? = null, val createdAt: String? = null)
data class ApprovalRequest(val type: String, val title: String, val content: String)
data class ApprovalActionRequest(val comment: String? = null)

data class StoredFile(
    val id: String,
    val fileName: String,
    val fileSize: Long = 0,
    val contentType: String? = null,
    val createdAt: String? = null,
    val uploaderName: String? = null
)

data class WorkTask(
    val id: String,
    val title: String,
    val description: String? = null,
    val status: String,
    val dueAt: String? = null,
    val assigneeName: String? = null,
    val creatorName: String? = null
)

data class TaskRequest(val title: String, val description: String? = null, val assigneeId: String? = null, val dueAt: String? = null)
data class TaskStatusRequest(val status: String)

data class Meeting(
    val id: String,
    val title: String,
    val roomName: String? = null,
    val roomId: String? = null,
    val channelName: String? = null,
    val status: String,
    val scheduledStartAt: String? = null,
    val scheduledEndAt: String? = null,
    val memberCount: Int = 0,
    val createdBy: String? = null,
    val creatorName: String? = null,
    val members: List<MeetingMember> = emptyList()
)

data class MeetingMember(
    val userId: String,
    val userName: String? = null,
    val username: String? = null,
    val avatarUrl: String? = null,
    val role: String? = null,
    val invitedAt: String? = null,
    val joinedAt: String? = null,
    val leftAt: String? = null,
    val rtcIdentities: List<MeetingRtcIdentity>? = null
)

data class MeetingRtcIdentity(
    val clientType: String,
    val uid: Long
)

data class MeetingRequest(
    val title: String? = null,
    val roomName: String? = null,
    val roomId: String? = null,
    val memberUserIds: List<String>? = null,
    val scheduledStartAt: String? = null,
    val scheduledEndAt: String? = null
)

data class InviteMeetingMembersRequest(val memberUserIds: List<String>)

data class MeetingJoinRequest(val autoCamera: Boolean = true, val autoMic: Boolean = true)

data class MeetingJoinData(
    val provider: String,
    val meeting: Meeting,
    val appId: String,
    val channelName: String,
    val roomId: String,
    val uid: Long,
    val rtcToken: String? = null,
    val tokenRequired: Boolean = true,
    val tokenExpireAt: String? = null
)

data class WikiSpace(val id: String, val name: String, val description: String? = null, val visibility: String? = null, val ownerName: String? = null)
data class WikiSpaceRequest(val name: String, val description: String? = null, val visibility: String = "Organization")
data class WikiNode(
    val id: String,
    val wikiSpaceId: String? = null,
    val parentId: String? = null,
    val documentId: String? = null,
    val title: String,
    val sortOrder: Int = 0,
    val hasChildren: Boolean = false
)
data class WikiSpaceDetail(val space: WikiSpace, val nodes: List<WikiNode> = emptyList())

data class NotificationItem(
    val id: String,
    val title: String,
    val content: String,
    val type: String,
    val resourceType: String? = null,
    val resourceId: String? = null,
    val isRead: Boolean,
    val createdAt: String? = null
)
