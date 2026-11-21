import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API } from './api';
import { AgentProfile } from './models';

@Injectable({ providedIn: 'root' })
export class AgentService {
  private http = inject(HttpClient);

  get(userId: string): Observable<AgentProfile> {
    return this.http.get<AgentProfile>(`${API}/agents/${encodeURIComponent(userId)}`);
  }
}
