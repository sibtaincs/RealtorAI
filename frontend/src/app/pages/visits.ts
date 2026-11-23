import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../core/auth.service';
import { VisitService } from '../core/visit.service';
import { Visit } from '../core/models';

@Component({
  selector: 'app-visits',
  imports: [DatePipe, FormsModule],
  templateUrl: './visits.html',
  styleUrl: './visits.scss'
})
export class Visits implements OnInit {
  private visits = inject(VisitService);
  auth = inject(AuthService);

  items = signal<Visit[]>([]);
  loading = signal(false);
  error = signal('');
  notice = signal('');

  createOpen = signal(false);
  propertyListingId = 0;
  leadId = 0;
  scheduledAt = '';
  status = 'Scheduled';
  notes = '';

  statuses = ['Scheduled', 'Completed', 'Cancelled', 'NoShow'];

  ngOnInit(): void {
    if (this.auth.isLoggedIn()) this.load();
  }

  load(): void {
    this.loading.set(true);
    this.visits.all().subscribe({
      next: items => { this.items.set(items); this.loading.set(false); },
      error: () => { this.error.set('Visits load nahi hue.'); this.loading.set(false); }
    });
  }

  toggleCreate(): void {
    this.createOpen.set(!this.createOpen);
    this.error.set('');
    this.notice.set('');
  }

  create(): void {
    if (!this.propertyListingId || !this.scheduledAt) { this.error.set('Property aur visit time required hai.'); return; }
    const input = {
      propertyListingId: this.propertyListingId,
      leadId: this.leadId || null,
      scheduledAt: new Date(this.scheduledAt).toISOString(),
      status: 'Scheduled',
      notes: this.notes.trim() || null
    };
    this.visits.create(input).subscribe({
      next: res => {
        this.notice.set('Visit create ho gaya.');
        this.createOpen.set(false);
        this.leadId = 0;
        this.scheduledAt = '';
        this.notes = '';
        this.load();
      },
      error: () => this.error.set('Visit create fail ho gaya.')
    });
  }

  complete(id: number): void {
    this.visits.setStatus(id, 'Completed').subscribe({
      next: updated => this.items.update(list => list.map(i => i.id === updated.id ? updated : i)),
      error: () => this.error.set('Visit status update fail ho gaya.')
    });
  }

  cancel(id: number): void {
    this.visits.setStatus(id, 'Cancelled').subscribe({
      next: updated => this.items.update(list => list.map(i => i.id === updated.id ? updated : i)),
      error: () => this.error.set('Visit status update fail ho gaya.')
    });
  }

  remove(item: Visit): void {
    if (!confirm('Is visit ko delete karein?')) return;
    this.visits.remove(item.id).subscribe({
      next: () => this.items.update(list => list.filter(i => i.id !== item.id))
    });
  }
}
