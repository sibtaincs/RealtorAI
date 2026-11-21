import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DisplayService } from '../core/display.service';
import { AREA_GUIDES, AREA_TRENDS, PRICE_INDEX } from '../core/market.data';
import { CITIES } from '../core/models';

@Component({
  selector: 'app-market',
  imports: [FormsModule],
  templateUrl: './market.html',
  styleUrl: './directory.scss'
})
export class MarketPage {
  display = inject(DisplayService);
  cities = CITIES;
  guides = AREA_GUIDES;
  trends = AREA_TRENDS;
  index = PRICE_INDEX;

  city = signal('');

  filteredGuides = computed(() => this.guides.filter(g => !this.city() || g.city === this.city()));
  filteredTrends = computed(() => this.trends.filter(t => !this.city() || t.city === this.city()));
  filteredIndex = computed(() => this.index.filter(i => !this.city() || i.city === this.city()));
}
