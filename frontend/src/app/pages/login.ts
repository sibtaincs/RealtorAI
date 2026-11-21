import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-login',
  imports: [FormsModule],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class Login {
  private auth = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  mode = signal<'login' | 'register'>('login');
  fullName = '';
  email = '';
  password = '';
  agencyName = '';
  error = signal('');
  info = signal('');
  busy = signal(false);
  fieldErrors = signal<{ fullName?: string; email?: string; password?: string }>({});

  switchMode(mode: 'login' | 'register'): void {
    this.mode.set(mode);
    this.error.set('');
    this.info.set('');
    this.fieldErrors.set({});
  }

  private validate(): boolean {
    const errors: { fullName?: string; email?: string; password?: string } = {};
    const email = this.email.trim();
    if (!email) errors.email = 'Email required hai.';
    else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) errors.email = 'Valid email enter karein.';
    if (!this.password) errors.password = 'Password required hai.';
    else if (this.password.length < 6) errors.password = 'Password kam az kam 6 characters ka hona chahiye.';
    if (this.mode() === 'register' && !this.fullName.trim()) errors.fullName = 'Full name required hai.';
    this.fieldErrors.set(errors);
    return Object.keys(errors).length === 0;
  }

  submit(): void {
    this.error.set('');
    this.info.set('');
    if (!this.validate()) return;

    this.busy.set(true);
    const request = this.mode() === 'login'
      ? this.auth.login(this.email.trim(), this.password)
      : this.auth.register(this.fullName.trim(), this.email.trim(), this.password, this.agencyName.trim() || undefined);

    request.subscribe({
      next: res => {
        this.busy.set(false);
        if (res.approvalStatus === 'Pending') {
          this.info.set('Account pending admin approval hai. Login ho gaya, lekin property posting approval ke baad enable hogi.');
        }
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') || '/';
        this.router.navigateByUrl(returnUrl);
      },
      error: err => {
        this.error.set(err?.error?.message ?? err?.error?.errors?.[0] ?? 'Login/register fail ho gaya. Dobara try karein.');
        this.busy.set(false);
      }
    });
  }
}
