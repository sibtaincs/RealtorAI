import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Visit, VisitInput } from './models';
import { API } from './api';

@Injectable({ providedIn: 'root' })
export class VisitService {
  private http = inject(HttpClient);

  all(): Observable<Visit[]> {
    return this.http.get<Visit[]>(`${API}/visits`);
  }

  create(input: VisitInput): Observable<Visit> {
    return this.http.post<Visit>(`${API}/visits`, input);
  }

  update(id: number, input: VisitInput): Observable<Visit> {
    return this.http.put<Visit>(`${API}/visits/${id}`, input);
  }

  remove(id: number): Observable<void> {
    return this.http.delete<void>(`${API}/visits/${id}`);
  }

  setStatus(id: number, value: string): Observable<Visit> {
    return this.http.post<Visit>(`${API}/visits/${id}/status`, { value });
  }
}
