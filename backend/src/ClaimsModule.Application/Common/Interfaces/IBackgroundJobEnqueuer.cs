namespace ClaimsModule.Application.Common.Interfaces;

/// <summary>
/// Application-owned abstraction over the background job runner (Hangfire in Infrastructure) -
/// keeps command handlers free of a direct Hangfire dependency.
/// </summary>
public interface IBackgroundJobEnqueuer
{
    /// <summary>Enqueues PostGLReserveChangeJob for a reserve transaction that was just (auto-)approved.</summary>
    void EnqueueGlPosting(Guid reserveHistoryId);
}
