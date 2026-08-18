package com.fangfeishu.android.ui

import android.content.Context
import android.net.Uri
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.clickable
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.RowScope
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.lazy.rememberLazyListState
import androidx.compose.foundation.selection.toggleable
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.DarkMode
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.Edit
import androidx.compose.material.icons.filled.Info
import androidx.compose.material.icons.filled.Logout
import androidx.compose.material.icons.filled.Notifications
import androidx.compose.material.icons.filled.Search
import androidx.compose.material.icons.filled.Send
import androidx.compose.material.icons.filled.VideoCall
import androidx.compose.material.icons.filled.Save
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.Checkbox
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FloatingActionButton
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Surface
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.activity.compose.BackHandler
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.foundation.text.KeyboardActions
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import com.fangfeishu.android.data.Conversation
import com.fangfeishu.android.data.ConversationMember
import com.fangfeishu.android.data.Document
import com.fangfeishu.android.data.FangRepository
import com.fangfeishu.android.data.Message
import com.fangfeishu.android.data.Meeting
import com.fangfeishu.android.data.MeetingRequest
import com.fangfeishu.android.data.UpdateProfileRequest
import com.fangfeishu.android.data.User
import com.fangfeishu.android.realtime.ImRealtimeClient
import dev.chrisbanes.haze.HazeState
import dev.chrisbanes.haze.blur.blurEffect
import dev.chrisbanes.haze.hazeEffect
import dev.chrisbanes.haze.rememberHazeState
import kotlinx.coroutines.launch
import kotlinx.coroutines.delay
import kotlinx.coroutines.isActive
import java.io.File

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ConversationsScreen(
    repository: FangRepository,
    currentUserId: String,
    displayName: String,
    hazeState: HazeState,
    refreshSignal: Int,
    onOpenConversation: (String) -> Unit
) {
    var conversations by remember { mutableStateOf<List<Conversation>>(emptyList()) }
    var loading by remember { mutableStateOf(true) }
    var error by remember { mutableStateOf<String?>(null) }
    var showGroupDialog by remember { mutableStateOf(false) }
    var searchActive by remember { mutableStateOf(false) }
    var searchQuery by remember { mutableStateOf("") }
    var messageMatches by remember { mutableStateOf<List<com.fangfeishu.android.data.MessageSearchResult>>(emptyList()) }
    var searchingMessages by remember { mutableStateOf(false) }
    val scope = rememberCoroutineScope()
    val visibleConversations = remember(conversations, searchQuery) {
        val query = searchQuery.trim()
        if (query.isBlank()) conversations else conversations.filter { conversation ->
            conversation.title.orEmpty().contains(query, ignoreCase = true) ||
                conversation.lastMessage?.content.orEmpty().contains(query, ignoreCase = true) ||
                conversation.members.any { member ->
                    member.userId != currentUserId && member.realName.orEmpty().contains(query, ignoreCase = true)
                }
        }
    }
    fun refresh() {
        scope.launch {
            loading = true
            error = null
            runCatching { repository.conversations() }
                .onSuccess { conversations = it }
                .onFailure { error = it.message }
            loading = false
        }
    }
    LaunchedEffect(refreshSignal) { refresh() }
    LaunchedEffect(Unit) {
        while (isActive) {
            delay(3_000)
            runCatching { repository.conversations() }.onSuccess { conversations = it }
        }
    }
    LaunchedEffect(searchQuery) {
        val query = searchQuery.trim()
        if (query.length < 2) {
            messageMatches = emptyList()
            searchingMessages = false
            return@LaunchedEffect
        }
        searchingMessages = true
        runCatching { repository.searchMessages(query) }
            .onSuccess { messageMatches = it }
            .onFailure { messageMatches = emptyList() }
        searchingMessages = false
    }
    BackHandler(enabled = searchActive) {
        searchActive = false
        searchQuery = ""
    }

    Box(Modifier.fillMaxSize()) {
        Column(Modifier.fillMaxSize()) {
            GlassTopBar(
                hazeState = hazeState,
                title = "消息",
                actions = {
                    IconButton(onClick = {
                        searchActive = !searchActive
                        if (!searchActive) searchQuery = ""
                    }) { Icon(if (searchActive) Icons.Default.Close else Icons.Default.Search, if (searchActive) "关闭搜索" else "搜索会话") }
                    IconButton(onClick = { showGroupDialog = true }) { Icon(Icons.Default.Add, "创建群聊") }
                }
            )
            if (searchActive) {
                OutlinedTextField(
                    value = searchQuery,
                    onValueChange = { searchQuery = it },
                    modifier = Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 8.dp),
                    placeholder = { Text("搜索群名称、成员或消息") },
                    singleLine = true,
                    leadingIcon = { Icon(Icons.Default.Search, "搜索") },
                    trailingIcon = {
                        if (searchQuery.isNotBlank()) {
                            IconButton(onClick = { searchQuery = "" }) { Icon(Icons.Default.Close, "清空搜索") }
                        }
                    }
                )
            }
            LazyColumn(
                modifier = Modifier.fillMaxSize(),
                contentPadding = androidx.compose.foundation.layout.PaddingValues(start = 16.dp, end = 16.dp, top = 14.dp, bottom = 98.dp),
                verticalArrangement = Arrangement.spacedBy(10.dp)
            ) {
                if (!searchActive) item {
                    GlassCard(hazeState, modifier = Modifier.fillMaxWidth()) {
                        Row(
                            modifier = Modifier.padding(16.dp),
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            BrandMark(Modifier.size(44.dp))
                            Spacer(Modifier.width(12.dp))
                            Column {
                                Text(displayName, style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold)
                                Text("已连接协同工作台", color = MaterialTheme.colorScheme.onSurfaceVariant, style = MaterialTheme.typography.bodySmall)
                            }
                        }
                    }
                }
                when {
                    loading -> item { LoadingBlock() }
                    error != null -> item { ErrorBlock(error.orEmpty()) { refresh() } }
                    conversations.isEmpty() -> item { EmptyBlock("暂无会话", "点击右上角 + 创建多人群聊") }
                    visibleConversations.isEmpty() && messageMatches.isEmpty() && !searchingMessages -> item { EmptyBlock("未找到会话", "尝试搜索群名称、成员或消息内容") }
                    else -> items(visibleConversations, key = { it.id }) { conversation ->
                        ConversationRow(conversation, hazeState) { onOpenConversation(conversation.id) }
                    }
                }
                if (searchActive && searchQuery.trim().length >= 2) {
                    if (searchingMessages) {
                        item {
                            Row(Modifier.padding(12.dp), verticalAlignment = Alignment.CenterVertically) {
                                CircularProgressIndicator(Modifier.size(18.dp), strokeWidth = 2.dp)
                                Spacer(Modifier.width(8.dp))
                                Text("正在搜索历史消息", color = MaterialTheme.colorScheme.onSurfaceVariant)
                            }
                        }
                    } else if (messageMatches.isNotEmpty()) {
                        item { Text("消息匹配", modifier = Modifier.padding(top = 8.dp), style = MaterialTheme.typography.labelLarge, color = MaterialTheme.colorScheme.onSurfaceVariant) }
                        items(messageMatches, key = { it.message.id }) { result ->
                            GlassCard(hazeState, modifier = Modifier.fillMaxWidth().clickable { onOpenConversation(result.message.conversationId) }) {
                                Column(Modifier.padding(14.dp)) {
                                    Text(result.conversationTitle?.ifBlank { null } ?: "聊天", fontWeight = FontWeight.SemiBold)
                                    Text(result.message.content, maxLines = 2, overflow = TextOverflow.Ellipsis)
                                    Text(result.message.senderName ?: "成员", style = MaterialTheme.typography.labelSmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    if (showGroupDialog) {
        CreateGroupDialog(
            repository = repository,
            onDismiss = { showGroupDialog = false },
            onCreated = {
                showGroupDialog = false
                refresh()
            }
        )
    }
}

@Composable
private fun ConversationRow(conversation: Conversation, hazeState: HazeState, onClick: () -> Unit) {
    GlassCard(hazeState, modifier = Modifier.fillMaxWidth().clickable(onClick = onClick)) {
        Row(
            modifier = Modifier.padding(14.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Box(
                Modifier.size(48.dp).clip(CircleShape).background(MaterialTheme.colorScheme.primary.copy(alpha = .82f)),
                contentAlignment = Alignment.Center
            ) {
                Text((conversation.title ?: "群").take(1), color = Color.White, fontWeight = FontWeight.Bold)
            }
            Spacer(Modifier.width(12.dp))
            Column(Modifier.weight(1f)) {
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Text(
                        conversation.title ?: if (conversation.type.equals("Single", true)) "单聊" else "未命名群聊",
                        style = MaterialTheme.typography.titleMedium,
                        fontWeight = FontWeight.SemiBold,
                        maxLines = 1,
                        overflow = TextOverflow.Ellipsis
                    )
                    if (conversation.type.equals("Group", true)) {
                        Spacer(Modifier.width(6.dp))
                        Text("${conversation.members.size}人", color = MaterialTheme.colorScheme.onSurfaceVariant, style = MaterialTheme.typography.labelSmall)
                    }
                }
                Text(
                    conversation.lastMessage?.content ?: "点击进入会话开始交流",
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                    maxLines = 1,
                    overflow = TextOverflow.Ellipsis,
                    style = MaterialTheme.typography.bodyMedium
                )
            }
            if (conversation.unreadCount > 0) {
                Surface(shape = CircleShape, color = MaterialTheme.colorScheme.primary) {
                    Text(
                        conversation.unreadCount.toString(),
                        modifier = Modifier.padding(horizontal = 7.dp, vertical = 3.dp),
                        color = MaterialTheme.colorScheme.onPrimary,
                        style = MaterialTheme.typography.labelSmall
                    )
                }
            }
        }
    }
}

@Composable
private fun CreateGroupDialog(repository: FangRepository, onDismiss: () -> Unit, onCreated: () -> Unit) {
    var title by remember { mutableStateOf("") }
    var contacts by remember { mutableStateOf<List<User>>(emptyList()) }
    var loading by remember { mutableStateOf(true) }
    var saving by remember { mutableStateOf(false) }
    var error by remember { mutableStateOf<String?>(null) }
    val selected = remember { mutableStateListOf<String>() }
    val scope = rememberCoroutineScope()
    LaunchedEffect(Unit) {
        runCatching { repository.contacts() }
            .onSuccess { contacts = it }
            .onFailure { error = it.message }
        loading = false
    }
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("创建多人群聊") },
        text = {
            Column {
                OutlinedTextField(title, { title = it }, label = { Text("群名称") }, singleLine = true, modifier = Modifier.fillMaxWidth())
                Spacer(Modifier.height(10.dp))
                Text("选择成员", style = MaterialTheme.typography.labelLarge)
                if (loading) CircularProgressIndicator(Modifier.padding(14.dp).size(24.dp))
                else LazyColumn(Modifier.height(220.dp)) {
                    items(contacts, key = { it.id }) { user ->
                        Row(
                            modifier = Modifier.fillMaxWidth().toggleable(
                                value = selected.contains(user.id),
                                onValueChange = { checked -> if (checked) selected.add(user.id) else selected.remove(user.id) }
                            ),
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Checkbox(selected.contains(user.id), onCheckedChange = null)
                            Text(user.realName)
                        }
                    }
                }
                error?.let { Text(it, color = MaterialTheme.colorScheme.error, style = MaterialTheme.typography.bodySmall) }
            }
        },
        confirmButton = {
            Button(
                onClick = {
                    saving = true
                    scope.launch {
                        runCatching { repository.createGroup(title.ifBlank { "项目群聊" }, selected.toList()) }
                            .onSuccess { onCreated() }
                            .onFailure { error = it.message; saving = false }
                    }
                },
                enabled = !saving && selected.isNotEmpty()
            ) { Text(if (saving) "创建中" else "创建") }
        },
        dismissButton = { TextButton(onClick = onDismiss) { Text("取消") } }
    )
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ChatScreen(
    repository: FangRepository,
    conversationId: String,
    onStartGroupMeeting: (Meeting) -> Unit,
    onBack: () -> Unit
) {
    val hazeState = rememberHazeState()
    var messages by remember { mutableStateOf<List<Message>>(emptyList()) }
    var draft by remember { mutableStateOf("") }
    var loading by remember { mutableStateOf(true) }
    var error by remember { mutableStateOf<String?>(null) }
    var currentUserId by remember { mutableStateOf<String?>(null) }
    var conversationTitle by remember { mutableStateOf("聊天") }
    var conversation by remember { mutableStateOf<Conversation?>(null) }
    var showMembers by remember { mutableStateOf(false) }
    var startingMeeting by remember { mutableStateOf(false) }
    var meetingError by remember { mutableStateOf<String?>(null) }
    val scope = rememberCoroutineScope()
    val listState = rememberLazyListState()
    val realtimeClient = remember(conversationId) { ImRealtimeClient() }

    suspend fun refreshMessages(showLoading: Boolean = false) {
        if (showLoading) loading = true
        runCatching { repository.messages(conversationId) }
            .onSuccess { latest ->
                val changed = latest != messages
                messages = latest
                error = null
                if (changed) runCatching { repository.markConversationRead(conversationId) }
            }
            .onFailure { failure ->
                if (showLoading || messages.isEmpty()) error = failure.message
            }
        if (showLoading) loading = false
    }

    LaunchedEffect(conversationId) {
        refreshMessages(showLoading = true)
        runCatching { repository.me() }.onSuccess { currentUserId = it.id }
        runCatching { repository.conversation(conversationId) }
            .onSuccess {
                conversation = it
                conversationTitle = it.title?.ifBlank { null } ?: "聊天"
            }

        realtimeClient.connect {
            scope.launch { refreshMessages() }
        }
        try {
            while (isActive) {
                delay(10_000)
                refreshMessages()
                realtimeClient.connect {
                    scope.launch { refreshMessages() }
                }
            }
        } finally {
            realtimeClient.disconnect()
        }
    }
    LaunchedEffect(messages.size) { if (messages.isNotEmpty()) listState.animateScrollToItem(messages.lastIndex) }
    LaunchedEffect(draft) { if (messages.isNotEmpty()) listState.scrollToItem(messages.lastIndex) }

    fun send() {
        val content = draft.trim()
        if (content.isBlank()) return
        draft = ""
        scope.launch {
            runCatching { repository.sendMessage(conversationId, content) }
                .onSuccess { messages = messages + it; error = null }
                .onFailure { error = it.message }
        }
    }

    AtmosphericSurface(darkStyle = MaterialTheme.colorScheme.background.red < .2f, hazeState = hazeState) {
        Column(Modifier.fillMaxSize()) {
            GlassTopBar(hazeState, conversationTitle, navigation = {
                    IconButton(onClick = onBack) { Icon(Icons.Default.ArrowBack, "返回") }
            }, actions = {
                if (conversation?.type.equals("Group", true)) {
                    IconButton(
                        enabled = !startingMeeting,
                        onClick = {
                            val group = conversation ?: return@IconButton
                            startingMeeting = true
                            scope.launch {
                                val title = "${group.title?.ifBlank { null } ?: "群聊"}的视频会议"
                                runCatching {
                                    repository.createMeeting(
                                        MeetingRequest(
                                            title = title,
                                            roomName = "群聊即时会议",
                                            memberUserIds = group.members.map { it.userId }
                                        )
                                    )
                                }.onSuccess { meeting ->
                                    startingMeeting = false
                                    onStartGroupMeeting(meeting)
                                }.onFailure { error ->
                                    startingMeeting = false
                                    meetingError = error.message ?: "发起视频会议失败"
                                }
                            }
                        }
                    ) { Icon(Icons.Default.VideoCall, "发起视频会议") }
                    IconButton(onClick = { showMembers = true }) { Icon(Icons.Default.Info, "查看群成员") }
                }
            })
            LazyColumn(
                state = listState,
                modifier = Modifier.fillMaxWidth().weight(1f),
                contentPadding = androidx.compose.foundation.layout.PaddingValues(14.dp, 14.dp, 14.dp, 10.dp),
                verticalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                if (loading) item { LoadingBlock() }
                error?.let { item { Text(it, color = MaterialTheme.colorScheme.error, style = MaterialTheme.typography.bodySmall) } }
                items(messages, key = { it.id }) { message -> MessageBubble(message, message.senderId == currentUserId) }
            }
            Surface(
                modifier = Modifier.fillMaxWidth().hazeEffect(hazeState) { blurEffect { blurRadius = 16.dp } }
                    .navigationBarsPadding().imePadding(),
                color = MaterialTheme.colorScheme.surface.copy(alpha = .88f),
                contentColor = MaterialTheme.colorScheme.onSurface
            ) {
                Row(Modifier.padding(horizontal = 10.dp, vertical = 8.dp), verticalAlignment = Alignment.Bottom) {
                    OutlinedTextField(
                        value = draft,
                        onValueChange = { draft = it },
                        modifier = Modifier.weight(1f),
                        placeholder = { Text("输入消息") },
                        shape = RoundedCornerShape(22.dp),
                        maxLines = 4,
                        keyboardOptions = KeyboardOptions(imeAction = ImeAction.Send),
                        keyboardActions = KeyboardActions(onSend = { send() })
                    )
                    Spacer(Modifier.width(6.dp))
                    IconButton(enabled = draft.isNotBlank(), onClick = { send() }) {
                        Icon(Icons.Default.Send, "发送", tint = if (draft.isNotBlank()) MaterialTheme.colorScheme.primary else MaterialTheme.colorScheme.onSurfaceVariant)
                    }
                }
            }
        }
    }
    if (showMembers) {
        ConversationMembersDialog(
            members = conversation?.members.orEmpty(),
            onDismiss = { showMembers = false }
        )
    }
    meetingError?.let { message ->
        AlertDialog(
            onDismissRequest = { meetingError = null },
            title = { Text("暂时无法发起会议") },
            text = { Text(message) },
            confirmButton = { TextButton(onClick = { meetingError = null }) { Text("知道了") } }
        )
    }
}

@Composable
private fun ConversationMembersDialog(members: List<ConversationMember>, onDismiss: () -> Unit) {
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("群成员（${members.size}）") },
        text = {
            if (members.isEmpty()) {
                Text("暂无成员信息", color = MaterialTheme.colorScheme.onSurfaceVariant)
            } else {
                LazyColumn(Modifier.fillMaxWidth().heightIn(max = 360.dp)) {
                    items(members, key = { it.userId }) { member ->
                        Row(Modifier.fillMaxWidth().padding(vertical = 8.dp), verticalAlignment = Alignment.CenterVertically) {
                            UserAvatar(member.avatar, member.realName ?: "成员", Modifier.size(38.dp))
                            Spacer(Modifier.width(10.dp))
                            Text(member.realName ?: "未命名成员")
                        }
                    }
                }
            }
        },
        confirmButton = { TextButton(onClick = onDismiss) { Text("关闭") } }
    )
}

@Composable
private fun MessageBubble(message: Message, isMine: Boolean) {
    val recalled = message.isRecalled
    Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = if (isMine) Arrangement.End else Arrangement.Start,
        verticalAlignment = Alignment.Bottom
    ) {
        if (!isMine) {
            Box(
                Modifier.size(34.dp).clip(CircleShape).background(MaterialTheme.colorScheme.primary.copy(alpha = .82f)),
                contentAlignment = Alignment.Center
            ) { Text((message.senderName ?: "成").take(1), color = Color.White, fontWeight = FontWeight.Bold) }
            Spacer(Modifier.width(8.dp))
        }
        Column(horizontalAlignment = if (isMine) Alignment.End else Alignment.Start, modifier = Modifier.fillMaxWidth(.78f)) {
            if (!isMine) Text(message.senderName ?: "成员", style = MaterialTheme.typography.labelSmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
            Surface(
                shape = RoundedCornerShape(
                    topStart = 18.dp, topEnd = 18.dp,
                    bottomStart = if (isMine) 18.dp else 5.dp,
                    bottomEnd = if (isMine) 5.dp else 18.dp
                ),
                color = if (isMine) MaterialTheme.colorScheme.primary else MaterialTheme.colorScheme.surface.copy(alpha = .92f),
                contentColor = if (isMine) MaterialTheme.colorScheme.onPrimary else MaterialTheme.colorScheme.onSurface
            ) {
                Text(if (recalled) "此消息已撤回" else message.content, modifier = Modifier.padding(horizontal = 13.dp, vertical = 9.dp))
            }
            message.createdAt?.let {
                Text(formatMoment(it).takeLast(5), color = MaterialTheme.colorScheme.onSurfaceVariant, style = MaterialTheme.typography.labelSmall)
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun DocumentsScreen(repository: FangRepository, hazeState: HazeState) {
    var documents by remember { mutableStateOf<List<Document>>(emptyList()) }
    var loading by remember { mutableStateOf(true) }
    var error by remember { mutableStateOf<String?>(null) }
    var create by remember { mutableStateOf(false) }
    var selectedDocumentId by remember { mutableStateOf<String?>(null) }
    val scope = rememberCoroutineScope()
    fun refresh() {
        scope.launch {
            loading = true
            runCatching {
                repository.documents().map { summary ->
                    if (summary.content != null) summary else runCatching { repository.document(summary.id) }.getOrDefault(summary)
                }
            }.onSuccess { documents = it }.onFailure { error = it.message }
            loading = false
        }
    }
    LaunchedEffect(Unit) { refresh() }
    BackHandler(enabled = selectedDocumentId != null) {
        selectedDocumentId = null
        refresh()
    }
    selectedDocumentId?.let { documentId ->
        DocumentDetailScreen(
            repository = repository,
            hazeState = hazeState,
            documentId = documentId,
            onBack = { selectedDocumentId = null; refresh() }
        )
        return
    }
    Box(Modifier.fillMaxSize()) {
        Column(Modifier.fillMaxSize()) {
            GlassTopBar(hazeState, "云文档")
            LazyColumn(
                modifier = Modifier.fillMaxSize(),
                contentPadding = androidx.compose.foundation.layout.PaddingValues(16.dp, 14.dp, 16.dp, 98.dp),
                verticalArrangement = Arrangement.spacedBy(10.dp)
            ) {
                when {
                    loading -> item { LoadingBlock() }
                    error != null -> item { ErrorBlock(error.orEmpty()) { refresh() } }
                    documents.isEmpty() -> item { EmptyBlock("暂无文档", "点击右上角 + 创建协作文档") }
                    else -> items(documents, key = { it.id }) { document ->
                        GlassCard(hazeState, modifier = Modifier.fillMaxWidth().clickable { selectedDocumentId = document.id }) {
                            Column(Modifier.padding(15.dp)) {
                                Text(document.title, fontWeight = FontWeight.SemiBold, style = MaterialTheme.typography.titleMedium)
                                Text(document.content.orEmpty().ifBlank { "暂无正文内容" }, maxLines = 2, overflow = TextOverflow.Ellipsis, color = MaterialTheme.colorScheme.onSurfaceVariant)
                                Text("${document.ownerName ?: "我"} · ${document.updatedAt?.let(::formatMoment) ?: "刚刚"}", style = MaterialTheme.typography.labelSmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
                            }
                        }
                    }
                }
            }
        }
        FloatingActionButton(
            onClick = { create = true },
            modifier = Modifier.align(Alignment.BottomEnd).padding(22.dp)
        ) { Icon(Icons.Default.Add, "新建文档") }
    }
    if (create) CreateDocumentDialog(repository, { create = false }, { create = false; refresh() })
}

@Composable
fun DocumentDetailScreen(
    repository: FangRepository,
    hazeState: HazeState,
    documentId: String,
    onBack: () -> Unit
) {
    var title by remember(documentId) { mutableStateOf("") }
    var content by remember(documentId) { mutableStateOf("") }
    var loading by remember(documentId) { mutableStateOf(true) }
    var saving by remember(documentId) { mutableStateOf(false) }
    var error by remember(documentId) { mutableStateOf<String?>(null) }
    val scope = rememberCoroutineScope()

    LaunchedEffect(documentId) {
        runCatching { repository.document(documentId) }
            .onSuccess { document -> title = document.title; content = document.content.orEmpty() }
            .onFailure { error = it.message }
        loading = false
    }

    Column(Modifier.fillMaxSize()) {
        GlassTopBar(
            hazeState = hazeState,
            title = if (title.isBlank()) "文档详情" else title,
            navigation = { IconButton(onClick = onBack) { Icon(Icons.Default.ArrowBack, "返回") } },
            actions = {
                IconButton(
                    enabled = title.isNotBlank() && !saving,
                    onClick = {
                        saving = true
                        scope.launch {
                            runCatching { repository.updateDocument(documentId, title, content) }
                                .onSuccess { saving = false; error = null }
                                .onFailure { error = it.message; saving = false }
                        }
                    }
                ) { Icon(Icons.Default.Save, "保存") }
            }
        )
        if (loading) {
            LoadingBlock()
        } else {
            Column(Modifier.fillMaxSize().padding(16.dp)) {
                OutlinedTextField(title, { title = it }, label = { Text("标题") }, modifier = Modifier.fillMaxWidth(), singleLine = true)
                Spacer(Modifier.height(10.dp))
                OutlinedTextField(
                    value = content,
                    onValueChange = { content = it },
                    label = { Text("正文") },
                    modifier = Modifier.fillMaxWidth().weight(1f),
                    minLines = 12
                )
                error?.let { Text(it, color = MaterialTheme.colorScheme.error, modifier = Modifier.padding(top = 8.dp)) }
                TextButton(
                    onClick = {
                        saving = true
                        scope.launch {
                            runCatching { repository.updateDocument(documentId, title, content) }
                                .onSuccess { saving = false; error = null }
                                .onFailure { error = it.message; saving = false }
                        }
                    },
                    enabled = title.isNotBlank() && !saving,
                    modifier = Modifier.align(Alignment.End)
                ) { Text(if (saving) "保存中" else "保存修改") }
            }
        }
    }
}

@Composable
private fun CreateDocumentDialog(repository: FangRepository, onDismiss: () -> Unit, onCreated: () -> Unit) {
    var title by remember { mutableStateOf("") }
    var content by remember { mutableStateOf("") }
    var error by remember { mutableStateOf<String?>(null) }
    var saving by remember { mutableStateOf(false) }
    val scope = rememberCoroutineScope()
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("新建协作文档") },
        text = {
            Column {
                OutlinedTextField(title, { title = it }, label = { Text("标题") }, modifier = Modifier.fillMaxWidth())
                Spacer(Modifier.height(8.dp))
                OutlinedTextField(content, { content = it }, label = { Text("正文") }, modifier = Modifier.fillMaxWidth(), minLines = 4)
                error?.let { Text(it, color = MaterialTheme.colorScheme.error) }
            }
        },
        confirmButton = {
            Button(
                enabled = title.isNotBlank() && !saving,
                onClick = {
                    saving = true
                    scope.launch {
                        runCatching { repository.createDocument(title, content) }.onSuccess { onCreated() }.onFailure { error = it.message; saving = false }
                    }
                }
            ) { Text(if (saving) "创建中" else "创建") }
        },
        dismissButton = { TextButton(onClick = onDismiss) { Text("取消") } }
    )
}

@Composable
fun WorkbenchScreen(hazeState: HazeState, onOpenFeature: (Feature) -> Unit) {
    LazyColumn(
        modifier = Modifier.fillMaxSize(),
        contentPadding = androidx.compose.foundation.layout.PaddingValues(16.dp, 14.dp, 16.dp, 98.dp),
        verticalArrangement = Arrangement.spacedBy(14.dp)
    ) {
        item { GlassTopBar(hazeState, "工作台") }
        item {
            androidx.compose.foundation.lazy.grid.LazyVerticalGrid(
                columns = androidx.compose.foundation.lazy.grid.GridCells.Fixed(2),
                modifier = Modifier.height(520.dp),
                verticalArrangement = Arrangement.spacedBy(12.dp),
                horizontalArrangement = Arrangement.spacedBy(12.dp)
            ) {
                items(Feature.entries.size) { index ->
                    val feature = Feature.entries[index]
                    GlassCard(hazeState, modifier = Modifier.fillMaxWidth().height(116.dp).clickable { onOpenFeature(feature) }) {
                        Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(10.dp)) {
                            Surface(shape = RoundedCornerShape(12.dp), color = MaterialTheme.colorScheme.primary.copy(alpha = .15f)) {
                                Icon(feature.icon, feature.title, modifier = Modifier.padding(9.dp), tint = MaterialTheme.colorScheme.primary)
                            }
                            Text(feature.title, fontWeight = FontWeight.SemiBold)
                        }
                    }
                }
            }
        }
    }
}

@Composable
fun MineScreen(
    initialUser: User,
    repository: FangRepository,
    darkStyle: Boolean,
    hazeState: HazeState,
    onThemeChanged: (Boolean) -> Unit,
    onProfileUpdated: (User) -> Unit,
    onLogout: () -> Unit
) {
    var showSettings by remember { mutableStateOf(false) }
    var editingProfile by remember { mutableStateOf(false) }
    var currentUser by remember(initialUser) { mutableStateOf(initialUser) }

    BackHandler(enabled = editingProfile) { editingProfile = false }
    if (editingProfile) {
        ProfileEditor(
            repository = repository,
            user = currentUser,
            hazeState = hazeState,
            onBack = { editingProfile = false },
            onUpdated = { updated ->
                currentUser = updated
                onProfileUpdated(updated)
                editingProfile = false
            }
        )
        return
    }
    LazyColumn(
        modifier = Modifier.fillMaxSize(),
        contentPadding = androidx.compose.foundation.layout.PaddingValues(16.dp, 14.dp, 16.dp, 98.dp),
        verticalArrangement = Arrangement.spacedBy(14.dp)
    ) {
        item { GlassTopBar(hazeState, "我的") }
        item {
            GlassCard(hazeState, modifier = Modifier.fillMaxWidth().clickable { editingProfile = true }) {
                Row(Modifier.padding(20.dp), verticalAlignment = Alignment.CenterVertically) {
                    UserAvatar(currentUser.avatarUrl, currentUser.realName, Modifier.size(62.dp))
                    Spacer(Modifier.width(15.dp))
                    Column(Modifier.weight(1f)) {
                        Text(currentUser.realName, style = MaterialTheme.typography.headlineSmall, fontWeight = FontWeight.Bold)
                        Text("已登录协同工作台", color = MaterialTheme.colorScheme.onSurfaceVariant)
                    }
                    Icon(Icons.Default.Edit, "编辑个人信息", tint = MaterialTheme.colorScheme.primary)
                }
            }
        }
        item {
            GlassCard(hazeState, modifier = Modifier.fillMaxWidth().clickable { showSettings = true }) {
                Row(Modifier.padding(18.dp), verticalAlignment = Alignment.CenterVertically) {
                    Icon(Icons.Default.DarkMode, "外观", tint = MaterialTheme.colorScheme.primary)
                    Spacer(Modifier.width(12.dp))
                    Column {
                        Text("外观与设置", style = MaterialTheme.typography.titleMedium)
                        Text(if (darkStyle) "深色飞书风格" else "淡青至淡蓝动态渐变", color = MaterialTheme.colorScheme.onSurfaceVariant)
                    }
                }
            }
        }
        item {
            TextButton(onClick = onLogout, modifier = Modifier.fillMaxWidth()) {
                Icon(Icons.Default.Logout, "退出登录")
                Spacer(Modifier.width(8.dp))
                Text("退出登录")
            }
        }
    }
    if (showSettings) {
        AlertDialog(
            onDismissRequest = { showSettings = false },
            title = { Text("外观设置") },
            text = {
                Column {
                    Text("默认使用淡青色至淡蓝色的低频动态 MeshGradient。")
                    Spacer(Modifier.height(12.dp))
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        Checkbox(checked = darkStyle, onCheckedChange = onThemeChanged)
                        Column {
                            Text("启用黑色样式")
                            Text("关闭后恢复动态渐变背景", style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
                        }
                    }
                }
            },
            confirmButton = { TextButton(onClick = { showSettings = false }) { Text("完成") } }
        )
    }
}

@Composable
private fun ProfileEditor(
    repository: FangRepository,
    user: User,
    hazeState: HazeState,
    onBack: () -> Unit,
    onUpdated: (User) -> Unit
) {
    val context = LocalContext.current
    val scope = rememberCoroutineScope()
    var realName by remember(user.id) { mutableStateOf(user.realName) }
    var email by remember(user.id) { mutableStateOf(user.email.orEmpty()) }
    var phone by remember(user.id) { mutableStateOf(user.phone.orEmpty()) }
    var position by remember(user.id) { mutableStateOf(user.position.orEmpty()) }
    var workPlace by remember(user.id) { mutableStateOf(user.workPlace.orEmpty()) }
    var bio by remember(user.id) { mutableStateOf(user.bio.orEmpty()) }
    var avatarUrl by remember(user.id) { mutableStateOf(user.avatarUrl.orEmpty()) }
    var uploadingAvatar by remember { mutableStateOf(false) }
    var saving by remember { mutableStateOf(false) }
    var error by remember { mutableStateOf<String?>(null) }
    val imagePicker = rememberLauncherForActivityResult(ActivityResultContracts.GetContent()) { uri ->
        if (uri == null) return@rememberLauncherForActivityResult
        scope.launch {
            uploadingAvatar = true
            error = null
            runCatching {
                val file = copyAvatarToCache(context, uri)
                repository.uploadFile(file, context.contentResolver.getType(uri) ?: "image/*")
            }.onSuccess { uploaded ->
                avatarUrl = repository.filePreviewUrl(uploaded.id)
            }.onFailure { error = it.message ?: "头像上传失败" }
            uploadingAvatar = false
        }
    }

    Column(Modifier.fillMaxSize()) {
        GlassTopBar(
            hazeState = hazeState,
            title = "个人信息",
            navigation = { IconButton(onClick = onBack) { Icon(Icons.Default.ArrowBack, "返回") } }
        )
        LazyColumn(
            modifier = Modifier.fillMaxSize(),
            contentPadding = androidx.compose.foundation.layout.PaddingValues(16.dp, 14.dp, 16.dp, 32.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            item {
                GlassCard(hazeState, modifier = Modifier.fillMaxWidth()) {
                    Column(
                        modifier = Modifier.fillMaxWidth().padding(20.dp),
                        horizontalAlignment = Alignment.CenterHorizontally,
                        verticalArrangement = Arrangement.spacedBy(10.dp)
                    ) {
                        Box(contentAlignment = Alignment.BottomEnd) {
                            UserAvatar(avatarUrl, realName, Modifier.size(92.dp), "当前头像")
                            Surface(
                                modifier = Modifier.size(32.dp).clickable(enabled = !uploadingAvatar) { imagePicker.launch("image/*") },
                                shape = CircleShape,
                                color = MaterialTheme.colorScheme.primary
                            ) {
                                Box(contentAlignment = Alignment.Center) {
                                    if (uploadingAvatar) {
                                        CircularProgressIndicator(
                                            modifier = Modifier.size(18.dp),
                                            strokeWidth = 2.dp,
                                            color = Color.White
                                        )
                                    } else {
                                        Icon(Icons.Default.Edit, "选择头像", tint = Color.White, modifier = Modifier.size(18.dp))
                                    }
                                }
                            }
                        }
                        Text(if (uploadingAvatar) "正在上传头像" else "点击头像更换照片", color = MaterialTheme.colorScheme.onSurfaceVariant)
                    }
                }
            }
            item {
                GlassCard(hazeState, modifier = Modifier.fillMaxWidth()) {
                    Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(10.dp)) {
                        OutlinedTextField(realName, { realName = it }, label = { Text("姓名") }, modifier = Modifier.fillMaxWidth(), singleLine = true)
                        OutlinedTextField(email, { email = it }, label = { Text("邮箱") }, modifier = Modifier.fillMaxWidth(), singleLine = true)
                        OutlinedTextField(phone, { phone = it }, label = { Text("手机号") }, modifier = Modifier.fillMaxWidth(), singleLine = true)
                        OutlinedTextField(position, { position = it }, label = { Text("岗位") }, modifier = Modifier.fillMaxWidth(), singleLine = true)
                        OutlinedTextField(workPlace, { workPlace = it }, label = { Text("办公地点") }, modifier = Modifier.fillMaxWidth(), singleLine = true)
                        OutlinedTextField(bio, { bio = it }, label = { Text("个人简介") }, modifier = Modifier.fillMaxWidth(), minLines = 3, maxLines = 5)
                        error?.let { Text(it, color = MaterialTheme.colorScheme.error, style = MaterialTheme.typography.bodySmall) }
                        Button(
                            onClick = {
                                saving = true
                                error = null
                                scope.launch {
                                    runCatching {
                                        repository.updateProfile(
                                            UpdateProfileRequest(
                                                realName = realName,
                                                email = email,
                                                phone = phone,
                                                position = position,
                                                avatarUrl = avatarUrl,
                                                workPlace = workPlace,
                                                bio = bio
                                            )
                                        )
                                    }.onSuccess(onUpdated)
                                        .onFailure { error = it.message ?: "保存个人信息失败"; saving = false }
                                }
                            },
                            enabled = realName.isNotBlank() && !saving && !uploadingAvatar,
                            modifier = Modifier.fillMaxWidth()
                        ) { Text(if (saving) "正在保存" else "保存个人信息") }
                    }
                }
            }
        }
    }
}

private fun copyAvatarToCache(context: Context, uri: Uri): File {
    val target = File(context.cacheDir, "avatar_${System.currentTimeMillis()}.jpg")
    context.contentResolver.openInputStream(uri)?.use { input ->
        target.outputStream().use { output -> input.copyTo(output) }
    } ?: error("无法读取选择的头像")
    return target
}

@Composable
fun LoadingBlock() {
    Box(Modifier.fillMaxWidth().padding(38.dp), contentAlignment = Alignment.Center) { CircularProgressIndicator() }
}

@Composable
fun EmptyBlock(title: String, detail: String) {
    Column(Modifier.fillMaxWidth().padding(38.dp), horizontalAlignment = Alignment.CenterHorizontally) {
        Text(title, fontWeight = FontWeight.SemiBold)
        Spacer(Modifier.height(6.dp))
        Text(detail, color = MaterialTheme.colorScheme.onSurfaceVariant, textAlign = androidx.compose.ui.text.style.TextAlign.Center)
    }
}

@Composable
fun ErrorBlock(message: String, onRetry: () -> Unit) {
    Column(Modifier.fillMaxWidth().padding(24.dp), horizontalAlignment = Alignment.CenterHorizontally) {
        Text(message, color = MaterialTheme.colorScheme.error, textAlign = androidx.compose.ui.text.style.TextAlign.Center)
        TextButton(onClick = onRetry) { Text("重新加载") }
    }
}
