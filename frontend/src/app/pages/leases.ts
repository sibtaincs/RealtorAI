import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../core/auth.service';
import { LeaseService } from '../core/lease.service';
import { Lease } from '../core/models';

@Component({
  selector: 'app-leases',
  imports: [DatePipe, DecimalPipe, FormsModule],
  templateUrl: './leases.html',
  styleUrl: './leases.scss'
})
export class Leases implements OnInit {
  private leases = inject(LeaseService);
  auth = inject(AuthService);

  items = signal<Lease[]>([]);
  loading = signal(false);
  error = signal('');
  notice = signal('');

  createOpen = signal(false);
  propertyListingId = 0;
  tenantName = '';
  tenantEmail = '';
  startDate = '';
  endDate = '';
  monthlyRent = 250000;
  notes = '';

  ngOnInit(): void {
    if (this.auth.isLoggedIn()) this.load();
  }

  load(): void {
    this.loading.set(true);
    this.leases.all().subscribe({
      next: items => { this.items.set(items); this.loading.set(false); },
      error: () => { this.error.set('Leases load nahi hue.'); this.loading.set(false); }
    });
  }

  toggleCreate(): void {
    this.createOpen.set(!this.createOpen);
    this.error.set('');
    this.notice.set('');
  }

  create(): void {
    if (!this.propertyListingId || !this.tenantName.trim()) { this.error.set('Property aur tenant name required hai.'); return; }
    const input = {
      propertyListingId: this.propertyListingId,
      tenantName: this.tenantName.trim(),
      tenantEmail: this.tenantEmail.trim() || null,
      startDate: this.startDate ? new Date(this.startDate + 'T00:00:00Z').toISOString() : new Date().toISOString(),
      endDate: this.endDate ? new Date(this.endDate + 'T00:00:00Z').toISOString() : new Date(Date.now() + 365 * 24 * 3600 * 1000).toISOString(),
      monthlyRent: this.monthlyRent,
      notes: this.notes.trim() || null
    };
    this.leases.create(input).subscribe({
      next: res => {
        this.notice.set('Lease create ho gaya.');
        this.createOpen.set(false);
        this.tenantName = '';
        this.tenantEmail = '';
        this.startDate = '';
        this.endDate = '';
        this.monthlyRent = 250000;
        this.notes = '';
        this.load();
      },
      error: () => this.error.set('Lease create fail ho gaya.')
    });
  }

  terminate(item: Lease): void {
    this.leases.terminate(item.id).subscribe({
      next: updated => this.items.update(list => list.map(i => i.id === updated.id ? updated : i)),
      error: () => this.error.set('Terminate fail ho gaya.')
    });
  }

  remove(item: Lease): void {
    if (!confirm('Is lease ko delete karein?')) return;
    this.leases.remove(item.id).subscribe({
      next: () => this.items.update(list => list.filter(i => i.id !== item.id))
    });
  }
}
