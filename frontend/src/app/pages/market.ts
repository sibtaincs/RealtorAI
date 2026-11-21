import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ContentService } from '../core/content.service';
import { DisplayService } from '../core/display.service';
import { AreaGuide, AreaTrend, PriceIndexRow } from '../core/market.data';
import { CITIES } from '../core/models';

@Component({
  selector: 'app-market',
  imports: [FormsModule],
  templateUrl: './market.html',
  styleUrl: './directory.scss'
})
export class MarketPage implements OnInit {
  display = inject(DisplayService);
  private content = inject(ContentService);
  cities = CITIES;
  guides = signal<AreaGuide[]>([]);
  trends = signal<AreaTrend[]>([]);
  index = signal<PriceIndexRow[]>([]);

  city = signal('');

  ngOnInit(): void {
    this.content.list<AreaGuide>('area-guide').subscribe(items => this.guides.set(items));
    this.content.list<AreaTrend>('area-trend').subscribe(items => this.trends.set(items));
    this.content.list<PriceIndexRow>('price-index').subscribe(items => this.index.set(items));
  }

  filteredGuides = computed(() => this.guides().filter(g => !this.city() || g.city === this.city()));
  filteredTrends = computed(() => this.trends().filter(t => !this.city() || t.city === this.city()));
  filteredIndex = computed(() => this.index().filter(i => !this.city() || i.city === this.city()));
}
