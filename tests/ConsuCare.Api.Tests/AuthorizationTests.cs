using System.Net;
using System.Net.Http.Headers;

namespace ConsuCare.Api.Tests;

public class AuthorizationTests : IClassFixture<ConsuCareFactory>
{
    private readonly ConsuCareFactory _factory;
    public AuthorizationTests(ConsuCareFactory factory) => _factory = factory;

    [Theory]
    [InlineData("api/supporters")]
    [InlineData("api/goals/patient/1")]
    [InlineData("api/notifications/1")]
    [InlineData("api/messages/conversations/1")]
    [InlineData("api/dashboard/patient/1")]
    [InlineData("api/admin/verifications")]
    public async Task Protected_endpoints_require_a_token(string url)
    {
        var response = await _factory.CreateClient().GetAsync(url);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_forged_token_is_rejected()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not.a.real-token");

        var response = await client.GetAsync("api/supporters");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Signed_in_patient_can_browse_supporters()
    {
        var (client, _) = await _factory.SignedInClientAsync(ConsuCareFactory.PatientEmail);
        var response = await client.GetAsync("api/supporters");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData(ConsuCareFactory.PatientEmail)]
    [InlineData(ConsuCareFactory.SupporterEmail)]
    public async Task Only_admins_can_see_verifications(string email)
    {
        var (client, _) = await _factory.SignedInClientAsync(email);
        var response = await client.GetAsync("api/admin/verifications");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_see_verifications()
    {
        var (client, _) = await _factory.SignedInClientAsync(ConsuCareFactory.AdminEmail);
        var response = await client.GetAsync("api/admin/verifications");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
