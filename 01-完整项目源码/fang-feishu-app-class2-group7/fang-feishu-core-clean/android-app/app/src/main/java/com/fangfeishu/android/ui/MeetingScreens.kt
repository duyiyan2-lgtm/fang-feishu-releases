package com.fangfeishu.android.ui

import android.Manifest
import android.content.pm.PackageManager
import android.os.Build
import android.view.View
import android.widget.FrameLayout
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.aspectRatio
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.lazy.grid.GridCells
import androidx.compose.foundation.lazy.grid.LazyVerticalGrid
import androidx.compose.foundation.lazy.grid.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.CallEnd
import androidx.compose.material.icons.filled.CameraAlt
import androidx.compose.material.icons.filled.Mic
import androidx.compose.material.icons.filled.MicOff
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.PersonAdd
import androidx.compose.material.icons.filled.VideoCall
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.Checkbox
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.core.content.ContextCompat
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.viewinterop.AndroidView
import com.fangfeishu.android.data.FangRepository
import com.fangfeishu.android.data.Meeting
import com.fangfeishu.android.data.MeetingJoinData
import com.fangfeishu.android.data.MeetingRequest
import com.fangfeishu.android.data.User
import com.fangfeishu.android.rtc.AgoraRtcManager
import com.fangfeishu.android.rtc.RemoteRtcParticipant
import dev.chrisbanes.haze.HazeState
import dev.chrisbanes.haze.blur.blurEffect
import dev.chrisbanes.haze.hazeEffect
import dev.chrisbanes.haze.rememberHazeState
import kotlinx.coroutines.launch
import kotlinx.coroutines.delay
import kotlinx.coroutines.isActive

@Composable
fun MeetingsFeature(repository: FangRepository, hazeState: HazeState, currentUser: User, onBack: () -> Unit) {
    var items by remember { mutableStateOf<List<Meeting>>(emptyList()) }
    var loading by remember { mutableStateOf(true) }
    var loadError by remember { mutableStateOf<String?>(null) }
    var actionError by remember { mutableStateOf<String?>(null) }
    var create by remember { mutableStateOf(false) }
    var pendingJoin by remember { mutableStateOf<Meeting?>(null) }
    var joinedMeeting by remember { mutableStateOf<MeetingJoinData?>(null) }
    val scope = rememberCoroutineScope()
    val requestedPermissions = remember {
        buildList {
            add(Manifest.permission.CAMERA)
            add(Manifest.permission.RECORD_AUDIO)
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) add(Manifest.permission.BLUETOOTH_CONNECT)
        }.toTypedArray()
    }
    fun load() = scope.launch {
        loading = true
        loadError = null
        runCatching { repository.meetings() }
            .onSuccess { items = it }
            .onFailure { loadError = it.message }
        loading = false
    }
    val permissionLauncher = rememberLauncherForActivityResult(ActivityResultContracts.RequestMultiplePermissions()) { grants ->
        val meeting = pendingJoin ?: return@rememberLauncherForActivityResult
        if (grants[Manifest.permission.CAMERA] == true && grants[Manifest.permission.RECORD_AUDIO] == true) {
            scope.launch {
                runCatching { repository.joinMeeting(meeting.id) }
                    .onSuccess { joinedMeeting = it }
                    .onFailure { actionError = it.message ?: "加入会议失败，请检查 RTC 服务配置" }
            }
        } else {
            actionError = "加入视频会议需要相机和麦克风权限"
        }
        pendingJoin = null
    }
    LaunchedEffect(Unit) { load() }
    LaunchedEffect(Unit) {
        while (isActive) {
            delay(3_000)
            runCatching { repository.meetings() }.onSuccess { items = it }
        }
    }

    joinedMeeting?.let { joinData ->
        MeetingCallScreen(
            repository = repository,
            joinData = joinData,
            initialCurrentUser = currentUser,
            onLeave = { joinedMeeting = null; load() }
        )
        return
    }

    val darkStyle = MaterialTheme.colorScheme.background.red < .2f
    AtmosphericSurface(darkStyle = darkStyle, hazeState = hazeState) {
        Scaffold(
            containerColor = Color.Transparent,
            topBar = {
                GlassTopBar(hazeState, "视频会议", navigation = {
                    IconButton(onClick = onBack) { Icon(Icons.Default.ArrowBack, "返回") }
                })
            }
        ) { padding ->
            Box(Modifier.fillMaxSize().padding(padding)) {
                FeatureList(hazeState, items, loading, loadError, { load() }, { create = true }) { item ->
                    GlassCard(hazeState, modifier = Modifier.fillMaxWidth().clickable {
                        pendingJoin = item
                        permissionLauncher.launch(requestedPermissions)
                    }) {
                        Row(Modifier.padding(15.dp), verticalAlignment = Alignment.CenterVertically) {
                            Surface(shape = RoundedCornerShape(12.dp), color = MaterialTheme.colorScheme.primary.copy(alpha = .15f)) {
                                Icon(Icons.Default.VideoCall, item.title, modifier = Modifier.padding(9.dp), tint = MaterialTheme.colorScheme.primary)
                            }
                            Spacer(Modifier.width(12.dp))
                            Column(Modifier.weight(1f)) {
                                Text(item.title, fontWeight = FontWeight.SemiBold)
                                Text(item.scheduledStartAt?.let(::formatMoment) ?: "即时会议", color = MaterialTheme.colorScheme.onSurfaceVariant)
                            }
                            StatusChip(item.status)
                        }
                    }
                }
            }
        }
    }
    if (create) MeetingDialog(repository, { create = false }, { create = false; load() })
    actionError?.let { message ->
        AlertDialog(
            onDismissRequest = { actionError = null },
            title = { Text("暂时无法加入会议") },
            text = { Text(message) },
            confirmButton = { TextButton(onClick = { actionError = null }) { Text("知道了") } }
        )
    }
}

