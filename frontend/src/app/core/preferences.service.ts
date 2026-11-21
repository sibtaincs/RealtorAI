import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API } from './api';
import { NotificationPreferences } from './models';

@Injectable({ providedIn: 'root' })
export class PreferencesService {
  private http = inject(HttpClient);

  get(): Observable<NotificationPreferences> {
    return this.http.get<NotificationPreferences>(`${API}/notifications/preferences`);
  }

  update(value: NotificationPreferences): Observable<NotificationPreferences> {
    return this.http.put<NotificationPreferences>(`${API}/notifications/preferences`, value);
  }
}
