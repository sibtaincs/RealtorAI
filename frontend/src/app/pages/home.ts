import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PropertyService } from '../core/property.service';
import { AiService } from '../core/ai.service';
import { SavedSearchService } from '../core/saved-search.service';
import { AuthService } from '../core/auth.service';
import { PropertyCard } from '../shared/property-card';
import { CITIES, PROPERTY_TYPES, PagedResult, PriceEstimate, PropertyDto, PropertyQuery, formatPkr } from '../core/models';

/** Minimal shape of the browser Web Speech API used for voice search. */
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

  cities = CITIES;
  propertyTypes = PROPERTY_TYPES;
  format = formatPkr;

  // filters
  search = '';
  city = '';
  type = '';
  listingType = '';
  minBeds: number | null = null;
  page = 1;
  pageSize = 9;

  result = signal<PagedResult<PropertyDto>>({ items: [], total: 0, page: 1, pageSize: 9 });
  loading = signal(true);
  error = signal('');

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
              this.type = q.type ?? '';
              this.listingType = q.listingType ?? '';
              this.minBeds = q.minBeds ?? null;
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
      type: this.type,
      listingType: this.listingType,
      minBeds: this.minBeds ?? undefined,
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
    this.page = 1;
    this.load();
  }

  clear(): void {
    this.search = '';
    this.city = '';
    this.type = '';
    this.listingType = '';
    this.minBeds = null;
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
      type: this.type,
      listingType: this.listingType,
      minBeds: this.minBeds ?? undefined
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
