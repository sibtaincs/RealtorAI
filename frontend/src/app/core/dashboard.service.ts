import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API } from './api';
import { DashboardSummary, LeadDto, ListingPerformance, PagedResult } from './models';

export interface LeadQueueParams {
  status?: string;
  temperature?: string;
  page?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private http = inject(HttpClient);

  summary(): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(`${API}/dashboard/summary`);
  }

  leads(params: LeadQueueParams = {}): Observable<PagedResult<LeadDto>> {
    const query: Record<string, string | number> = {};
    if (params.status) query['status'] = params.status;
    if (params.temperature) query['temperature'] = params.temperature;
    if (params.page) query['page'] = params.page;
    if (params.pageSize) query['pageSize'] = params.pageSize;

    return this.http.get<PagedResult<LeadDto>>(`${API}/dashboard/leads`, { params: query });
  }

  listings(): Observable<ListingPerformance[]> {
    return this.http.get<ListingPerformance[]>(`${API}/dashboard/listings`);
  }
}
