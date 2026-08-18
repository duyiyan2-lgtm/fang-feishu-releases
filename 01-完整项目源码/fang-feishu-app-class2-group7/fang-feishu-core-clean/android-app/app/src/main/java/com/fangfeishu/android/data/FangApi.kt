package com.fangfeishu.android.data

import com.google.gson.GsonBuilder
import com.google.gson.JsonParser
import okhttp3.Interceptor
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.MultipartBody
import okhttp3.OkHttpClient
import okhttp3.ResponseBody
import okhttp3.RequestBody.Companion.asRequestBody
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import retrofit2.HttpException
import retrofit2.converter.gson.GsonConverterFactory
import retrofit2.http.Body
import retrofit2.http.DELETE
import retrofit2.http.GET
import retrofit2.http.Multipart
import retrofit2.http.POST
import retrofit2.http.PATCH
import retrofit2.http.PUT
import retrofit2.http.Part
import retrofit2.http.Path
import retrofit2.http.Query
import retrofit2.http.Streaming
import java.io.File
import java.util.concurrent.TimeUnit

private const val API_BASE_URL = "https://alxy.fun/api/v1/"
const val IM_HUB_URL = "https://alxy.fun/hubs/im"

object SessionHolder {
    @Volatile
    var token: String? = null

    @Volatile
    var onUnauthorized: (() -> Unit)? = null

    fun invalidateCurrentClientSession() {
        token = null
        onUnauthorized?.invoke()
    }
}

interface FangApi {
    @POST("auth/login")
    suspend fun login(@Body request: LoginRequest): ApiResponse<LoginData>

    @POST("auth/register")
    suspend fun register(@Body request: RegisterRequest): ApiResponse<LoginData>

    @POST("auth/logout")
    suspend fun logout(): ApiResponse<Any>

    @GET("auth/me")
    suspend fun me(): ApiResponse<User>

    @PATCH("auth/me")
    suspend fun updateProfile(@Body request: UpdateProfileRequest): ApiResponse<User>

    @GET("contacts")
    suspend fun contacts(): ApiResponse<List<User>>

    @GET("contacts/discover")
    suspend fun discoverContacts(@Query("keyword") keyword: String? = null): ApiResponse<List<User>>

    @GET("contacts/requests")
    suspend fun friendRequests(): ApiResponse<List<FriendRequest>>

    @POST("contacts/requests")
    suspend fun sendFriendRequest(@Body request: CreateFriendRequest): ApiResponse<FriendRequest>

    @PATCH("contacts/requests/{id}/accept")
    suspend fun acceptFriendRequest(@Path("id") id: String): ApiResponse<FriendRequest>

    @PATCH("contacts/requests/{id}/reject")
    suspend fun rejectFriendRequest(@Path("id") id: String): ApiResponse<FriendRequest>

    @DELETE("contacts/friends/{userId}")
    suspend fun removeFriend(@Path("userId") userId: String): ApiResponse<Any>

    @GET("im/conversations")
    suspend fun conversations(): ApiResponse<List<Conversation>>

    @GET("im/conversations/{id}")
    suspend fun conversation(@Path("id") id: String): ApiResponse<Conversation>

    @POST("im/conversations")
    suspend fun createConversation(@Body request: CreateConversationRequest): ApiResponse<Conversation>

    @GET("im/conversations/{id}/messages")
    suspend fun messages(@Path("id") id: String): ApiResponse<MessagePage>

    @PATCH("im/conversations/{id}/read")
    suspend fun markConversationRead(@Path("id") id: String): ApiResponse<Any>

    @GET("im/messages/search")
    suspend fun searchMessages(@Query("keyword") keyword: String): ApiResponse<List<MessageSearchResult>>

    @POST("im/messages")
    suspend fun sendMessage(@Body request: SendMessageRequest): ApiResponse<Message>

    @GET("documents")
    suspend fun documents(): ApiResponse<List<Document>>

    @POST("documents")
    suspend fun createDocument(@Body request: DocumentRequest): ApiResponse<Document>

    @GET("documents/{id}")
    suspend fun document(@Path("id") id: String): ApiResponse<Document>

