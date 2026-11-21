import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PropertyService } from '../core/property.service';
import { AiService } from '../core/ai.service';
import { SavedSearchService } from '../core/saved-search.service';
import { AuthService } from '../core/auth.service';
import { PropertyCard } from '../shared/property-card';
import { PagedResult, PriceEstimate, PropertyDto, PropertyQuery } from '../core/models';
import { DisplayService } from '../core/display.service';
import { MasterDataService } from '../core/master-data.service';

/** Minimal shape of the browser Web Speech API used for voice search. */
interface FilterChip {
  key: string;
  label: string;
}

interface SpeechRecognizer {
  lang: string;
  interimResults: boolean;
  continuous: boolean;
  onresult: ((event: { results: ArrayLike<ArrayLike<{ transcript: string }>> }) => void) | null;
  onerror: (() => void) | null;
  onend: (() => void) | null;
  start(): void;
}

@Component({
  selector: 'app-home',
  imports: [FormsModule, RouterLink, PropertyCard],
  templateUrl: './home.html',
  styleUrl: './home.scss'
})
export class Home implements OnInit {
  private route = inject(ActivatedRoute);
  private properties = inject(PropertyService);
  private ai = inject(AiService);
  private savedSearches = inject(SavedSearchService);
  auth = inject(AuthService);
  master = inject(MasterDataService);
  display = inject(DisplayService);

  // filters
  search = '';
  city = '';
  locality = '';
  type = '';
  listingType = '';
  minBeds: number | null = null;
  minPrice: number | null = null;
  maxPrice: number | null = null;
  minMarla: number | null = null;
  sort = 'newest';
  page = 1;
  pageSize = 9;

  result = signal<PagedResult<PropertyDto>>({ items: [], total: 0, page: 1, pageSize: 9 });
  loading = signal(true);
  error = signal('');
  filterError = signal('');

  // AI price estimate
  estimateType = 'House';
  estimateCity = 'Lahore';
  estimateLocality = 'DHA Lahore';
  estimateMarla: number | null = 10;
  estimateBeds: number | null = 4;
  estimateBaths: number | null = 4;
  estimating = signal(false);
  estimateResult = signal<PriceEstimate | null>(null);
  estimateError = signal('');

  // Saved search
  saveName = '';
  savingSearch = signal(false);
  saveNotice = signal('');

  // Phase 2: voice search via the browser's speech recognition (Chrome/Edge)
  listening = signal(false);
  voiceError = signal('');

  ngOnInit(): void {
    const savedId = Number(this.route.snapshot.queryParamMap.get('saved'));
    if (savedId && this.auth.isLoggedIn()) {
      this.savedSearches.all().subscribe({
        next: list => {
          const found = list.find(s => s.id === savedId);
          if (found) {
            try {
              const q = JSON.parse(found.queryJson);
              this.search = q.search ?? '';
              this.city = q.city ?? '';
              this.locality = q.locality ?? '';
              this.type = q.type ?? '';
              this.listingType = q.listingType ?? '';
              this.minBeds = q.minBeds ?? null;
              this.minPrice = q.minPrice ?? null;
              this.maxPrice = q.maxPrice ?? null;
              this.minMarla = q.minMarla ?? null;
              this.sort = q.sort ?? 'newest';
            } catch { /* ignore malformed saved query */ }
          }
          this.load();
        },
        error: () => this.load()
      });
      return;
    }
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.properties.search({
      search: this.search,
      city: this.city,
      locality: this.locality,
      type: this.type,
      listingType: this.listingType,
      minBeds: this.minBeds ?? undefined,
      minPrice: this.minPrice ?? undefined,
      maxPrice: this.maxPrice ?? undefined,
      minMarla: this.minMarla ?? undefined,
      sort: this.sort === 'newest' ? undefined : this.sort,
      page: this.page,
      pageSize: this.pageSize
    }).subscribe({
      next: res => {
        this.result.set(res);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load properties. Is the API running on port 5000?');
        this.loading.set(false);
      }
    });
  }

  apply(): void {
    this.filterError.set('');
    if (this.minPrice !== null && this.maxPrice !== null && this.minPrice > this.maxPrice) {
      this.filterError.set('Min price, max price se bada nahi ho sakta.');
      return;
    }
    this.page = 1;
    this.load();
  }

  clear(): void {
    this.search = '';
    this.city = '';
    this.locality = '';
    this.type = '';
    this.listingType = '';
    this.minBeds = null;
    this.minPrice = null;
    this.maxPrice = null;
    this.minMarla = null;
    this.sort = 'newest';
    this.apply();
  }

