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

  ngOnInit(): void {
    this.content.list<Society>('society').subscribe(items => this.societies.set(items));
    this.content.list<CommunityArticle>('article').subscribe(items => this.articles.set(items));
    this.content.list<LandRecord>('land-record').subscribe(items => this.landRecords.set(items));
  }

  filteredSocieties = computed(() => this.societies().filter(s => !this.city() || s.city === this.city()));
  filteredArticles = computed(() => this.articles().filter(a => !this.category() || a.category === this.category()));
  record = computed(() => {
    const plot = this.recordPlot().trim().toLowerCase();
    if (!plot) return null;
    return this.landRecords().find(r => r.city === this.recordCity() && r.plot.toLowerCase() === plot) ?? null;
  });
}
