package com.fangfeishu.android.data

import android.content.Context
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.booleanPreferencesKey
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.datastore.preferences.preferencesDataStore
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.map

private val Context.sessionDataStore by preferencesDataStore("fang_feishu_session")

data class SavedSession(val token: String?, val darkStyle: Boolean)

class SessionStore(private val context: Context) {
    private val tokenKey = stringPreferencesKey("token")
    private val darkStyleKey = booleanPreferencesKey("dark_style")

    val session: Flow<SavedSession> = context.sessionDataStore.data.map { preferences: Preferences ->
        SavedSession(preferences[tokenKey], preferences[darkStyleKey] ?: false)
    }

    suspend fun saveToken(token: String) {
        context.sessionDataStore.edit { it[tokenKey] = token }
        SessionHolder.token = token
    }

    suspend fun clearToken() {
        context.sessionDataStore.edit { it.remove(tokenKey) }
        SessionHolder.token = null
    }

    suspend fun setDarkStyle(enabled: Boolean) {
        context.sessionDataStore.edit { it[darkStyleKey] = enabled }
    }
}
