package com.fangfeishu.android.ui

import androidx.compose.animation.AnimatedContent
import androidx.activity.compose.BackHandler
import androidx.compose.animation.core.FastOutSlowInEasing
import androidx.compose.animation.core.RepeatMode
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.tween
import androidx.compose.foundation.background
import androidx.compose.foundation.Canvas
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.RowScope
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Assignment
import androidx.compose.material.icons.filled.Chat
import androidx.compose.material.icons.filled.Cloud
import androidx.compose.material.icons.filled.Description
import androidx.compose.material.icons.filled.Event
import androidx.compose.material.icons.filled.Folder
import androidx.compose.material.icons.filled.Groups
import androidx.compose.material.icons.filled.MenuBook
import androidx.compose.material.icons.filled.Notifications
import androidx.compose.material.icons.filled.Person
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material.icons.filled.VideoCall
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.CenterAlignedTopAppBar
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FloatingActionButton
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.NavigationBar
import androidx.compose.material3.NavigationBarItem
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SnackbarHost
import androidx.compose.material3.SnackbarHostState
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TopAppBarDefaults
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.geometry.Size
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.fangfeishu.android.AppViewModel
import com.fangfeishu.android.data.RegisterRequest
import com.fangfeishu.android.data.SessionHolder
import com.fangfeishu.android.data.Meeting
import com.fangfeishu.android.data.User
import dev.chrisbanes.haze.HazeState
import dev.chrisbanes.haze.blur.blurEffect
import dev.chrisbanes.haze.hazeEffect
import dev.chrisbanes.haze.hazeSource
import dev.chrisbanes.haze.rememberHazeState
import io.github.om252345.composemeshgradient.MeshGradient
import kotlinx.coroutines.delay
import coil.compose.AsyncImage
import coil.request.ImageRequest

private enum class MainTab(val title: String, val icon: ImageVector) {
    Messages("消息", Icons.Default.Chat),
    Documents("云文档", Icons.Default.Description),
    Workbench("工作台", Icons.Default.Cloud),
    Mine("我的", Icons.Default.Person)
}

enum class Feature(val title: String, val icon: ImageVector) {
    Calendar("日历", Icons.Default.Event),
    Approvals("审批", Icons.Default.Assignment),
    Files("云盘", Icons.Default.Folder),
    Tasks("任务", Icons.Default.Assignment),
    Meetings("视频会议", Icons.Default.VideoCall),
    Contacts("通讯录", Icons.Default.Groups),
    Wiki("知识库", Icons.Default.MenuBook),
    Notifications("通知", Icons.Default.Notifications)
}

@Composable
fun FangFeishuApp(viewModel: AppViewModel = viewModel()) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    var splashVisible by remember { mutableStateOf(true) }
    LaunchedEffect(Unit) {
        delay(1200)
        splashVisible = false
    }

    FangFeishuTheme(darkStyle = state.darkStyle) {
        AnimatedContent(targetState = splashVisible || state.isBooting, label = "launch-transition") { loading ->
            when {
                loading -> LaunchScreen(state.darkStyle)
                state.token.isNullOrBlank() -> LoginScreen(
                    isWorking = state.isWorking,
                    error = state.error,
                    onLogin = viewModel::login,
                    onRegister = viewModel::register,
                    onClearError = viewModel::clearError
                )
                state.user == null -> LoadingProfileScreen(
                    error = state.error,
                    onRetry = viewModel::refreshProfile,
                    onLogout = viewModel::logout
                )
                else -> HomeScreen(
                    viewModel = viewModel,
                    user = state.user!!,
                    darkStyle = state.darkStyle,
                    onThemeChanged = viewModel::setDarkStyle,
                    onLogout = viewModel::logout
                )
            }
        }
    }
}

