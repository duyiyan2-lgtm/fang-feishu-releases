namespace FangFeishu.Api.Contracts;

public sealed record LoginRequest(string Username, string Password, string? ClientType = null);

public sealed record LoginResponse(string Token, DateTime ExpiresAt, CurrentUserResponse User);

public sealed record RegisterRequest(
    string Username,
    string Password,
    string RealName,
    string? Email,
    string? Phone,
    string? ClientType = null);

public sealed record LogoutResponse(string TokenId, DateTime ExpiresAt);

public sealed record CurrentUserResponse(
    Guid Id,
    string Username,
    string RealName,
    string? Email,
    string? Phone,
    Guid? DepartmentId,
    string? DepartmentName,
    IReadOnlyList<string> Roles,
    string? Position,
    string? AvatarUrl,
    string? WorkPlace,
    string? Bio);

public sealed record UpdateCurrentUserProfileRequest(
    string? RealName,
    string? Email,
    string? Phone,
    string? Position,
    string? AvatarUrl,
    string? WorkPlace,
    string? Bio);

public sealed record CreateFriendRequest(Guid UserId, string? Greeting);

public sealed record CreateUserRequest(
    string Username,
    string Password,
    string RealName,
    string? Email,
    string? Phone,
    Guid? DepartmentId,
    IReadOnlyList<string>? RoleCodes,
    string? Position);

public sealed record UpdateUserRequest(
    string? RealName,
    string? Email,
    string? Phone,
    Guid? DepartmentId,
    string? Status,
    IReadOnlyList<string>? RoleCodes,
    string? Position);

public sealed record SetStatusRequest(string Status);

public sealed record RoleRequest(
    string RoleName,
    string RoleCode,
    string? Description,
    IReadOnlyList<string>? Permissions = null);

public sealed record UpdateRolePermissionsRequest(IReadOnlyList<string>? Permissions);

public sealed record AssignRolesRequest(IReadOnlyList<string> RoleCodes);

public sealed record DepartmentRequest(Guid? ParentId, string Name, int SortOrder);

public sealed record CreateConversationRequest(string Type, string? Title, IReadOnlyList<Guid> MemberUserIds);

public sealed record ConversationSettingsRequest(
    string? InvitePermission,
    string? KickPermission,
    string? EditNamePermission);

public sealed record UpdateConversationRequest(
    string? Title,
    string? Avatar,
    ConversationSettingsRequest? Settings,
    string? Status);

public sealed record SetConversationAdminsRequest(IReadOnlyList<Guid> AdminIds);

public sealed record UpdateConversationMembersRequest(IReadOnlyList<Guid> MemberUserIds);

public sealed record UpdateConversationAnnouncementRequest(string? Content);

public sealed record SendMessageRequest(
    Guid ConversationId,
    string Content,
    string MessageType = "Text",
    Guid? FileId = null,
    IReadOnlyList<Guid>? MentionUserIds = null);

public sealed record MessageReactionRequest(string ReactionType);

public sealed record DocumentRequest(string Title, string? Content);

public sealed record DocumentCommentRequest(string Content);

public sealed record DocumentCollaboratorRequest(IReadOnlyList<Guid> UserIds, string Permission);

public sealed record UpdateDocumentVisibilityRequest(string Visibility);

public sealed record DictionaryCategoryRequest(
    string Code,
    string Name,
    string? Description,
    bool IsEnabled = true);

public sealed record DictionaryItemRequest(
    string Code,
    string Label,
    string Value,
    string? Description,
    int SortOrder = 0,
    bool IsEnabled = true);

public sealed record CalendarEventRequest(
    string Title,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string? Location,
    string? Description,
    IReadOnlyList<Guid>? AttendeeUserIds = null,
    string? RecurrenceType = null,
    DateTimeOffset? RecurrenceUntil = null);

public sealed record CalendarAttendanceRequest(string Status);

public sealed record ApprovalRequest(
    string Type,
    string Title,
    string Content,
    Guid? TemplateId = null,
    IReadOnlyList<Guid>? CcUserIds = null);

public sealed record ApprovalActionRequest(string? Comment);

public sealed record ApprovalTemplateRequest(
    string Name,
    string Type,
    string? Description,
    IReadOnlyList<Guid> ApproverUserIds,
    bool IsActive = true);

public sealed record CreateTaskRequest(
    string Title,
    string? Description,
    Guid? AssigneeId,
    DateTimeOffset? DueAt);

public sealed record UpdateTaskRequest(
    string? Title,
    string? Description,
    Guid? AssigneeId,
    DateTimeOffset? DueAt);

public sealed record UpdateTaskStatusRequest(string Status);

public sealed class FileUploadRequest
{
    public IFormFile File { get; set; } = null!;
    public Guid? FolderId { get; set; }
}

public sealed record CreateFolderRequest(string Name, Guid? ParentId);

public sealed record UpdateFolderRequest(string Name, Guid? ParentId);

public sealed record MoveFileRequest(Guid? FolderId);

public sealed record FileShareRequest(IReadOnlyList<Guid> UserIds, string Permission);

public sealed record CreateMeetingRequest(
    string? Title,
    string? RoomName,
    string? RoomId,
    IReadOnlyList<Guid>? MemberUserIds,
    DateTimeOffset? ScheduledStartAt = null,
    DateTimeOffset? ScheduledEndAt = null);

public sealed record InviteMeetingMembersRequest(IReadOnlyList<Guid> MemberUserIds);

public sealed record MeetingJoinRequest(bool AutoCamera = true, bool AutoMic = true);

public sealed record UpdateMeetingScheduleRequest(DateTimeOffset? ScheduledStartAt, DateTimeOffset? ScheduledEndAt);

public sealed record SendMeetingChatMessageRequest(string Content);

public sealed record WikiSpaceRequest(string Name, string? Description, string Visibility = "Organization");

public sealed record WikiMemberRequest(IReadOnlyList<Guid> UserIds, string Permission);

public sealed record WikiNodeRequest(Guid? ParentId, Guid? DocumentId, string Title, int SortOrder = 0);
