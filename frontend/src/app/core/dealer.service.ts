import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API } from './api';

export interface DealerApproval {
  id: string;
  fullName: string;
  email: string;
  agencyName?: string | null;
  approvalStatus: string;
  createdAt: string;
  isKycVerified: boolean;
}

@Injectable({ providedIn: 'root' })
export class DealerService {
  private http = inject(HttpClient);

  list(status = 'Pending'): Observable<DealerApproval[]> {
    return this.http.get<DealerApproval[]>(`${API}/admin/dealers`, { params: { status } });
  }

  approve(id: string): Observable<void> {
    return this.http.post<void>(`${API}/admin/dealers/${id}/approve`, {});
  }

  reject(id: string): Observable<void> {
    return this.http.post<void>(`${API}/admin/dealers/${id}/reject`, {});
  }

  verifyKyc(id: string): Observable<void> {
    return this.http.post<void>(`${API}/admin/dealers/${id}/kyc`, {});
  }
}