/** Opens an existing meeting directly from a group chat after the creator has invited the group members. */
@Composable
fun MeetingJoinScreen(repository: FangRepository, meeting: Meeting, currentUser: User, onLeave: () -> Unit) {
    val context = LocalContext.current
    val scope = rememberCoroutineScope()
    var joining by remember(meeting.id) { mutableStateOf(true) }
    var joinData by remember(meeting.id) { mutableStateOf<MeetingJoinData?>(null) }
    var error by remember(meeting.id) { mutableStateOf<String?>(null) }
    val requestedPermissions = remember {
        buildList {
            add(Manifest.permission.CAMERA)
            add(Manifest.permission.RECORD_AUDIO)
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) add(Manifest.permission.BLUETOOTH_CONNECT)
        }.toTypedArray()
    }
    fun join() {
        joining = true
        scope.launch {
            runCatching { repository.joinMeeting(meeting.id) }
                .onSuccess { joinData = it; joining = false }
                .onFailure { error = it.message ?: "加入视频会议失败，请检查 RTC 服务配置"; joining = false }
        }
    }
    val permissionLauncher = rememberLauncherForActivityResult(ActivityResultContracts.RequestMultiplePermissions()) { grants ->
        if (grants[Manifest.permission.CAMERA] == true && grants[Manifest.permission.RECORD_AUDIO] == true) {
            join()
        } else {
            joining = false
            error = "加入视频会议需要相机和麦克风权限"
        }
    }
    LaunchedEffect(meeting.id) {
        val hasMediaPermissions = listOf(Manifest.permission.CAMERA, Manifest.permission.RECORD_AUDIO)
            .all { ContextCompat.checkSelfPermission(context, it) == PackageManager.PERMISSION_GRANTED }
        if (hasMediaPermissions) join() else permissionLauncher.launch(requestedPermissions)
    }
    joinData?.let { data ->
        MeetingCallScreen(
            repository = repository,
            joinData = data,
            initialCurrentUser = currentUser,
            onLeave = onLeave
        )
        return
    }
    AtmosphericSurface(darkStyle = false, hazeState = rememberHazeState()) {
        Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
            if (joining) {
                Column(horizontalAlignment = Alignment.CenterHorizontally) {
                    CircularProgressIndicator()
                    Spacer(Modifier.height(14.dp))
                    Text("正在进入 ${meeting.title}")
                }
            }
        }
    }
    error?.let { message ->
        AlertDialog(
            onDismissRequest = onLeave,
            title = { Text("暂时无法加入会议") },
            text = { Text(message) },
            confirmButton = { TextButton(onClick = onLeave) { Text("返回群聊") } }
        )
    }
}

