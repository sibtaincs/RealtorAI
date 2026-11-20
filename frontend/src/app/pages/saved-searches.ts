import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { SavedSearchService } from '../core/saved-search.service';
import { AuthService } from '../core/auth.service';
import { SavedSearch } from '../core/models';

@Component({
  selector: 'app-saved-searches',
  imports: [DatePipe, RouterLink],
  templateUrl: './saved-searches.html',
  styleUrl: './saved-searches.scss'
})
export class SavedSearches implements OnInit {
  private service = inject(SavedSearchService);
  private router = inject(Router);
  auth = inject(AuthService);

  items = signal<SavedSearch[]>([]);
  loading = signal(true);
  error = signal('');
  notice = signal('');

  ngOnInit(): void {
    if (this.auth.isLoggedIn()) this.load();
    else this.loading.set(false);
  }

  load(): void {
    this.service.all().subscribe({
      next: items => {
        this.items.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Saved searches load nahi hue.');
        this.loading.set(false);
      }
    });
  }

  describe(json: string): string {
    try {
      const q = JSON.parse(json);
      const parts = [q.city, q.type, q.listingType, q.search].filter(Boolean);
      if (q.minBeds) parts.push(q.minBeds + '+ beds');
      return parts.length ? parts.join(' · ') : 'All properties';
    } catch {
      return 'All properties';
    }
  }

  toggle(item: SavedSearch): void {
    this.service.update(item.id, item.name, !item.alertEnabled).subscribe({
      next: updated => this.items.update(list => list.map(i => i.id === updated.id ? updated : i))
    });
  }

  run(item: SavedSearch): void {
    this.service.run(item.id).subscribe({
      next: res => {
        this.notice.set(res.newMatches > 0
          ? `${res.newMatches} naye matches mile — notification bhej diya gaya.`
          : 'Abhi koi naya match nahi.');
      }
    });
  }

  open(item: SavedSearch): void {
    this.router.navigate(['/'], { queryParams: { saved: item.id } });
  }

  remove(item: SavedSearch): void {
    this.service.remove(item.id).subscribe({
      next: () => this.items.update(list => list.filter(i => i.id !== item.id))
    });
  }
}
