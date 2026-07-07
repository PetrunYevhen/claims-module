export type ClaimStatus =
  | 'Draft'
  | 'Open'
  | 'UnderInvestigation'
  | 'PendingPayment'
  | 'Closed'
  | 'Reopened'
  | 'Withdrawn';

export type ClaimSeverity = 'Minor' | 'Standard' | 'Critical' | 'Catastrophic';
export type PartyRole = 'Claimant' | 'Insured' | 'ThirdParty' | 'Witness' | 'Attorney';
export type PartyType = 'Person' | 'Company';
export type AssetType = 'Vehicle' | 'Property' | 'Person' | 'Equipment' | 'Other';

export interface ClaimSummary {
  claimId: string;
  claimNumber: string;
  policyNumber: string | null;
  clientName: string | null;
  lossDate: string | null;
  causeOfLossCode: string | null;
  status: ClaimStatus;
  totalReserves: number;
}

export interface LossEvent {
  lossEventId: string;
  lossDate: string;
  lossDescription: string;
  lossLocation: string | null;
  causeOfLossCode: string;
  estimatedLossAmount: number | null;
  reportDate: string;
  policeReportNumber: string | null;
}

export interface ClaimParty {
  claimPartyId: string;
  partyRole: PartyRole;
  partyType: PartyType;
  firstName: string | null;
  lastName: string | null;
  companyName: string | null;
  email: string | null;
  phone: string | null;
  isActive: boolean;
}

export interface ClaimRiskObject {
  claimRiskObjectId: string;
  assetType: AssetType;
  assetDescription: string;
  damageDescription: string | null;
  isPrimary: boolean;
  assetReference: string | null;
}

export interface ValidationIssue {
  code: string;
  severity: string;
  message: string;
}

export interface ClaimDetail {
  claimId: string;
  claimNumber: string;
  policyId: string | null;
  policyNumber: string | null;
  clientName: string | null;
  status: ClaimStatus;
  severity: ClaimSeverity | null;
  reportedDate: string;
  assignedHandlerId: string | null;
  closedAt: string | null;
  closureReason: string | null;
  notes: string | null;
  lossEvent: LossEvent | null;
  parties: ClaimParty[];
  riskObjects: ClaimRiskObject[];
  validationIssues: ValidationIssue[];
  validNextStatuses: ClaimStatus[];
}

export interface AuditLogEntry {
  auditLogId: string;
  eventType: string;
  description: string;
  oldValue: string | null;
  newValue: string | null;
  relatedEntityId: string | null;
  relatedEntityType: string | null;
  correlationId: string | null;
  createdAt: string;
  createdByUserId: string | null;
}

export interface ClaimDocument {
  claimDocumentId: string;
  documentType: string;
  documentName: string;
  contentType: string;
  fileSizeBytes: number;
  uploadedAt: string;
  uploadedByUserId: string | null;
  notes: string | null;
  downloadUrl: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface CreatePartyRequest {
  partyRole: PartyRole;
  partyType: PartyType;
  firstName?: string | null;
  lastName?: string | null;
  companyName?: string | null;
  email?: string | null;
  phone?: string | null;
}

export interface CreateRiskObjectRequest {
  assetType: AssetType;
  assetDescription: string;
  damageDescription?: string | null;
  isPrimary: boolean;
  assetReference?: string | null;
}

export interface CreateClaimRequest {
  policyId?: string | null;
  lossDate: string;
  lossDescription: string;
  lossLocation?: string | null;
  causeOfLossCode: string;
  estimatedLossAmount?: number | null;
  policeReportNumber?: string | null;
  parties: CreatePartyRequest[];
  riskObjects: CreateRiskObjectRequest[];
}

export interface ListClaimsQueryParams {
  statuses?: ClaimStatus[];
  dateFrom?: string;
  dateTo?: string;
  assignedHandlerId?: string;
  causeOfLossCode?: string;
  policyId?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}
