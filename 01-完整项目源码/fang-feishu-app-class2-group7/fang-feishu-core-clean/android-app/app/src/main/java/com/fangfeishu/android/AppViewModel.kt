package com.fangfeishu.android

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.fangfeishu.android.data.FangRepository
import com.fangfeishu.android.data.RegisterRequest
import com.fangfeishu.android.data.SessionHolder
import com.fangfeishu.android.data.SessionStore
import com.fangfeishu.android.data.User
import com.fangfeishu.android.data.registrationValidationMessage
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.collectLatest
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class AppState(
    val isBooting: Boolean = true,
    val token: String? = null,
    val user: User? = null,
    val darkStyle: Boolean = false,
    val isWorking: Boolean = false,
    val error: String? = null
)

class AppViewModel(application: Application) : AndroidViewModel(application) {
    private val sessionStore = SessionStore(application)
    val repository = FangRepository()

    private val _state = MutableStateFlow(AppState())
    val state: StateFlow<AppState> = _state.asStateFlow()

    init {
        SessionHolder.onUnauthorized = {
            viewModelScope.launch {
                sessionStore.clearToken()
                _state.update {
                    it.copy(
                        token = null,
                        user = null,
                        isBooting = false,
                        isWorking = false,
                        error = "该账号已在另一台 Android 设备登录"
                    )
                }
            }
        }
        viewModelScope.launch {
            sessionStore.session.collectLatest { saved ->
                SessionHolder.token = saved.token
                _state.update { it.copy(token = saved.token, darkStyle = saved.darkStyle, isBooting = false) }
                if (!saved.token.isNullOrBlank() && _state.value.user == null) {
                    refreshProfile()
                }
            }
        }
    }

    fun login(username: String, password: String) = runOperation {
        val result = repository.login(username.trim(), password)
        sessionStore.saveToken(result.token)
        _state.update { it.copy(user = result.user) }
    }

    fun register(request: RegisterRequest) {
        val normalized = request.copy(
            username = request.username.trim(),
            realName = request.realName.trim()
        )
        registrationValidationMessage(normalized)?.let { message ->
            _state.update { it.copy(error = message, isWorking = false) }
            return
        }
        runOperation {
            val result = repository.register(normalized)
            sessionStore.saveToken(result.token)
            _state.update { it.copy(user = result.user) }
        }
    }

    fun refreshProfile() = runOperation(showProgress = false) {
        _state.update { it.copy(user = repository.me()) }
    }

    fun updateCurrentUser(user: User) {
        _state.update { it.copy(user = user) }
    }

    fun logout() = runOperation {
        runCatching { repository.logout() }
        sessionStore.clearToken()
        _state.value = AppState(isBooting = false, darkStyle = _state.value.darkStyle)
    }

    fun setDarkStyle(enabled: Boolean) {
        viewModelScope.launch { sessionStore.setDarkStyle(enabled) }
    }

    fun clearError() {
        _state.update { it.copy(error = null) }
    }

    private fun runOperation(showProgress: Boolean = true, block: suspend () -> Unit) {
        viewModelScope.launch {
            if (showProgress) _state.update { it.copy(isWorking = true, error = null) }
            try {
                block()
            } catch (error: Throwable) {
                _state.update { it.copy(error = error.message ?: "网络请求失败") }
            }
            if (showProgress) _state.update { it.copy(isWorking = false) }
        }
    }
}
