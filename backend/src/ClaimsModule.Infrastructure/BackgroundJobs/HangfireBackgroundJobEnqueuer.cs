using ClaimsModule.Application.Common.Interfaces;
using Hangfire;

namespace ClaimsModule.Infrastructure.BackgroundJobs;

public class HangfireBackgroundJobEnqueuer(IBackgroundJobClient client) : IBackgroundJobEnqueuer
{
    public void EnqueueGlPosting(Guid reserveHistoryId)
    {
        // The null PerformContext argument is a Hangfire convention - it's a special-cased
        // parameter type that Hangfire injects itself at execution time, not part of the call args.
        client.Enqueue<PostGLReserveChangeJob>(job => job.ExecuteAsync(reserveHistoryId, null, CancellationToken.None));
    }
}
