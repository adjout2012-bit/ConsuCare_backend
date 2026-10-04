using ConsuCare.Api.Services;
using ConsuCare.Shared.Models;

namespace ConsuCare.Api.Data;

public static class SeedData
{
    public static void Ensure(AppDbContext db)
    {
        if (db.Users.Any()) return;

        var now = DateTime.UtcNow;
        var demoPassword = PasswordHasher.Hash("demo1234");
        var patients = new[]
        {
            new User { FullName = "Amara Osei", Email = "amara@patient.dev", Password = demoPassword, Role = UserRole.Patient, Community = "Newly diagnosed", Bio = "Finding my footing after a recent diagnosis and looking for practical peer support." },
            new User { FullName = "David Kim", Email = "david@patient.dev", Password = demoPassword, Role = UserRole.Patient, Community = "Building routines", Bio = "Taking things one day at a time while I figure out routines that work for me." },
            new User { FullName = "Priya Sharma", Email = "priya@patient.dev", Password = demoPassword, Role = UserRole.Patient, Community = "Emotional wellbeing" },
            new User { FullName = "Tunde Bakare", Email = "tunde@patient.dev", Password = demoPassword, Role = UserRole.Patient, Community = "Everyday support" },
            new User { FullName = "Fatima Al-Hassan", Email = "fatima@patient.dev", Password = demoPassword, Role = UserRole.Patient, Community = "Living with chronic illness" }
        };
        var nnamdi = new User
        {
            FullName = "Nnamdi Okafor", Email = "nnamdi@supporter.dev", Password = demoPassword,
            Role = UserRole.Supporter, Community = "Peer support",
            Bio = "I live with Type 1 diabetes and can share what has helped me build routines around daily life. Everyone's experience is different; I listen without judgment and encourage people to work with their care team."
        };
        var sarah = new User
        {
            FullName = "Sarah Chen", Email = "sarah@supporter.dev", Password = demoPassword,
            Role = UserRole.Supporter, Community = "Peer support",
            Bio = "I live with Crohn's disease. I can talk about planning for appointments and travel, keeping notes for care-team visits, and making space for the emotional ups and downs."
        };
        var james = new User
        {
            FullName = "James Whitfield", Email = "james@supporter.dev", Password = demoPassword,
            Role = UserRole.Supporter, Community = "Peer support",
            Bio = "I live with multiple sclerosis. I can share my experience with pacing, rest breaks, and asking for help on difficult days."
        };
        var leila = new User
        {
            FullName = "Leila Haddad", Email = "leila@supporter.dev", Password = demoPassword,
            Role = UserRole.Supporter, Community = "Peer support",
            Bio = "I live with Type 1 diabetes. Preparing supplies and keeping a simple daily log have helped me; everyone's experience is different, and I'm here to listen."
        };
        var marcus = new User
        {
            FullName = "Marcus Reid", Email = "marcus@supporter.dev", Password = demoPassword,
            Role = UserRole.Supporter, Community = "Peer support",
            Bio = "I live with Crohn's disease. I find flexible routines and preparing questions for care-team appointments useful in my own life."
        };
        var elena = new User
        {
            FullName = "Elena Petrova", Email = "elena@supporter.dev", Password = demoPassword,
            Role = UserRole.Supporter, Community = "Peer support",
            Bio = "I live with multiple sclerosis. I use pacing, rest breaks, and a flexible weekly plan when my energy changes."
        };
        var robert = new User
        {
            FullName = "Robert Osei", Email = "robert@supporter.dev", Password = demoPassword,
            Role = UserRole.Supporter, Community = "Peer support",
            Bio = "I live with Type 1 diabetes. Keeping a consistent routine and preparing for schedule changes help me manage day to day."
        };
        var admin = new User
        {
            FullName = "Rita Adeyemi", Email = "admin@consucare.dev", Password = demoPassword,
            Role = UserRole.Admin, Community = "Operations"
        };

        var supporters = new[] { nnamdi, sarah, james, leila, marcus, elena, robert };
        var allUsers = patients.Concat(supporters).Append(admin).ToArray();
        foreach (var user in allUsers) user.IsPublic = true;
        db.Users.AddRange(allUsers);
        db.SaveChanges();

        db.SupporterProfiles.AddRange(
            Profile(nnamdi, "Type 1 Diabetes", 2012, "Plan meals and activity around my routine; keep supplies together; review patterns with my care team.", 4.9, 31, true, now.AddMonths(-14)),
            Profile(sarah, "Crohn's Disease", 2016, "Plan ahead for appointments and travel, keep notes for care-team visits, and build flexible routines.", 4.8, 24, true, now.AddMonths(-10)),
            Profile(james, "Multiple Sclerosis", 2019, "Pace activities, schedule rest breaks, and track symptoms to discuss with my care team.", 4.7, 18, true, now.AddMonths(-9)),
            Profile(marcus, "Crohn's Disease", 2014, "Keep routines flexible, note changes, and prepare questions for care-team appointments.", 4.6, 12, true, now.AddMonths(-8)),
            Profile(elena, "Multiple Sclerosis", 2020, "Use pacing, rest breaks, and a flexible weekly plan when energy levels change.", 4.9, 27, true, now.AddMonths(-6)),
            Profile(leila, "Type 1 Diabetes", 2011, "Prepare supplies before leaving home and use a simple daily log to discuss patterns with my care team.", 5.0, 9, true, now.AddDays(-2), VerificationStatus.Pending),
            Profile(robert, "Type 1 Diabetes", 2018, "Keep a consistent routine, prepare for schedule changes, and ask my care team about concerns.", 0, 0, true, now.AddDays(-3), VerificationStatus.Pending)
        );

        db.Reviews.AddRange(
            new Review { SupporterUserId = nnamdi.Id, PatientName = "Amara Osei", Rating = 5, Text = "Nnamdi listened without judgment and shared practical ideas from his own experience. It helped me feel less alone." },
            new Review { SupporterUserId = nnamdi.Id, PatientName = "David Kim", Rating = 5, Text = "A thoughtful conversation with useful ideas I could discuss with my care team." },
            new Review { SupporterUserId = sarah.Id, PatientName = "Priya Sharma", Rating = 5, Text = "Sarah made space for the hard parts and shared how she plans for appointments." },
            new Review { SupporterUserId = elena.Id, PatientName = "Tunde Bakare", Rating = 5, Text = "Elena shared her experience openly and reminded me that routines can change over time." }
        );

        var routineGoal = new Goal
        {
            PatientId = patients[0].Id, SupporterUserId = nnamdi.Id, Title = "Find a daily routine that feels manageable",
            Type = CareFocus.GeneralHealth, Status = GoalStatus.InProgress,
            Milestones =
            {
                new Milestone { Title = "Write down the parts of the day that feel hardest", IsDone = true },
                new Milestone { Title = "Discuss routine questions with my care team" },
                new Milestone { Title = "Check in with my supporter about what feels helpful" }
            }
        };
        var wellbeingGoal = new Goal
        {
            PatientId = patients[0].Id, SupporterUserId = sarah.Id, Title = "Make room for emotional wellbeing",
            Type = CareFocus.MentalWellbeing, Status = GoalStatus.InProgress,
            Milestones =
            {
                new Milestone { Title = "Name one person I can reach out to", IsDone = true },
                new Milestone { Title = "Try one calming activity that suits me" },
                new Milestone { Title = "Talk about what has been difficult" }
            }
        };
        var movementGoal = new Goal
        {
            PatientId = patients[1].Id, SupporterUserId = elena.Id, Title = "Explore a comfortable activity rhythm",
            Type = CareFocus.PhysicalActivity, Status = GoalStatus.InProgress,
            Milestones =
            {
                new Milestone { Title = "Notice when I have the most energy", IsDone = true },
                new Milestone { Title = "Ask my care team about activity questions" },
                new Milestone { Title = "Try a flexible rest-and-activity plan" }
            }
        };
        db.Goals.AddRange(routineGoal, wellbeingGoal, movementGoal);

        db.SupportConnections.AddRange(
            new SupportConnection { PatientId = patients[0].Id, SupporterUserId = nnamdi.Id, Status = ConnectionStatus.Active, StartedAt = now.AddMonths(-2), LastSessionAt = now.AddDays(-2) },
            new SupportConnection { PatientId = patients[0].Id, SupporterUserId = sarah.Id, Status = ConnectionStatus.Active, StartedAt = now.AddMonths(-1), LastSessionAt = now.AddDays(-5) },
            new SupportConnection { PatientId = patients[1].Id, SupporterUserId = elena.Id, Status = ConnectionStatus.Active, StartedAt = now.AddDays(-20), LastSessionAt = now.AddDays(-3) },
            new SupportConnection { PatientId = patients[2].Id, SupporterUserId = marcus.Id, Status = ConnectionStatus.Completed, StartedAt = now.AddMonths(-5), LastSessionAt = now.AddDays(-12), CompletedAt = now.AddDays(-10) }
        );
        db.SupportRequests.AddRange(
            new SupportRequest { PatientId = patients[0].Id, SupporterUserId = james.Id, CareFocus = CareFocus.GeneralHealth, Frequency = "Weekly", Status = SupportRequestStatus.Pending, CreatedAt = now.AddDays(-1), Message = "I'm newly diagnosed and would value hearing how you found a routine that worked for you." },
            new SupportRequest { PatientId = patients[3].Id, SupporterUserId = nnamdi.Id, CareFocus = CareFocus.MentalWellbeing, Frequency = "Weekly", Status = SupportRequestStatus.Pending, CreatedAt = now.AddDays(-3), Message = "I'm looking for someone who understands the emotional side of adjusting to a chronic condition." },
            new SupportRequest { PatientId = patients[4].Id, SupporterUserId = sarah.Id, CareFocus = CareFocus.GeneralHealth, Frequency = "Monthly", Status = SupportRequestStatus.Pending, CreatedAt = now.AddDays(-1), Message = "Would you be open to sharing what has helped you prepare for care-team appointments?" }
        );

        db.Messages.AddRange(
            new ChatMessage { SenderId = nnamdi.Id, RecipientId = patients[0].Id, SentAt = now.AddHours(-3), IsRead = true, Content = "How has the week been for you? No pressure to have it all figured out." },
            new ChatMessage { SenderId = patients[0].Id, RecipientId = nnamdi.Id, SentAt = now.AddHours(-2.8), IsRead = true, Content = "A little overwhelming, but I wrote down a few questions for my care team." },
            new ChatMessage { SenderId = nnamdi.Id, RecipientId = patients[0].Id, SentAt = now.AddHours(-2.5), IsRead = true, Content = "That sounds like a helpful next step. I can share how I organize my own notes if you'd like." },
            new ChatMessage { SenderId = sarah.Id, RecipientId = patients[0].Id, SentAt = now.AddHours(-6), IsRead = false, Content = "I remember appointments feeling like a lot at first. Would a simple question list be useful?" },
            new ChatMessage { SenderId = elena.Id, RecipientId = patients[1].Id, SentAt = now.AddDays(-1), IsRead = false, Content = "Rest and flexibility are part of my routine too. Everyone finds their own balance." }
        );
        db.Notifications.AddRange(
            new Notification { UserId = patients[0].Id, Kind = NotificationKind.RequestAccepted, CreatedAt = now.AddHours(-2), IsRead = false, Title = "Nnamdi accepted your support request", Body = "Your peer-support connection is active. Say hello whenever you're ready." },
            new Notification { UserId = patients[0].Id, Kind = NotificationKind.NewMessage, CreatedAt = now.AddHours(-6), IsRead = false, Title = "New message from Sarah Chen", Body = "Would a simple question list be useful?" },
            new Notification { UserId = patients[0].Id, Kind = NotificationKind.MilestoneCompleted, CreatedAt = now.AddHours(-9), IsRead = false, Title = "Milestone completed: Write down the parts of the day that feel hardest", Body = "Finding a daily routine that feels manageable is moving forward." },
            new Notification { UserId = nnamdi.Id, Kind = NotificationKind.RequestReceived, CreatedAt = now.AddDays(-3), IsRead = false, Title = "New peer-support request", Body = "Tunde Bakare is looking for emotional support." },
            new Notification { UserId = nnamdi.Id, Kind = NotificationKind.RequestReceived, CreatedAt = now.AddDays(-1), IsRead = false, Title = "New peer-support request", Body = "Fatima Al-Hassan would like to talk about care-team appointments." }
        );

        db.SaveChanges();
    }

    private static SupporterProfile Profile(
        User user, string condition, int year, string strategies, double rating, int reviews,
        bool available, DateTime appliedOn, VerificationStatus verification = VerificationStatus.Approved)
        => new()
        {
            UserId = user.Id,
            Title = "Peer Supporter",
            ConditionType = condition,
            DiagnosisYear = year,
            ManagementStrategies = strategies,
            Rating = rating,
            ReviewCount = reviews,
            Available = available,
            Verification = verification,
            AppliedOn = appliedOn
        };
}