@Composable
private fun MeetingDialog(repository: FangRepository, onDismiss: () -> Unit, onCreated: () -> Unit) {
    var title by remember { mutableStateOf("") }
    var roomName by remember { mutableStateOf("") }
    var contacts by remember { mutableStateOf<List<User>>(emptyList()) }
    var selectedMemberIds by remember { mutableStateOf<Set<String>>(emptySet()) }
    var error by remember { mutableStateOf<String?>(null) }
    val scope = rememberCoroutineScope()
    LaunchedEffect(Unit) {
        runCatching { repository.contacts() }
            .onSuccess { contacts = it }
            .onFailure { error = it.message }
    }
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("创建视频会议") },
        text = { Column {
            if (contacts.isNotEmpty()) {
                Text("邀请好友参会（可选）", style = MaterialTheme.typography.labelLarge)
                LazyColumn(Modifier.fillMaxWidth().height(128.dp)) {
                    items(contacts, key = { it.id }) { contact ->
                        Row(
                            modifier = Modifier.fillMaxWidth().clickable {
                                selectedMemberIds = if (contact.id in selectedMemberIds) selectedMemberIds - contact.id else selectedMemberIds + contact.id
                            }.padding(vertical = 2.dp),
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Checkbox(
                                checked = contact.id in selectedMemberIds,
                                onCheckedChange = { checked ->
                                    selectedMemberIds = if (checked) selectedMemberIds + contact.id else selectedMemberIds - contact.id
                                }
                            )
                            UserAvatar(contact.avatarUrl, contact.realName, Modifier.size(30.dp))
                            Spacer(Modifier.width(8.dp))
                            Text(contact.realName)
                        }
                    }
                }
            }
            OutlinedTextField(title, { title = it }, label = { Text("会议主题") }, modifier = Modifier.fillMaxWidth())
            OutlinedTextField(roomName, { roomName = it }, label = { Text("会议室名称") }, modifier = Modifier.fillMaxWidth())
            error?.let { Text(it, color = MaterialTheme.colorScheme.error) }
        } },
        confirmButton = { Button(onClick = { scope.launch { runCatching { repository.createMeeting(MeetingRequest(title, roomName.ifBlank { null }, null, selectedMemberIds.toList())) }.onSuccess { onCreated() }.onFailure { error = it.message } } }, enabled = title.isNotBlank()) { Text("创建") } },
        dismissButton = { TextButton(onClick = onDismiss) { Text("取消") } }
    )
}

