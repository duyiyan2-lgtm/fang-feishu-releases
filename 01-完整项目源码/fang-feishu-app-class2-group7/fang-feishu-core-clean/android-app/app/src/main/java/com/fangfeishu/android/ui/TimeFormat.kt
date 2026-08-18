package com.fangfeishu.android.ui

import java.time.Instant
import java.time.LocalDateTime
import java.time.OffsetDateTime
import java.time.ZoneId
import java.time.ZoneOffset
import java.time.format.DateTimeFormatter

private val momentFormatter: DateTimeFormatter = DateTimeFormatter.ofPattern("yyyy-MM-dd HH:mm")

/** Formats server UTC timestamps in the phone's current time zone. */
fun formatMoment(value: String): String {
    val deviceZone = ZoneId.systemDefault()
    return runCatching {
        Instant.parse(value)
            .atZone(deviceZone)
            .format(momentFormatter)
    }.recoverCatching {
        OffsetDateTime.parse(value)
            .atZoneSameInstant(deviceZone)
            .format(momentFormatter)
    }.recoverCatching {
        // The API persists all timestamps in UTC. Some historic records have no
        // trailing offset, so treat those values as UTC instead of local time.
        LocalDateTime.parse(value)
            .atOffset(ZoneOffset.UTC)
            .atZoneSameInstant(deviceZone)
            .format(momentFormatter)
    }.getOrElse {
        value.replace("T", " ").take(16)
    }
}
