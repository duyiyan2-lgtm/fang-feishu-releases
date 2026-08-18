package com.fangfeishu.android.ui

internal fun resolveAvatarUrl(avatarUrl: String?): String? {
    val value = avatarUrl?.trim()?.takeIf { it.isNotEmpty() } ?: return null
    return when {
        value.startsWith("http://", ignoreCase = true) ||
            value.startsWith("https://", ignoreCase = true) ||
            value.startsWith("content://", ignoreCase = true) ||
            value.startsWith("file://", ignoreCase = true) ||
            value.startsWith("data:", ignoreCase = true) -> value
        value.startsWith("//") -> "https:$value"
        value.startsWith("/") -> "https://alxy.fun$value"
        else -> "https://alxy.fun/${value.trimStart('/')}"
    }
}