@Composable
fun MeetingCallScreen(
    repository: FangRepository,
    joinData: MeetingJoinData,
    initialCurrentUser: User,
    onLeave: () -> Unit
) {
    val appContext = LocalContext.current.applicationContext
    val hazeState = rememberHazeState()
    val rtcManager = remember(appContext) { AgoraRtcManager(appContext) }
    val scope = rememberCoroutineScope()
    var localContainer by remember { mutableStateOf<FrameLayout?>(null) }
    val remoteParticipants = remember { mutableStateListOf<RemoteRtcParticipant>() }
    var muted by remember { mutableStateOf(false) }
    var cameraEnabled by remember { mutableStateOf(true) }
    var rtcError by remember { mutableStateOf<String?>(null) }
    var currentUser by remember(initialCurrentUser.id) { mutableStateOf<User?>(initialCurrentUser) }
    var meeting by remember(joinData.meeting.id) { mutableStateOf(joinData.meeting) }
    var showInviteMembers by remember { mutableStateOf(false) }
    var showEndMeetingConfirm by remember { mutableStateOf(false) }
    var endingMeeting by remember { mutableStateOf(false) }
    var meetingEnded by remember { mutableStateOf(false) }
    val participantDisplays = remember(meeting.members) {
        buildMeetingParticipantDisplays(meeting.members)
    }
    val currentMember = meeting.members.firstOrNull { it.userId == currentUser?.id }
    val localUsername = currentUser?.username?.takeIf { it.isNotBlank() }
        ?: currentMember?.username?.takeIf { it.isNotBlank() }
        ?: currentUser?.realName?.takeIf { it.isNotBlank() }
        ?: currentMember?.userName?.takeIf { it.isNotBlank() }
        ?: "我"
    val localAvatarUrl = currentUser?.avatarUrl?.takeIf { it.isNotBlank() }
        ?: currentMember?.avatarUrl?.takeIf { it.isNotBlank() }
    val canEndMeeting = currentUser?.id == meeting.createdBy ||
        currentUser?.roles?.any { it.equals("Admin", true) } == true

    LaunchedEffect(initialCurrentUser.id) {
        runCatching { repository.me() }.onSuccess { refreshed ->
            currentUser = refreshed.copy(
                avatarUrl = refreshed.avatarUrl?.takeIf { it.isNotBlank() }
                    ?: currentUser?.avatarUrl?.takeIf { it.isNotBlank() }
            )
        }
    }

    LaunchedEffect(joinData.meeting.id) {
        while (isActive) {
            runCatching { repository.meeting(joinData.meeting.id) }
                .onSuccess { latest -> meeting = mergeMeetingSnapshot(meeting, latest) }
            delay(3_000)
        }
    }

    LaunchedEffect(localContainer, joinData) {
        val local = localContainer
        if (local != null) {
            runCatching { rtcManager.join(joinData, local) }
                .onFailure { rtcError = it.message ?: "RTC 初始化失败，请稍后重试" }
        }
    }
    DisposableEffect(Unit) {
        rtcManager.onRemoteParticipantsChanged = { participants ->
            remoteParticipants.clear()
            remoteParticipants.addAll(participants)
        }
        onDispose {
            rtcManager.onRemoteParticipantsChanged = null
            rtcManager.leave()
            if (!meetingEnded) {
                scope.launch { runCatching { repository.leaveMeeting(joinData.meeting.id) } }
            }
        }
    }

    AtmosphericSurface(darkStyle = false, hazeState = hazeState) {
        Box(Modifier.fillMaxSize()) {
            // SurfaceView is intentionally outside hazeSource. Haze only blurs Compose layers,
            // so the controls retain a translucent tint even when a remote video is underneath.
            LazyVerticalGrid(
                columns = GridCells.Fixed(3),
                modifier = Modifier.fillMaxSize().statusBarsPadding()
                    .padding(top = 72.dp, bottom = 112.dp, start = 8.dp, end = 8.dp),
                contentPadding = PaddingValues(bottom = 8.dp),
                horizontalArrangement = Arrangement.spacedBy(8.dp),
                verticalArrangement = Arrangement.spacedBy(8.dp)
            ) {
                item(key = "local") {
                    VideoTile(
                        label = "$localUsername（我）",
                        modifier = Modifier.aspectRatio(.72f),
                        showAvatar = !cameraEnabled,
                        muted = muted,
                        avatarUrl = localAvatarUrl,
                        avatarName = localUsername
                    ) { frame -> localContainer = frame }
                }
                items(remoteParticipants, key = { it.uid }) { participant ->
                    val display = participantDisplays[participant.uid]
                    RemoteVideoTile(
                        participant = participant,
                        label = display?.label ?: "参会人 ${participant.uid}",
                        avatarUrl = display?.avatarUrl,
                        modifier = Modifier.aspectRatio(.72f),
                        onContainer = { frame -> rtcManager.bindRemoteContainer(participant.uid, frame) }
                    )
                }
                if (remoteParticipants.isEmpty()) {
                    item(key = "waiting") {
                        ParticipantTile("等待参会人加入", Modifier.aspectRatio(.72f))
                    }
                }
            }
            Column(Modifier.fillMaxWidth().align(Alignment.TopCenter)) {
                Spacer(Modifier.fillMaxWidth().statusBarsPadding())
                Surface(
                    modifier = Modifier.fillMaxWidth()
                        .height(64.dp)
                        .hazeEffect(hazeState) { blurEffect { blurRadius = 18.dp } },
                    color = Color(0xD9111315)
                ) {
                    Row(Modifier.fillMaxSize().padding(horizontal = 8.dp), verticalAlignment = Alignment.CenterVertically) {
                        IconButton(onClick = onLeave) { Icon(Icons.Default.ArrowBack, "离开会议", tint = Color.White) }
                        Column(
                            Modifier.weight(1f).padding(horizontal = 4.dp),
                            horizontalAlignment = Alignment.CenterHorizontally
                        ) {
                            Text(
                                meeting.title,
                                color = Color.White,
                                style = MaterialTheme.typography.titleMedium,
                                fontWeight = FontWeight.Bold,
                                maxLines = 1,
                                overflow = TextOverflow.Ellipsis
                            )
                            Text(
                                "Agora RTC · ${joinData.channelName}",
                                color = Color(0xFFD4DBE7),
                                style = MaterialTheme.typography.labelSmall,
                                maxLines = 1,
                                overflow = TextOverflow.Ellipsis
                            )
                        }
                        if (currentUser?.id == meeting.createdBy || currentUser?.roles?.any { it.equals("Admin", true) } == true) {
                            IconButton(onClick = { showInviteMembers = true }) {
                                Icon(Icons.Default.PersonAdd, "邀请参会人", tint = Color.White)
                            }
                        } else {
                            Spacer(Modifier.size(48.dp))
                        }
                    }
                }
            }
            Surface(
                modifier = Modifier.fillMaxWidth().align(Alignment.BottomCenter).hazeEffect(hazeState) { blurEffect { blurRadius = 20.dp } },
                color = Color(0xC9111315),
                shape = RoundedCornerShape(topStart = 28.dp, topEnd = 28.dp)
            ) {
                Row(
                    Modifier.fillMaxWidth().padding(vertical = 16.dp, horizontal = 28.dp),
                    horizontalArrangement = Arrangement.SpaceEvenly,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    CallControl(if (muted) Icons.Default.MicOff else Icons.Default.Mic, if (muted) "取消静音" else "静音") { muted = !muted; rtcManager.setMuted(muted) }
                    CallControl(Icons.Default.CameraAlt, if (cameraEnabled) "关闭镜头" else "打开镜头") { cameraEnabled = !cameraEnabled; rtcManager.setCameraEnabled(cameraEnabled) }
                    Surface(
                        shape = RoundedCornerShape(18.dp),
                        color = Color(0xFFE84C4C),
                        modifier = Modifier.size(56.dp).clickable(onClick = {
                            if (canEndMeeting) showEndMeetingConfirm = true else onLeave()
                        })
                    ) {
                        Box(contentAlignment = Alignment.Center) {
                            Icon(Icons.Default.CallEnd, if (canEndMeeting) "结束会议" else "离开会议", tint = Color.White)
                        }
                    }
                }
            }
        }
    }
    rtcError?.let { message ->
        AlertDialog(
            onDismissRequest = onLeave,
            title = { Text("视频会议启动失败") },
            text = { Text(message) },
            confirmButton = { TextButton(onClick = onLeave) { Text("返回会议列表") } }
        )
    }
    if (showInviteMembers) {
        MeetingInviteDialog(
            repository = repository,
            meeting = meeting,
            onDismiss = { showInviteMembers = false },
            onInvited = { updated ->
                meeting = mergeMeetingSnapshot(meeting, updated)
                showInviteMembers = false
            }
        )
    }
    if (showEndMeetingConfirm) {
        AlertDialog(
            onDismissRequest = { if (!endingMeeting) showEndMeetingConfirm = false },
            title = { Text("结束会议") },
            text = { Text("结束后所有参会人都将无法再次加入本次会议。") },
            confirmButton = {
                Button(
                    enabled = !endingMeeting,
                    onClick = {
                        endingMeeting = true
                        scope.launch {
                            runCatching { repository.endMeeting(joinData.meeting.id) }
                                .onSuccess {
                                    meetingEnded = true
                                    onLeave()
                                }
                                .onFailure {
                                    rtcError = it.message ?: "结束会议失败"
                                    endingMeeting = false
                                }
                        }
                    }
                ) { Text(if (endingMeeting) "正在结束" else "确认结束") }
            },
            dismissButton = {
                TextButton(onClick = { showEndMeetingConfirm = false }, enabled = !endingMeeting) { Text("取消") }
            }
        )
    }
}

