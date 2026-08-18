package com.fangfeishu.android.ui

import android.app.DatePickerDialog
import android.app.TimePickerDialog
import android.content.Context
import android.content.Intent
import android.widget.Toast
import androidx.activity.compose.BackHandler
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Assignment
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.CalendarMonth
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.CloudUpload
import androidx.compose.material.icons.filled.Done
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.MenuBook
import androidx.compose.material.icons.filled.Chat
import androidx.compose.material.icons.filled.OpenInNew
import androidx.compose.material.icons.filled.VideoCall
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FloatingActionButton
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Surface
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.core.content.FileProvider
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import com.fangfeishu.android.data.Approval
import com.fangfeishu.android.data.ApprovalRequest
import com.fangfeishu.android.data.CalendarEvent
import com.fangfeishu.android.data.CalendarEventRequest
import com.fangfeishu.android.data.FangRepository
import com.fangfeishu.android.data.FriendRequest
import com.fangfeishu.android.data.Meeting
import com.fangfeishu.android.data.MeetingJoinData
import com.fangfeishu.android.data.MeetingRequest
import com.fangfeishu.android.data.NotificationItem
import com.fangfeishu.android.data.StoredFile
import com.fangfeishu.android.data.TaskRequest
import com.fangfeishu.android.data.User
import com.fangfeishu.android.data.WikiSpace
import com.fangfeishu.android.data.WikiSpaceDetail
import com.fangfeishu.android.data.WikiSpaceRequest
import com.fangfeishu.android.data.WorkTask
import dev.chrisbanes.haze.HazeState
import dev.chrisbanes.haze.rememberHazeState
import java.io.File
import java.time.OffsetDateTime
import java.time.LocalDateTime
import java.time.ZoneId
import kotlinx.coroutines.launch
import kotlinx.coroutines.delay
import kotlinx.coroutines.isActive

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun FeatureScreen(
    repository: FangRepository,
    feature: Feature,
    currentUser: User,
    onOpenConversation: (String) -> Unit,
    onOpenMeeting: (Meeting) -> Unit,
    onBack: () -> Unit
) {
    val hazeState = rememberHazeState()
    if (feature == Feature.Meetings) {
        MeetingsFeature(repository, hazeState, currentUser, onBack)
        return
    }
    val darkStyle = MaterialTheme.colorScheme.background.red < .2f
    AtmosphericSurface(darkStyle = darkStyle, hazeState = hazeState) {
        Scaffold(
            containerColor = Color.Transparent,
            topBar = {
                GlassTopBar(hazeState, feature.title, navigation = {
                    IconButton(onClick = onBack) { Icon(Icons.Default.ArrowBack, "返回") }
                })
            }
        ) { padding ->
            Box(Modifier.fillMaxSize().padding(padding)) {
                when (feature) {
                    Feature.Calendar -> CalendarFeature(repository, hazeState)
                    Feature.Approvals -> ApprovalsFeature(repository, hazeState)
                    Feature.Files -> FilesFeature(repository, hazeState)
                    Feature.Tasks -> TasksFeature(repository, hazeState)
                    Feature.Meetings -> Unit
                    Feature.Contacts -> ContactsFeatureV2(repository, hazeState, onOpenConversation)
                    Feature.Wiki -> WikiFeature(repository, hazeState)
                    Feature.Notifications -> NotificationsFeature(repository, hazeState, onOpenMeeting)
                }
            }
        }
    }
}

@Composable
private fun CalendarFeature(repository: FangRepository, hazeState: HazeState) {
    var items by remember { mutableStateOf<List<CalendarEvent>>(emptyList()) }
    var loading by remember { mutableStateOf(true) }
    var error by remember { mutableStateOf<String?>(null) }
    var showCreate by remember { mutableStateOf(false) }
    var selectedEvent by remember { mutableStateOf<CalendarEvent?>(null) }
    val scope = rememberCoroutineScope()
    fun load() = scope.launch {
        loading = true
        runCatching { repository.calendarEvents() }.onSuccess { items = it }.onFailure { error = it.message }
        loading = false
    }
    LaunchedEffect(Unit) { load() }
    FeatureList(hazeState, items, loading, error, { load() }, { showCreate = true }) { item ->
        GlassCard(hazeState, modifier = Modifier.fillMaxWidth().clickable { selectedEvent = item }) {
            Row(Modifier.padding(15.dp), verticalAlignment = Alignment.CenterVertically) {
                Icon(Icons.Default.CalendarMonth, item.title, tint = MaterialTheme.colorScheme.primary)
                Spacer(Modifier.width(12.dp))
                Column {
                    Text(item.title, fontWeight = FontWeight.SemiBold)
                    Text("${formatMoment(item.startTime)} 至 ${formatMoment(item.endTime)}", color = MaterialTheme.colorScheme.onSurfaceVariant)
                    item.location?.let { Text(it, color = MaterialTheme.colorScheme.onSurfaceVariant, style = MaterialTheme.typography.bodySmall) }
                }
            }
        }
    }
    if (showCreate) CalendarDialog(repository, { showCreate = false }, { showCreate = false; load() })
    selectedEvent?.let { event ->
        CalendarEventDetailDialog(event = event, onDismiss = { selectedEvent = null })
    }
}

@Composable
private fun CalendarEventDetailDialog(event: CalendarEvent, onDismiss: () -> Unit) {
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text(event.title) },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(10.dp)) {
                DetailLine("开始", formatMoment(event.startTime))
                DetailLine("结束", formatMoment(event.endTime))
                event.location?.takeIf { it.isNotBlank() }?.let { DetailLine("地点", it) }
                event.description?.takeIf { it.isNotBlank() }?.let { DetailLine("说明", it) }
                if (event.attendees.isNotEmpty()) {
                    DetailLine("参与人", event.attendees.joinToString("、") { it.userName ?: "成员" })
                }
            }
        },
        confirmButton = { TextButton(onClick = onDismiss) { Text("关闭") } }
    )
}

