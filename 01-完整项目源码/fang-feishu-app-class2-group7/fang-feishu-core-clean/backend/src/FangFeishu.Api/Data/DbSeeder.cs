using FangFeishu.Api.Common;
using FangFeishu.Api.Domain;
using FangFeishu.Api.Security;
using FangFeishu.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace FangFeishu.Api.Data;

public sealed class DbSeeder(
    AppDbContext db,
    PasswordHasher passwordHasher,
    IFileStorageService fileStorageService,
    ILogger<DbSeeder> logger)
{
    public async Task EnsureCreatedAndSeedAsync()
    {
        await db.Database.EnsureCreatedAsync();
        await EnsureConversationManagementColumnsAsync();
        await EnsureCollaborationFeatureSchemaAsync();
        await EnsureSocialSchemaAsync();

        if (await db.Users.AnyAsync())
        {
            return;
        }

        logger.LogInformation("Seeding initial FangFeishu demo data.");

        var adminRole = new Role
        {
            RoleName = "Administrator",
            RoleCode = AppRoles.Admin,
            Description = "System administrator",
            PermissionsJson = "[\"*\"]"
        };
        var userRole = new Role
        {
            RoleName = "User",
            RoleCode = AppRoles.User,
            Description = "Normal user",
            PermissionsJson = "[]"
        };

        var root = new Department { Name = "FangFeishu Demo Company", SortOrder = 1 };
        var tech = new Department { Name = "Technology", Parent = root, SortOrder = 1 };
        var product = new Department { Name = "Product", Parent = root, SortOrder = 2 };
        var ops = new Department { Name = "Operations", Parent = root, SortOrder = 3 };

        var admin = CreateUser("admin", "Admin User", tech, "Project Lead", adminRole);
        var userA = CreateUser("user_a", "User A", tech, "Frontend Developer", userRole);
        var userB = CreateUser("user_b", "User B", product, "Product Manager", userRole);
        var userC = CreateUser("user_c", "User C", ops, "DevOps Engineer", userRole);
        var userD = CreateUser("user_d", "User D", tech, "Backend Developer", userRole);

        db.Roles.AddRange(adminRole, userRole);
        db.Departments.AddRange(root, tech, product, ops);
        db.Users.AddRange(admin, userA, userB, userC, userD);

        var single = new Conversation { Type = "Single", Title = "User A and User B", CreatedBy = userA.Id };
        single.Members.AddRange(new[]
        {
            new ConversationMember { Conversation = single, User = userA },
            new ConversationMember { Conversation = single, User = userB }
        });
        single.Messages.AddRange(new[]
        {
            new Message { Conversation = single, Sender = userA, Content = "Hello, this is a seeded message." },
            new Message { Conversation = single, Sender = userB, Content = "Got it. The shared API works across clients." }
        });

        var group = new Conversation { Type = "Group", Title = "Project Team", CreatedBy = admin.Id };
        group.Members.AddRange(new[]
        {
            new ConversationMember { Conversation = group, User = admin },
            new ConversationMember { Conversation = group, User = userA },
            new ConversationMember { Conversation = group, User = userB },
            new ConversationMember { Conversation = group, User = userC },
            new ConversationMember { Conversation = group, User = userD }
        });
        group.Messages.Add(new Message { Conversation = group, Sender = admin, Content = "Welcome to the 10-day MVP sprint." });

        var document = new Document
        {
            Title = "MVP Project Plan",
            Content = "<h1>MVP Project Plan</h1><p>This seeded document is shared by all clients.</p>",
            Owner = userA,
            UpdatedBy = userA.Id
        };
        document.Comments.Add(new DocumentComment { Document = document, User = userB, Content = "Looks good. Please keep the scope focused." });
        document.Versions.Add(new DocumentVersion { Document = document, CreatedBy = userA.Id, ContentSnapshot = document.Content });

        var sampleFile = await CreateSampleFileAsync(userA.Id);

        db.Conversations.AddRange(single, group);
        db.Documents.Add(document);
        db.Files.Add(sampleFile);
        db.CalendarEvents.Add(new CalendarEvent
        {
            User = userA,
            Title = "Daily Standup",
            StartTime = DateTime.UtcNow.Date.AddHours(2),
            EndTime = DateTime.UtcNow.Date.AddHours(2.5),
            Location = "Online",
            Description = "Check progress and blockers."
        });
        db.ApprovalInstances.Add(new ApprovalInstance
        {
            Applicant = userA,
            Type = "Leave",
            Title = "Leave request",
            Content = "Request one day leave for personal affairs.",
            Status = "Pending"
        });
        db.Notifications.Add(new Notification
        {
            User = userB,
            Title = "Demo notification",
            Content = "This notification is visible from all clients.",
            Type = "System"
        });

        await db.SaveChangesAsync();
    }

    private async Task EnsureConversationManagementColumnsAsync()
    {
        if (!db.Database.IsRelational() ||
            !string.Equals(db.Database.ProviderName, "Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.Ordinal))
        {
            return;
        }

        await db.Database.ExecuteSqlRawAsync("""
            ALTER TABLE conversations ADD COLUMN IF NOT EXISTS "Avatar" text;
            ALTER TABLE conversations ADD COLUMN IF NOT EXISTS "Status" character varying(32) NOT NULL DEFAULT 'active';
            ALTER TABLE conversations ADD COLUMN IF NOT EXISTS "AdminIdsJson" text;
            ALTER TABLE conversations ADD COLUMN IF NOT EXISTS "InvitePermission" character varying(32) NOT NULL DEFAULT 'all';
            ALTER TABLE conversations ADD COLUMN IF NOT EXISTS "KickPermission" character varying(32) NOT NULL DEFAULT 'admin';
            ALTER TABLE conversations ADD COLUMN IF NOT EXISTS "EditNamePermission" character varying(32) NOT NULL DEFAULT 'admin';

            UPDATE conversations
               SET "Status" = 'active'
             WHERE "Status" IS NULL OR "Status" = '';

            UPDATE conversations
               SET "InvitePermission" = 'all'
             WHERE "InvitePermission" IS NULL OR "InvitePermission" = '';

            UPDATE conversations
               SET "KickPermission" = 'admin'
             WHERE "KickPermission" IS NULL OR "KickPermission" = '';

            UPDATE conversations
               SET "EditNamePermission" = 'admin'
             WHERE "EditNamePermission" IS NULL OR "EditNamePermission" = '';
            """);
    }

    private async Task EnsureCollaborationFeatureSchemaAsync()
    {
        if (!db.Database.IsRelational() ||
            !string.Equals(db.Database.ProviderName, "Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.Ordinal))
        {
            return;
        }

        await db.Database.ExecuteSqlRawAsync("""
            ALTER TABLE roles ADD COLUMN IF NOT EXISTS "PermissionsJson" text;
            ALTER TABLE notifications ADD COLUMN IF NOT EXISTS "ResourceType" character varying(40);
            ALTER TABLE notifications ADD COLUMN IF NOT EXISTS "ResourceId" uuid;
            ALTER TABLE documents ADD COLUMN IF NOT EXISTS "Visibility" character varying(32) NOT NULL DEFAULT 'Organization';
            ALTER TABLE documents ADD COLUMN IF NOT EXISTS "IsDeleted" boolean NOT NULL DEFAULT false;
            ALTER TABLE documents ADD COLUMN IF NOT EXISTS "DeletedAt" timestamp with time zone;
            ALTER TABLE documents ADD COLUMN IF NOT EXISTS "DeletedBy" uuid;
            ALTER TABLE files ADD COLUMN IF NOT EXISTS "FolderId" uuid;
            ALTER TABLE files ADD COLUMN IF NOT EXISTS "IsDeleted" boolean NOT NULL DEFAULT false;
            ALTER TABLE files ADD COLUMN IF NOT EXISTS "DeletedAt" timestamp with time zone;
            ALTER TABLE approval_instances ADD COLUMN IF NOT EXISTS "TemplateId" uuid;
            ALTER TABLE approval_instances ADD COLUMN IF NOT EXISTS "CurrentStep" integer NOT NULL DEFAULT 1;
            ALTER TABLE approval_instances ADD COLUMN IF NOT EXISTS "CcUserIdsJson" text;
            ALTER TABLE messages ADD COLUMN IF NOT EXISTS "MentionUserIdsJson" text;
            ALTER TABLE conversations ADD COLUMN IF NOT EXISTS "Announcement" text;
            ALTER TABLE conversations ADD COLUMN IF NOT EXISTS "AnnouncementUpdatedAt" timestamp with time zone;
            ALTER TABLE calendar_events ADD COLUMN IF NOT EXISTS "RecurrenceType" character varying(32) NOT NULL DEFAULT 'None';
            ALTER TABLE calendar_events ADD COLUMN IF NOT EXISTS "RecurrenceUntil" timestamp with time zone;

            CREATE INDEX IF NOT EXISTS "IX_documents_IsDeleted_UpdatedAt" ON documents ("IsDeleted", "UpdatedAt");

            CREATE TABLE IF NOT EXISTS dictionary_categories (
                "Id" uuid NOT NULL PRIMARY KEY,
                "Code" character varying(64) NOT NULL,
                "Name" character varying(120) NOT NULL,
                "Description" text NULL,
                "IsEnabled" boolean NOT NULL DEFAULT true,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_dictionary_categories_Code" ON dictionary_categories ("Code");

            CREATE TABLE IF NOT EXISTS dictionary_items (
                "Id" uuid NOT NULL PRIMARY KEY,
                "CategoryId" uuid NOT NULL REFERENCES dictionary_categories("Id") ON DELETE CASCADE,
                "Code" character varying(64) NOT NULL,
                "Label" character varying(160) NOT NULL,
                "Value" character varying(500) NOT NULL,
                "Description" text NULL,
                "SortOrder" integer NOT NULL DEFAULT 0,
                "IsEnabled" boolean NOT NULL DEFAULT true,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_dictionary_items_CategoryId_Code" ON dictionary_items ("CategoryId", "Code");
            CREATE INDEX IF NOT EXISTS "IX_dictionary_items_CategoryId_SortOrder" ON dictionary_items ("CategoryId", "SortOrder");

            CREATE TABLE IF NOT EXISTS meetings (
                "Id" uuid NOT NULL PRIMARY KEY,
                "Provider" character varying(40) NOT NULL DEFAULT 'Agora',
                "RoomId" character varying(80) NOT NULL,
                "ChannelName" character varying(80) NOT NULL,
                "Title" character varying(160) NOT NULL,
                "Status" character varying(32) NOT NULL DEFAULT 'Active',
                "CreatedBy" uuid NOT NULL REFERENCES users("Id") ON DELETE RESTRICT,
                "CreatedAt" timestamp with time zone NOT NULL,
                "EndedAt" timestamp with time zone NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_meetings_RoomId" ON meetings ("RoomId");
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_meetings_ChannelName" ON meetings ("ChannelName");

            CREATE TABLE IF NOT EXISTS meeting_members (
                "Id" uuid NOT NULL PRIMARY KEY,
                "MeetingId" uuid NOT NULL REFERENCES meetings("Id") ON DELETE CASCADE,
                "UserId" uuid NOT NULL REFERENCES users("Id") ON DELETE RESTRICT,
                "Role" character varying(32) NOT NULL DEFAULT 'Member',
                "InvitedAt" timestamp with time zone NOT NULL,
                "JoinedAt" timestamp with time zone NULL,
                "LeftAt" timestamp with time zone NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_meeting_members_MeetingId_UserId" ON meeting_members ("MeetingId", "UserId");

            ALTER TABLE meetings ADD COLUMN IF NOT EXISTS "ScheduledStartAt" timestamp with time zone;
            ALTER TABLE meetings ADD COLUMN IF NOT EXISTS "ScheduledEndAt" timestamp with time zone;

            CREATE TABLE IF NOT EXISTS meeting_chat_messages (
                "Id" uuid NOT NULL PRIMARY KEY,
                "MeetingId" uuid NOT NULL REFERENCES meetings("Id") ON DELETE CASCADE,
                "SenderId" uuid NOT NULL REFERENCES users("Id") ON DELETE RESTRICT,
                "Content" text NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL
            );

            CREATE INDEX IF NOT EXISTS "IX_meeting_chat_messages_MeetingId_CreatedAt" ON meeting_chat_messages ("MeetingId", "CreatedAt");

            CREATE TABLE IF NOT EXISTS message_reactions (
                "Id" uuid NOT NULL PRIMARY KEY,
                "MessageId" uuid NOT NULL REFERENCES messages("Id") ON DELETE CASCADE,
                "UserId" uuid NOT NULL REFERENCES users("Id") ON DELETE RESTRICT,
                "ReactionType" character varying(32) NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_message_reactions_MessageId_UserId_ReactionType" ON message_reactions ("MessageId", "UserId", "ReactionType");

            CREATE TABLE IF NOT EXISTS document_collaborators (
                "Id" uuid NOT NULL PRIMARY KEY,
                "DocumentId" uuid NOT NULL REFERENCES documents("Id") ON DELETE CASCADE,
                "UserId" uuid NOT NULL REFERENCES users("Id") ON DELETE RESTRICT,
                "Permission" character varying(32) NOT NULL DEFAULT 'View',
                "CreatedAt" timestamp with time zone NOT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_document_collaborators_DocumentId_UserId" ON document_collaborators ("DocumentId", "UserId");

            CREATE TABLE IF NOT EXISTS file_folders (
                "Id" uuid NOT NULL PRIMARY KEY,
                "Name" character varying(160) NOT NULL,
                "ParentId" uuid NULL REFERENCES file_folders("Id") ON DELETE RESTRICT,
                "OwnerId" uuid NOT NULL REFERENCES users("Id") ON DELETE RESTRICT,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL
            );

            CREATE INDEX IF NOT EXISTS "IX_file_folders_OwnerId_ParentId_Name" ON file_folders ("OwnerId", "ParentId", "Name");

            CREATE TABLE IF NOT EXISTS file_shares (
                "Id" uuid NOT NULL PRIMARY KEY,
                "FileId" uuid NOT NULL REFERENCES files("Id") ON DELETE CASCADE,
                "UserId" uuid NOT NULL REFERENCES users("Id") ON DELETE RESTRICT,
                "Permission" character varying(32) NOT NULL DEFAULT 'View',
                "CreatedAt" timestamp with time zone NOT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_file_shares_FileId_UserId" ON file_shares ("FileId", "UserId");

            CREATE TABLE IF NOT EXISTS calendar_event_attendees (
                "Id" uuid NOT NULL PRIMARY KEY,
                "CalendarEventId" uuid NOT NULL REFERENCES calendar_events("Id") ON DELETE CASCADE,
                "UserId" uuid NOT NULL REFERENCES users("Id") ON DELETE RESTRICT,
                "Status" character varying(32) NOT NULL DEFAULT 'Pending',
                "InvitedAt" timestamp with time zone NOT NULL,
                "RespondedAt" timestamp with time zone NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_calendar_event_attendees_CalendarEventId_UserId" ON calendar_event_attendees ("CalendarEventId", "UserId");

            CREATE TABLE IF NOT EXISTS approval_templates (
                "Id" uuid NOT NULL PRIMARY KEY,
                "Name" character varying(160) NOT NULL,
                "Type" character varying(40) NOT NULL,
                "Description" text NULL,
                "CreatedBy" uuid NOT NULL REFERENCES users("Id") ON DELETE RESTRICT,
                "IsActive" boolean NOT NULL DEFAULT true,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL
            );

            CREATE TABLE IF NOT EXISTS approval_template_steps (
                "Id" uuid NOT NULL PRIMARY KEY,
                "TemplateId" uuid NOT NULL REFERENCES approval_templates("Id") ON DELETE CASCADE,
                "StepOrder" integer NOT NULL,
                "ApproverId" uuid NOT NULL REFERENCES users("Id") ON DELETE RESTRICT
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_approval_template_steps_TemplateId_StepOrder" ON approval_template_steps ("TemplateId", "StepOrder");

            CREATE TABLE IF NOT EXISTS wiki_spaces (
                "Id" uuid NOT NULL PRIMARY KEY,
                "Name" character varying(160) NOT NULL,
                "Description" text NULL,
                "Visibility" character varying(32) NOT NULL DEFAULT 'Organization',
                "OwnerId" uuid NOT NULL REFERENCES users("Id") ON DELETE RESTRICT,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL
            );

            CREATE TABLE IF NOT EXISTS wiki_space_members (
                "Id" uuid NOT NULL PRIMARY KEY,
                "WikiSpaceId" uuid NOT NULL REFERENCES wiki_spaces("Id") ON DELETE CASCADE,
                "UserId" uuid NOT NULL REFERENCES users("Id") ON DELETE RESTRICT,
                "Permission" character varying(32) NOT NULL DEFAULT 'View',
                "CreatedAt" timestamp with time zone NOT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_wiki_space_members_WikiSpaceId_UserId" ON wiki_space_members ("WikiSpaceId", "UserId");

            CREATE TABLE IF NOT EXISTS wiki_nodes (
                "Id" uuid NOT NULL PRIMARY KEY,
                "WikiSpaceId" uuid NOT NULL REFERENCES wiki_spaces("Id") ON DELETE CASCADE,
                "ParentId" uuid NULL REFERENCES wiki_nodes("Id") ON DELETE RESTRICT,
                "DocumentId" uuid NULL REFERENCES documents("Id") ON DELETE SET NULL,
                "Title" character varying(200) NOT NULL,
                "SortOrder" integer NOT NULL DEFAULT 0,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL
            );

            CREATE INDEX IF NOT EXISTS "IX_wiki_nodes_WikiSpaceId_ParentId_SortOrder" ON wiki_nodes ("WikiSpaceId", "ParentId", "SortOrder");

            CREATE TABLE IF NOT EXISTS work_tasks (
                "Id" uuid NOT NULL PRIMARY KEY,
                "Title" character varying(160) NOT NULL,
                "Description" text NULL,
                "CreatorId" uuid NOT NULL REFERENCES users("Id") ON DELETE RESTRICT,
                "AssigneeId" uuid NULL REFERENCES users("Id") ON DELETE SET NULL,
                "Status" character varying(32) NOT NULL DEFAULT 'Todo',
                "DueAt" timestamp with time zone NULL,
                "CompletedAt" timestamp with time zone NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL
            );

            CREATE INDEX IF NOT EXISTS "IX_work_tasks_AssigneeId_Status" ON work_tasks ("AssigneeId", "Status");
            CREATE INDEX IF NOT EXISTS "IX_work_tasks_CreatorId_Status" ON work_tasks ("CreatorId", "Status");
            """);
    }

    private async Task EnsureSocialSchemaAsync()
    {
        if (!db.Database.IsRelational() ||
            !string.Equals(db.Database.ProviderName, "Npgsql.EntityFrameworkCore.PostgreSQL", StringComparison.Ordinal))
        {
            return;
        }

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS friendships (
                "Id" uuid NOT NULL PRIMARY KEY,
                "RequesterId" uuid NOT NULL REFERENCES users("Id") ON DELETE RESTRICT,
                "AddresseeId" uuid NOT NULL REFERENCES users("Id") ON DELETE RESTRICT,
                "Status" character varying(24) NOT NULL DEFAULT 'Pending',
                "Greeting" character varying(280) NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_friendships_RequesterId_AddresseeId" ON friendships ("RequesterId", "AddresseeId");
            CREATE INDEX IF NOT EXISTS "IX_friendships_AddresseeId_Status" ON friendships ("AddresseeId", "Status");
            CREATE INDEX IF NOT EXISTS "IX_friendships_RequesterId_Status" ON friendships ("RequesterId", "Status");

            CREATE TABLE IF NOT EXISTS user_client_sessions (
                "Id" uuid NOT NULL PRIMARY KEY,
                "UserId" uuid NOT NULL REFERENCES users("Id") ON DELETE CASCADE,
                "ClientType" character varying(32) NOT NULL,
                "SessionVersion" integer NOT NULL DEFAULT 1,
                "UpdatedAt" timestamp with time zone NOT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_user_client_sessions_UserId_ClientType" ON user_client_sessions ("UserId", "ClientType");
            """);
    }

    private User CreateUser(string username, string realName, Department department, string position, Role role)
    {
        var user = new User
        {
            Username = username,
            PasswordHash = passwordHasher.Hash("123456"),
            RealName = realName,
            Email = $"{username}@example.com",
            Phone = "13800000000",
            Department = department,
            Profile = new EmployeeProfile
            {
                Position = position,
                WorkPlace = "Demo Office",
                Bio = "Seed user for four-client data interoperability demo."
            }
        };

        user.UserRoles.Add(new UserRole { User = user, Role = role });
        return user;
    }

    private async Task<StoredFile> CreateSampleFileAsync(Guid uploaderId)
    {
        var fileName = "sample.txt";
        var content = "This is a seeded demo file for FangFeishu.";
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        await using var stream = new MemoryStream(bytes, writable: false);
        var storageResult = await fileStorageService.SaveAsync(new StorageWriteRequest(
            fileName,
            "text/plain",
            stream,
            bytes.Length,
            "seed/sample.txt"));

        return new StoredFile
        {
            FileName = fileName,
            FilePath = storageResult.RelativePath,
            FileSize = storageResult.Size,
            ContentType = storageResult.ContentType,
            UploaderId = uploaderId
        };
    }
}
