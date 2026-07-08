using ClaimsModule.Domain.Common;
using ClaimsModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClaimsModule.Tests.Persistence;

public class AppendOnlyAuditTests
{
    private static ClaimAuditLog NewEntry() => new()
    {
        AuditLogId = Guid.NewGuid(),
        ClaimId = Guid.NewGuid(),
        OrganisationId = SeedConstants.OrganisationId,
        EventType = "CLAIM_CREATED",
        Description = "created",
        CreatedAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task Inserting_an_audit_entry_is_allowed()
    {
        using var context = TestDb.Create();
        context.ClaimAuditLog.Add(NewEntry());

        await context.SaveChangesAsync(); // no throw
        Assert.Equal(1, await context.ClaimAuditLog.CountAsync());
    }

    [Fact]
    public async Task Updating_an_audit_entry_is_rejected() // BR-A-01
    {
        using var context = TestDb.Create();
        var entry = NewEntry();
        context.ClaimAuditLog.Add(entry);
        await context.SaveChangesAsync();

        entry.Description = "tampered";

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        Assert.Contains("append-only", ex.Message);
    }

    [Fact]
    public async Task Deleting_an_audit_entry_is_rejected() // BR-A-01
    {
        using var context = TestDb.Create();
        var entry = NewEntry();
        context.ClaimAuditLog.Add(entry);
        await context.SaveChangesAsync();

        context.ClaimAuditLog.Remove(entry);

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }
}