@Composable
private fun DetailLine(label: String, value: String) {
    Column {
        Text(label, style = MaterialTheme.typography.labelMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
        Text(value, style = MaterialTheme.typography.bodyLarge)
    }
}

@Composable
private fun CalendarDialog(repository: FangRepository, onDismiss: () -> Unit, onCreated: () -> Unit) {
    var title by remember { mutableStateOf("") }
    var start by remember { mutableStateOf(OffsetDateTime.now().plusHours(1).withMinute(0).withSecond(0).withNano(0)) }
    var end by remember { mutableStateOf(OffsetDateTime.now().plusHours(2).withMinute(0).withSecond(0).withNano(0)) }
    var location by remember { mutableStateOf("") }
    var description by remember { mutableStateOf("") }
    var error by remember { mutableStateOf<String?>(null) }
    val scope = rememberCoroutineScope()
    val context = LocalContext.current
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("新建日程") },
        text = {
            Column {
                OutlinedTextField(title, { title = it }, label = { Text("标题") }, modifier = Modifier.fillMaxWidth())
                Spacer(Modifier.height(8.dp))
                OutlinedButton(
                    onClick = { showCalendarDateTimePicker(context, start) { start = it; if (!end.isAfter(it)) end = it.plusHours(1) } },
                    modifier = Modifier.fillMaxWidth()
                ) { Text("开始时间：${formatMoment(start.toString())}") }
                Spacer(Modifier.height(6.dp))
                OutlinedButton(
                    onClick = { showCalendarDateTimePicker(context, end) { end = it } },
                    modifier = Modifier.fillMaxWidth()
                ) { Text("结束时间：${formatMoment(end.toString())}") }
                OutlinedTextField(location, { location = it }, label = { Text("地点") }, modifier = Modifier.fillMaxWidth())
                OutlinedTextField(description, { description = it }, label = { Text("说明（可选）") }, modifier = Modifier.fillMaxWidth(), minLines = 2)
                error?.let { Text(it, color = MaterialTheme.colorScheme.error) }
            }
        },
        confirmButton = {
            Button(
                onClick = {
                    if (!end.isAfter(start)) {
                        error = "结束时间必须晚于开始时间"
                    } else {
                        scope.launch {
                            runCatching {
                                repository.createCalendarEvent(
                                    CalendarEventRequest(title, start.toString(), end.toString(), location.ifBlank { null }, description.ifBlank { null })
                                )
                            }.onSuccess { onCreated() }.onFailure { error = it.message }
                        }
                    }
                },
                enabled = title.isNotBlank()
            ) { Text("创建") }
        },
        dismissButton = { TextButton(onClick = onDismiss) { Text("取消") } }
    )
}

private fun showCalendarDateTimePicker(context: Context, initial: OffsetDateTime, onSelected: (OffsetDateTime) -> Unit) {
    val local = initial.atZoneSameInstant(ZoneId.systemDefault()).toLocalDateTime()
    DatePickerDialog(context, { _, year, month, day ->
        TimePickerDialog(context, { _, hour, minute ->
            onSelected(LocalDateTime.of(year, month + 1, day, hour, minute).atZone(ZoneId.systemDefault()).toOffsetDateTime())
        }, local.hour, local.minute, true).show()
    }, local.year, local.monthValue - 1, local.dayOfMonth).show()
}

@Composable
private fun ApprovalsFeature(repository: FangRepository, hazeState: HazeState) {
    var items by remember { mutableStateOf<List<Approval>>(emptyList()) }
    var loading by remember { mutableStateOf(true) }
    var error by remember { mutableStateOf<String?>(null) }
    var create by remember { mutableStateOf(false) }
    var selectedApproval by remember { mutableStateOf<Approval?>(null) }
    var canProcessApprovals by remember { mutableStateOf(false) }
    val scope = rememberCoroutineScope()
    fun load() = scope.launch { loading = true; runCatching { repository.approvals() }.onSuccess { items = it }.onFailure { error = it.message }; loading = false }
    LaunchedEffect(Unit) {
        load()
        runCatching { repository.me() }
            .onSuccess { user -> canProcessApprovals = user.roles.any { it.equals("Admin", ignoreCase = true) } }
    }
    FeatureList(hazeState, items, loading, error, { load() }, { create = true }) { item ->
        GlassCard(hazeState, modifier = Modifier.fillMaxWidth().clickable { selectedApproval = item }) {
            Column(Modifier.padding(15.dp)) {
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Text(item.title, fontWeight = FontWeight.SemiBold, modifier = Modifier.weight(1f))
                    StatusChip(item.status)
                }
                Text(item.content, maxLines = 2, overflow = TextOverflow.Ellipsis, color = MaterialTheme.colorScheme.onSurfaceVariant)
                Text("${item.type} · ${item.applicantName ?: "我"}", style = MaterialTheme.typography.labelSmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
            }
        }
    }
    if (create) ApprovalDialog(repository, { create = false }, { create = false; load() })
    selectedApproval?.let { approval ->
        ApprovalDetailDialog(
            repository = repository,
            approval = approval,
            canProcess = canProcessApprovals,
            onDismiss = { selectedApproval = null },
            onUpdated = { selectedApproval = null; load() }
        )
    }
}

