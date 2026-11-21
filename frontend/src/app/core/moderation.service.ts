import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API } from './api';
import { Report, Review } from './models';

@Injectable({ providedIn: 'root' })
export class ModerationService {
  private http = inject(HttpClient);

  pendingReviews(): Observable<Review[]> {
    return this.http.get<Review[]>(`${API}/moderation/reviews`);
  }

  openReports(): Observable<Report[]> {
    return this.http.get<Report[]>(`${API}/moderation/reports`);
  }

  moderate(id: number, decision: 'approve' | 'reject'): Observable<void> {
    return this.http.post<void>(`${API}/moderation/reviews/${id}/${decision}`, {});
  }

  resolve(id: number): Observable<void> {
    return this.http.post<void>(`${API}/moderation/reports/${id}/resolve`, {});
  }

  report(input: { propertyListingId: number; reason: string }): Observable<Report> {
    return this.http.post<Report>(`${API}/reports`, input);
  }
}
