import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResult, PropertyDto, PropertyInput, PropertyQuery } from './models';
import { API } from './api';

@Injectable({ providedIn: 'root' })
export class PropertyService {
  private http = inject(HttpClient);

  search(query: PropertyQuery): Observable<PagedResult<PropertyDto>> {
    let params = new HttpParams();
    Object.entries(query).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    });
    return this.http.get<PagedResult<PropertyDto>>(`${API}/properties`, { params });
  }

  get(id: number): Observable<PropertyDto> {
    return this.http.get<PropertyDto>(`${API}/properties/${id}`);
  }

  create(input: PropertyInput): Observable<PropertyDto> {
    return this.http.post<PropertyDto>(`${API}/properties`, input);
  }

  update(id: number, input: PropertyInput): Observable<PropertyDto> {
    return this.http.put<PropertyDto>(`${API}/properties/${id}`, input);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${API}/properties/${id}`);
  }
}