@Composable
private fun ApprovalDetailDialog(
    repository: FangRepository,
    approval: Approval,
    canProcess: Boolean,
    onDismiss: () -> Unit,
    onUpdated: () -> Unit
) {
    var comment by remember(approval.id) { mutableStateOf("") }
    var error by remember(approval.id) { mutableStateOf<String?>(null) }
    var processing by remember(approval.id) { mutableStateOf(false) }
    val scope = rememberCoroutineScope()
    val pending = approval.status.equals("Pending", ignoreCase = true)

    fun submit(approved: Boolean) {
        processing = true
        scope.launch {
            runCatching {
                if (approved) repository.approve(approval.id, comment.ifBlank { null })
                else repository.reject(approval.id, comment.ifBlank { null })
            }.onSuccess { onUpdated() }
                .onFailure { error = it.message; processing = false }
        }
    }

    AlertDialog(
        onDismissRequest = { if (!processing) onDismiss() },
        title = { Text(approval.title) },
        text = {
            Column(verticalArrangement = Arrangement.spacedBy(10.dp)) {
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Text(approval.type, modifier = Modifier.weight(1f), color = MaterialTheme.colorScheme.onSurfaceVariant)
                    StatusChip(approval.status)
                }
                DetailLine("申请人", approval.applicantName ?: "未知")
                approval.createdAt?.let { DetailLine("提交时间", formatMoment(it)) }
                DetailLine("申请内容", approval.content)
                if (approval.records.isNotEmpty()) {
                    Text("审批记录", style = MaterialTheme.typography.labelLarge)
                    approval.records.forEach { record ->
                        Text(
                            "${record.approverName ?: "审批人"} · ${record.action}${record.comment?.takeIf { it.isNotBlank() }?.let { "：$it" }.orEmpty()}",
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }
                }
                if (pending && canProcess) {
                    OutlinedTextField(
                        value = comment,
                        onValueChange = { comment = it },
                        modifier = Modifier.fillMaxWidth(),
                        label = { Text("审批备注（可选）") },
                        minLines = 2
                    )
                }
                error?.let { Text(it, color = MaterialTheme.colorScheme.error, style = MaterialTheme.typography.bodySmall) }
            }
        },
        confirmButton = {
            if (pending && canProcess) {
                Button(onClick = { submit(true) }, enabled = !processing) { Text(if (processing) "处理中" else "通过") }
            } else {
                TextButton(onClick = onDismiss) { Text("关闭") }
            }
        },
        dismissButton = {
            if (pending && canProcess) {
                TextButton(onClick = { submit(false) }, enabled = !processing) { Text("驳回") }
            }
        }
    )
}

@Composable
private fun ApprovalDialog(repository: FangRepository, onDismiss: () -> Unit, onCreated: () -> Unit) {
    var type by remember { mutableStateOf("Leave") }
    var title by remember { mutableStateOf("") }
    var content by remember { mutableStateOf("") }
    var error by remember { mutableStateOf<String?>(null) }
    val scope = rememberCoroutineScope()
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("发起审批") },
        text = { Column {
            OutlinedTextField(type, { type = it }, label = { Text("审批类型") }, modifier = Modifier.fillMaxWidth())
            OutlinedTextField(title, { title = it }, label = { Text("标题") }, modifier = Modifier.fillMaxWidth())
            OutlinedTextField(content, { content = it }, label = { Text("申请内容") }, modifier = Modifier.fillMaxWidth(), minLines = 3)
            error?.let { Text(it, color = MaterialTheme.colorScheme.error) }
        } },
        confirmButton = { Button(onClick = { scope.launch { runCatching { repository.createApproval(ApprovalRequest(type, title, content)) }.onSuccess { onCreated() }.onFailure { error = it.message } } }, enabled = title.isNotBlank() && content.isNotBlank()) { Text("提交") } },
        dismissButton = { TextButton(onClick = onDismiss) { Text("取消") } }
    )
}

@Composable
private fun TasksFeature(repository: FangRepository, hazeState: HazeState) {
    var items by remember { mutableStateOf<List<WorkTask>>(emptyList()) }
    var loading by remember { mutableStateOf(true) }
    var error by remember { mutableStateOf<String?>(null) }
    var create by remember { mutableStateOf(false) }
    val scope = rememberCoroutineScope()
    fun load() = scope.launch { loading = true; runCatching { repository.tasks() }.onSuccess { items = it }.onFailure { error = it.message }; loading = false }
    LaunchedEffect(Unit) { load() }
    FeatureList(hazeState, items, loading, error, { load() }, { create = true }) { item ->
        GlassCard(hazeState, modifier = Modifier.fillMaxWidth().clickable {
            if (!item.status.equals("Completed", true)) scope.launch { runCatching { repository.updateTaskStatus(item.id, "Completed") }.onSuccess { load() }.onFailure { error = it.message } }
        }) {
            Row(Modifier.padding(15.dp), verticalAlignment = Alignment.CenterVertically) {
                Icon(if (item.status.equals("Completed", true)) Icons.Default.CheckCircle else Icons.Default.Assignment, item.title, tint = MaterialTheme.colorScheme.primary)
                Spacer(Modifier.width(12.dp))
                Column(Modifier.weight(1f)) {
                    Text(item.title, fontWeight = FontWeight.SemiBold)
                    item.description?.let { Text(it, maxLines = 1, overflow = TextOverflow.Ellipsis, color = MaterialTheme.colorScheme.onSurfaceVariant) }
                    item.dueAt?.let { Text("截止 ${formatMoment(it)}", style = MaterialTheme.typography.labelSmall, color = MaterialTheme.colorScheme.onSurfaceVariant) }
                }
                StatusChip(item.status)
            }
        }
    }
    if (create) TaskDialog(repository, { create = false }, { create = false; load() })
}

@Composable
private fun TaskDialog(repository: FangRepository, onDismiss: () -> Unit, onCreated: () -> Unit) {
    var title by remember { mutableStateOf("") }
    var description by remember { mutableStateOf("") }
    var error by remember { mutableStateOf<String?>(null) }
    val scope = rememberCoroutineScope()
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("新建任务") },
        text = { Column {
            OutlinedTextField(title, { title = it }, label = { Text("任务标题") }, modifier = Modifier.fillMaxWidth())
            OutlinedTextField(description, { description = it }, label = { Text("任务描述") }, modifier = Modifier.fillMaxWidth(), minLines = 3)
            error?.let { Text(it, color = MaterialTheme.colorScheme.error) }
        } },
        confirmButton = { Button(onClick = { scope.launch { runCatching { repository.createTask(TaskRequest(title, description.ifBlank { null })) }.onSuccess { onCreated() }.onFailure { error = it.message } } }, enabled = title.isNotBlank()) { Text("创建") } },
        dismissButton = { TextButton(onClick = onDismiss) { Text("取消") } }
    )
}

