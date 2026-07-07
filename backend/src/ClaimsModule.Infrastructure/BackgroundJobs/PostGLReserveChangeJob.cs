using ClaimsModule.Application.Common.Interfaces;
using ClaimsModule.Domain.Entities;
using ClaimsModule.Domain.Enums;
using Hangfire;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClaimsModule.Infrastructure.BackgroundJobs;

/// <summary>
/// Simulates posting an approved reserve change to a general ledger - FRS 12.1. Enqueued
/// immediately after a reserve is auto-approved or manually approved (see IBackgroundJobEnqueuer).
/// </summary>
public class PostGLReserveChangeJob(
    IClaimsDbContext context,
    IAuditLogService auditLog,
    ILogger<PostGLReserveChangeJob> logger)
{
    private const int MaxAttempts = 5;

    [AutomaticRetry(Attempts = MaxAttempts, DelaysInSeconds = [5, 15, 30, 60, 120])]
    public async Task ExecuteAsync(Guid reserveHistoryId, PerformContext? performContext, CancellationToken cancellationToken)
    {
        var history = await context.ReserveHistory
            .FirstOrDefaultAsync(h => h.ReserveHistoryId == reserveHistoryId, cancellationToken);

        if (history is null)
        {
            logger.LogWarning("PostGLReserveChangeJob: ReserveHistory {Id} not found - skipping", reserveHistoryId);
            return;
        }

        // Re-entrancy guard (FRS 12.1): a re-delivered job after a successful run must no-op,
        // and must be checked before any write.
        if (history.PostingStatus == ReservePostingStatus.Posted)
        {
            return;
        }

        try
        {
            await auditLog.LogAsync(history.ClaimId, "GL_POSTING_SIMULATED",
                $"Simulated GL journal entry: DR Change in Outstanding Reserves / CR Outstanding Loss Reserves, Amount = {history.Amount:N2}",
                newValue: new { Dr = "Change in Outstanding Reserves", Cr = "Outstanding Loss Reserves", history.Amount },
                relatedEntityId: history.ReserveHistoryId, relatedEntityType: nameof(ReserveHistory),
                cancellationToken: cancellationToken);

            history.PostingStatus = ReservePostingStatus.Posted;
            history.PostingJobId = performContext?.BackgroundJob.Id;

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (IsFinalAttempt(performContext))
        {
            // Last allowed attempt failed - record the terminal failure ourselves instead of
            // rethrowing, which would also mark the Hangfire job itself as Failed.
            history.PostingStatus = ReservePostingStatus.Failed;

            await auditLog.LogAsync(history.ClaimId, "GL_POSTING_FAILED",
                $"GL posting failed after all retries: {ex.Message}",
                relatedEntityId: history.ReserveHistoryId, relatedEntityType: nameof(ReserveHistory),
                cancellationToken: cancellationToken);

            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private static bool IsFinalAttempt(PerformContext? context)
    {
        if (context is null)
        {
            return true; // no Hangfire context (e.g. direct invocation) - treat as final
        }

        var retryCount = context.GetJobParameter<int?>("RetryCount") ?? 0;
        return retryCount >= MaxAttempts - 1;
    }
}
