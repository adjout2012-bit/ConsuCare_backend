namespace ConsuCare.Shared.Models;

public enum UserRole { Patient, Supporter, Admin }
public enum CareFocus { GeneralHealth, Nutrition, PhysicalActivity, MentalWellbeing, MedicationAdherence }
public enum GoalStatus { InProgress, Completed }
public enum SupportRequestStatus { Pending, Accepted, Declined }
public enum ConnectionStatus { Active, Completed }
public enum VerificationStatus { Pending, Approved, Rejected }
public enum NotificationKind { RequestAccepted, NewMessage, MilestoneCompleted, RequestReceived, System }

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    // PBKDF2 hash ("iterations.salt.hash"), produced by Api.Services.PasswordHasher.
    public string Password { get; set; } = "";
    public UserRole Role { get; set; }
    public string Community { get; set; } = "";
    public string Bio { get; set; } = "";
    public string? PhotoUrl { get; set; }
    public bool EmailNotifs { get; set; } = true;
    public bool InAppNotifs { get; set; } = true;
    public bool RequestAlerts { get; set; } = true;
    public bool GoalAlerts { get; set; }
    public bool IsPublic { get; set; }
}

public class SupporterProfile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = "";
    public string ConditionType { get; set; } = "";
    public int DiagnosisYear { get; set; }
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public bool Available { get; set; }
    public string ManagementStrategies { get; set; } = "";
    public VerificationStatus Verification { get; set; }
    public DateTime AppliedOn { get; set; }
}

public class Review
{
    public int Id { get; set; }
    public int SupporterUserId { get; set; }
    public string PatientName { get; set; } = "";
    public int Rating { get; set; }
    public string Text { get; set; } = "";
}

public class SupportRequest
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int SupporterUserId { get; set; }
    public CareFocus CareFocus { get; set; }
    public string Message { get; set; } = "";
    public string Frequency { get; set; } = "Weekly";
    public SupportRequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SupportConnection
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int SupporterUserId { get; set; }
    public ConnectionStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? LastSessionAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class Goal
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int? SupporterUserId { get; set; }
    public string Title { get; set; } = "";
    public CareFocus Type { get; set; }
    public GoalStatus Status { get; set; }
    public List<Milestone> Milestones { get; set; } = new();
}

public class Milestone
{
    public int Id { get; set; }
    public int GoalId { get; set; }
    public string Title { get; set; } = "";
    public bool IsDone { get; set; }
}

public class ChatMessage
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public int RecipientId { get; set; }
    public string Content { get; set; } = "";
    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; }
}

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public NotificationKind Kind { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
}
