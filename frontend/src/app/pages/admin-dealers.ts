import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { DealerApproval, DealerService } from '../core/dealer.service';

@Component({
  selector: 'app-admin-dealers',
  imports: [RouterLink],
  templateUrl: './admin-dealers.html',
  styleUrl: './admin-content.scss'
})
export class AdminDealers implements OnInit {
  auth = inject(AuthService);
  private dealers = inject(DealerService);

  items = signal<DealerApproval[]>([]);
  status = signal('Pending');
  error = signal('');
  message = signal('');
  busy = signal(false);

  ngOnInit(): void {
    if (this.auth.isAdmin()) this.load();
  }

  load(): void {
    this.error.set('');
    this.dealers.list(this.status()).subscribe({
      next: items => this.items.set(items),
      error: () => this.error.set('Dealers load nahi huye.')
    });
  }

  setStatus(status: string): void {
    this.status.set(status);
    this.load();
  }

  approve(id: string): void {
    this.busy.set(true);
    this.dealers.approve(id).subscribe({
      next: () => { this.busy.set(false); this.message.set('Dealer approved.'); this.load(); },
      error: () => { this.busy.set(false); this.error.set('Approve fail ho gaya.'); }
    });
  }

  reject(id: string): void {
    this.busy.set(true);
    this.dealers.reject(id).subscribe({
      next: () => { this.busy.set(false); this.message.set('Dealer rejected.'); this.load(); },
      error: () => { this.busy.set(false); this.error.set('Reject fail ho gaya.'); }
    });
  }
}