package com.fangfeishu.android.ui;

import com.fangfeishu.android.data.MeetingMember;
import com.fangfeishu.android.data.MeetingRtcIdentity;

import org.junit.Test;

import java.util.Collections;
import java.util.Map;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertNotNull;

public final class MeetingParticipantMappingTest {
    @Test
    public void avatarUrlResolver_ShouldSupportAbsoluteAndServerRelativeUrls() {
        assertEquals(
            "https://cdn.example.com/avatar/admin.png",
            AvatarUrlResolverKt.resolveAvatarUrl("https://cdn.example.com/avatar/admin.png")
        );
        assertEquals(
            "https://alxy.fun/api/v1/files/avatar-id/preview",
            AvatarUrlResolverKt.resolveAvatarUrl("/api/v1/files/avatar-id/preview")
        );
        assertEquals(
            "https://alxy.fun/api/v1/files/avatar-id/preview",
            AvatarUrlResolverKt.resolveAvatarUrl("api/v1/files/avatar-id/preview")
        );
    }

    @Test
    public void serverRtcIdentityMapsToMemberNameAndAvatar() {
        MeetingMember member = new MeetingMember(
            "6e10f052-345e-4ac0-bb60-f72164c42a8f",
            "管理员",
            "admin",
            "https://example.com/avatar/admin.png",
            null,
            null,
            null,
            null,
            Collections.singletonList(new MeetingRtcIdentity("Android", 123456L))
        );

        MeetingParticipantDisplay display = MeetingParticipantMappingKt
            .buildMeetingParticipantDisplays(Collections.singletonList(member))
            .get(123456);

        assertNotNull(display);
        assertEquals("admin", display.getLabel());
        assertEquals("https://example.com/avatar/admin.png", display.getAvatarUrl());
    }

    @Test
    public void fallbackRtcIdentityKeepsMemberAvatarForOlderBackendResponses() {
        String userId = "0bb2d41b-2396-4d81-87d9-c411ee5fc57d";
        MeetingMember member = new MeetingMember(
            userId,
            "测试用户",
            null,
            "https://example.com/avatar/user.png",
            null,
            null,
            null,
            null,
            null
        );
        Integer androidUid = MeetingParticipantMappingKt.stableAgoraUid(userId, "Android");
        Map<Integer, MeetingParticipantDisplay> displays = MeetingParticipantMappingKt
            .buildMeetingParticipantDisplays(Collections.singletonList(member));

        MeetingParticipantDisplay display = displays.get(androidUid);

        assertNotNull(display);
        assertEquals("测试用户", display.getLabel());
        assertEquals("https://example.com/avatar/user.png", display.getAvatarUrl());
    }

    @Test
    public void legacyRtcIdentityMapsParticipantsFromOldBackendDeployments() {
        String userId = "0bb2d41b-2396-4d81-87d9-c411ee5fc57d";
        MeetingMember member = new MeetingMember(
            userId,
            "测试用户",
            "user_a",
            "https://example.com/avatar/user-a.png",
            null,
            null,
            null,
            null,
            null
        );

        Integer legacyUid = MeetingParticipantMappingKt.stableAgoraUid(userId, null);
        Map<Integer, MeetingParticipantDisplay> displays = MeetingParticipantMappingKt
            .buildMeetingParticipantDisplays(Collections.singletonList(member));

        assertEquals(Integer.valueOf(1630889624), legacyUid);
        assertNotNull(displays.get(legacyUid));
        assertEquals("user_a", displays.get(legacyUid).getLabel());
        assertEquals("https://example.com/avatar/user-a.png", displays.get(legacyUid).getAvatarUrl());
    }

    @Test
    public void typedRtcIdentityUsesTheSameStableVectorAsBackend() {
        Integer androidUid = MeetingParticipantMappingKt.stableAgoraUid(
            "0bb2d41b-2396-4d81-87d9-c411ee5fc57d",
            "Android"
        );

        assertEquals(Integer.valueOf(1003467861), androidUid);
    }
}
