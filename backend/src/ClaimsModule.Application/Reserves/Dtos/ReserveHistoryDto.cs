using ClaimsModule.Domain.Enums;

namespace ClaimsModule.Application.Reserves.Dtos;

public class ReserveHistoryDto
{
    public Guid ReserveHistoryId { get; set; }
    public Guid ReserveComponentId { get; set; }
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
    public string IdempotencyKey { get; set; } = null!;
    public int ChangeSequence { get; set; }
    public Guid? SubmittedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
