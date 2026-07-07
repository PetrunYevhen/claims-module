using ClaimsModule.Domain.Common;
using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Domain.Entities;

/// <summary>
/// Append-only ledger of reserve transactions (event-sourcing pattern - see FRS 6.6).
/// Never updated after insert; the current balance is the sum of transactions for a component.
/// </summary>
public class ReserveHistory : AppendOnlyEntity
{
    public Guid ReserveHistoryId { get; set; }
    public Guid ReserveComponentId { get; set; }
    public Guid ClaimId { get; set; }
    public ReserveTransactionType TransactionType { get; set; }
    public decimal Amount { get; set; }
    public decimal PreviousBalance { get; set; }
    public decimal NewBalance { get; set; }
    public ReserveApprovalStatus ApprovalStatus { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public Guid? RejectedByUserId { get; set; }
    public DateTimeOffset? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string ChangeReason { get; set; } = null!;
    public ReservePostingStatus PostingStatus { get; set; }
    public string? PostingJobId { get; set; }
    public string IdempotencyKey { get; set; } = null!;
    public int ChangeSequence { get; set; }
    public Guid? SubmittedByUserId { get; set; }

    public ClaimReserveComponent ReserveComponent { get; set; } = null!;
}
