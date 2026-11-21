import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { Observable, shareReplay, tap } from 'rxjs';
import { API } from './api';

export interface MasterDataItem {
  id: number;
  kind: string;
  value: string;
  label: string;
  sortOrder: number;
  isActive: boolean;
}

export interface MasterDataUpsert {
  kind: string;
  value: string;
  label: string;
  sortOrder: number;
  isActive: boolean;
}

export const MASTER_DATA_KINDS = [
  'city', 'property-type', 'listing-type', 'currency', 'area-unit',
  'bedroom', 'sort-option', 'project-status', 'agency-specialty',
  'article-category', 'land-record-status', 'lead-status',
  'listing-status', 'report-status', 'moderation-status', 'price-advisor-days'
] as const;

// These values are read by fixed backend logic or conversion tables; admins can relabel them only.
export const FIXED_VALUE_KINDS = [
  'currency', 'area-unit', 'lead-status', 'listing-status',
  'report-status', 'moderation-status'
] as const;

@Injectable({ providedIn: 'root' })
export class MasterDataService {
  private http = inject(HttpClient);
  private items = signal<MasterDataItem[]>([]);
  private request?: Observable<MasterDataItem[]>;

  load(): Observable<MasterDataItem[]> {
    if (!this.request) {
      this.request = this.http.get<MasterDataItem[]>(`${API}/masterdata`).pipe(
        tap(items => this.items.set(items)),
        shareReplay(1)
      );
    }
    return this.request;
  }

  all(): MasterDataItem[] {
    return this.items();
  }

  reload(): Observable<MasterDataItem[]> {
    this.request = undefined;
    return this.load();
  }

  create(input: MasterDataUpsert): Observable<MasterDataItem> {
    return this.http.post<MasterDataItem>(`${API}/masterdata`, input);
  }

  update(id: number, input: MasterDataUpsert): Observable<MasterDataItem> {
    return this.http.put<MasterDataItem>(`${API}/masterdata/${id}`, input);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${API}/masterdata/${id}`);
  }

  options(kind: string): MasterDataItem[] {
    return this.items()
      .filter(item => item.kind === kind && item.isActive)
      .sort((a, b) => a.sortOrder - b.sortOrder || a.id - b.id);
  }

  values(kind: string): string[] {
    return this.options(kind).map(item => item.value);
  }

  isFixed(kind: string): boolean {
    return (FIXED_VALUE_KINDS as readonly string[]).includes(kind);
  }
}
