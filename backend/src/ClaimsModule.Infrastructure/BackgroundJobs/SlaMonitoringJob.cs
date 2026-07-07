using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ClaimsModule.Infrastructure.BackgroundJobs;

/// <summary>
/// Recurring job (every 15 minutes) flagging Draft/Open claims not updated in 48+ hours - FRS 12.2.
/// Writes an audit entry only; never changes claim status or sends notifications.
/// </summary>
public class SlaMonitoringJob(IClaimsDbContext context, IAuditLogService auditLog)
{
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromHours(48);
    private static readonly TimeSpan BreachThrottle = TimeSpan.FromHours(24);

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTimeOffset.UtcNow - StaleThreshold;
        var throttleCutoff = DateTimeOffset.UtcNow - BreachThrottle;

        var staleClaims = await context.Claims
            .Where(c => c.Status == ClaimStatus.Draft || c.Status == ClaimStatus.Open)
            .Where(c => (c.UpdatedAt ?? c.CreatedAt) < cutoff)
            .ToListAsync(cancellationToken);

        foreach (var claim in staleClaims)
        {
            // Idempotency: don't add a new breach entry if one fired within the last 24h.
            var lastBreachAt = await context.ClaimAuditLog
                .Where(a => a.ClaimId == claim.ClaimId && a.EventType == "SLA_BREACH_DETECTED")
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => (DateTimeOffset?)a.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastBreachAt.HasValue && lastBreachAt.Value > throttleCutoff)
            {
                continue;
            }

            await auditLog.LogAsync(claim.ClaimId, "SLA_BREACH_DETECTED",
                "Claim has not been updated in 48 hours.", cancellationToken: cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
