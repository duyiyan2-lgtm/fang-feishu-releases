using FangFeishu.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace FangFeishu.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<OperationLog> OperationLogs => Set<OperationLog>();
    public DbSet<RevokedToken> RevokedTokens => Set<RevokedToken>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<EmployeeProfile> EmployeeProfiles => Set<EmployeeProfile>();
    public DbSet<UserClientSession> UserClientSessions => Set<UserClientSession>();
    public DbSet<Friendship> Friendships => Set<Friendship>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationMember> ConversationMembers => Set<ConversationMember>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageReaction> MessageReactions => Set<MessageReaction>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentComment> DocumentComments => Set<DocumentComment>();
    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();
    public DbSet<DocumentCollaborator> DocumentCollaborators => Set<DocumentCollaborator>();
    public DbSet<DictionaryCategory> DictionaryCategories => Set<DictionaryCategory>();
    public DbSet<DictionaryItem> DictionaryItems => Set<DictionaryItem>();
    public DbSet<StoredFile> Files => Set<StoredFile>();
    public DbSet<FileFolder> FileFolders => Set<FileFolder>();
    public DbSet<FileShareRecord> FileShares => Set<FileShareRecord>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();
    public DbSet<CalendarEventAttendee> CalendarEventAttendees => Set<CalendarEventAttendee>();
    public DbSet<ApprovalInstance> ApprovalInstances => Set<ApprovalInstance>();
    public DbSet<ApprovalRecord> ApprovalRecords => Set<ApprovalRecord>();
    public DbSet<ApprovalTemplate> ApprovalTemplates => Set<ApprovalTemplate>();
    public DbSet<ApprovalTemplateStep> ApprovalTemplateSteps => Set<ApprovalTemplateStep>();
    public DbSet<Meeting> Meetings => Set<Meeting>();
    public DbSet<MeetingMember> MeetingMembers => Set<MeetingMember>();
    public DbSet<MeetingChatMessage> MeetingChatMessages => Set<MeetingChatMessage>();
    public DbSet<WorkTask> WorkTasks => Set<WorkTask>();
    public DbSet<WikiSpace> WikiSpaces => Set<WikiSpace>();
    public DbSet<WikiSpaceMember> WikiSpaceMembers => Set<WikiSpaceMember>();
    public DbSet<WikiNode> WikiNodes => Set<WikiNode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasIndex(x => x.Username).IsUnique();
            entity.Property(x => x.Username).HasMaxLength(64);
            entity.Property(x => x.RealName).HasMaxLength(64);
            entity.Property(x => x.Status).HasMaxLength(24);
            entity.HasOne(x => x.Department).WithMany(x => x.Users).HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasIndex(x => x.RoleCode).IsUnique();
            entity.Property(x => x.RoleCode).HasMaxLength(64);
            entity.Property(x => x.RoleName).HasMaxLength(64);
            entity.Property(x => x.PermissionsJson).HasColumnType("text");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("user_roles");
            entity.HasIndex(x => new { x.UserId, x.RoleId }).IsUnique();
            entity.HasOne(x => x.User).WithMany(x => x.UserRoles).HasForeignKey(x => x.UserId);
            entity.HasOne(x => x.Role).WithMany(x => x.UserRoles).HasForeignKey(x => x.RoleId);
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("departments");
            entity.Property(x => x.Name).HasMaxLength(100);
            entity.HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeProfile>(entity =>
        {
            entity.ToTable("employee_profiles");
            entity.HasIndex(x => x.UserId).IsUnique();
            entity.HasOne(x => x.User).WithOne(x => x.Profile).HasForeignKey<EmployeeProfile>(x => x.UserId);
        });

        modelBuilder.Entity<UserClientSession>(entity =>
        {
            entity.ToTable("user_client_sessions");
            entity.HasIndex(x => new { x.UserId, x.ClientType }).IsUnique();
            entity.Property(x => x.ClientType).HasMaxLength(32);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Friendship>(entity =>
        {
            entity.ToTable("friendships");
            entity.HasIndex(x => new { x.RequesterId, x.AddresseeId }).IsUnique();
            entity.HasIndex(x => new { x.AddresseeId, x.Status });
            entity.HasIndex(x => new { x.RequesterId, x.Status });
            entity.Property(x => x.Status).HasMaxLength(24);
            entity.Property(x => x.Greeting).HasMaxLength(280);
            entity.HasOne(x => x.Requester).WithMany().HasForeignKey(x => x.RequesterId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Addressee).WithMany().HasForeignKey(x => x.AddresseeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OperationLog>(entity =>
        {
            entity.ToTable("operation_logs");
            entity.Property(x => x.Module).HasMaxLength(64);
            entity.Property(x => x.Action).HasMaxLength(64);
        });

        modelBuilder.Entity<RevokedToken>(entity =>
        {
            entity.ToTable("revoked_tokens");
            entity.HasIndex(x => x.TokenId).IsUnique();
            entity.Property(x => x.TokenId).HasMaxLength(128);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.ToTable("conversations");
            entity.Property(x => x.Type).HasMaxLength(24);
            entity.Property(x => x.Title).HasMaxLength(120);
            entity.Property(x => x.Avatar).HasMaxLength(500);
            entity.Property(x => x.Status).HasMaxLength(32);
            entity.Property(x => x.AdminIdsJson).HasColumnType("text");
            entity.Property(x => x.InvitePermission).HasMaxLength(32);
            entity.Property(x => x.KickPermission).HasMaxLength(32);
            entity.Property(x => x.EditNamePermission).HasMaxLength(32);
            entity.Property(x => x.Announcement).HasColumnType("text");
        });

        modelBuilder.Entity<ConversationMember>(entity =>
        {
            entity.ToTable("conversation_members");
            entity.HasIndex(x => new { x.ConversationId, x.UserId }).IsUnique();
            entity.HasOne(x => x.Conversation).WithMany(x => x.Members).HasForeignKey(x => x.ConversationId);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.ToTable("messages");
            entity.Property(x => x.Content).HasColumnType("text");
            entity.Property(x => x.MessageType).HasMaxLength(32);
            entity.Property(x => x.MentionUserIdsJson).HasColumnType("text");
            entity.HasOne(x => x.Conversation).WithMany(x => x.Messages).HasForeignKey(x => x.ConversationId);
            entity.HasOne(x => x.Sender).WithMany().HasForeignKey(x => x.SenderId);
            entity.HasOne(x => x.File).WithMany().HasForeignKey(x => x.FileId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<MessageReaction>(entity =>
        {
            entity.ToTable("message_reactions");
            entity.HasIndex(x => new { x.MessageId, x.UserId, x.ReactionType }).IsUnique();
            entity.Property(x => x.ReactionType).HasMaxLength(32);
            entity.HasOne(x => x.Message).WithMany(x => x.Reactions).HasForeignKey(x => x.MessageId);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("documents");
            entity.Property(x => x.Title).HasMaxLength(200);
            entity.Property(x => x.Content).HasColumnType("text");
            entity.Property(x => x.Visibility).HasMaxLength(32);
            entity.HasQueryFilter(x => !x.IsDeleted);
            entity.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId);
        });

        modelBuilder.Entity<DictionaryCategory>(entity =>
        {
            entity.ToTable("dictionary_categories");
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(64);
            entity.Property(x => x.Name).HasMaxLength(120);
            entity.Property(x => x.Description).HasColumnType("text");
        });

        modelBuilder.Entity<DictionaryItem>(entity =>
        {
            entity.ToTable("dictionary_items");
            entity.HasIndex(x => new { x.CategoryId, x.Code }).IsUnique();
            entity.HasIndex(x => new { x.CategoryId, x.SortOrder });
            entity.Property(x => x.Code).HasMaxLength(64);
            entity.Property(x => x.Label).HasMaxLength(160);
            entity.Property(x => x.Value).HasMaxLength(500);
            entity.Property(x => x.Description).HasColumnType("text");
            entity.HasOne(x => x.Category).WithMany(x => x.Items).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentComment>(entity =>
        {
            entity.ToTable("document_comments");
            entity.Property(x => x.Content).HasColumnType("text");
            entity.HasOne(x => x.Document).WithMany(x => x.Comments).HasForeignKey(x => x.DocumentId);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<DocumentCollaborator>(entity =>
        {
            entity.ToTable("document_collaborators");
            entity.HasIndex(x => new { x.DocumentId, x.UserId }).IsUnique();
            entity.Property(x => x.Permission).HasMaxLength(32);
            entity.HasOne(x => x.Document).WithMany(x => x.Collaborators).HasForeignKey(x => x.DocumentId);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<DocumentVersion>(entity =>
        {
            entity.ToTable("document_versions");
            entity.Property(x => x.ContentSnapshot).HasColumnType("text");
            entity.HasOne(x => x.Document).WithMany(x => x.Versions).HasForeignKey(x => x.DocumentId);
        });

        modelBuilder.Entity<StoredFile>(entity =>
        {
            entity.ToTable("files");
            entity.Property(x => x.FileName).HasMaxLength(260);
            entity.Property(x => x.FilePath).HasMaxLength(500);
            entity.HasOne(x => x.Uploader).WithMany().HasForeignKey(x => x.UploaderId);
            entity.HasOne(x => x.Folder).WithMany(x => x.Files).HasForeignKey(x => x.FolderId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<FileFolder>(entity =>
        {
            entity.ToTable("file_folders");
            entity.Property(x => x.Name).HasMaxLength(160);
            entity.HasIndex(x => new { x.OwnerId, x.ParentId, x.Name });
            entity.HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId);
        });

        modelBuilder.Entity<FileShareRecord>(entity =>
        {
            entity.ToTable("file_shares");
            entity.HasIndex(x => new { x.FileId, x.UserId }).IsUnique();
            entity.Property(x => x.Permission).HasMaxLength(32);
            entity.HasOne(x => x.File).WithMany(x => x.Shares).HasForeignKey(x => x.FileId);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("notifications");
            entity.Property(x => x.Title).HasMaxLength(160);
            entity.Property(x => x.Type).HasMaxLength(40);
            entity.Property(x => x.ResourceType).HasMaxLength(40);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<CalendarEvent>(entity =>
        {
            entity.ToTable("calendar_events");
            entity.Property(x => x.Title).HasMaxLength(160);
            entity.Property(x => x.RecurrenceType).HasMaxLength(32);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<CalendarEventAttendee>(entity =>
        {
            entity.ToTable("calendar_event_attendees");
            entity.HasIndex(x => new { x.CalendarEventId, x.UserId }).IsUnique();
            entity.Property(x => x.Status).HasMaxLength(32);
            entity.HasOne(x => x.CalendarEvent).WithMany(x => x.Attendees).HasForeignKey(x => x.CalendarEventId);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<ApprovalInstance>(entity =>
        {
            entity.ToTable("approval_instances");
            entity.Property(x => x.Type).HasMaxLength(40);
            entity.Property(x => x.Status).HasMaxLength(40);
            entity.Property(x => x.CcUserIdsJson).HasColumnType("text");
            entity.HasOne(x => x.Applicant).WithMany().HasForeignKey(x => x.ApplicantId);
            entity.HasOne(x => x.Template).WithMany(x => x.Instances).HasForeignKey(x => x.TemplateId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ApprovalRecord>(entity =>
        {
            entity.ToTable("approval_records");
            entity.Property(x => x.Action).HasMaxLength(40);
            entity.HasOne(x => x.Instance).WithMany(x => x.Records).HasForeignKey(x => x.InstanceId);
            entity.HasOne(x => x.Approver).WithMany().HasForeignKey(x => x.ApproverId);
        });

        modelBuilder.Entity<ApprovalTemplate>(entity =>
        {
            entity.ToTable("approval_templates");
            entity.Property(x => x.Name).HasMaxLength(160);
            entity.Property(x => x.Type).HasMaxLength(40);
            entity.Property(x => x.Description).HasColumnType("text");
            entity.HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatedBy);
        });

        modelBuilder.Entity<ApprovalTemplateStep>(entity =>
        {
            entity.ToTable("approval_template_steps");
            entity.HasIndex(x => new { x.TemplateId, x.StepOrder }).IsUnique();
            entity.HasOne(x => x.Template).WithMany(x => x.Steps).HasForeignKey(x => x.TemplateId);
            entity.HasOne(x => x.Approver).WithMany().HasForeignKey(x => x.ApproverId);
        });

        modelBuilder.Entity<Meeting>(entity =>
        {
            entity.ToTable("meetings");
            entity.HasIndex(x => x.RoomId).IsUnique();
            entity.HasIndex(x => x.ChannelName).IsUnique();
            entity.Property(x => x.Provider).HasMaxLength(40);
            entity.Property(x => x.RoomId).HasMaxLength(80);
            entity.Property(x => x.ChannelName).HasMaxLength(80);
            entity.Property(x => x.Title).HasMaxLength(160);
            entity.Property(x => x.Status).HasMaxLength(32);
            entity.HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatedBy);
        });

        modelBuilder.Entity<MeetingMember>(entity =>
        {
            entity.ToTable("meeting_members");
            entity.HasIndex(x => new { x.MeetingId, x.UserId }).IsUnique();
            entity.Property(x => x.Role).HasMaxLength(32);
            entity.HasOne(x => x.Meeting).WithMany(x => x.Members).HasForeignKey(x => x.MeetingId);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<MeetingChatMessage>(entity =>
        {
            entity.ToTable("meeting_chat_messages");
            entity.Property(x => x.Content).HasColumnType("text");
            entity.HasOne(x => x.Meeting).WithMany(x => x.ChatMessages).HasForeignKey(x => x.MeetingId);
            entity.HasOne(x => x.Sender).WithMany().HasForeignKey(x => x.SenderId);
        });

        modelBuilder.Entity<WorkTask>(entity =>
        {
            entity.ToTable("work_tasks");
            entity.HasIndex(x => new { x.AssigneeId, x.Status });
            entity.HasIndex(x => new { x.CreatorId, x.Status });
            entity.Property(x => x.Title).HasMaxLength(160);
            entity.Property(x => x.Description).HasColumnType("text");
            entity.Property(x => x.Status).HasMaxLength(32);
            entity.HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Assignee).WithMany().HasForeignKey(x => x.AssigneeId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<WikiSpace>(entity =>
        {
            entity.ToTable("wiki_spaces");
            entity.Property(x => x.Name).HasMaxLength(160);
            entity.Property(x => x.Description).HasColumnType("text");
            entity.Property(x => x.Visibility).HasMaxLength(32);
            entity.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId);
        });

        modelBuilder.Entity<WikiSpaceMember>(entity =>
        {
            entity.ToTable("wiki_space_members");
            entity.HasIndex(x => new { x.WikiSpaceId, x.UserId }).IsUnique();
            entity.Property(x => x.Permission).HasMaxLength(32);
            entity.HasOne(x => x.WikiSpace).WithMany(x => x.Members).HasForeignKey(x => x.WikiSpaceId);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<WikiNode>(entity =>
        {
            entity.ToTable("wiki_nodes");
            entity.Property(x => x.Title).HasMaxLength(200);
            entity.HasIndex(x => new { x.WikiSpaceId, x.ParentId, x.SortOrder });
            entity.HasOne(x => x.WikiSpace).WithMany(x => x.Nodes).HasForeignKey(x => x.WikiSpaceId);
            entity.HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.SetNull);
        });
    }
}