@Composable
private fun FilesFeature(repository: FangRepository, hazeState: HazeState) {
    val context = LocalContext.current
    var items by remember { mutableStateOf<List<StoredFile>>(emptyList()) }
    var loading by remember { mutableStateOf(true) }
    var error by remember { mutableStateOf<String?>(null) }
    var showingTrash by remember { mutableStateOf(false) }
    var pendingDelete by remember { mutableStateOf<StoredFile?>(null) }
    var deleting by remember { mutableStateOf(false) }
    var openingId by remember { mutableStateOf<String?>(null) }
    val scope = rememberCoroutineScope()
    fun load() = scope.launch {
        loading = true
        error = null
        runCatching { if (showingTrash) repository.trashedFiles() else repository.files() }
            .onSuccess { items = it }
            .onFailure { error = it.message }
        loading = false
    }
    val picker = rememberLauncherForActivityResult(ActivityResultContracts.GetContent()) { uri ->
        if (uri != null) scope.launch {
            runCatching {
                val file = copyUriToCache(context, uri.toString())
                repository.uploadFile(file, context.contentResolver.getType(uri) ?: "application/octet-stream")
            }.onSuccess { load() }.onFailure { error = it.message }
        }
    }
    LaunchedEffect(showingTrash) { load() }
    Column(Modifier.fillMaxSize()) {
        Row(Modifier.fillMaxWidth().padding(horizontal = 10.dp, vertical = 4.dp)) {
            TextButton(
                onClick = { showingTrash = false },
                colors = ButtonDefaults.textButtonColors(
                    contentColor = if (!showingTrash) MaterialTheme.colorScheme.primary else MaterialTheme.colorScheme.onSurfaceVariant
                )
            ) { Text("文件") }
            TextButton(
                onClick = { showingTrash = true },
                colors = ButtonDefaults.textButtonColors(
                    contentColor = if (showingTrash) MaterialTheme.colorScheme.primary else MaterialTheme.colorScheme.onSurfaceVariant
                )
            ) { Text("回收站") }
        }
        Box(Modifier.weight(1f)) {
            FeatureList(
                hazeState = hazeState,
                items = items,
                loading = loading,
                error = error,
                onRefresh = { load() },
                onCreate = if (showingTrash) null else ({ picker.launch("*/*") }),
                fabIcon = Icons.Default.CloudUpload,
                emptyTitle = if (showingTrash) "回收站为空" else "暂无文件",
                emptyDescription = if (showingTrash) "删除的文件会显示在这里，可随时恢复" else "点击右下角按钮上传第一个文件"
            ) { item ->
                GlassCard(
                    hazeState,
                    modifier = Modifier.fillMaxWidth().clickable(enabled = !showingTrash && openingId == null) {
                        openingId = item.id
                        scope.launch {
                            runCatching {
                                val safeName = item.fileName.replace(Regex("[\\\\/:*?\"<>|]"), "_")
                                val target = File(context.cacheDir, "shared_files/$safeName")
                                repository.downloadFile(item.id, target)
                                openLocalFile(context, target, item.contentType)
                            }.onFailure {
                                error = it.message ?: "文件打开失败"
                            }
                            openingId = null
                        }
                    }
                ) {
                    Row(Modifier.padding(15.dp), verticalAlignment = Alignment.CenterVertically) {
                        Icon(Icons.Default.CloudUpload, item.fileName, tint = MaterialTheme.colorScheme.primary)
                        Spacer(Modifier.width(12.dp))
                        Column(Modifier.weight(1f)) {
                            Text(item.fileName, fontWeight = FontWeight.SemiBold, maxLines = 1, overflow = TextOverflow.Ellipsis)
                            Text(
                                if (openingId == item.id) "正在打开…" else "${formatSize(item.fileSize)} · ${item.uploaderName ?: "我"}",
                                color = MaterialTheme.colorScheme.onSurfaceVariant,
                                style = MaterialTheme.typography.bodySmall
                            )
                        }
                        if (!showingTrash) Icon(Icons.Default.OpenInNew, "查看文件", tint = MaterialTheme.colorScheme.onSurfaceVariant)
                        if (showingTrash) {
                            IconButton(onClick = {
                                scope.launch {
                                    runCatching { repository.restoreFile(item.id) }
                                        .onSuccess { load() }
                                        .onFailure { error = it.message }
                                }
                            }) { Icon(Icons.Default.Done, "恢复文件", tint = MaterialTheme.colorScheme.primary) }
                        }
                        IconButton(onClick = { pendingDelete = item }) {
                            Icon(Icons.Default.Delete, if (showingTrash) "永久删除" else "移入回收站", tint = MaterialTheme.colorScheme.error)
                        }
                    }
                }
            }
        }
    }
    pendingDelete?.let { file ->
        AlertDialog(
            onDismissRequest = { if (!deleting) pendingDelete = null },
            title = { Text(if (showingTrash) "永久删除文件" else "移入回收站") },
            text = { Text(if (showingTrash) "“${file.fileName}”将被永久删除，无法恢复。" else "“${file.fileName}”将移入回收站，可在回收站中恢复。") },
            confirmButton = {
                Button(onClick = {
                    deleting = true
                    scope.launch {
                        runCatching {
                            if (showingTrash) repository.permanentlyDeleteFile(file.id) else repository.moveFileToTrash(file.id)
                        }.onSuccess { pendingDelete = null; deleting = false; load() }
                            .onFailure { error = it.message; deleting = false }
                    }
                }, enabled = !deleting) { Text(if (deleting) "处理中" else "确认") }
            },
            dismissButton = { TextButton(onClick = { pendingDelete = null }, enabled = !deleting) { Text("取消") } }
        )
    }
}

@Composable
private fun ContactsFeature(repository: FangRepository, hazeState: HazeState) {
    var items by remember { mutableStateOf<List<User>>(emptyList()) }
    var loading by remember { mutableStateOf(true) }
    var error by remember { mutableStateOf<String?>(null) }
    val scope = rememberCoroutineScope()
    fun load() = scope.launch { loading = true; runCatching { repository.contacts() }.onSuccess { items = it }.onFailure { error = it.message }; loading = false }
    LaunchedEffect(Unit) { load() }
    FeatureList(hazeState, items, loading, error, { load() }, null) { item ->
        GlassCard(hazeState, modifier = Modifier.fillMaxWidth()) {
            Row(Modifier.padding(15.dp), verticalAlignment = Alignment.CenterVertically) {
                Surface(shape = androidx.compose.foundation.shape.CircleShape, color = MaterialTheme.colorScheme.primary.copy(alpha = .15f)) {
                    Icon(Icons.Default.Person, item.realName, modifier = Modifier.padding(9.dp), tint = MaterialTheme.colorScheme.primary)
                }
                Spacer(Modifier.width(12.dp))
                Column {
                    Text(item.realName, fontWeight = FontWeight.SemiBold)
                    Text(listOfNotNull(item.departmentName, item.position).joinToString(" · ").ifBlank { item.username }, color = MaterialTheme.colorScheme.onSurfaceVariant)
                }
            }
        }
    }
}

