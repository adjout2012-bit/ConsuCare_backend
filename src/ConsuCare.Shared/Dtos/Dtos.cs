using ConsuCare.Shared.Models;

namespace ConsuCare.Shared.Dtos;

// ---- Auth ----
public record LoginRequest(string Email, string Password);
public record SignupRequest(
    string FullName,
    string Email,
    string Community,
    string Password,
    UserRole Role,
    string ConditionType = "",
    int DiagnosisYear = 0,
    bool ShareHealthDetails = false);
public record UserDto(int Id, string FullName, string Email, UserRole Role, string Community, string Bio,
    string? PhotoUrl, bool EmailNotifs, bool InAppNotifs, bool RequestAlerts, bool GoalAlerts, bool IsPublic);
public record AuthResponse(UserDto User, string Token);
public record UpdateProfileRequest(string FullName, string Email, string Community, string Bio, string? PhotoUrl);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record UpdatePreferencesRequest(bool EmailNotifs, bool InAppNotifs, bool RequestAlerts, bool GoalAlerts, bool IsPublic);

// ---- Supporters / discovery ----
public record SupporterCardDto(int UserId, string Name, string Title, string ConditionType, int DiagnosisYear,
    string Community, double Rating, int ReviewCount, bool Available, string ManagementStrategies, string Bio);
public record ReviewDto(string PatientName, int Rating, string Text);
public record SupporterProfileDto(SupporterCardDto Card, string FullBio, List<ReviewDto> Reviews);
public record CreateReviewRequest(string PatientName, int Rating, string Text);

// ---- Goals ----
public record MilestoneDto(int Id, string Title, bool IsDone);
public record GoalDto(int Id, string Title, CareFocus Type, GoalStatus Status, string SupporterName, List<MilestoneDto> Milestones)
{
    public int Progress => Milestones.Count == 0 ? 0
        : (int)Math.Round(100.0 * Milestones.Count(m => m.IsDone) / Milestones.Count);
}
public record CreateGoalRequest(int PatientId, string Title, CareFocus Type, int? SupporterUserId, List<string> Milestones);

// ---- Messaging ----
public record ConversationDto(int PartnerId, string PartnerName, string PartnerRoleLine, string LastMessage, DateTime LastAt, int Unread);
public record ChatMessageDto(int Id, int SenderId, int RecipientId, string Content, DateTime SentAt);
public record SendMessageRequest(int SenderId, int RecipientId, string Content);

// ---- Peer-support requests and connections ----
public record CreateSupportRequest(int PatientId, int SupporterUserId, CareFocus CareFocus, string Message, string Frequency);
public record SupportRequestDto(int Id, int PatientId, string PatientName, string PatientCommunity, int SupporterUserId, string SupporterName,
    CareFocus CareFocus, string Message, string Frequency, SupportRequestStatus Status, DateTime CreatedAt);
public record SupportConnectionDto(int Id, int PartnerId, string PartnerName, string PartnerRoleLine, string GoalTitle,
    int Progress, DateTime? LastSessionAt, ConnectionStatus Status, bool IsPendingRequest);

// ---- Notifications ----
public record NotificationDto(int Id, NotificationKind Kind, string Title, string Body, DateTime CreatedAt, bool IsRead);

// ---- Dashboards ----
public record PatientDashboardDto(int ActiveSupporters, int GoalsInProgress, int UnreadMessages,
    List<SupporterCardDto> Supporters, List<GoalDto> Goals);
public record PatientRowDto(int PatientId, string Name, string Community, string GoalTitle, int Progress, DateTime? LastSessionAt);
public record SupporterDashboardDto(int ActivePatients, int PendingRequests, int SessionsThisMonth,
    List<SupportRequestDto> Requests, List<PatientRowDto> Patients);

// ---- Admin ----
public record VerificationDto(int ProfileId, int UserId, string Name, string Community, string Title,
    string ConditionType, int DiagnosisYear, string ManagementStrategies, DateTime AppliedOn,
    VerificationStatus Status);
