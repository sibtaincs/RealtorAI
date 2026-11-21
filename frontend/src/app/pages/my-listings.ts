import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../core/auth.service';
import { PropertyService } from '../core/property.service';
import { PagedResult, PropertyDto, PropertyQuery } from '../core/models';
import { MasterDataService } from '../core/master-data.service';
import { DisplayService } from '../core/display.service';
import { PropertyCard } from '../shared/property-card';

@Component({
  selector: 'app-my-listings',
  imports: [FormsModule, PropertyCard],
  templateUrl: './my-listings.html',
  styleUrl: './directory.scss'
})
export class MyListings implements OnInit {
  auth = inject(AuthService);
  private properties = inject(PropertyService);
  master = inject(MasterDataService);
  display = inject(DisplayService);

  result = signal<PagedResult<PropertyDto> | null>(null);
  loading = signal(false);
  error = signal('');

  search = '';
  city = '';
  type = '';
  listingType = '';
  status = '';
  page = 1;
  pageSize = 12;

  ngOnInit(): void {
    this.master.load().subscribe();
    if (this.auth.isLoggedIn()) this.load();
  }

  load(): void {
    const userId = this.auth.user()?.id;
    if (!userId) return;
    this.loading.set(true);
    const query: PropertyQuery = {
      agentId: userId,
      search: this.search || undefined,
      city: this.city || undefined,
      type: this.type || undefined,
      listingType: this.listingType || undefined,
      status: this.status || undefined,
      page: this.page,
      pageSize: this.pageSize
    };
    this.properties.search(query).subscribe({
      next: res => { this.result.set(res); this.loading.set(false); },
      error: () => { this.error.set('Listings load nahi huye.'); this.loading.set(false); }
    });
  }

  apply(): void { this.page = 1; this.load(); }
  clear(): void { this.search = ''; this.city = ''; this.type = ''; this.listingType = ''; this.status = ''; this.apply(); }
  previous(): void { if (this.page > 1) { this.page--; this.load(); } }
  next(): void { const r = this.result(); if (r && this.page * this.pageSize < r.total) { this.page++; this.load(); } }

  items = computed(() => this.result()?.items ?? []);
  total = computed(() => this.result()?.total ?? 0);
}