    @PUT("documents/{id}")
    suspend fun updateDocument(@Path("id") id: String, @Body request: DocumentRequest): ApiResponse<Document>

    @GET("calendar/events")
    suspend fun calendarEvents(): ApiResponse<List<CalendarEvent>>

    @POST("calendar/events")
    suspend fun createCalendarEvent(@Body request: CalendarEventRequest): ApiResponse<CalendarEvent>

    @GET("approvals")
    suspend fun approvals(): ApiResponse<List<Approval>>

    @POST("approvals")
    suspend fun createApproval(@Body request: ApprovalRequest): ApiResponse<Approval>

    @PATCH("approvals/{id}/approve")
    suspend fun approve(@Path("id") id: String, @Body request: ApprovalActionRequest): ApiResponse<Approval>

    @PATCH("approvals/{id}/reject")
    suspend fun reject(@Path("id") id: String, @Body request: ApprovalActionRequest): ApiResponse<Approval>

    @GET("files")
    suspend fun files(): ApiResponse<List<StoredFile>>

    @Multipart
    @POST("files/upload")
    suspend fun uploadFile(@Part file: MultipartBody.Part): ApiResponse<StoredFile>

    @GET("files/trash")
    suspend fun trashedFiles(): ApiResponse<List<StoredFile>>

    @Streaming
    @GET("files/{id}/download")
    suspend fun downloadFile(@Path("id") id: String): ResponseBody

    @DELETE("files/{id}")
    suspend fun moveFileToTrash(@Path("id") id: String): ApiResponse<Any>

    @POST("files/{id}/restore")
    suspend fun restoreFile(@Path("id") id: String): ApiResponse<StoredFile>

    @DELETE("files/{id}/permanent")
    suspend fun permanentlyDeleteFile(@Path("id") id: String): ApiResponse<Any>

    @GET("tasks")
    suspend fun tasks(): ApiResponse<List<WorkTask>>

    @POST("tasks")
    suspend fun createTask(@Body request: TaskRequest): ApiResponse<WorkTask>

    @PATCH("tasks/{id}/status")
    suspend fun updateTaskStatus(@Path("id") id: String, @Body request: TaskStatusRequest): ApiResponse<WorkTask>

    @GET("meetings")
    suspend fun meetings(): ApiResponse<List<Meeting>>

    @POST("meetings")
    suspend fun createMeeting(@Body request: MeetingRequest): ApiResponse<Meeting>

    @GET("meetings/{id}")
    suspend fun meeting(@Path("id") id: String): ApiResponse<Meeting>

    @POST("meetings/{id}/join")
    suspend fun joinMeeting(@Path("id") id: String, @Body request: MeetingJoinRequest = MeetingJoinRequest()): ApiResponse<MeetingJoinData>

    @POST("meetings/{id}/invite")
    suspend fun inviteMeetingMembers(@Path("id") id: String, @Body request: InviteMeetingMembersRequest): ApiResponse<Meeting>

    @POST("meetings/{id}/leave")
    suspend fun leaveMeeting(@Path("id") id: String): ApiResponse<Any>

    @PATCH("meetings/{id}/end")
    suspend fun endMeeting(@Path("id") id: String): ApiResponse<Meeting>

    @GET("wiki/spaces")
    suspend fun wikiSpaces(): ApiResponse<List<WikiSpace>>

    @POST("wiki/spaces")
    suspend fun createWikiSpace(@Body request: WikiSpaceRequest): ApiResponse<WikiSpace>

    @GET("wiki/spaces/{id}")
    suspend fun wikiSpace(@Path("id") id: String): ApiResponse<WikiSpaceDetail>

    @GET("notifications")
    suspend fun notifications(): ApiResponse<List<NotificationItem>>

    @PATCH("notifications/{id}/read")
    suspend fun markNotificationRead(@Path("id") id: String): ApiResponse<Any>

    @PATCH("notifications/read-all")
    suspend fun markAllNotificationsRead(): ApiResponse<Any>
}

data class MessagePage(val items: List<Message> = emptyList())

