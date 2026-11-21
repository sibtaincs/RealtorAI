import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { DisplayService } from '../core/display.service';
import { COMMUNITY_ARTICLES, LAND_RECORDS, SOCIETIES } from '../core/explore.data';
import { CITIES } from '../core/models';

@Component({
  selector: 'app-explore',
  imports: [FormsModule, RouterLink],
  templateUrl: './explore.html',
  styleUrl: './directory.scss'
})
export class ExplorePage {
  display = inject(DisplayService);
  cities = CITIES;
  societies = SOCIETIES;
  articles = COMMUNITY_ARTICLES;

  city = signal('');
  category = signal('');
  recordCity = signal('Lahore');
  recordPlot = signal('');

  categories = ['News', 'Guide', 'Community'];

  filteredSocieties = computed(() => this.societies.filter(s => !this.city() || s.city === this.city()));
  filteredArticles = computed(() => this.articles.filter(a => !this.category() || a.category === this.category()));
  record = computed(() => {
    const plot = this.recordPlot().trim().toLowerCase();
    if (!plot) return null;
    return LAND_RECORDS.find(r => r.city === this.recordCity() && r.plot.toLowerCase() === plot) ?? null;
  });
}
