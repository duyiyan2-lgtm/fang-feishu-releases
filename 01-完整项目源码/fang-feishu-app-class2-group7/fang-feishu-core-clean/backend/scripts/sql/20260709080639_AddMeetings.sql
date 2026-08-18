START TRANSACTION;

CREATE TABLE meetings (
    "Id" uuid NOT NULL,
    "Provider" character varying(40) NOT NULL,
    "RoomId" character varying(80) NOT NULL,
    "ChannelName" character varying(80) NOT NULL,
    "Title" character varying(160) NOT NULL,
    "Status" character varying(32) NOT NULL,
    "CreatedBy" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "EndedAt" timestamp with time zone,
    CONSTRAINT "PK_meetings" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_meetings_users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES users ("Id") ON DELETE CASCADE
);

CREATE TABLE meeting_members (
    "Id" uuid NOT NULL,
    "MeetingId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Role" character varying(32) NOT NULL,
    "InvitedAt" timestamp with time zone NOT NULL,
    "JoinedAt" timestamp with time zone,
    "LeftAt" timestamp with time zone,
    CONSTRAINT "PK_meeting_members" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_meeting_members_meetings_MeetingId" FOREIGN KEY ("MeetingId") REFERENCES meetings ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_meeting_members_users_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_meeting_members_MeetingId_UserId" ON meeting_members ("MeetingId", "UserId");

CREATE INDEX "IX_meeting_members_UserId" ON meeting_members ("UserId");

CREATE UNIQUE INDEX "IX_meetings_ChannelName" ON meetings ("ChannelName");

CREATE INDEX "IX_meetings_CreatedBy" ON meetings ("CreatedBy");

CREATE UNIQUE INDEX "IX_meetings_RoomId" ON meetings ("RoomId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260709080639_AddMeetings', '8.0.11');

COMMIT;