@Composable
private fun ContactsFeatureV2(repository: FangRepository, hazeState: HazeState, onOpenConversation: (String) -> Unit) {
    var friends by remember { mutableStateOf<List<User>>(emptyList()) }
    var requests by remember { mutableStateOf<List<FriendRequest>>(emptyList()) }
    var loading by remember { mutableStateOf(true) }
    var error by remember { mutableStateOf<String?>(null) }
    var showRequests by remember { mutableStateOf(false) }
    var showDiscover by remember { mutableStateOf(false) }
    var processingId by remember { mutableStateOf<String?>(null) }
    val scope = rememberCoroutineScope()

    fun load() = scope.launch {
        loading = true
        error = null
        runCatching {
            val loadedFriends = repository.contacts()
            val loadedRequests = repository.friendRequests()
            loadedFriends to loadedRequests
        }.onSuccess { (loadedFriends, loadedRequests) ->
            friends = loadedFriends
            requests = loadedRequests
        }.onFailure { error = it.message }
        loading = false
    }

    LaunchedEffect(Unit) { load() }
    val incomingCount = requests.count { it.direction.equals("Incoming", true) }
    val shownRequests = if (showRequests) requests else emptyList()

    Box(Modifier.fillMaxSize()) {
        Column(Modifier.fillMaxSize()) {
            Row(
                modifier = Modifier.fillMaxWidth().padding(horizontal = 16.dp, vertical = 10.dp),
                horizontalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                TextButton(onClick = { showRequests = false }) { Text("好友 ${friends.size}") }
                TextButton(onClick = { showRequests = true }) { Text(if (incomingCount > 0) "好友申请 $incomingCount" else "好友申请") }
            }
            when {
                loading -> LoadingBlock()
                error != null -> ErrorBlock(error.orEmpty()) { load() }
                !showRequests && friends.isEmpty() -> EmptyBlock("暂无好友", "点击右下角添加好友，好友同意后才会显示在这里")
                showRequests && shownRequests.isEmpty() -> EmptyBlock("暂无好友申请", "收到申请后可在这里同意或拒绝")
                else -> LazyColumn(
                    modifier = Modifier.fillMaxSize(),
                    contentPadding = androidx.compose.foundation.layout.PaddingValues(16.dp, 6.dp, 16.dp, 96.dp),
                    verticalArrangement = Arrangement.spacedBy(10.dp)
                ) {
                    if (showRequests) {
                        items(shownRequests, key = { it.id }) { request ->
                            GlassCard(hazeState, modifier = Modifier.fillMaxWidth()) {
                                Row(Modifier.padding(14.dp), verticalAlignment = Alignment.CenterVertically) {
                                    UserAvatar(request.user.avatarUrl, request.user.realName, Modifier.size(42.dp))
                                    Spacer(Modifier.width(10.dp))
                                    Column(Modifier.weight(1f)) {
                                        Text(request.user.realName, fontWeight = FontWeight.SemiBold)
                                        Text(
                                            request.greeting?.takeIf { it.isNotBlank() }
                                                ?: if (request.direction.equals("Incoming", true)) "请求添加你为好友" else "等待对方同意",
                                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                                            style = MaterialTheme.typography.bodySmall
                                        )
                                    }
                                    if (request.direction.equals("Incoming", true)) {
                                        Column(horizontalAlignment = Alignment.End) {
                                            Button(
                                                enabled = processingId != request.id,
                                                onClick = {
                                                    processingId = request.id
                                                    scope.launch {
                                                        runCatching { repository.acceptFriendRequest(request.id) }
                                                            .onSuccess { load() }
                                                            .onFailure { error = it.message; processingId = null }
                                                    }
                                                }
                                            ) { Text("同意") }
                                            TextButton(
                                                enabled = processingId != request.id,
                                                onClick = {
                                                    processingId = request.id
                                                    scope.launch {
                                                        runCatching { repository.rejectFriendRequest(request.id) }
                                                            .onSuccess { load() }
                                                            .onFailure { error = it.message; processingId = null }
                                                    }
                                                }
                                            ) { Text("拒绝") }
                                        }
                                    } else {
                                        StatusChip("等待同意")
                                    }
                                }
                            }
                        }
                    } else {
                        items(friends, key = { it.id }) { friend ->
                            GlassCard(hazeState, modifier = Modifier.fillMaxWidth()) {
                                Row(Modifier.padding(14.dp), verticalAlignment = Alignment.CenterVertically) {
                                    UserAvatar(friend.avatarUrl, friend.realName, Modifier.size(42.dp))
                                    Spacer(Modifier.width(10.dp))
                                    Column(Modifier.weight(1f)) {
                                        Text(friend.realName, fontWeight = FontWeight.SemiBold)
                                        Text(
                                            listOfNotNull(friend.departmentName, friend.position).joinToString(" · ").ifBlank { friend.username },
                                            color = MaterialTheme.colorScheme.onSurfaceVariant
                                        )
                                    }
                                    IconButton(
                                        onClick = {
                                            processingId = friend.id
                                            scope.launch {
                                                runCatching { repository.openOrCreateSingle(friend.id) }
                                                    .onSuccess { onOpenConversation(it.id) }
                                                    .onFailure { error = it.message; processingId = null }
                                            }
                                        },
                                        enabled = processingId != friend.id
                                    ) { Icon(Icons.Default.Chat, "发消息") }
                                    IconButton(
                                        onClick = {
                                            processingId = friend.id
                                            scope.launch {
                                                runCatching { repository.removeFriend(friend.id) }
                                                    .onSuccess { load() }
                                                    .onFailure { error = it.message; processingId = null }
                                            }
                                        },
                                        enabled = processingId != friend.id
                                    ) { Icon(Icons.Default.Delete, "删除好友") }
                                }
                            }
                        }
                    }
                }
            }
        }
        FloatingActionButton(
            onClick = { showDiscover = true },
            modifier = Modifier.align(Alignment.BottomEnd).padding(22.dp)
        ) { Icon(Icons.Default.Add, "添加好友") }
    }
    if (showDiscover) {
        ContactDiscoveryDialog(
            repository = repository,
            onDismiss = { showDiscover = false },
            onRequested = { load() }
        )
    }
}

