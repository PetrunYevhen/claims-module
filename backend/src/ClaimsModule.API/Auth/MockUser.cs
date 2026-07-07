namespace ClaimsModule.API.Auth;

public record MockUser(Guid UserId, string Username, string Password, string Name, string Role);

/// <summary>
/// Hardcoded accounts for the FRS 11.4 mock auth service - three roles, matching FRS section 3.
/// Passwords are deliberately trivial; this is a pet-project simulation, not real auth.
/// </summary>
public static class MockUsers
{
    public static readonly IReadOnlyList<MockUser> All =
    [
        new(new Guid("a1a1a1a1-0000-0000-0000-000000000001"), "handler@claims.local", "password123", "Olena Handler", "handler"),
        new(new Guid("a1a1a1a1-0000-0000-0000-000000000002"), "supervisor@claims.local", "password123", "Oleh Supervisor", "supervisor"),
        new(new Guid("a1a1a1a1-0000-0000-0000-000000000003"), "manager@claims.local", "password123", "Maria Manager", "manager")
    ];

    public static MockUser? FindByCredentials(string username, string password) =>
        All.FirstOrDefault(u =>
            u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) && u.Password == password);
}
