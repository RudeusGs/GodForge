using GodForge.Domain.Entities.Identity;

namespace GodForge.UnitTests.Domain.Identity;

public sealed class UserTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 6, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_EmailLongerThanPersistenceLimit_ThrowsArgumentException()
    {
        var email = new string('a', User.MaxEmailLength - "@example.com".Length + 1) + "@example.com";

        var exception = Assert.Throws<ArgumentException>(() =>
            User.Create(email, "Test User", "password-hash", Now));

        Assert.Equal("email", exception.ParamName);
    }

    [Fact]
    public void Create_ValuesAtPersistenceLimits_Succeeds()
    {
        var email = new string('a', User.MaxEmailLength - "@example.com".Length) + "@example.com";
        var displayName = new string('d', User.MaxDisplayNameLength);
        var passwordHash = new string('h', User.MaxPasswordHashLength);

        var user = User.Create(email, displayName, passwordHash, Now);

        Assert.Equal(User.MaxEmailLength, user.Email.Length);
        Assert.Equal(User.MaxDisplayNameLength, user.DisplayName.Length);
        Assert.Equal(User.MaxPasswordHashLength, user.PasswordHash.Length);
    }

    [Fact]
    public void UpdatePassword_HashLongerThanPersistenceLimit_ThrowsArgumentException()
    {
        var user = User.Create("user@example.com", "Test User", "password-hash", Now);

        var exception = Assert.Throws<ArgumentException>(() =>
            user.UpdatePassword(new string('h', User.MaxPasswordHashLength + 1), Now));

        Assert.Equal("passwordHash", exception.ParamName);
    }
}
