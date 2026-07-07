export type ReserveComponentType = 'Indemnity' | 'Expense' | 'ALAE' | 'SubrogationRecoverable';
export type ReserveComponentStatus = 'Active' | 'Closed';
export type ReserveTransactionType = 'Add' | 'Adjust' | 'Reverse';
export type ReserveApprovalStatus = 'AutoApproved' | 'PendingApproval' | 'Approved' | 'Rejected' | 'Cancelled';
export type ReservePostingStatus = 'Pending' | 'Posted' | 'Failed' | 'Cancelled';

export interface ReserveComponent {
  reserveComponentId: string;
  component: ReserveComponentType;
  currentAmount: number;
  status: ReserveComponentStatus;
  pendingAmount: number;
}

export interface ReserveHistoryEntry {
  reserveHistoryId: string;
  reserveComponentId: string;
  transactionType: ReserveTransactionType;
  amount: number;
  previousBalance: number;
  newBalance: number;
  approvalStatus: ReserveApprovalStatus;
  approvedByUserId: string | null;
  approvedAt: string | null;
  rejectedByUserId: string | null;
  rejectedAt: string | null;
  rejectionReason: string | null;
  changeReason: string;
  postingStatus: ReservePostingStatus;
  idempotencyKey: string;
  changeSequence: number;
  submittedByUserId: string | null;
  createdAt: string;
}

export interface ReserveSummary {
  components: ReserveComponent[];
  history: ReserveHistoryEntry[];
  totalApproved: number;
  aggregateLimitOverride: boolean;
}

export interface CreateReserveRequest {
  component: ReserveComponentType;
  amount: number;
  changeReason: string;
  transactionType: ReserveTransactionType;
}