object FangApiFactory {
    val api: FangApi by lazy {
        val authInterceptor = Interceptor { chain ->
            val request = chain.request().newBuilder().apply {
                SessionHolder.token?.takeIf { it.isNotBlank() }?.let { header("Authorization", "Bearer $it") }
            }.build()
            chain.proceed(request).also { response ->
                if (response.code == 401 && request.header("Authorization") != null) {
                    SessionHolder.invalidateCurrentClientSession()
                }
            }
        }
        val logger = HttpLoggingInterceptor().apply { level = HttpLoggingInterceptor.Level.BASIC }
        val client = OkHttpClient.Builder()
            .addInterceptor(authInterceptor)
            .addInterceptor(logger)
            .connectTimeout(15, TimeUnit.SECONDS)
            .readTimeout(20, TimeUnit.SECONDS)
            .build()
        Retrofit.Builder()
            .baseUrl(API_BASE_URL)
            .client(client)
            .addConverterFactory(GsonConverterFactory.create(GsonBuilder().create()))
            .build()
            .create(FangApi::class.java)
    }
}

class FangRepository(private val api: FangApi = FangApiFactory.api) {
    private suspend fun <T> unwrap(block: suspend () -> ApiResponse<T>): T {
        try {
            val response = block()
            if (response.code != 0 || response.data == null) {
                throw IllegalStateException(response.message.ifBlank { "请求失败，错误码 ${response.code}" })
            }
            return response.data
        } catch (error: HttpException) {
            val errorBody = error.response()?.errorBody()?.string()
            val serverMessage = runCatching {
                val root = JsonParser.parseString(errorBody).asJsonObject
                root.get("message")?.asString
                    ?: root.get("title")?.asString
                    ?: root.getAsJsonObject("errors")?.entrySet()?.firstNotNullOfOrNull { entry ->
                        entry.value.takeIf { it.isJsonArray }?.asJsonArray?.firstOrNull()?.asString
                    }
            }.getOrNull()
            val readableMessage = when {
                serverMessage?.contains("Agora AppId is not configured", ignoreCase = true) == true ->
                    "服务器尚未配置 Agora AppId，请运维设置 Agora__AppId 后重启后端"
                serverMessage?.contains("Username must be", ignoreCase = true) == true ->
                    "用户名需为2-64个字符，仅支持中文、英文字母、数字、下划线或连字符"
                serverMessage?.contains("Password must be at least", ignoreCase = true) == true ->
                    "密码至少需要6个字符"
                serverMessage?.contains("Real name must be", ignoreCase = true) == true ->
                    "姓名需为1-64个字符"
                serverMessage?.contains("Username already exists", ignoreCase = true) == true ->
                    "该用户名已存在，请更换后重试"
                else -> serverMessage
            }
            throw IllegalStateException(readableMessage ?: "请求失败（HTTP ${error.code()}）")
        }
    }

