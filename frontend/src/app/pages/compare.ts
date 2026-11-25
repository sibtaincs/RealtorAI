import { Component, inject, OnInit, signal } from '@angular/core';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { RouterLink } from '@angular/router';
import { PropertyService } from '../core/property.service';
import { ShortlistService } from '../core/shortlist.service';
import { PropertyDto } from '../core/models';
import { assetUrl } from '../core/api';
import { FormsModule } from '@angular/forms';
import { DisplayService } from '../core/display.service';

@Component({
  selector: 'app-compare',
  imports: [FormsModule, RouterLink],
  templateUrl: './compare.html',
  styleUrl: './compare.scss'
})
export class Compare implements OnInit {
  private properties = inject(PropertyService);
  shortlist = inject(ShortlistService);
  display = inject(DisplayService);

  items = signal<PropertyDto[]>([]);
  loading = signal(true);
  error = signal('');

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    const ids = this.shortlist.compare();
    if (ids.length === 0) {
      this.items.set([]);
      this.loading.set(false);
      return;
    }

    forkJoin(ids.map(id =>
      this.properties.get(id).pipe(catchError(() => of(null)))
    )).subscribe({
      next: results => {
        const list = results
          .map(p => p ? { ...p, imageUrls: p.imageUrls.map(assetUrl) } : null)
          .filter((p): p is PropertyDto => !!p);
        this.items.set(list);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Compare load nahi hua.');
        this.loading.set(false);
      }
    });
  }

  remove(id: number): void {
    this.shortlist.toggleCompare(id);
    this.load();
  }

  clear(): void {
    this.shortlist.clearCompare();
    this.items.set([]);
  }

  hasImage(p: PropertyDto): boolean {
    return p.imageUrls.length > 0;
  }

  image(p: PropertyDto): string {
    return assetUrl(p.imageUrls[0]);
  }

  amenitiesOf(p: PropertyDto): string[] {
    if (!p.amenitiesJson) return [];
    try {
      const parsed = JSON.parse(p.amenitiesJson);
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }
}
