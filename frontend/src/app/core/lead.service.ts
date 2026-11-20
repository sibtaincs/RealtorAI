import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { CreateLead, LeadDto } from './models';
import { API } from './api';

@Injectable({ providedIn: 'root' })
export class LeadService {
  private http = inject(HttpClient);

  create(input: CreateLead): Observable<LeadDto> {
    return this.http.post<LeadDto>(`${API}/leads`, input);
  }

  all(status?: string): Observable<LeadDto[]> {
    return this.http.get<LeadDto[]>(`${API}/leads`, {
      params: status ? { status } : undefined
    });
  }

  setStatus(id: number, status: string): Observable<LeadDto> {
    return this.http.patch<LeadDto>(`${API}/leads/${id}/status`, null, {
      params: { status }
    });
  }
}