@Composable
private fun VideoTile(
    label: String,
    modifier: Modifier,
    showAvatar: Boolean = false,
    muted: Boolean = false,
    avatarUrl: String? = null,
    avatarName: String = "我",
    onContainer: (FrameLayout) -> Unit
) {
    Surface(modifier = modifier.fillMaxSize(), shape = RoundedCornerShape(14.dp), color = Color(0xFF171A20)) {
        Box(Modifier.fillMaxSize()) {
            AndroidView(
                factory = { context -> FrameLayout(context).also(onContainer) },
                modifier = Modifier.fillMaxSize(),
                update = { frame -> frame.visibility = if (showAvatar) View.INVISIBLE else View.VISIBLE }
            )
            if (showAvatar) {
                Surface(modifier = Modifier.fillMaxSize(), color = Color(0xFF242A35)) {
                    Column(
                        modifier = Modifier.fillMaxSize(),
                        horizontalAlignment = Alignment.CenterHorizontally,
                        verticalArrangement = Arrangement.Center
                    ) {
                        UserAvatar(avatarUrl, avatarName, Modifier.size(64.dp), "我的头像")
                        Spacer(Modifier.height(8.dp))
                        Text("已关闭镜头", color = Color.White, style = MaterialTheme.typography.labelSmall)
                    }
                }
            }
            Text(
                if (showAvatar) avatarName else label,
                modifier = Modifier.align(Alignment.BottomStart).padding(8.dp),
                color = Color.White,
                style = MaterialTheme.typography.labelSmall
            )
            if (muted) {
                MutedBadge(Modifier.align(Alignment.TopEnd).padding(8.dp))
            }
        }
    }
}

