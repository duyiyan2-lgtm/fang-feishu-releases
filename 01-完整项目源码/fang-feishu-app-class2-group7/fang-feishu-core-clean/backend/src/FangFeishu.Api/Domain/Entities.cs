namespace FangFeishu.Api.Domain;

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string RealName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public Guid? DepartmentId { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Department? Department { get; set; }
    public EmployeeProfile? Profile { get; set; }
    public List<UserRole> UserRoles { get; set; } = new();
}

public sealed class Role
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string RoleName { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PermissionsJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<UserRole> UserRoles { get; set; } = new();
}

public sealed class UserRole
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}

public sealed class OperationLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string? TargetId { get; set; }
    public string? Ip { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public User? User { get; set; }
}

public sealed class RevokedToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string TokenId { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime RevokedAt { get; set; } = DateTime.UtcNow;
    public User User { get; set; } = null!;
}

public sealed class Department
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Department? Parent { get; set; }
    public List<Department> Children { get; set; } = new();
    public List<User> Users { get; set; } = new();
}

public sealed class EmployeeProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string? Position { get; set; }
    public string? AvatarUrl { get; set; }
    public string? WorkPlace { get; set; }
    public string? Bio { get; set; }
    public User User { get; set; } = null!;
}

public sealed class UserClientSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string ClientType { get; set; } = "Web";
    public int SessionVersion { get; set; } = 1;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public User User { get; set; } = null!;
}

public sealed class Friendship
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RequesterId { get; set; }
    public Guid AddresseeId { get; set; }
    public string Status { get; set; } = "Pending";
    public string? Greeting { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public User Requester { get; set; } = null!;
    public User Addressee { get; set; } = null!;
}

public sealed class Conversation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = "Single";
    public string? Title { get; set; }
    public string? Avatar { get; set; }
    public string Status { get; set; } = "active";
    public string? AdminIdsJson { get; set; }
    public string InvitePermission { get; set; } = "all";
    public string KickPermission { get; set; } = "admin";
    public string EditNamePermission { get; set; } = "admin";
    public string? Announcement { get; set; }
    public DateTime? AnnouncementUpdatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<ConversationMember> Members { get; set; } = new();
    public List<Message> Messages { get; set; } = new();
}

public sealed class ConversationMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
    public Guid? LastReadMessageId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public Conversation Conversation { get; set; } = null!;
    public User User { get; set; } = null!;
}

public sealed class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string MessageType { get; set; } = "Text";
    public Guid? FileId { get; set; }
    public string? MentionUserIdsJson { get; set; }
    public bool IsRecalled { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Conversation Conversation { get; set; } = null!;
    public User Sender { get; set; } = null!;
    public StoredFile? File { get; set; }
    public List<MessageReaction> Reactions { get; set; } = new();
}

public sealed class MessageReaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MessageId { get; set; }
    public Guid UserId { get; set; }
    public string ReactionType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Message Message { get; set; } = null!;
    public User User { get; set; } = null!;
}

public sealed class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public Guid UpdatedBy { get; set; }
    public string Visibility { get; set; } = "Organization";
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public User Owner { get; set; } = null!;
    public List<DocumentComment> Comments { get; set; } = new();
    public List<DocumentVersion> Versions { get; set; } = new();
    public List<DocumentCollaborator> Collaborators { get; set; } = new();
}

public sealed class DictionaryCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<DictionaryItem> Items { get; set; } = new();
}

public sealed class DictionaryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DictionaryCategory Category { get; set; } = null!;
}

public sealed class DocumentCollaborator
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public Guid UserId { get; set; }
    public string Permission { get; set; } = "View";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Document Document { get; set; } = null!;
    public User User { get; set; } = null!;
}

public sealed class DocumentComment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public Guid UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Document Document { get; set; } = null!;
    public User User { get; set; } = null!;
}

public sealed class DocumentVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public string ContentSnapshot { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Document Document { get; set; } = null!;
}

public sealed class StoredFile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = "application/octet-stream";
    public Guid UploaderId { get; set; }
    public Guid? FolderId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public User Uploader { get; set; } = null!;
    public FileFolder? Folder { get; set; }
    public List<FileShareRecord> Shares { get; set; } = new();
}

