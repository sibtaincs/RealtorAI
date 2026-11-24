import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ContentService } from '../core/content.service';
import { DisplayService } from '../core/display.service';
import { CommunityArticle, LandRecord, Society } from '../core/explore.data';
import { MasterDataService } from '../core/master-data.service';

@Component({
  selector: 'app-explore',
  imports: [FormsModule, RouterLink],
  templateUrl: './explore.html',
  styleUrl: './directory.scss'
})
export class ExplorePage implements OnInit {
  display = inject(DisplayService);
  private content = inject(ContentService);
  master = inject(MasterDataService);
  societies = signal<Society[]>([]);
  articles = signal<CommunityArticle[]>([]);
  landRecords = signal<LandRecord[]>([]);

  city = signal('');
  category = signal('');
  recordCity = signal('Lahore');
  recordPlot = signal('');

  minPrice = 0;
  maxPrice = 0;
  minMarla = 0;
  maxMarla = 0;
  status = '';

  private marlaOf(text: string): number {
    const m = text.match(/\d+(\.\d+)?/);
    return m ? Number(m[0]) : 0;
  }

  toNumber(value: unknown): number {
    return Number(value) || 0;
  }

  ngOnInit(): void {
    this.content.list<Society>('society').subscribe(items => this.societies.set(items));
    this.content.list<CommunityArticle>('article').subscribe(items => this.articles.set(items));
    this.content.list<LandRecord>('land-record').subscribe(items => this.landRecords.set(items));
  }

  filteredSocieties = computed(() => {
    const minP = this.minPrice;
    const maxP = this.maxPrice;
    const minM = this.minMarla;
    const maxM = this.maxMarla;
    const st = this.status;
    return this.societies().filter(s => {
      if (this.city() && s.city !== this.city()) return false;
      if (minP > 0 && s.priceFrom < minP) return false;
      if (maxP > 0 && s.priceFrom > maxP) return false;
      if (st && s.status !== st) return false;
      const marla = this.marlaOf(s.plotSizes);
      if (minM > 0 && marla < minM) return false;
      if (maxM > 0 && marla > maxM) return false;
      return true;
    });
  });

  statuses = computed(() => [...new Set(this.societies().map(s => s.status))].sort());
  filteredArticles = computed(() => this.articles().filter(a => !this.category() || a.category === this.category()));
  record = computed(() => {
    const plot = this.recordPlot().trim().toLowerCase();
    if (!plot) return null;
    return this.landRecords().find(r => r.city === this.recordCity() && r.plot.toLowerCase() === plot) ?? null;
  });
}
