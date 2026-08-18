package com.fangfeishu.android.ui

import com.fangfeishu.android.data.Meeting
import com.fangfeishu.android.data.MeetingMember
import java.nio.ByteBuffer
import java.nio.ByteOrder
import java.nio.charset.StandardCharsets
import java.security.MessageDigest
import java.util.Locale
import java.util.UUID

private val RTC_CLIENT_TYPES = listOf("Android", "Desktop", "Web", "MiniProgram")

internal data class MeetingParticipantDisplay(
    val label: String,
    val avatarUrl: String?
)

internal fun buildMeetingParticipantDisplays(
    members: List<MeetingMember>
): Map<Int, MeetingParticipantDisplay> = buildMap {
    members.forEach { member ->
        val display = MeetingParticipantDisplay(
            label = member.username?.takeIf { it.isNotBlank() }
                ?: member.userName?.takeIf { it.isNotBlank() }
                ?: "参会人",
            avatarUrl = member.avatarUrl?.takeIf { it.isNotBlank() }
        )

        member.rtcIdentities.orEmpty().forEach { identity ->
            if (identity.uid in 1..Int.MAX_VALUE.toLong()) {
                put(identity.uid.toInt(), display)
            }
        }

        // Compatibility fallback for backends that do not yet return rtcIdentities.
        // Older deployments generated the UID from userId only. Keep that legacy
        // identity as well as the newer per-client identities so a rolling backend
        // deployment does not turn participant names back into raw Agora numbers.
        stableAgoraUid(member.userId, null)?.let { uid ->
            if (!containsKey(uid)) put(uid, display)
        }
        RTC_CLIENT_TYPES.forEach { clientType ->
            stableAgoraUid(member.userId, clientType)?.let { uid ->
                if (!containsKey(uid)) put(uid, display)
            }
        }
    }
}

internal fun stableAgoraUid(userId: String, clientType: String?): Int? = runCatching {
    val normalizedUserId = UUID.fromString(userId).toString().replace("-", "")
    val normalizedClientType = clientType?.trim()?.takeIf { it.isNotEmpty() }
    val identity = normalizedClientType?.let {
        "$normalizedUserId:${it.uppercase(Locale.ROOT)}"
    } ?: normalizedUserId
    val hash = MessageDigest.getInstance("SHA-256")
        .digest(identity.toByteArray(StandardCharsets.UTF_8))
    val value = ByteBuffer.wrap(hash, 0, 4)
        .order(ByteOrder.LITTLE_ENDIAN)
        .int and Int.MAX_VALUE
    if (value == 0) 1 else value
}.getOrNull()

internal fun mergeMeetingSnapshot(previous: Meeting, latest: Meeting): Meeting {
    if (latest.members.isEmpty() && previous.members.isNotEmpty()) {
        return latest.copy(members = previous.members)
    }

    val previousMembers = previous.members.associateBy { it.userId }
    return latest.copy(
        members = latest.members.map { current ->
            val old = previousMembers[current.userId] ?: return@map current
            current.copy(
                userName = current.userName?.takeIf { it.isNotBlank() } ?: old.userName,
                username = current.username?.takeIf { it.isNotBlank() } ?: old.username,
                avatarUrl = current.avatarUrl?.takeIf { it.isNotBlank() } ?: old.avatarUrl,
                rtcIdentities = current.rtcIdentities?.takeIf { it.isNotEmpty() } ?: old.rtcIdentities
            )
        }
    )
}
