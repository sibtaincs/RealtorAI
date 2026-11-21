import { AfterViewInit, Component, ElementRef, OnDestroy, ViewChild, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import type * as Leaflet from 'leaflet';
import { PropertyService } from '../core/property.service';
import { CITIES, PropertyDto } from '../core/models';
import { DisplayService } from '../core/display.service';

@Component({
  selector: 'app-map',
  imports: [FormsModule, RouterLink],
  templateUrl: './map.html',
  styleUrl: './map.scss'
})
export class MapPage implements AfterViewInit, OnDestroy {
  @ViewChild('canvas', { static: true }) canvas!: ElementRef<HTMLElement>;

  private props = inject(PropertyService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  display = inject(DisplayService);
  private map?: Leaflet.Map;
  private markers?: Leaflet.LayerGroup;

  items = signal<PropertyDto[]>([]);
  loading = signal(false);
  error = signal('');
  cities = CITIES;
  city = this.route.snapshot.queryParamMap.get('city') ?? '';
  listingType = '';

  async ngAfterViewInit(): Promise<void> {
    const L = await import('leaflet');
    this.map = L.map(this.canvas.nativeElement).setView([31.5, 73.0], 6);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; OpenStreetMap contributors',
      maxZoom: 18
    }).addTo(this.map);
    this.markers = L.layerGroup().addTo(this.map);
    this.map.on('moveend', () => void this.load());
    await this.load();
  }

  ngOnDestroy(): void {
    this.map?.remove();
  }

  apply(): void {
    void this.load();
  }

  private async load(): Promise<void> {
    const bounds = this.map?.getBounds();
    this.loading.set(true);
    this.props.search({
      city: this.city || undefined,
      listingType: this.listingType || undefined,
      minLat: bounds?.getSouth(),
      maxLat: bounds?.getNorth(),
      minLng: bounds?.getWest(),
      maxLng: bounds?.getEast(),
      pageSize: 48
    }).subscribe({
      next: result => {
        this.items.set(result.items);
        this.loading.set(false);
        void this.draw();
      },
      error: () => {
        this.error.set('Map data load nahi hua.');
        this.loading.set(false);
      }
    });
  }

  private async draw(): Promise<void> {
    if (!this.markers) return;
    const L = await import('leaflet');
    this.markers.clearLayers();
    for (const p of this.items()) {
      if (!p.latitude || !p.longitude) continue;
      L.circleMarker([p.latitude, p.longitude], {
        radius: 8, color: '#1d4ed8', weight: 2, fillColor: '#3b82f6', fillOpacity: 0.85
      }).bindPopup(this.popup(p)).addTo(this.markers);
    }
  }

  // DOM nodes instead of HTML strings so listing titles can never inject markup.
  private popup(p: PropertyDto): HTMLElement {
    const box = document.createElement('div');
    const title = document.createElement('strong');
    title.textContent = p.title;
    const price = document.createElement('div');
    price.textContent = this.display.money(p.price, p.listingType);
    const link = document.createElement('a');
    link.href = `/property/${p.id}`;
    link.textContent = 'View details →';
    link.addEventListener('click', event => {
      event.preventDefault();
      void this.router.navigate(['/property', p.id]);
    });
    box.append(title, price, link);
    return box;
  }
}
