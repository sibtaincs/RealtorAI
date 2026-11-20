import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { NotificationItem } from './models';
import { API } from './api';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private http = inject(HttpClient);

  unread = signal(0);
  items = signal<NotificationItem[]>([]);

  all(): Observable<NotificationItem[]> {
    return this.http.get<NotificationItem[]>(`${API}/notifications`)
      .pipe(tap(items => this.items.set(items)));
  }

  refreshCount(): void {
    this.http.get<{ count: number }>(`${API}/notifications/unread-count`)
      .subscribe({ next: res => this.unread.set(res.count), error: () => {} });
  }

  markRead(id: number): Observable<void> {
    return this.http.patch<void>(`${API}/notifications/${id}/read`, {}).pipe(
      tap(() => {
        this.items.update(list => list.map(n => n.id === id ? { ...n, isRead: true } : n));
        this.unread.update(n => Math.max(0, n - 1));
      })
    );
  }

  markAll(): Observable<{ updated: number }> {
    return this.http.patch<{ updated: number }>(`${API}/notifications/read-all`, {}).pipe(
      tap(() => {
        this.items.update(list => list.map(n => ({ ...n, isRead: true })));
        this.unread.set(0);
      })
    );
  }
}
