import { Component, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { DashboardService } from '../core/dashboard.service';
import { AuthService } from '../core/auth.service';
import { DashboardSummary, LeadDto, ListingPerformance } from '../core/models';

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink, DatePipe],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class Dashboard implements OnInit {
  private dashboardService = inject(DashboardService);
  auth = inject(AuthService);

  summary = signal<DashboardSummary | null>(null);
  leads = signal<LeadDto[]>([]);
  listings = signal<ListingPerformance[]>([]);
  loading = signal(true);
  error = signal('');

  temperatures = ['Hot', 'Warm', 'Cold'];
  temperature = '';
  page = 1;
  pageSize = 8;
  total = 0;

  ngOnInit(): void {
    if (this.auth.isLoggedIn()) this.load();
  }

  load(): void {
    this.loading.set(true);
    this.dashboardService.summary().subscribe({
      next: value => this.summary.set(value),
      error: () => this.error.set('Dashboard summary load nahi hua.')
    });
    this.dashboardService.listings().subscribe({
      next: value => this.listings.set(value)
    });
    this.loadLeads();
  }

  loadLeads(): void {
    this.dashboardService.leads({
      temperature: this.temperature || undefined,
      page: this.page,
      pageSize: this.pageSize
    }).subscribe({
      next: result => {
        this.leads.set(result.items);
        this.total = result.total;
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Lead queue load nahi hua.');
        this.loading.set(false);
      }
    });
  }

  filterBy(temperature: string): void {
    this.temperature = this.temperature === temperature ? '' : temperature;
    this.page = 1;
    this.loadLeads();
  }

  previousPage(): void {
    if (this.page > 1) {
      this.page--;
      this.loadLeads();
    }
  }

  nextPage(): void {
    if (this.page * this.pageSize < this.total) {
      this.page++;
      this.loadLeads();
    }
  }
}
