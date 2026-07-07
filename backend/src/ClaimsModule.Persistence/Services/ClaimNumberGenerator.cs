using ClaimsModule.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Persistence.Services;

/// <summary>
/// Atomic per-year sequence via UPDATE ... OUTPUT INSERTED against ClaimNumberCounters (BR-C-04).
/// The UPDATE statement itself is what SQL Server serializes on - concurrent callers block on the
/// row lock and each gets a distinct sequence value, so no explicit transaction is required here.
/// </summary>
public class ClaimNumberGenerator(IClaimsDbContext context) : IClaimNumberGenerator
{
    public async Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var year = DateTimeOffset.UtcNow.Year;

        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             IF NOT EXISTS (SELECT 1 FROM ClaimNumberCounters WHERE Year = {year})
                 INSERT INTO ClaimNumberCounters (Year, LastSequence) VALUES ({year}, 0)
             """,
            cancellationToken);

        // UPDATE ... OUTPUT is not composable SQL - ToListAsync materialises it directly instead
        // of EF Core trying to wrap it as a subquery (which SingleAsync would attempt).
        var results = await context.Database
            .SqlQueryRaw<int>(
                "UPDATE ClaimNumberCounters SET LastSequence = LastSequence + 1 OUTPUT INSERTED.LastSequence WHERE Year = {0}",
                year)
            .ToListAsync(cancellationToken);

        var sequence = results.Single();

        return $"CLM-{year}-{sequence:D7}";
    }
}
