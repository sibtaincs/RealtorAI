import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Lease, LeaseInput } from './models';
import { API } from './api';

@Injectable({ providedIn: 'root' })
export class LeaseService {
  private http = inject(HttpClient);

  all(): Observable<Lease[]> {
    return this.http.get<Lease[]>(`${API}/leases`);
  }

  create(input: LeaseInput): Observable<Lease> {
    return this.http.post<Lease>(`${API}/leases`, input);
  }

  update(id: number, input: LeaseInput): Observable<Lease> {
    return this.http.put<Lease>(`${API}/leases/${id}`, input);
  }

  remove(id: number): Observable<void> {
    return this.http.delete<void>(`${API}/leases/${id}`);
  }

  terminate(id: number): Observable<Lease> {
    return this.http.post<Lease>(`${API}/leases/${id}/terminate`, {});
  }
}