@Composable
private fun LaunchScreen(darkStyle: Boolean) {
    AtmosphericSurface(darkStyle = darkStyle) { hazeState ->
        Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
            GlassCard(hazeState, modifier = Modifier.padding(32.dp)) {
                Column(
                    horizontalAlignment = Alignment.CenterHorizontally,
                    modifier = Modifier.padding(horizontal = 42.dp, vertical = 34.dp)
                ) {
                    BrandMark(Modifier.size(68.dp))
                    Spacer(Modifier.height(20.dp))
                    Text("仿飞书协同办公", style = MaterialTheme.typography.headlineSmall, fontWeight = FontWeight.Bold)
                    Spacer(Modifier.height(8.dp))
                    Text("连接团队，开始高效协作", color = MaterialTheme.colorScheme.onSurfaceVariant)
                    Spacer(Modifier.height(24.dp))
                    CircularProgressIndicator(modifier = Modifier.size(24.dp), strokeWidth = 2.dp)
                }
            }
        }
    }
}

@Composable
private fun LoadingProfileScreen(error: String?, onRetry: () -> Unit, onLogout: () -> Unit) {
    AtmosphericSurface(darkStyle = false) { hazeState ->
        Box(Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
            GlassCard(hazeState, modifier = Modifier.padding(28.dp)) {
                Column(
                    horizontalAlignment = Alignment.CenterHorizontally,
                    modifier = Modifier.padding(28.dp)
                ) {
                    CircularProgressIndicator()
                    Spacer(Modifier.height(18.dp))
                    Text(error ?: "正在恢复登录状态")
                    if (error != null) {
                        Spacer(Modifier.height(12.dp))
                        Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                            OutlinedButton(onClick = onLogout) { Text("退出") }
                            Button(onClick = onRetry) { Text("重试") }
                        }
                    }
                }
            }
        }
    }
}

@Composable
private fun LoginScreen(
    isWorking: Boolean,
    error: String?,
    onLogin: (String, String) -> Unit,
    onRegister: (RegisterRequest) -> Unit,
    onClearError: () -> Unit
) {
    // Never prefill a privileged test account after logout (or on a fresh launch).
    var username by remember { mutableStateOf("") }
    var password by remember { mutableStateOf("") }
    var registering by remember { mutableStateOf(false) }
    var realName by remember { mutableStateOf("") }
    val hazeState = rememberHazeState()

    AtmosphericSurface(darkStyle = false, hazeState = hazeState) {
        Box(
            modifier = Modifier.fillMaxSize().padding(horizontal = 24.dp),
            contentAlignment = Alignment.Center
        ) {
            GlassCard(hazeState, modifier = Modifier.fillMaxWidth()) {
                Column(modifier = Modifier.padding(26.dp)) {
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        BrandMark(Modifier.size(52.dp))
                        Spacer(Modifier.width(14.dp))
                        Column {
                            Text("仿飞书协同办公", style = MaterialTheme.typography.titleLarge, fontWeight = FontWeight.Bold)
                            Text("Android 原生客户端", color = MaterialTheme.colorScheme.onSurfaceVariant)
                        }
                    }
                    Spacer(Modifier.height(28.dp))
                    Text(if (registering) "创建新账号" else "登录", style = MaterialTheme.typography.headlineSmall, fontWeight = FontWeight.SemiBold)
                    Spacer(Modifier.height(12.dp))
                    OutlinedTextField(
                        value = username,
                        onValueChange = { username = it; onClearError() },
                        label = { Text("用户名") },
                        singleLine = true,
                        modifier = Modifier.fillMaxWidth()
                    )
                    if (registering) {
                        Spacer(Modifier.height(10.dp))
                        OutlinedTextField(
                            value = realName,
                            onValueChange = { realName = it; onClearError() },
                            label = { Text("姓名") },
                            singleLine = true,
                            modifier = Modifier.fillMaxWidth()
                        )
                    }
                    Spacer(Modifier.height(10.dp))
                    OutlinedTextField(
                        value = password,
                        onValueChange = { password = it; onClearError() },
                        label = { Text("密码") },
                        singleLine = true,
                        visualTransformation = PasswordVisualTransformation(),
                        modifier = Modifier.fillMaxWidth()
                    )
                    if (!error.isNullOrBlank()) {
                        Spacer(Modifier.height(10.dp))
                        Text(error, color = MaterialTheme.colorScheme.error, style = MaterialTheme.typography.bodySmall)
                    }
                    Spacer(Modifier.height(20.dp))
                    Button(
                        onClick = {
                            if (registering) onRegister(RegisterRequest(username, password, realName.ifBlank { username }, null, null))
                            else onLogin(username, password)
                        },
                        enabled = !isWorking && username.isNotBlank() && password.isNotBlank() && (!registering || realName.isNotBlank()),
                        modifier = Modifier.fillMaxWidth(),
                        colors = ButtonDefaults.buttonColors(containerColor = MaterialTheme.colorScheme.primary)
                    ) {
                        if (isWorking) CircularProgressIndicator(Modifier.size(20.dp), strokeWidth = 2.dp)
                        else Text(if (registering) "注册并登录" else "登录")
                    }
                    TextButton(
                        onClick = { registering = !registering; onClearError() },
                        modifier = Modifier.align(Alignment.CenterHorizontally)
                    ) {
                        Text(if (registering) "已有账号，去登录" else "没有账号，立即注册")
                    }
                }
            }
        }
    }
}

