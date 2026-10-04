using System.Net;
using System.Net.Http.Json;
using ConsuCare.Shared.Dtos;
using ConsuCare.Shared.Models;

namespace ConsuCare.Api.Tests;

public class AuthTests : IClassFixture<ConsuCareFactory>
{
    private readonly ConsuCareFactory _factory;
    public AuthTests(ConsuCareFactory factory) => _factory = factory;

    [Theory]
    [InlineData(ConsuCareFactory.PatientEmail, UserRole.Patient)]
    [InlineData(ConsuCareFactory.SupporterEmail, UserRole.Supporter)]
    [InlineData(ConsuCareFactory.AdminEmail, UserRole.Admin)]
    public async Task Demo_accounts_can_log_in(string email, UserRole role)
    {
        var auth = await _factory.LoginAsync(email);

        Assert.Equal(email, auth.User.Email);
        Assert.Equal(role, auth.User.Role);
        Assert.False(string.IsNullOrWhiteSpace(auth.Token));
    }

    [Fact]
    public async Task Login_ignores_email_case_and_whitespace()
    {
        var auth = await _factory.LoginAsync("  AMARA@Patient.dev ");
        Assert.Equal(ConsuCareFactory.PatientEmail, auth.User.Email);
    }

    [Fact]
    public async Task Wrong_password_is_rejected()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("api/auth/login",
            new LoginRequest(ConsuCareFactory.PatientEmail, "not-the-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_email_is_rejected()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("api/auth/login",
            new LoginRequest("nobody@example.com", "whatever"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task New_patient_can_sign_up_and_then_log_in()
    {
        var email = $"patient-{Guid.NewGuid():N}@test.dev";
        var signup = await _factory.CreateClient().PostAsJsonAsync("api/auth/signup",
            new SignupRequest("Test Patient", email, "Newly diagnosed", "pass-1234", UserRole.Patient));

        Assert.Equal(HttpStatusCode.OK, signup.StatusCode);
        var created = await signup.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal(UserRole.Patient, created!.User.Role);

        var login = await _factory.LoginAsync(email, "pass-1234");
        Assert.Equal(created.User.Id, login.User.Id);
    }

    [Fact]
    public async Task Signing_up_with_an_existing_email_returns_conflict()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("api/auth/signup",
            new SignupRequest("Copy", ConsuCareFactory.PatientEmail, "Anything", "pass-1234", UserRole.Patient));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task New_supporter_starts_private_and_pending_verification()
    {
        var email = $"supporter-{Guid.NewGuid():N}@test.dev";
        var signup = await _factory.CreateClient().PostAsJsonAsync("api/auth/signup",
            new SignupRequest("Test Supporter", email, "", "pass-1234", UserRole.Supporter,
                "Type 1 Diabetes", 2018));
        signup.EnsureSuccessStatusCode();
        var auth = await signup.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.False(auth!.User.IsPublic);

        var (client, _) = await _factory.SignedInClientAsync(ConsuCareFactory.PatientEmail);
        var supporters = await client.GetFromJsonAsync<List<SupporterCardDto>>("api/supporters");
        Assert.DoesNotContain(supporters!, m => m.Name == "Test Supporter");

        var (adminClient, _) = await _factory.SignedInClientAsync(ConsuCareFactory.AdminEmail);
        var verifications = await adminClient.GetFromJsonAsync<List<VerificationDto>>("api/admin/verifications");
        var profile = Assert.Single(verifications!, item => item.Name == "Test Supporter");
        Assert.Equal("Type 1 Diabetes", profile.ConditionType);
        Assert.Equal(2018, profile.DiagnosisYear);
        await adminClient.PostAsync($"api/admin/verifications/{profile.ProfileId}/approve", null);
        supporters = await client.GetFromJsonAsync<List<SupporterCardDto>>("api/supporters");
        Assert.DoesNotContain(supporters!, m => m.Name == "Test Supporter");
    }

    [Fact]
    public async Task Supporter_signup_requires_condition_and_valid_diagnosis_year()
    {
        var email = $"supporter-{Guid.NewGuid():N}@test.dev";
        var signup = await _factory.CreateClient().PostAsJsonAsync("api/auth/signup",
            new SignupRequest("Test Supporter", email, "", "pass-1234", UserRole.Supporter));

        Assert.Equal(HttpStatusCode.BadRequest, signup.StatusCode);
    }

    [Fact]
    public async Task Supporter_health_details_require_opt_in_and_can_be_hidden_again()
    {
        var email = $"supporter-{Guid.NewGuid():N}@test.dev";
        var signup = await _factory.CreateClient().PostAsJsonAsync("api/auth/signup",
            new SignupRequest("Sharing Supporter", email, "Peer support", "pass-1234", UserRole.Supporter,
                "Crohn's Disease", 2018, ShareHealthDetails: true));
        signup.EnsureSuccessStatusCode();
        var auth = await signup.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.True(auth!.User.IsPublic);

        var (admin, _) = await _factory.SignedInClientAsync(ConsuCareFactory.AdminEmail);
        var profiles = await admin.GetFromJsonAsync<List<VerificationDto>>("api/admin/verifications");
        var profile = Assert.Single(profiles!, item => item.Name == "Sharing Supporter");
        await admin.PostAsync($"api/admin/verifications/{profile.ProfileId}/approve", null);

        var (patient, _) = await _factory.SignedInClientAsync(ConsuCareFactory.PatientEmail);
        var visible = await patient.GetFromJsonAsync<List<SupporterCardDto>>("api/supporters");
        Assert.Contains(visible!, item => item.Name == "Sharing Supporter");

        var (supporter, user) = await _factory.SignedInClientAsync(email, "pass-1234");
        var privacy = await supporter.PutAsJsonAsync($"api/users/{user.Id}/preferences",
            new UpdatePreferencesRequest(true, true, true, false, false));
        privacy.EnsureSuccessStatusCode();

        visible = await patient.GetFromJsonAsync<List<SupporterCardDto>>("api/supporters");
        Assert.DoesNotContain(visible!, item => item.Name == "Sharing Supporter");
        var profileResponse = await patient.GetAsync($"api/supporters/{user.Id}");
        Assert.Equal(HttpStatusCode.NotFound, profileResponse.StatusCode);
    }
}
