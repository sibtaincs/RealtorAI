import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { PropertyService } from '../core/property.service';
import { ShortlistService } from '../core/shortlist.service';
import { PropertyDto } from '../core/models';
import { assetUrl } from '../core/api';
import { PropertyCard } from '../shared/property-card';

@Component({
  selector: 'app-shortlist',
  imports: [RouterLink, PropertyCard],
  templateUrl: './shortlist.html',
  styleUrl: './shortlist.scss'
})
export class ShortlistPage implements OnInit {
  private properties = inject(PropertyService);
  shortlist = inject(ShortlistService);

  favorites = signal<PropertyDto[]>([]);
  recent = signal<PropertyDto[]>([]);
  loading = signal(true);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    const favoriteIds = this.shortlist.favorites();
    const recentIds = this.shortlist.recent();
    const ids = [...new Set([...favoriteIds, ...recentIds])];

    if (ids.length === 0) {
      this.favorites.set([]);
      this.recent.set([]);
      this.loading.set(false);
      return;
    }

    forkJoin(ids.map(id => this.properties.get(id).pipe(catchError(() => of(null))))).subscribe({
      next: results => {
        const byId = new Map<number, PropertyDto>();
        results.forEach(property => {
          if (property) {
            byId.set(property.id, { ...property, imageUrls: property.imageUrls.map(assetUrl) });
          }
        });
        this.favorites.set(favoriteIds.map(id => byId.get(id)).filter((p): p is PropertyDto => !!p));
        this.recent.set(recentIds.map(id => byId.get(id)).filter((p): p is PropertyDto => !!p));
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