@Composable
private fun ContactDiscoveryDialog(repository: FangRepository, onDismiss: () -> Unit, onRequested: () -> Unit) {
    var keyword by remember { mutableStateOf("") }
    var users by remember { mutableStateOf<List<User>>(emptyList()) }
    var loading by remember { mutableStateOf(true) }
    var requestingId by remember { mutableStateOf<String?>(null) }
    var error by remember { mutableStateOf<String?>(null) }
    var successMessage by remember { mutableStateOf<String?>(null) }
    val scope = rememberCoroutineScope()

    LaunchedEffect(keyword) {
        loading = true
        error = null
        runCatching { repository.discoverContacts(keyword.ifBlank { null }) }
            .onSuccess { users = it }
            .onFailure { error = it.message }
        loading = false
    }

    AlertDialog(
        onDismissRequest = { if (requestingId == null) onDismiss() },
        title = { Text("添加好友") },
        text = {
            Column(Modifier.fillMaxWidth()) {
                OutlinedTextField(
                    value = keyword,
                    onValueChange = { keyword = it },
                    modifier = Modifier.fillMaxWidth(),
                    singleLine = true,
                    label = { Text("搜索姓名或用户名") }
                )
                Spacer(Modifier.height(8.dp))
                when {
                    loading -> Box(Modifier.fillMaxWidth().padding(18.dp), contentAlignment = Alignment.Center) { CircularProgressIndicator() }
                    users.isEmpty() -> Text("没有可添加的用户", color = MaterialTheme.colorScheme.onSurfaceVariant)
                    else -> LazyColumn(Modifier.fillMaxWidth().height(230.dp)) {
                        items(users, key = { it.id }) { user ->
                            Row(
                                modifier = Modifier.fillMaxWidth().padding(vertical = 8.dp),
                                verticalAlignment = Alignment.CenterVertically
                            ) {
                                UserAvatar(user.avatarUrl, user.realName, Modifier.size(38.dp))
                                Spacer(Modifier.width(10.dp))
                                Column(Modifier.weight(1f)) {
                                    Text(user.realName, fontWeight = FontWeight.SemiBold)
                                    Text(user.username, style = MaterialTheme.typography.bodySmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
                                }
                                TextButton(
                                    enabled = requestingId == null,
                                    onClick = {
                                        requestingId = user.id
                                        scope.launch {
                                            runCatching { repository.sendFriendRequest(user.id) }
                                                .onSuccess {
                                                    successMessage = "已向 ${user.realName} 发送好友申请，请等待对方同意"
                                                    users = users.filterNot { it.id == user.id }
                                                    onRequested()
                                                    requestingId = null
                                                }
                                                .onFailure { error = it.message; requestingId = null }
                                        }
                                    }
                                ) { Text(if (requestingId == user.id) "发送中" else "申请") }
                            }
                        }
                    }
                }
                error?.let { Text(it, color = MaterialTheme.colorScheme.error, style = MaterialTheme.typography.bodySmall) }
                successMessage?.let { Text(it, color = MaterialTheme.colorScheme.primary, style = MaterialTheme.typography.bodySmall) }
            }
        },
        confirmButton = { TextButton(onClick = onDismiss, enabled = requestingId == null) { Text("完成") } }
    )
}

@Composable
private fun WikiFeature(repository: FangRepository, hazeState: HazeState) {
    val contextForToast = LocalContext.current
    var items by remember { mutableStateOf<List<WikiSpace>>(emptyList()) }
    var loading by remember { mutableStateOf(true) }
    var error by remember { mutableStateOf<String?>(null) }
    var create by remember { mutableStateOf(false) }
    var selectedSpaceId by remember { mutableStateOf<String?>(null) }
    var selectedSpace by remember { mutableStateOf<WikiSpaceDetail?>(null) }
    var selectedDocumentId by remember { mutableStateOf<String?>(null) }
    val scope = rememberCoroutineScope()
    fun load() = scope.launch { loading = true; runCatching { repository.wikiSpaces() }.onSuccess { items = it }.onFailure { error = it.message }; loading = false }
    LaunchedEffect(Unit) { load() }
    LaunchedEffect(selectedSpaceId) {
        val id = selectedSpaceId ?: return@LaunchedEffect
        loading = true
        error = null
        runCatching { repository.wikiSpace(id) }
            .onSuccess { selectedSpace = it }
            .onFailure { error = it.message }
        loading = false
    }
    BackHandler(enabled = selectedSpaceId != null) {
        selectedSpaceId = null
        selectedSpace = null
    }
    if (selectedSpaceId != null) {
        Column(Modifier.fillMaxSize()) {
            Row(Modifier.fillMaxWidth().padding(horizontal = 8.dp, vertical = 6.dp), verticalAlignment = Alignment.CenterVertically) {
                IconButton(onClick = { selectedSpaceId = null; selectedSpace = null }) { Icon(Icons.Default.ArrowBack, "返回知识空间") }
                Column {
                    Text(selectedSpace?.space?.name ?: "知识空间", fontWeight = FontWeight.SemiBold)
                    selectedSpace?.space?.description?.let { Text(it, color = MaterialTheme.colorScheme.onSurfaceVariant, style = MaterialTheme.typography.bodySmall) }
                }
            }
            when {
                loading -> LoadingBlock()
                error != null -> ErrorBlock(error.orEmpty()) {
                    selectedSpaceId?.let { id ->
                        scope.launch {
                            loading = true
                            runCatching { repository.wikiSpace(id) }
                                .onSuccess { selectedSpace = it; error = null }
                                .onFailure { error = it.message }
                            loading = false
                        }
                    }
                }
                selectedSpace?.nodes.isNullOrEmpty() -> EmptyBlock("暂无知识条目", "该知识空间还没有关联文档")
                else -> LazyColumn(
                    modifier = Modifier.fillMaxSize(),
                    contentPadding = androidx.compose.foundation.layout.PaddingValues(16.dp, 8.dp, 16.dp, 96.dp),
                    verticalArrangement = Arrangement.spacedBy(10.dp)
                ) {
                    items(selectedSpace!!.nodes, key = { it.id }) { node ->
                        GlassCard(
                            hazeState,
                            modifier = Modifier.fillMaxWidth().clickable {
                                if (node.documentId == null) {
                                    Toast.makeText(contextForToast, "该条目暂未关联文档", Toast.LENGTH_SHORT).show()
                                } else {
                                    selectedDocumentId = node.documentId
                                }
                            }
                        ) {
                            Row(Modifier.padding(15.dp), verticalAlignment = Alignment.CenterVertically) {
                                Icon(Icons.Default.MenuBook, node.title, tint = MaterialTheme.colorScheme.primary)
                                Spacer(Modifier.width(12.dp))
                                Column(Modifier.weight(1f)) {
                                    Text(node.title, fontWeight = FontWeight.SemiBold)
                                    Text(if (node.documentId == null) "未关联文档" else "点击查看正文", color = MaterialTheme.colorScheme.onSurfaceVariant, style = MaterialTheme.typography.bodySmall)
                                }
                                if (node.documentId != null) Icon(Icons.Default.OpenInNew, "查看")
                            }
                        }
                    }
                }
            }
        }
        selectedDocumentId?.let { id -> WikiDocumentDialog(repository, id) { selectedDocumentId = null } }
        return
    }
    FeatureList(hazeState, items, loading, error, { load() }, { create = true }) { item ->
        GlassCard(hazeState, modifier = Modifier.fillMaxWidth().clickable { selectedSpaceId = item.id }) {
            Row(Modifier.padding(15.dp), verticalAlignment = Alignment.CenterVertically) {
                Icon(Icons.Default.MenuBook, item.name, tint = MaterialTheme.colorScheme.primary)
                Spacer(Modifier.width(12.dp))
                Column {
                    Text(item.name, fontWeight = FontWeight.SemiBold)
                    Text(item.description ?: "知识空间", color = MaterialTheme.colorScheme.onSurfaceVariant, maxLines = 1, overflow = TextOverflow.Ellipsis)
                }
            }
        }
    }
    if (create) WikiDialog(repository, { create = false }, { create = false; load() })
}

@Composable
private fun WikiDocumentDialog(repository: FangRepository, documentId: String, onDismiss: () -> Unit) {
    var document by remember(documentId) { mutableStateOf<com.fangfeishu.android.data.Document?>(null) }
    var error by remember(documentId) { mutableStateOf<String?>(null) }
    LaunchedEffect(documentId) {
        runCatching { repository.document(documentId) }
            .onSuccess { document = it }
            .onFailure { error = it.message }
    }
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text(document?.title ?: "知识条目") },
        text = {
            when {
                error != null -> Text(error.orEmpty(), color = MaterialTheme.colorScheme.error)
                document == null -> Box(Modifier.fillMaxWidth().padding(24.dp), contentAlignment = Alignment.Center) { CircularProgressIndicator() }
                else -> LazyColumn(Modifier.fillMaxWidth().height(360.dp)) {
                    item { Text(document?.content.orEmpty().ifBlank { "暂无正文内容" }) }
                }
            }
        },
        confirmButton = { TextButton(onClick = onDismiss) { Text("关闭") } }
    )
}

