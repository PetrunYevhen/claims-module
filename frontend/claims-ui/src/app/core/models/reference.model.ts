export interface CauseOfLossCode {
  code: string;
  name: string;
  perilCategory: string;
  sortOrder: number;
}

export interface Policy {
  policyId: string;
  policyNumber: string;
  clientName: string;
  effectiveDate: string;
  expirationDate: string;
  status: string;
  coverageTypes: string;
}

export interface ClaimStatusInfo {
  status: string;
  validNextStatuses: string[];
}
