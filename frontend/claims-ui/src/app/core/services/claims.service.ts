import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AuditLogEntry,
  ClaimDetail,
  ClaimDocument,
  ClaimParty,
  ClaimStatus,
  ClaimSummary,
  CreateClaimRequest,
  CreatePartyRequest,
  ListClaimsQueryParams,
  PagedResult
} from '../models/claim.model';

const BASE = `${environment.apiBaseUrl}/claims`;

@Injectable({ providedIn: 'root' })
export class ClaimsService {
  private readonly http = inject(HttpClient);

  create(request: CreateClaimRequest): Observable<ClaimDetail> {
    return this.http.post<ClaimDetail>(BASE, request);
  }

  list(params: ListClaimsQueryParams): Observable<PagedResult<ClaimSummary>> {
    let httpParams = new HttpParams();

    for (const status of params.statuses ?? []) {
      httpParams = httpParams.append('statuses', status);
    }
    if (params.dateFrom) httpParams = httpParams.set('dateFrom', params.dateFrom);
    if (params.dateTo) httpParams = httpParams.set('dateTo', params.dateTo);
    if (params.assignedHandlerId) httpParams = httpParams.set('assignedHandlerId', params.assignedHandlerId);
    if (params.causeOfLossCode) httpParams = httpParams.set('causeOfLossCode', params.causeOfLossCode);
    if (params.policyId) httpParams = httpParams.set('policyId', params.policyId);
    if (params.search) httpParams = httpParams.set('search', params.search);
    httpParams = httpParams.set('page', params.page ?? 1);
    httpParams = httpParams.set('pageSize', params.pageSize ?? 20);

    return this.http.get<PagedResult<ClaimSummary>>(BASE, { params: httpParams });
  }

  getById(claimId: string): Observable<ClaimDetail> {
    return this.http.get<ClaimDetail>(`${BASE}/${claimId}`);
  }

  transitionStatus(claimId: string, targetStatus: ClaimStatus, reason?: string): Observable<ClaimDetail> {
    return this.http.put<ClaimDetail>(`${BASE}/${claimId}/status`, { targetStatus, reason });
  }

  updateNotes(claimId: string, notes: string | null): Observable<ClaimDetail> {
    return this.http.put<ClaimDetail>(`${BASE}/${claimId}/notes`, { notes });
  }

  addParty(claimId: string, request: CreatePartyRequest): Observable<ClaimParty> {
    return this.http.post<ClaimParty>(`${BASE}/${claimId}/parties`, request);
  }

  removeParty(claimId: string, partyId: string): Observable<void> {
    return this.http.delete<void>(`${BASE}/${claimId}/parties/${partyId}`);
  }

  getAuditLog(claimId: string, page = 1, pageSize = 50): Observable<PagedResult<AuditLogEntry>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResult<AuditLogEntry>>(`${BASE}/${claimId}/audit`, { params });
  }

  getDocuments(claimId: string): Observable<ClaimDocument[]> {
    return this.http.get<ClaimDocument[]>(`${BASE}/${claimId}/documents`);
  }

  uploadDocument(claimId: string, file: File, documentType: string, notes?: string): Observable<ClaimDocument> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('documentType', documentType);
    if (notes) formData.append('notes', notes);
    return this.http.post<ClaimDocument>(`${BASE}/${claimId}/documents`, formData);
  }
}