@Composable
private fun HomeScreen(
    viewModel: AppViewModel,
    user: User,
    darkStyle: Boolean,
    onThemeChanged: (Boolean) -> Unit,
    onLogout: () -> Unit
) {
    var tab by remember { mutableStateOf(MainTab.Messages) }
    var feature by remember { mutableStateOf<Feature?>(null) }
    var chatId by remember { mutableStateOf<String?>(null) }
    var groupMeeting by remember { mutableStateOf<Meeting?>(null) }
    var conversationsRefreshVersion by remember { mutableStateOf(0) }

    BackHandler(enabled = groupMeeting != null || chatId != null || feature != null) {
        when {
            groupMeeting != null -> groupMeeting = null
            chatId != null -> {
                chatId = null
                conversationsRefreshVersion++
            }
            feature != null -> feature = null
        }
    }

    when {
        groupMeeting != null -> MeetingJoinScreen(
            repository = viewModel.repository,
            meeting = groupMeeting!!,
            currentUser = user,
            onLeave = { groupMeeting = null }
        )
        chatId != null -> ChatScreen(
            repository = viewModel.repository,
            conversationId = chatId.orEmpty(),
            onStartGroupMeeting = { meeting -> groupMeeting = meeting },
            onBack = {
                chatId = null
                conversationsRefreshVersion++
            }
        )
        feature != null -> FeatureScreen(
            repository = viewModel.repository,
            feature = feature!!,
            currentUser = user,
            onOpenConversation = { conversationId ->
                feature = null
                chatId = conversationId
            },
            onOpenMeeting = { meeting ->
                feature = null
                groupMeeting = meeting
            },
            onBack = { feature = null }
        )
        else -> {
            val hazeState = rememberHazeState()
            AtmosphericSurface(darkStyle = darkStyle, hazeState = hazeState) {
                val snackbarHostState = remember { SnackbarHostState() }
                Scaffold(
                    containerColor = Color.Transparent,
                    snackbarHost = { SnackbarHost(snackbarHostState) },
                    bottomBar = {
                        NavigationBar(
                            modifier = Modifier.hazeEffect(hazeState) { blurEffect { blurRadius = 18.dp } },
                            containerColor = MaterialTheme.colorScheme.surface.copy(alpha = 0.76f)
                        ) {
                            MainTab.entries.forEach { item ->
                                NavigationBarItem(
                                    selected = tab == item,
                                    onClick = { tab = item },
                                    icon = { Icon(item.icon, contentDescription = item.title) },
                                    label = { Text(item.title) }
                                )
                            }
                        }
                    }
                ) { innerPadding ->
                    Box(Modifier.fillMaxSize().padding(innerPadding)) {
                        when (tab) {
                            MainTab.Messages -> ConversationsScreen(
                                repository = viewModel.repository,
                                currentUserId = user.id,
                                displayName = user.realName,
                                hazeState = hazeState,
                                refreshSignal = conversationsRefreshVersion,
                                onOpenConversation = { chatId = it }
                            )
                            MainTab.Documents -> DocumentsScreen(viewModel.repository, hazeState)
                            MainTab.Workbench -> WorkbenchScreen(hazeState) { feature = it }
                            MainTab.Mine -> MineScreen(
                                initialUser = user,
                                repository = viewModel.repository,
                                darkStyle = darkStyle,
                                hazeState = hazeState,
                                onThemeChanged = onThemeChanged,
                                onProfileUpdated = viewModel::updateCurrentUser,
                                onLogout = onLogout
                            )
                        }
                    }
                }
            }
        }
    }
}