@Composable
private fun RemoteVideoTile(
    participant: RemoteRtcParticipant,
    label: String,
    avatarUrl: String?,
    modifier: Modifier,
    onContainer: (FrameLayout) -> Unit
) {
    Surface(modifier = modifier, shape = RoundedCornerShape(14.dp), color = Color(0xFF171A20)) {
        Box(Modifier.fillMaxSize()) {
            AndroidView(
                factory = { context -> FrameLayout(context).also(onContainer) },
                update = { frame -> frame.visibility = if (participant.cameraEnabled) View.VISIBLE else View.INVISIBLE },
                modifier = Modifier.fillMaxSize()
            )
            if (!participant.cameraEnabled) {
                Surface(modifier = Modifier.fillMaxSize(), color = Color(0xFF242A35)) {
                    Column(
                        modifier = Modifier.fillMaxSize(),
                        horizontalAlignment = Alignment.CenterHorizontally,
                        verticalArrangement = Arrangement.Center
                    ) {
                        UserAvatar(avatarUrl, label, Modifier.size(64.dp), "$label 的头像")
                        Spacer(Modifier.height(8.dp))
                        Text("摄像头已关闭", color = Color.White, style = MaterialTheme.typography.labelSmall)
                    }
                }
            }
            Text(
                label,
                modifier = Modifier.align(Alignment.BottomStart).padding(8.dp),
                color = Color.White,
                style = MaterialTheme.typography.labelSmall
            )
            if (participant.muted) {
                MutedBadge(Modifier.align(Alignment.TopEnd).padding(8.dp))
            }
        }
    }
}

