import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';
import { AuthResponse } from './models';
import { API } from './api';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);

  private stored = this.readStored();
  user = signal<AuthResponse | null>(this.stored);
  isLoggedIn = computed(() => !!this.user());
  isAdmin = computed(() => this.user()?.roles.includes('Admin') ?? false);
  isApproved = computed(() => this.isAdmin() || this.user()?.approvalStatus === 'Approved');

  login(email: string, password: string) {
    return this.http.post<AuthResponse>(`${API}/auth/login`, { email, password })
      .pipe(tap(res => this.save(res)));
  }

  register(fullName: string, email: string, password: string, agencyName?: string) {
    return this.http.post<AuthResponse>(`${API}/auth/register`, {
      fullName, email, password, agencyName
    }).pipe(tap(res => this.save(res)));
  }

  logout(): void {
    localStorage.removeItem('reai_token');
    localStorage.removeItem('reai_user');
    this.user.set(null);
  }

  private save(res: AuthResponse): void {
    localStorage.setItem('reai_token', res.token);
    localStorage.setItem('reai_user', JSON.stringify(res));
    this.user.set(res);
  }

  private readStored(): AuthResponse | null {
    try {
      const raw = localStorage.getItem('reai_user');
      return raw ? JSON.parse(raw) as AuthResponse : null;
    } catch {
      return null;
    }
  }
}
