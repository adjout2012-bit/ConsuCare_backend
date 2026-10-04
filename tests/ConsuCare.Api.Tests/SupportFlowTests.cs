using System.Net;
using System.Net.Http.Json;
using ConsuCare.Shared.Dtos;
using ConsuCare.Shared.Models;

namespace ConsuCare.Api.Tests;

/// <summary>A patient requests peer support, then the supporter accepts or declines.</summary>
public class SupportFlowTests : IClassFixture<ConsuCareFactory>
{
    private readonly ConsuCareFactory _factory;
    public SupportFlowTests(ConsuCareFactory factory) => _factory = factory;

    private async Task<(HttpClient Client, UserDto User)> NewPatientAsync()
    {
        var email = $"flow-{Guid.NewGuid():N}@test.dev";
        var signup = await _factory.CreateClient().PostAsJsonAsync("api/auth/signup",
            new SignupRequest("Flow Patient", email, "Statistics", "pass-1234", UserRole.Patient));
        signup.EnsureSuccessStatusCode();
        return await _factory.SignedInClientAsync(email, "pass-1234");
    }

    private static async Task<SupportRequestDto> SendRequestAsync(HttpClient patient, int patientId, int supporterId)
    {
        var response = await patient.PostAsJsonAsync("api/support-requests",
            new CreateSupportRequest(patientId, supporterId, CareFocus.GeneralHealth, "  I'd appreciate hearing about your experience.  ", "Weekly"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SupportRequestDto>())!;
    }

    [Fact]
    public async Task Sending_a_request_notifies_the_supporter()
    {
        var (patient, me) = await NewPatientAsync();
        var (supporter, supporterUser) = await _factory.SignedInClientAsync(ConsuCareFactory.SupporterEmail);

        var request = await SendRequestAsync(patient, me.Id, supporterUser.Id);

        Assert.Equal(SupportRequestStatus.Pending, request.Status);
        Assert.Equal("I'd appreciate hearing about your experience.", request.Message);

        var notifications = await supporter.GetFromJsonAsync<List<NotificationDto>>($"api/notifications/{supporterUser.Id}");
        Assert.Contains(notifications!, n => n.Kind == NotificationKind.RequestReceived && n.Body.Contains("Flow Patient"));
    }

    [Fact]
    public async Task Accepting_creates_a_peer_support_connection_and_notifies_the_patient()
    {
        var (patient, me) = await NewPatientAsync();
        var (supporter, supporterUser) = await _factory.SignedInClientAsync(ConsuCareFactory.SupporterEmail);
        var request = await SendRequestAsync(patient, me.Id, supporterUser.Id);

        var accept = await supporter.PostAsync($"api/support-requests/{request.Id}/accept", null);
        Assert.Equal(HttpStatusCode.NoContent, accept.StatusCode);

        var connections = await patient.GetFromJsonAsync<List<SupportConnectionDto>>($"api/support-connections/patient/{me.Id}");
        Assert.Contains(connections!, m => m.PartnerId == supporterUser.Id && m.Status == ConnectionStatus.Active);

        var notifications = await patient.GetFromJsonAsync<List<NotificationDto>>($"api/notifications/{me.Id}");
        Assert.Contains(notifications!, n => n.Kind == NotificationKind.RequestAccepted);
    }

    [Fact]
    public async Task A_support_request_cannot_be_accepted_twice()
    {
        var (patient, me) = await NewPatientAsync();
        var (supporter, supporterUser) = await _factory.SignedInClientAsync(ConsuCareFactory.SupporterEmail);
        var request = await SendRequestAsync(patient, me.Id, supporterUser.Id);

        await supporter.PostAsync($"api/support-requests/{request.Id}/accept", null);
        var second = await supporter.PostAsync($"api/support-requests/{request.Id}/accept", null);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Declining_does_not_create_a_support_connection()
    {
        var (patient, me) = await NewPatientAsync();
        var (supporter, supporterUser) = await _factory.SignedInClientAsync(ConsuCareFactory.SupporterEmail);
        var request = await SendRequestAsync(patient, me.Id, supporterUser.Id);

        var decline = await supporter.PostAsync($"api/support-requests/{request.Id}/decline", null);
        Assert.Equal(HttpStatusCode.NoContent, decline.StatusCode);

        var connections = await patient.GetFromJsonAsync<List<SupportConnectionDto>>($"api/support-connections/patient/{me.Id}");
        Assert.DoesNotContain(connections!, m => m.PartnerId == supporterUser.Id && m.Status == ConnectionStatus.Active);
    }

    [Fact]
    public async Task Requesting_an_unknown_supporter_returns_not_found()
    {
        var (patient, me) = await NewPatientAsync();
        var response = await patient.PostAsJsonAsync("api/support-requests",
            new CreateSupportRequest(me.Id, 999_999, CareFocus.GeneralHealth, "Hi", "Weekly"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