@Composable
private fun WikiDialog(repository: FangRepository, onDismiss: () -> Unit, onCreated: () -> Unit) {
    var name by remember { mutableStateOf("") }
    var description by remember { mutableStateOf("") }
    var error by remember { mutableStateOf<String?>(null) }
    val scope = rememberCoroutineScope()
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("新建知识空间") },
        text = { Column {
            OutlinedTextField(name, { name = it }, label = { Text("空间名称") }, modifier = Modifier.fillMaxWidth())
            OutlinedTextField(description, { description = it }, label = { Text("空间描述") }, modifier = Modifier.fillMaxWidth())
            error?.let { Text(it, color = MaterialTheme.colorScheme.error) }
        } },
        confirmButton = { Button(onClick = { scope.launch { runCatching { repository.createWikiSpace(WikiSpaceRequest(name, description.ifBlank { null })) }.onSuccess { onCreated() }.onFailure { error = it.message } } }, enabled = name.isNotBlank()) { Text("创建") } },
        dismissButton = { TextButton(onClick = onDismiss) { Text("取消") } }
    )
}

@Composable
private fun NotificationsFeature(
    repository: FangRepository,
    hazeState: HazeState,
    onOpenMeeting: (Meeting) -> Unit
) {
    var items by remember { mutableStateOf<List<NotificationItem>>(emptyList()) }
    var loading by remember { mutableStateOf(true) }
    var error by remember { mutableStateOf<String?>(null) }
    var selectedNotification by remember { mutableStateOf<NotificationItem?>(null) }
    var markingAllRead by remember { mutableStateOf(false) }
    var joiningMeeting by remember { mutableStateOf(false) }
    val scope = rememberCoroutineScope()
    fun load() = scope.launch { loading = true; runCatching { repository.notifications() }.onSuccess { items = it }.onFailure { error = it.message }; loading = false }
    LaunchedEffect(Unit) { load() }
    LaunchedEffect(Unit) {
        while (isActive) {
            delay(5_000)
            runCatching { repository.notifications() }.onSuccess { items = it }
        }
    }
    Box(Modifier.fillMaxSize()) {
        when {
            loading -> LoadingBlock()
            error != null -> ErrorBlock(error.orEmpty()) { load() }
            items.isEmpty() -> EmptyBlock("暂无通知", "新的提醒会显示在这里")
            else -> LazyColumn(
                modifier = Modifier.fillMaxSize(),
                contentPadding = androidx.compose.foundation.layout.PaddingValues(16.dp, 8.dp, 16.dp, 94.dp),
                verticalArrangement = Arrangement.spacedBy(10.dp)
            ) {
                item {
                    Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
                        Text("未读 ${items.count { !it.isRead }} 条", modifier = Modifier.weight(1f), color = MaterialTheme.colorScheme.onSurfaceVariant)
                        TextButton(
                            enabled = items.any { !it.isRead } && !markingAllRead,
                            onClick = {
                                markingAllRead = true
                                scope.launch {
                                    runCatching { repository.markAllNotificationsRead() }
                                        .onSuccess { items = items.map { it.copy(isRead = true) }; markingAllRead = false }
                                        .onFailure { error = it.message; markingAllRead = false }
                                }
                            }
                        ) { Text(if (markingAllRead) "处理中" else "全部已读") }
                    }
                }
                items(items, key = { it.id }) { item ->
                    GlassCard(hazeState, modifier = Modifier.fillMaxWidth().clickable {
                        selectedNotification = item
                        if (!item.isRead) {
                            scope.launch {
                                runCatching { repository.markNotificationRead(item.id) }
                                    .onSuccess { items = items.map { current -> if (current.id == item.id) current.copy(isRead = true) else current } }
                                    .onFailure { error = it.message }
                            }
                        }
                    }) {
                        Column(Modifier.padding(15.dp)) {
                            Row { Text(item.title, fontWeight = FontWeight.SemiBold, modifier = Modifier.weight(1f)); if (!item.isRead) StatusChip("未读") }
                            Text(item.content, color = MaterialTheme.colorScheme.onSurfaceVariant)
                            Text(item.createdAt?.let(::formatMoment) ?: "刚刚", style = MaterialTheme.typography.labelSmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
                        }
                    }
                }
            }
        }
    }
    selectedNotification?.let { notification ->
        val meetingId = notification.resourceId?.takeIf {
            notification.type.equals("Meeting", true) || notification.resourceType.equals("Meeting", true)
        }
        AlertDialog(
            onDismissRequest = { if (!joiningMeeting) selectedNotification = null },
            title = { Text(notification.title) },
            text = {
                Column(verticalArrangement = Arrangement.spacedBy(10.dp)) {
                    Text(notification.content)
                    Text(notification.createdAt?.let(::formatMoment) ?: "刚刚", style = MaterialTheme.typography.labelSmall, color = MaterialTheme.colorScheme.onSurfaceVariant)
                    error?.let { Text(it, color = MaterialTheme.colorScheme.error, style = MaterialTheme.typography.bodySmall) }
                }
            },
            confirmButton = {
                if (meetingId != null) {
                    Button(
                        enabled = !joiningMeeting,
                        onClick = {
                            joiningMeeting = true
                            scope.launch {
                                runCatching { repository.meeting(meetingId) }
                                    .onSuccess {
                                        joiningMeeting = false
                                        selectedNotification = null
                                        onOpenMeeting(it)
                                    }
                                    .onFailure { error = it.message ?: "获取会议失败"; joiningMeeting = false }
                            }
                        }
                    ) { Text(if (joiningMeeting) "正在进入" else "加入视频会议") }
                } else {
                    TextButton(onClick = { selectedNotification = null }) { Text("关闭") }
                }
            },
            dismissButton = {
                if (meetingId != null) TextButton(onClick = { selectedNotification = null }, enabled = !joiningMeeting) { Text("稍后") }
            }
        )
    }
}

