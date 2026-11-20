import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DuplicateCheckInput, DuplicateCheckResponse, RiskReport } from './models';
import { API } from './api';

/** Duplicate/fraud detection and buyer-side market checks. */
@Injectable({ providedIn: 'root' })
export class TrustService {
  private http = inject(HttpClient);

  checkDraft(input: DuplicateCheckInput): Observable<DuplicateCheckResponse> {
    return this.http.post<DuplicateCheckResponse>(`${API}/trust/duplicate-check`, input);
  }

  report(propertyId: number): Observable<RiskReport> {
    return this.http.get<RiskReport>(`${API}/trust/property/${propertyId}`);
  }
}
