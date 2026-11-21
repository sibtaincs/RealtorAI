import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { API } from './api';

export interface ContentItem {
  id: number;
  kind: string;
  payloadJson: string;
  updatedAt: string;
}

export interface ContentUpsert {
  kind: string;
  payloadJson: string;
}

export const CONTENT_KINDS = [
  'project', 'agency', 'society', 'land-record',
  'article', 'area-guide', 'area-trend', 'price-index'
] as const;

@Injectable({ providedIn: 'root' })
export class ContentService {
  private http = inject(HttpClient);

  list<T>(kind: string): Observable<T[]> {
    return this.http.get<ContentItem[]>(`${API}/content`, { params: { kind } }).pipe(
      map(items => items.map(item => JSON.parse(item.payloadJson) as T))
    );
  }

  all(): Observable<ContentItem[]> {
    return this.http.get<ContentItem[]>(`${API}/content`);
  }

  create(input: ContentUpsert): Observable<ContentItem> {
    return this.http.post<ContentItem>(`${API}/content`, input);
  }

  update(id: number, input: ContentUpsert): Observable<ContentItem> {
    return this.http.put<ContentItem>(`${API}/content/${id}`, input);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${API}/content/${id}`);
  }
}
