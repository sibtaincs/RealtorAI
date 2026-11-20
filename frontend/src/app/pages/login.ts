import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
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

  mode = signal<'login' | 'register'>('login');
  fullName = '';
  email = '';
  password = '';
  agencyName = '';
  error = signal('');
  busy = signal(false);

  switchMode(mode: 'login' | 'register'): void {
    this.mode.set(mode);
    this.error.set('');
  }

  submit(): void {
    this.busy.set(true);
    this.error.set('');

    const request = this.mode() === 'login'
      ? this.auth.login(this.email, this.password)
      : this.auth.register(this.fullName, this.email, this.password, this.agencyName);

    request.subscribe({
      next: () => {
        this.busy.set(false);
        this.router.navigateByUrl('/');
      },
      error: err => {
        this.error.set(err?.error?.message ?? err?.error?.errors?.[0] ?? 'Something went wrong.');
        this.busy.set(false);
      }
    });
  }
}