@Composable
private fun MutedBadge(modifier: Modifier = Modifier) {
    Surface(
        modifier = modifier,
        shape = RoundedCornerShape(12.dp),
        color = Color(0xB3222730)
    ) {
        Row(
            modifier = Modifier.padding(horizontal = 7.dp, vertical = 4.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Icon(Icons.Default.MicOff, "已静音", modifier = Modifier.size(15.dp), tint = Color.White)
            Spacer(Modifier.width(3.dp))
            Text("已静音", color = Color.White, style = MaterialTheme.typography.labelSmall)
        }
    }
}

@Composable
private fun MeetingInviteDialog(
    repository: FangRepository,
    meeting: Meeting,
    onDismiss: () -> Unit,
    onInvited: (Meeting) -> Unit
) {
    var contacts by remember { mutableStateOf<List<User>>(emptyList()) }
    var selectedIds by remember { mutableStateOf<Set<String>>(emptySet()) }
    var loading by remember { mutableStateOf(true) }
    var submitting by remember { mutableStateOf(false) }
    var error by remember { mutableStateOf<String?>(null) }
    val scope = rememberCoroutineScope()
    val existingIds = remember(meeting.members) { meeting.members.map { it.userId }.toSet() }

    LaunchedEffect(meeting.id) {
        runCatching { repository.contacts() }
            .onSuccess { contacts = it.filter { contact -> contact.id !in existingIds } }
            .onFailure { error = it.message }
        loading = false
    }

    AlertDialog(
        onDismissRequest = { if (!submitting) onDismiss() },
        title = { Text("邀请参会人") },
        text = {
            Column(Modifier.fillMaxWidth().heightIn(max = 320.dp)) {
                when {
                    loading -> Box(Modifier.fillMaxWidth().padding(20.dp), contentAlignment = Alignment.Center) { CircularProgressIndicator() }
                    contacts.isEmpty() -> Text("暂无可邀请的好友")
                    else -> contacts.forEach { contact ->
                        Row(
                            modifier = Modifier.fillMaxWidth().clickable {
                                selectedIds = if (contact.id in selectedIds) selectedIds - contact.id else selectedIds + contact.id
                            }.padding(vertical = 7.dp),
                            verticalAlignment = Alignment.CenterVertically
                        ) {
                            Checkbox(
                                checked = contact.id in selectedIds,
                                onCheckedChange = { checked ->
                                    selectedIds = if (checked) selectedIds + contact.id else selectedIds - contact.id
                                }
                            )
                            UserAvatar(contact.avatarUrl, contact.realName, Modifier.size(36.dp))
                            Spacer(Modifier.width(10.dp))
                            Text(contact.realName)
                        }
                    }
                }
                error?.let { Text(it, color = MaterialTheme.colorScheme.error, style = MaterialTheme.typography.bodySmall) }
            }
        },
        confirmButton = {
            Button(
                enabled = selectedIds.isNotEmpty() && !submitting,
                onClick = {
                    submitting = true
                    scope.launch {
                        runCatching { repository.inviteMeetingMembers(meeting.id, selectedIds.toList()) }
                            .onSuccess(onInvited)
                            .onFailure { error = it.message ?: "邀请失败"; submitting = false }
                    }
                }
            ) { Text(if (submitting) "正在邀请" else "发送邀请") }
        },
        dismissButton = { TextButton(onClick = onDismiss, enabled = !submitting) { Text("取消") } }
    )
}

@Composable
private fun ParticipantTile(label: String, modifier: Modifier) {
    Surface(modifier = modifier.fillMaxSize(), shape = RoundedCornerShape(14.dp), color = Color(0xFF242A35)) {
        Column(Modifier.fillMaxSize(), horizontalAlignment = Alignment.CenterHorizontally, verticalArrangement = Arrangement.Center) {
            Surface(shape = RoundedCornerShape(16.dp), color = Color(0xFF4E72B8)) { Icon(Icons.Default.Person, label, modifier = Modifier.padding(12.dp), tint = Color.White) }
            Spacer(Modifier.height(7.dp))
            Text(label, color = Color.White, style = MaterialTheme.typography.labelSmall)
        }
    }
}

@Composable
private fun CallControl(icon: androidx.compose.ui.graphics.vector.ImageVector, label: String, onClick: () -> Unit) {
    Column(horizontalAlignment = Alignment.CenterHorizontally, modifier = Modifier.clickable(onClick = onClick)) {
        Surface(shape = RoundedCornerShape(18.dp), color = Color.White.copy(alpha = .16f), modifier = Modifier.size(52.dp)) {
            Box(contentAlignment = Alignment.Center) { Icon(icon, label, tint = Color.White) }
        }
        Spacer(Modifier.height(4.dp))
        Text(label, color = Color.White, style = MaterialTheme.typography.labelSmall)
    }
}
