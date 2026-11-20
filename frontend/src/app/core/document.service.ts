import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DocumentItem } from './models';
import { API } from './api';

@Injectable({ providedIn: 'root' })
export class DocumentService {
  private http = inject(HttpClient);

  forProperty(propertyId: number): Observable<DocumentItem[]> {
    return this.http.get<DocumentItem[]>(`${API}/documents/property/${propertyId}`);
  }

  add(propertyId: number, name: string, notes: string | null = null): Observable<DocumentItem> {
    return this.http.post<DocumentItem>(`${API}/documents/property/${propertyId}`, { name, notes });
  }

  update(id: number, isVerified: boolean, notes: string | null): Observable<DocumentItem> {
    return this.http.patch<DocumentItem>(`${API}/documents/${id}`, { isVerified, notes });
  }

  remove(id: number): Observable<void> {
    return this.http.delete<void>(`${API}/documents/${id}`);
  }
}
