package com.fangfeishu.android.ui

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

private val GradientColorScheme = lightColorScheme(
    primary = Color(0xFF236CEB),
    onPrimary = Color.White,
    secondary = Color(0xFF167D96),
    background = Color(0xFFF2FBFF),
    onBackground = Color(0xFF142235),
    surface = Color(0xF2FFFFFF),
    onSurface = Color(0xFF142235),
    surfaceVariant = Color(0xFFE0F4F8),
    onSurfaceVariant = Color(0xFF536173)
)

private val InkColorScheme = darkColorScheme(
    primary = Color(0xFF6A99FF),
    onPrimary = Color(0xFF081326),
    secondary = Color(0xFF78E3D2),
    background = Color(0xFF111315),
    onBackground = Color(0xFFE9ECF1),
    surface = Color(0xE61B1D20),
    onSurface = Color(0xFFE9ECF1),
    surfaceVariant = Color(0xFF24272C),
    onSurfaceVariant = Color(0xFFABB2BE)
)

@Composable
fun FangFeishuTheme(darkStyle: Boolean, content: @Composable () -> Unit) {
    MaterialTheme(
        colorScheme = if (darkStyle) InkColorScheme else GradientColorScheme,
        content = content
    )
}