  activeFilters(): FilterChip[] {
    const chips: FilterChip[] = [];
    const add = (key: string, label: string) => chips.push({ key, label });

    if (this.search.trim()) add('search', `Search: ${this.search.trim()}`);
    if (this.city) add('city', `City: ${this.city}`);
    if (this.locality.trim()) add('locality', `Area: ${this.locality.trim()}`);
    if (this.type) add('type', `Type: ${this.type}`);
    if (this.listingType) add('listingType', `Listing: ${this.listingType}`);
    if (this.minBeds !== null) add('minBeds', `${this.minBeds}+ beds`);
    if (this.minPrice !== null) add('minPrice', `Min: ${this.display.money(this.minPrice)}`);
    if (this.maxPrice !== null) add('maxPrice', `Max: ${this.display.money(this.maxPrice)}`);
    if (this.minMarla !== null) add('minMarla', `Min area: ${this.minMarla} marla`);
    if (this.sort !== 'newest') {
      const label = this.master.options('sort-option').find(o => o.value === this.sort)?.label;
      add('sort', `Sort: ${label ?? this.sort}`);
    }
    return chips;
  }

  removeFilter(key: string): void {
    switch (key) {
      case 'search': this.search = ''; break;
      case 'city': this.city = ''; break;
      case 'locality': this.locality = ''; break;
      case 'type': this.type = ''; break;
      case 'listingType': this.listingType = ''; break;
      case 'minBeds': this.minBeds = null; break;
      case 'minPrice': this.minPrice = null; break;
      case 'maxPrice': this.maxPrice = null; break;
      case 'minMarla': this.minMarla = null; break;
      case 'sort': this.sort = 'newest'; break;
    }
    this.apply();
  }

  goTo(page: number): void {
    if (page < 1 || page > this.totalPages()) return;
    this.page = page;
    this.load();
    window.scrollTo({ top: 400, behavior: 'smooth' });
  }

  totalPages(): number {
    return Math.max(1, Math.ceil(this.result().total / this.pageSize));
  }

  pages(): number[] {
    return Array.from({ length: this.totalPages() }, (_, i) => i + 1);
  }

  saveSearch(): void {
    if (!this.auth.isLoggedIn()) {
      this.saveNotice.set('Save karne ke liye pehle login karein.');
      return;
    }
    this.saveNotice.set('');
    this.savingSearch.set(true);

    const query: PropertyQuery = {
      search: this.search,
      city: this.city,
      locality: this.locality,
      type: this.type,
      listingType: this.listingType,
      minBeds: this.minBeds ?? undefined,
      minPrice: this.minPrice ?? undefined,
      maxPrice: this.maxPrice ?? undefined,
      minMarla: this.minMarla ?? undefined,
      sort: this.sort === 'newest' ? undefined : this.sort
    };
    const name = this.saveName.trim() || [this.city, this.type, this.search].filter(Boolean).join(' ') || 'My property search';

    this.savedSearches.create(name, query, true).subscribe({
      next: () => {
        this.saveNotice.set('✅ Search save ho gaya. Nayi properties par alert milega.');
        this.saveName = '';
        this.savingSearch.set(false);
      },
      error: () => {
        this.saveNotice.set('Save nahi hua. Dobara try karein.');
        this.savingSearch.set(false);
      }
    });
  }

  /** Starts browser speech recognition and searches with the transcript. */
  startVoiceSearch(): void {
    const browserWindow = window as unknown as {
      SpeechRecognition?: new () => SpeechRecognizer;
      webkitSpeechRecognition?: new () => SpeechRecognizer;
    };
    const Recognition = browserWindow.SpeechRecognition || browserWindow.webkitSpeechRecognition;
    if (!Recognition) {
      this.voiceError.set('Is browser voice search support nahi karta. Chrome ya Edge use karein.');
      return;
    }

    const recognition = new Recognition();
    recognition.lang = 'ur-PK';
    recognition.interimResults = false;
    recognition.continuous = false;
    this.voiceError.set('');
    this.listening.set(true);

    recognition.onresult = event => {
      const transcript = event.results[0][0].transcript;
      this.search = transcript;
      this.listening.set(false);
      this.apply();
    };
    recognition.onerror = () => {
      this.voiceError.set('Aawaz nahi suni gayi. Mic check karke dobara try karein.');
      this.listening.set(false);
    };
    recognition.onend = () => this.listening.set(false);

    try {
      recognition.start();
    } catch {
      this.voiceError.set('Voice search start nahi hua. Dobara try karein.');
      this.listening.set(false);
    }
  }

  runEstimate(): void {
    this.estimating.set(true);
    this.estimateError.set('');
    this.ai.estimate({
      type: this.estimateType,
      city: this.estimateCity,
      locality: this.estimateLocality,
      areaMarla: this.estimateMarla,
      bedrooms: this.estimateBeds,
      bathrooms: this.estimateBaths
    }).subscribe({
      next: res => {
        this.estimateResult.set(res);
        this.estimating.set(false);
      },
      error: () => {
        this.estimateError.set('Estimate unavailable right now.');
        this.estimating.set(false);
      }
    });
  }
}
