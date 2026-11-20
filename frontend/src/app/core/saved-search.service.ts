import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { SavedSearch } from './models';
import { API } from './api';

@Injectable({ providedIn: 'root' })
export class SavedSearchService {
  private http = inject(HttpClient);

  all(): Observable<SavedSearch[]> {
    return this.http.get<SavedSearch[]>(`${API}/savedsearches`);
  }

  create(name: string, query: object, alertEnabled: boolean): Observable<SavedSearch> {
    return this.http.post<SavedSearch>(`${API}/savedsearches`, {
      name, query, alertEnabled
    });
  }

  update(id: number, name: string, alertEnabled: boolean): Observable<SavedSearch> {
    return this.http.put<SavedSearch>(`${API}/savedsearches/${id}`, { name, alertEnabled });
  }

  remove(id: number): Observable<void> {
    return this.http.delete<void>(`${API}/savedsearches/${id}`);
  }

  run(id: number): Observable<{ newMatches: number }> {
    return this.http.post<{ newMatches: number }>(`${API}/savedsearches/${id}/run`, {});
  }
}
