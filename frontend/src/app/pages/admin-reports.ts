import { Component, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { ModerationService } from '../core/moderation.service';
import { Report, Review } from '../core/models';

@Component({
  selector: 'app-admin-reports',
  imports: [DatePipe, RouterLink],
  templateUrl: './admin-reports.html',
  styleUrl: './admin-reports.scss'
})
export class AdminReports implements OnInit {
  auth = inject(AuthService);
  private moderation = inject(ModerationService);

  pending = signal<Review[]>([]);
  reports = signal<Report[]>([]);
  error = signal('');
  working = signal<number | null>(null);

  ngOnInit(): void {
    if (this.auth.isAdmin()) this.load();
  }

  load(): void {
    this.moderation.pendingReviews().subscribe({
      next: items => this.pending.set(items),
      error: () => this.error.set('Pending reviews load nahi hue.')
    });
    this.moderation.openReports().subscribe({
      next: items => this.reports.set(items),
      error: () => this.error.set('Open reports load nahi hue.')
    });
  }

  decide(id: number, decision: 'approve' | 'reject'): void {
    this.working.set(id);
    this.moderation.moderate(id, decision).subscribe({
      next: () => {
        this.pending.update(items => items.filter(r => r.id !== id));
        this.working.set(null);
      },
      error: () => { this.error.set('Review action fail ho gaya.'); this.working.set(null); }
    });
  }

  resolve(id: number): void {
    this.working.set(id);
    this.moderation.resolve(id).subscribe({
      next: () => {
        this.reports.update(items => items.filter(r => r.id !== id));
        this.working.set(null);
      },
      error: () => { this.error.set('Report resolve nahi hua.'); this.working.set(null); }
    });
  }
}
