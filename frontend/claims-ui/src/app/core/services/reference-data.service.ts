import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CauseOfLossCode, ClaimStatusInfo, Policy } from '../models/reference.model';

@Injectable({ providedIn: 'root' })
export class ReferenceDataService {
  private readonly http = inject(HttpClient);

  getCauseOfLossCodes(perilCategory?: string): Observable<CauseOfLossCode[]> {
    const params = perilCategory ? new HttpParams().set('perilCategory', perilCategory) : undefined;
    return this.http.get<CauseOfLossCode[]>(`${environment.apiBaseUrl}/reference/cause-of-loss-codes`, { params });
  }

  getClaimStatuses(): Observable<ClaimStatusInfo[]> {
    return this.http.get<ClaimStatusInfo[]>(`${environment.apiBaseUrl}/reference/claim-statuses`);
  }

  searchPolicies(query?: string): Observable<Policy[]> {
    const params = query ? new HttpParams().set('q', query) : undefined;
    return this.http.get<Policy[]>(`${environment.apiBaseUrl}/policies/search`, { params });
  }
}