@Composable
fun <T> FeatureList(
    hazeState: HazeState,
    items: List<T>,
    loading: Boolean,
    error: String?,
    onRefresh: () -> Unit,
    onCreate: (() -> Unit)?,
    fabIcon: androidx.compose.ui.graphics.vector.ImageVector = Icons.Default.Add,
    emptyTitle: String = "暂无数据",
    emptyDescription: String = "使用右下角按钮创建第一条记录",
    row: @Composable (T) -> Unit
) {
    Box(Modifier.fillMaxSize()) {
        when {
            loading -> LoadingBlock()
            error != null -> ErrorBlock(error, onRefresh)
            items.isEmpty() -> EmptyBlock(emptyTitle, emptyDescription)
            else -> LazyColumn(
                modifier = Modifier.fillMaxSize(),
                contentPadding = androidx.compose.foundation.layout.PaddingValues(16.dp, 14.dp, 16.dp, 94.dp),
                verticalArrangement = Arrangement.spacedBy(10.dp)
            ) { items(items) { row(it) } }
        }
        if (onCreate != null) FloatingActionButton(onClick = onCreate, modifier = Modifier.align(Alignment.BottomEnd).padding(22.dp)) { Icon(fabIcon, "新建") }
    }
}

@Composable
fun StatusChip(value: String) {
    Surface(color = MaterialTheme.colorScheme.primary.copy(alpha = .14f), shape = androidx.compose.foundation.shape.RoundedCornerShape(8.dp)) {
        Text(value, modifier = Modifier.padding(horizontal = 8.dp, vertical = 3.dp), color = MaterialTheme.colorScheme.primary, style = MaterialTheme.typography.labelSmall)
    }
}

private fun copyUriToCache(context: Context, value: String): File {
    val uri = android.net.Uri.parse(value)
    val name = context.contentResolver.query(uri, null, null, null, null)?.use { cursor ->
        val column = cursor.getColumnIndex(android.provider.OpenableColumns.DISPLAY_NAME)
        if (column >= 0 && cursor.moveToFirst()) cursor.getString(column) else null
    } ?: "upload_${System.currentTimeMillis()}"
    return File(context.cacheDir, name).also { target ->
        context.contentResolver.openInputStream(uri)?.use { input -> target.outputStream().use { input.copyTo(it) } }
            ?: error("无法读取选择的文件")
    }
}

private fun openLocalFile(context: Context, file: File, contentType: String?) {
    val uri = FileProvider.getUriForFile(context, "${context.packageName}.fileprovider", file)
    val intent = Intent(Intent.ACTION_VIEW).apply {
        setDataAndType(uri, contentType?.takeIf { it.isNotBlank() } ?: "*/*")
        addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION or Intent.FLAG_ACTIVITY_NEW_TASK)
    }
    try {
        context.startActivity(intent)
    } catch (_: Exception) {
        Toast.makeText(context, "手机上没有可打开此文件类型的应用", Toast.LENGTH_LONG).show()
    }
}

private fun formatSize(bytes: Long): String = when {
    bytes < 1024 -> "$bytes B"
    bytes < 1024 * 1024 -> "${bytes / 1024} KB"
    else -> "${"%.1f".format(bytes / 1024f / 1024f)} MB"
}