@Composable
fun AtmosphericSurface(
    darkStyle: Boolean,
    hazeState: HazeState = rememberHazeState(),
    content: @Composable (HazeState) -> Unit
) {
    val transition = rememberInfiniteTransition(label = "ambient-mesh")
    // Each mesh axis follows a different, slow rhythm so the light theme feels alive
    // without turning cards and text into visually noisy moving targets.
    val topWave by transition.animateFloat(
        initialValue = .34f,
        targetValue = .66f,
        animationSpec = infiniteRepeatable(tween(5200, easing = FastOutSlowInEasing), RepeatMode.Reverse),
        label = "mesh-top-wave"
    )
    val leftWave by transition.animateFloat(
        initialValue = .34f,
        targetValue = .66f,
        animationSpec = infiniteRepeatable(tween(6800, easing = FastOutSlowInEasing), RepeatMode.Reverse),
        label = "mesh-left-wave"
    )
    val centerX by transition.animateFloat(
        initialValue = .31f,
        targetValue = .69f,
        animationSpec = infiniteRepeatable(tween(4600, easing = FastOutSlowInEasing), RepeatMode.Reverse),
        label = "mesh-center-x"
    )
    val centerY by transition.animateFloat(
        initialValue = .34f,
        targetValue = .66f,
        animationSpec = infiniteRepeatable(tween(7400, easing = FastOutSlowInEasing), RepeatMode.Reverse),
        label = "mesh-center-y"
    )
    val rightWave by transition.animateFloat(
        initialValue = .35f,
        targetValue = .65f,
        animationSpec = infiniteRepeatable(tween(6100, easing = FastOutSlowInEasing), RepeatMode.Reverse),
        label = "mesh-right-wave"
    )
    val bottomWave by transition.animateFloat(
        initialValue = .36f,
        targetValue = .64f,
        animationSpec = infiniteRepeatable(tween(5700, easing = FastOutSlowInEasing), RepeatMode.Reverse),
        label = "mesh-bottom-wave"
    )
    Box(Modifier.fillMaxSize()) {
        if (darkStyle) {
            Box(
                Modifier.fillMaxSize()
                    .background(Color(0xFF111315))
                    .hazeSource(hazeState)
            )
        } else {
            MeshGradient(
                width = 3,
                height = 3,
                points = arrayOf(
                    Offset(0f, 0f), Offset(topWave, 0f), Offset(1f, 0f),
                    Offset(0f, leftWave), Offset(centerX, centerY), Offset(1f, rightWave),
                    Offset(0f, 1f), Offset(bottomWave, 1f), Offset(1f, 1f)
                ),
                colors = arrayOf(
                    Color(0xFFA8F3EC), Color(0xFFE7FAFF), Color(0xFFAFCBFF),
                    Color(0xFF8BE0E9), Color(0xFF7FBAFF), Color(0xFFA9C5FF),
                    Color(0xFFD9FAF4), Color(0xFF9ADCF4), Color(0xFFB8CEFF)
                ),
                modifier = Modifier.fillMaxSize().hazeSource(hazeState)
            )
        }
        content(hazeState)
    }
}

