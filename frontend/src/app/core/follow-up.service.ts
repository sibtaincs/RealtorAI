import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { FollowUp } from './models';
import { API } from './api';

@Injectable({ providedIn: 'root' })
export class FollowUpService {
  private http = inject(HttpClient);

  forLead(leadId: number): Observable<FollowUp[]> {
    return this.http.get<FollowUp[]>(`${API}/followups/lead/${leadId}`);
  }

  create(leadId: number): Observable<FollowUp> {
    return this.http.post<FollowUp>(`${API}/followups/lead/${leadId}`, {});
  }

  aiReply(leadId: number, message: string): Observable<FollowUp> {
    return this.http.post<FollowUp>(`${API}/followups/lead/${leadId}/ai-reply`, { message });
  }
}