    suspend fun login(username: String, password: String) = unwrap { api.login(LoginRequest(username, password, "Android")) }
    suspend fun register(request: RegisterRequest) = unwrap { api.register(request) }
    suspend fun logout() = unwrap { api.logout() }
    suspend fun me() = unwrap { api.me() }
    suspend fun updateProfile(request: UpdateProfileRequest) = unwrap { api.updateProfile(request) }
    suspend fun contacts() = unwrap { api.contacts() }
    suspend fun discoverContacts(keyword: String? = null) = unwrap { api.discoverContacts(keyword) }
    suspend fun friendRequests() = unwrap { api.friendRequests() }
    suspend fun sendFriendRequest(userId: String, greeting: String? = null) = unwrap { api.sendFriendRequest(CreateFriendRequest(userId, greeting)) }
    suspend fun acceptFriendRequest(id: String) = unwrap { api.acceptFriendRequest(id) }
    suspend fun rejectFriendRequest(id: String) = unwrap { api.rejectFriendRequest(id) }
    suspend fun removeFriend(userId: String) = unwrap { api.removeFriend(userId) }
    suspend fun conversations() = unwrap { api.conversations() }
    suspend fun conversation(id: String) = unwrap { api.conversation(id) }
    suspend fun createGroup(title: String, memberIds: List<String>) = unwrap { api.createConversation(CreateConversationRequest("Group", title, memberIds)) }
    suspend fun openOrCreateSingle(userId: String): Conversation {
        conversations().firstOrNull { conversation ->
            conversation.type.equals("Single", true) && conversation.members.any { it.userId == userId }
        }?.let { return it }
        return unwrap { api.createConversation(CreateConversationRequest("Single", null, listOf(userId))) }
    }
    suspend fun messages(id: String) = unwrap { api.messages(id) }.items
    suspend fun markConversationRead(id: String) = unwrap { api.markConversationRead(id) }
    suspend fun searchMessages(keyword: String) = unwrap { api.searchMessages(keyword) }
    suspend fun sendMessage(conversationId: String, content: String) = unwrap { api.sendMessage(SendMessageRequest(conversationId, content)) }
    suspend fun documents() = unwrap { api.documents() }
    suspend fun createDocument(title: String, content: String) = unwrap { api.createDocument(DocumentRequest(title, content)) }
    suspend fun document(id: String) = unwrap { api.document(id) }
    suspend fun updateDocument(id: String, title: String, content: String) = unwrap { api.updateDocument(id, DocumentRequest(title, content)) }
    suspend fun calendarEvents() = unwrap { api.calendarEvents() }
    suspend fun createCalendarEvent(request: CalendarEventRequest) = unwrap { api.createCalendarEvent(request) }
    suspend fun approvals() = unwrap { api.approvals() }
    suspend fun createApproval(request: ApprovalRequest) = unwrap { api.createApproval(request) }
    suspend fun approve(id: String, comment: String?) = unwrap { api.approve(id, ApprovalActionRequest(comment)) }
    suspend fun reject(id: String, comment: String?) = unwrap { api.reject(id, ApprovalActionRequest(comment)) }
    suspend fun files() = unwrap { api.files() }
    suspend fun trashedFiles() = unwrap { api.trashedFiles() }
    suspend fun moveFileToTrash(id: String) = unwrap { api.moveFileToTrash(id) }
    suspend fun restoreFile(id: String) = unwrap { api.restoreFile(id) }
    suspend fun permanentlyDeleteFile(id: String) = unwrap { api.permanentlyDeleteFile(id) }
    suspend fun uploadFile(file: File, mimeType: String) = unwrap {
        val body = file.asRequestBody(mimeType.toMediaType())
        api.uploadFile(MultipartBody.Part.createFormData("file", file.name, body))
    }
    suspend fun downloadFile(id: String, target: File): File {
        val response = api.downloadFile(id)
        target.parentFile?.mkdirs()
        response.byteStream().use { input -> target.outputStream().use(input::copyTo) }
        return target
    }
    suspend fun tasks() = unwrap { api.tasks() }
    suspend fun createTask(request: TaskRequest) = unwrap { api.createTask(request) }
    suspend fun updateTaskStatus(id: String, status: String) = unwrap { api.updateTaskStatus(id, TaskStatusRequest(status)) }
    suspend fun meetings() = unwrap { api.meetings() }
    suspend fun createMeeting(request: MeetingRequest) = unwrap { api.createMeeting(request) }
    suspend fun meeting(id: String) = unwrap { api.meeting(id) }
    suspend fun joinMeeting(id: String) = unwrap { api.joinMeeting(id, MeetingJoinRequest()) }
    suspend fun inviteMeetingMembers(id: String, memberIds: List<String>) = unwrap { api.inviteMeetingMembers(id, InviteMeetingMembersRequest(memberIds)) }
    suspend fun leaveMeeting(id: String) = unwrap { api.leaveMeeting(id) }
    suspend fun endMeeting(id: String) = unwrap { api.endMeeting(id) }
    suspend fun wikiSpaces() = unwrap { api.wikiSpaces() }
    suspend fun createWikiSpace(request: WikiSpaceRequest) = unwrap { api.createWikiSpace(request) }
    suspend fun wikiSpace(id: String) = unwrap { api.wikiSpace(id) }
    suspend fun notifications() = unwrap { api.notifications() }
    suspend fun markNotificationRead(id: String) = unwrap { api.markNotificationRead(id) }
    suspend fun markAllNotificationsRead() = unwrap { api.markAllNotificationsRead() }

    fun filePreviewUrl(fileId: String) = "${API_BASE_URL}files/$fileId/preview"
}
