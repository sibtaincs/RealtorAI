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

export type ContentFieldType = 'text' | 'textarea' | 'number' | 'boolean' | 'select' | 'city';

export interface ContentField {
  name: string;
  label: string;
  type: ContentFieldType;
  required?: boolean;
  options?: string[];
}

export const CONTENT_FIELDS: Record<string, ContentField[]> = {
  project: [
    { name: 'name', label: 'Project name', type: 'text', required: true },
    { name: 'developer', label: 'Developer', type: 'text', required: true },
    { name: 'city', label: 'City', type: 'city', required: true },
    { name: 'locality', label: 'Locality', type: 'text', required: true },
    { name: 'type', label: 'Type', type: 'text', required: true },
    { name: 'priceFrom', label: 'Price from', type: 'number', required: true },
    { name: 'priceTo', label: 'Price to', type: 'number', required: true },
    { name: 'status', label: 'Status', type: 'select', required: true, options: ['Launching', 'Under Construction', 'Nearing Completion', 'Completed'] }
  ],
  agency: [
    { name: 'name', label: 'Agency name', type: 'text', required: true },
    { name: 'city', label: 'City', type: 'city', required: true },
    { name: 'agents', label: 'Agents', type: 'number', required: true },
    { name: 'listings', label: 'Listings', type: 'number', required: true },
    { name: 'rating', label: 'Rating', type: 'number', required: true },
    { name: 'verified', label: 'Verified', type: 'boolean' },
    { name: 'specialty', label: 'Specialty', type: 'select', required: true, options: ['Residential', 'Plots', 'Apartments', 'Commercial', 'Houses', 'Rentals', 'Farms'] }
  ],
  society: [
    { name: 'name', label: 'Society name', type: 'text', required: true },
    { name: 'city', label: 'City', type: 'city', required: true },
    { name: 'plotSizes', label: 'Plot sizes', type: 'text', required: true },
    { name: 'priceFrom', label: 'Price from', type: 'number', required: true },
    { name: 'status', label: 'Status', type: 'text', required: true }
  ],
  'land-record': [
    { name: 'plot', label: 'Plot', type: 'text', required: true },
    { name: 'city', label: 'City', type: 'city', required: true },
    { name: 'owner', label: 'Owner', type: 'text', required: true },
    { name: 'area', label: 'Area', type: 'text', required: true },
    { name: 'status', label: 'Status', type: 'select', required: true, options: ['Verified', 'Pending'] }
  ],
  article: [
    { name: 'title', label: 'Title', type: 'text', required: true },
    { name: 'category', label: 'Category', type: 'select', required: true, options: ['News', 'Guide', 'Community'] },
    { name: 'summary', label: 'Summary', type: 'textarea', required: true },
    { name: 'date', label: 'Date', type: 'text', required: true }
  ],
  'area-guide': [
    { name: 'city', label: 'City', type: 'city', required: true },
    { name: 'name', label: 'Area name', type: 'text', required: true },
    { name: 'summary', label: 'Summary', type: 'textarea', required: true },
    { name: 'pricePerSqFt', label: 'Price per sq ft', type: 'number', required: true },
    { name: 'safety', label: 'Safety', type: 'text', required: true },
    { name: 'commute', label: 'Commute', type: 'text', required: true },
    { name: 'schools', label: 'Schools', type: 'text', required: true }
  ],
  'area-trend': [
    { name: 'city', label: 'City', type: 'city', required: true },
    { name: 'area', label: 'Area', type: 'text', required: true },
    { name: 'priceChangePct', label: 'Price change %', type: 'number', required: true },
    { name: 'searchChangePct', label: 'Search change %', type: 'number', required: true }
  ],
  'price-index': [
    { name: 'city', label: 'City', type: 'city', required: true },
    { name: 'index', label: 'Index', type: 'number', required: true },
    { name: 'yearChangePct', label: 'Year change %', type: 'number', required: true },
    { name: 'medianPrice', label: 'Median price', type: 'number', required: true }
  ]
};

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
