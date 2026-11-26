using HelpDesk.Domain.Entities;

namespace HelpDesk.UnitTests.Domain;

public sealed class UserTests
{
    [Fact]
    public void Create_TrimsNameAndNormalizesEmail()
    {
        var createdAt = new DateTime(2026, 11, 25, 0, 0, 0, DateTimeKind.Utc);

        var user = User.Create("  Ana Torres  ", "  Ana.Torres@Example.COM ", createdAt);

        Assert.Equal("Ana Torres", user.Name);
        Assert.Equal("ana.torres@example.com", user.Email);
        Assert.Equal(createdAt, user.CreatedAt);
    }

    [Theory]
    [InlineData("ANA@EXAMPLE.COM", "ana@example.com")]
    [InlineData("  ana@example.com  ", "ana@example.com")]
    public void NormalizeEmail_ProducesTheSameValueForEquivalentEmails(string input, string expected)
    {
        Assert.Equal(expected, User.NormalizeEmail(input));
    }
}
