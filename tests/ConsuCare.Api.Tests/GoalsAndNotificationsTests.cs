using System.Net;
using System.Net.Http.Json;
using ConsuCare.Shared.Dtos;
using ConsuCare.Shared.Models;

namespace ConsuCare.Api.Tests;

public class GoalsAndNotificationsTests : IClassFixture<ConsuCareFactory>
{
    private readonly ConsuCareFactory _factory;
    public GoalsAndNotificationsTests(ConsuCareFactory factory) => _factory = factory;

    private async Task<(HttpClient Client, UserDto User)> NewPatientAsync()
    {
        var email = $"goals-{Guid.NewGuid():N}@test.dev";
        var signup = await _factory.CreateClient().PostAsJsonAsync("api/auth/signup",
            new SignupRequest("Goal Patient", email, "Mathematics", "pass-1234", UserRole.Patient));
        signup.EnsureSuccessStatusCode();
        return await _factory.SignedInClientAsync(email, "pass-1234");
    }

    private static async Task<GoalDto> CreateGoalAsync(HttpClient client, int patientId, params string[] milestones)
    {
        var response = await client.PostAsJsonAsync("api/goals",
            new CreateGoalRequest(patientId, "  Improve my wellbeing  ", CareFocus.GeneralHealth, null, milestones.ToList()));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GoalDto>())!;
    }

    [Fact]
    public async Task Creating_a_goal_skips_blank_milestones()
    {
        var (client, me) = await NewPatientAsync();
        var goal = await CreateGoalAsync(client, me.Id, "Draft", "  ", "", "Submit");

        Assert.Equal("Improve my wellbeing", goal.Title);
        Assert.Equal(GoalStatus.InProgress, goal.Status);
        Assert.Equal(new[] { "Draft", "Submit" }, goal.Milestones.Select(m => m.Title));
        Assert.Equal(0, goal.Progress);
        Assert.Equal("Unassigned", goal.SupporterName);
    }

    [Fact]
    public async Task Completing_every_milestone_completes_the_goal()
    {
        var (client, me) = await NewPatientAsync();
        var goal = await CreateGoalAsync(client, me.Id, "Draft", "Submit");

        var half = await (await client.PostAsync($"api/goals/milestones/{goal.Milestones[0].Id}/toggle", null))
            .Content.ReadFromJsonAsync<GoalDto>();
        Assert.Equal(50, half!.Progress);
        Assert.Equal(GoalStatus.InProgress, half.Status);

        var done = await (await client.PostAsync($"api/goals/milestones/{goal.Milestones[1].Id}/toggle", null))
            .Content.ReadFromJsonAsync<GoalDto>();
        Assert.Equal(100, done!.Progress);
        Assert.Equal(GoalStatus.Completed, done.Status);
    }

    [Fact]
    public async Task Unticking_a_milestone_reopens_a_completed_goal()
    {
        var (client, me) = await NewPatientAsync();
        var goal = await CreateGoalAsync(client, me.Id, "Only step");
        var milestoneId = goal.Milestones[0].Id;

        await client.PostAsync($"api/goals/milestones/{milestoneId}/toggle", null);
        var reopened = await (await client.PostAsync($"api/goals/milestones/{milestoneId}/toggle", null))
            .Content.ReadFromJsonAsync<GoalDto>();

        Assert.Equal(GoalStatus.InProgress, reopened!.Status);
        Assert.Equal(0, reopened.Progress);
    }

    [Fact]
    public async Task Completing_a_milestone_sends_a_notification()
    {
        var (client, me) = await NewPatientAsync();
        var goal = await CreateGoalAsync(client, me.Id, "Draft", "Submit");
        await client.PostAsync($"api/goals/milestones/{goal.Milestones[0].Id}/toggle", null);

        var notifications = await client.GetFromJsonAsync<List<NotificationDto>>($"api/notifications/{me.Id}");
        var n = Assert.Single(notifications!, x => x.Kind == NotificationKind.MilestoneCompleted);
        Assert.Contains("50%", n.Body);
    }

    [Fact]
    public async Task Toggling_an_unknown_milestone_returns_not_found()
    {
        var (client, _) = await NewPatientAsync();
        var response = await client.PostAsync("api/goals/milestones/999999/toggle", null);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Mark_all_read_clears_unread_notifications()
    {
        var (client, me) = await NewPatientAsync();
        var goal = await CreateGoalAsync(client, me.Id, "A", "B");
        await client.PostAsync($"api/goals/milestones/{goal.Milestones[0].Id}/toggle", null);
        await client.PostAsync($"api/goals/milestones/{goal.Milestones[1].Id}/toggle", null);

        var before = await client.GetFromJsonAsync<List<NotificationDto>>($"api/notifications/{me.Id}");
        Assert.Contains(before!, n => !n.IsRead);

        var result = await client.PostAsync($"api/notifications/{me.Id}/read-all", null);
        Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);

        var after = await client.GetFromJsonAsync<List<NotificationDto>>($"api/notifications/{me.Id}");
        Assert.All(after!, n => Assert.True(n.IsRead));
    }
}
