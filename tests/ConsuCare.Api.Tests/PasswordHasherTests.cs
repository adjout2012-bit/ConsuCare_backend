using ConsuCare.Api.Services;

namespace ConsuCare.Api.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void Verify_accepts_the_original_password()
    {
        var stored = PasswordHasher.Hash("correct horse");
        Assert.True(PasswordHasher.Verify("correct horse", stored));
    }

    [Fact]
    public void Verify_rejects_a_wrong_password()
    {
        var stored = PasswordHasher.Hash("correct horse");
        Assert.False(PasswordHasher.Verify("Correct horse", stored));
    }

    [Fact]
    public void Hash_uses_a_new_salt_each_time()
    {
        Assert.NotEqual(PasswordHasher.Hash("same"), PasswordHasher.Hash("same"));
    }

    [Fact]
    public void Hash_never_contains_the_plain_password()
    {
        Assert.DoesNotContain("s3cret-pass", PasswordHasher.Hash("s3cret-pass"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("plain-text-password")]
    [InlineData("abc.def")]
    public void Verify_rejects_malformed_stored_values(string stored)
    {
        Assert.False(PasswordHasher.Verify("anything", stored));
    }
}
