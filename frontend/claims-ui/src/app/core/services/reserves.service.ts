import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateReserveRequest, ReserveHistoryEntry, ReserveSummary } from '../models/reserve.model';

const base = (claimId: string) => `${environment.apiBaseUrl}/claims/${claimId}/reserves`;

@Injectable({ providedIn: 'root' })
export class ReservesService {
  private readonly http = inject(HttpClient);

  get(claimId: string): Observable<ReserveSummary> {
    return this.http.get<ReserveSummary>(base(claimId));
  }

  create(claimId: string, request: CreateReserveRequest): Observable<ReserveHistoryEntry> {
    return this.http.post<ReserveHistoryEntry>(base(claimId), request);
  }

  approve(claimId: string, reserveHistoryId: string): Observable<ReserveHistoryEntry> {
    return this.http.post<ReserveHistoryEntry>(`${base(claimId)}/${reserveHistoryId}/approve`, {});
  }

  reject(claimId: string, reserveHistoryId: string, rejectionReason: string): Observable<ReserveHistoryEntry> {
    return this.http.post<ReserveHistoryEntry>(`${base(claimId)}/${reserveHistoryId}/reject`, { rejectionReason });
  }

  retract(claimId: string, reserveHistoryId: string): Observable<void> {
    return this.http.post<void>(`${base(claimId)}/${reserveHistoryId}/retract`, {});
  }

  overrideAggregateLimit(claimId: string, reason: string): Observable<void> {
    return this.http.post<void>(`${base(claimId)}/override-limit`, { reason });
  }
}