public sealed class FileFolder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public Guid OwnerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public FileFolder? Parent { get; set; }
    public List<FileFolder> Children { get; set; } = new();
    public User Owner { get; set; } = null!;
    public List<StoredFile> Files { get; set; } = new();
}

public sealed class FileShareRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FileId { get; set; }
    public Guid UserId { get; set; }
    public string Permission { get; set; } = "View";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public StoredFile File { get; set; } = null!;
    public User User { get; set; } = null!;
}

public sealed class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = "System";
    public string? ResourceType { get; set; }
    public Guid? ResourceId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public User User { get; set; } = null!;
}

public sealed class CalendarEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public string RecurrenceType { get; set; } = "None";
    public DateTime? RecurrenceUntil { get; set; }
    public User User { get; set; } = null!;
    public List<CalendarEventAttendee> Attendees { get; set; } = new();
}

public sealed class CalendarEventAttendee
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CalendarEventId { get; set; }
    public Guid UserId { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime InvitedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }
    public CalendarEvent CalendarEvent { get; set; } = null!;
    public User User { get; set; } = null!;
}

public sealed class ApprovalInstance
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ApplicantId { get; set; }
    public string Type { get; set; } = "Leave";
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string? CcUserIdsJson { get; set; }
    public Guid? TemplateId { get; set; }
    public int CurrentStep { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public User Applicant { get; set; } = null!;
    public List<ApprovalRecord> Records { get; set; } = new();
    public ApprovalTemplate? Template { get; set; }
}

public sealed class ApprovalTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid CreatedBy { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public User Creator { get; set; } = null!;
    public List<ApprovalTemplateStep> Steps { get; set; } = new();
    public List<ApprovalInstance> Instances { get; set; } = new();
}

public sealed class ApprovalTemplateStep
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TemplateId { get; set; }
    public int StepOrder { get; set; }
    public Guid ApproverId { get; set; }
    public ApprovalTemplate Template { get; set; } = null!;
    public User Approver { get; set; } = null!;
}

public sealed class ApprovalRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InstanceId { get; set; }
    public Guid ApproverId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ApprovalInstance Instance { get; set; } = null!;
    public User Approver { get; set; } = null!;
}

public sealed class Meeting
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Provider { get; set; } = "Agora";
    public string RoomId { get; set; } = string.Empty;
    public string ChannelName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public DateTime? ScheduledStartAt { get; set; }
    public DateTime? ScheduledEndAt { get; set; }
    public User Creator { get; set; } = null!;
    public List<MeetingMember> Members { get; set; } = new();
    public List<MeetingChatMessage> ChatMessages { get; set; } = new();
}

public sealed class MeetingChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MeetingId { get; set; }
    public Guid SenderId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Meeting Meeting { get; set; } = null!;
    public User Sender { get; set; } = null!;
}

public sealed class MeetingMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MeetingId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = "Member";
    public DateTime InvitedAt { get; set; } = DateTime.UtcNow;
    public DateTime? JoinedAt { get; set; }
    public DateTime? LeftAt { get; set; }
    public Meeting Meeting { get; set; } = null!;
    public User User { get; set; } = null!;
}

public sealed class WorkTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid CreatorId { get; set; }
    public Guid? AssigneeId { get; set; }
    public string Status { get; set; } = "Todo";
    public DateTime? DueAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public User Creator { get; set; } = null!;
    public User? Assignee { get; set; }
}

public sealed class WikiSpace
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Visibility { get; set; } = "Organization";
    public Guid OwnerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public User Owner { get; set; } = null!;
    public List<WikiNode> Nodes { get; set; } = new();
    public List<WikiSpaceMember> Members { get; set; } = new();
}

public sealed class WikiSpaceMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WikiSpaceId { get; set; }
    public Guid UserId { get; set; }
    public string Permission { get; set; } = "View";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public WikiSpace WikiSpace { get; set; } = null!;
    public User User { get; set; } = null!;
}

public sealed class WikiNode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WikiSpaceId { get; set; }
    public Guid? ParentId { get; set; }
    public Guid? DocumentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public WikiSpace WikiSpace { get; set; } = null!;
    public WikiNode? Parent { get; set; }
    public List<WikiNode> Children { get; set; } = new();
    public Document? Document { get; set; }
}
