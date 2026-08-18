package com.fangfeishu.android.data

internal fun registrationValidationMessage(request: RegisterRequest): String? {
    val username = request.username.trim()
    if (username.length !in 2..64 || username.any { !it.isLetterOrDigit() && it != '_' && it != '-' }) {
        return "用户名需为2-64个字符，仅支持中文、英文字母、数字、下划线或连字符"
    }
    if (request.password.length < 6) {
        return "密码至少需要6个字符"
    }
    if (request.realName.trim().length !in 1..64) {
        return "姓名需为1-64个字符"
    }
    return null
}