@Composable
fun GlassCard(
    hazeState: HazeState,
    modifier: Modifier = Modifier,
    contentPadding: PaddingValues = PaddingValues(0.dp),
    content: @Composable () -> Unit
) {
    Surface(
        modifier = modifier
            .clip(RoundedCornerShape(22.dp))
            .hazeEffect(hazeState) { blurEffect { blurRadius = 18.dp } },
        shape = RoundedCornerShape(22.dp),
        color = MaterialTheme.colorScheme.surface.copy(alpha = 0.70f),
        contentColor = MaterialTheme.colorScheme.onSurface,
        tonalElevation = 0.dp,
        shadowElevation = 0.dp
    ) {
        Box(Modifier.padding(contentPadding)) { content() }
    }
}

@Composable
fun BrandMark(modifier: Modifier = Modifier) {
    Canvas(modifier = modifier.clip(RoundedCornerShape(14.dp)).background(MaterialTheme.colorScheme.primary)) {
        val tile = size.minDimension * .24f
        val gap = size.minDimension * .08f
        val left = (size.width - tile * 2 - gap) / 2
        val top = (size.height - tile * 2 - gap) / 2
        listOf(
            Offset(left, top), Offset(left + tile + gap, top),
            Offset(left, top + tile + gap), Offset(left + tile + gap, top + tile + gap)
        ).forEachIndexed { index, offset ->
            drawRoundRect(
                color = if (index == 3) Color(0xFF8EF0DF) else Color.White,
                topLeft = offset,
                size = Size(tile, tile),
                cornerRadius = androidx.compose.ui.geometry.CornerRadius(tile * .28f)
            )
        }
    }
}

@Composable
fun UserAvatar(
    avatarUrl: String?,
    fallbackName: String,
    modifier: Modifier = Modifier,
    contentDescription: String = "用户头像"
) {
    val context = LocalContext.current
    val resolvedAvatarUrl = remember(avatarUrl) { resolveAvatarUrl(avatarUrl) }
    var imageLoadFailed by remember(resolvedAvatarUrl, SessionHolder.token) { mutableStateOf(false) }
    Box(
        modifier = modifier
            .clip(CircleShape)
            .background(MaterialTheme.colorScheme.primary.copy(alpha = .82f)),
        contentAlignment = Alignment.Center
    ) {
        if (!resolvedAvatarUrl.isNullOrBlank() && !imageLoadFailed) {
            val request = remember(resolvedAvatarUrl, SessionHolder.token) {
                ImageRequest.Builder(context)
                    .data(resolvedAvatarUrl)
                    .apply {
                        SessionHolder.token?.takeIf { it.isNotBlank() }?.let { addHeader("Authorization", "Bearer $it") }
                    }
                    .crossfade(true)
                    .build()
            }
            AsyncImage(
                model = request,
                contentDescription = contentDescription,
                modifier = Modifier.fillMaxSize(),
                contentScale = ContentScale.Crop,
                onError = { imageLoadFailed = true }
            )
        } else {
            Text(
                text = fallbackName.trim().take(1).ifBlank { "我" },
                color = Color.White,
                fontWeight = FontWeight.Bold,
                style = MaterialTheme.typography.titleMedium
            )
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun GlassTopBar(hazeState: HazeState, title: String, navigation: @Composable (() -> Unit)? = null, actions: @Composable RowScope.() -> Unit = {}) {
    Surface(
        modifier = Modifier.fillMaxWidth().statusBarsPadding().height(72.dp)
            .hazeEffect(hazeState) { blurEffect { blurRadius = 16.dp } },
        color = MaterialTheme.colorScheme.surface.copy(alpha = .60f),
        contentColor = MaterialTheme.colorScheme.onSurface
    ) {
        Box(Modifier.fillMaxSize()) {
            Box(Modifier.align(Alignment.CenterStart)) { navigation?.invoke() }
            Text(
                text = title,
                modifier = Modifier.align(Alignment.Center),
                style = MaterialTheme.typography.titleLarge,
                fontWeight = FontWeight.Bold
            )
            Row(
                modifier = Modifier.align(Alignment.CenterEnd).padding(end = 4.dp),
                verticalAlignment = Alignment.CenterVertically,
                content = actions
            )
        }
    }
}
