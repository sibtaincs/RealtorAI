import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { PropertyService } from '../core/property.service';
import { AiService } from '../core/ai.service';
import { AuthService } from '../core/auth.service';
import { UploadService } from '../core/upload.service';
import { TrustService } from '../core/trust.service';
import { UPLOADS } from '../core/api';
import {
  DuplicateCheckResponse, GeneratedListing, ListingQualityResponse,
  PropertyInput, SellPriceResponse, formatPkr
} from '../core/models';
import { MasterDataService } from '../core/master-data.service';

@Component({
  selector: 'app-property-form',
  imports: [FormsModule, RouterLink],
  templateUrl: './property-form.html',
  styleUrl: './property-form.scss'
})
export class PropertyForm {
  private properties = inject(PropertyService);
  private ai = inject(AiService);
  private router = inject(Router);
  private uploads = inject(UploadService);
  private trust = inject(TrustService);
  auth = inject(AuthService);
  master = inject(MasterDataService);

  input: PropertyInput = {
    title: '',
    description: '',
    price: 0,
    type: 'House',
    listingType: 'Sale',
    city: 'Lahore',
    locality: '',
    address: '',
    latitude: 31.5204,
    longitude: 74.3587,
    bedrooms: 3,
    bathrooms: 3,
    areaMarla: 5,
    areaSqFt: null,
    imageUrls: [],
    isFeatured: false,
    plotNumber: '',
    isReadyForPossession: false,
    installmentAvailable: false,
    flooring: '',
    electricityBackup: '',
    view: '',
    builtInYear: null,
    parkingSpaces: null,
    floors: null,
    amenitiesJson: '',
    contactEmail: '',
    contactMobile: '',
    contactLandline: '',
    contactWhatsapp: ''
  };

  imageUrlsText = '';
  saving = signal(false);
  describing = signal(false);
  uploading = signal(false);
  uploadError = signal('');
  uploadedPaths = signal<string[]>([]);
  uploadBase = UPLOADS;
  error = signal('');

  // Phase 1 AI tools
  copilotNotes = '';
  generating = signal(false);
  generated = signal<GeneratedListing | null>(null);
  quality = signal<ListingQualityResponse | null>(null);
  checkingQuality = signal(false);
  priceAdvice = signal<SellPriceResponse | null>(null);
  checkingPrice = signal(false);
  targetDays = 30;
  pkr = formatPkr;

  // Phase 2: duplicate/fraud detection
  duplicateCheck = signal<DuplicateCheckResponse | null>(null);
  checkingDuplicates = signal(false);

  /** AI Listing Copilot: turns short notes + uploaded photos into a full listing. */
  runCopilot(): void {
    if (!this.copilotNotes.trim() && this.uploadedPaths().length === 0) {
      this.error.set('Kuch likhein ya pehle photos upload karein — AI tab listing banayega.');
      return;
    }

    this.generating.set(true);
    this.error.set('');
    this.ai.generateListing({
      notes: this.copilotNotes,
      city: this.input.city,
      type: this.input.type,
      listingType: this.input.listingType,
      imageUrls: this.uploadedPaths()
    }).subscribe({
      next: res => {
        this.input.title = res.title;
        this.input.description = res.description;
        this.input.type = res.type;
        this.input.listingType = res.listingType;
        this.input.city = res.city;
        this.input.locality = res.locality;
        this.input.bedrooms = res.bedrooms;
        this.input.bathrooms = res.bathrooms;
        this.input.areaMarla = res.areaMarla;
        if (res.price > 0) this.input.price = res.price;
        this.generated.set(res);
        this.generating.set(false);
      },
      error: () => {
        this.error.set('AI copilot abhi available nahi hai. Khud fields bhar lein.');
        this.generating.set(false);
      }
    });
  }

  /** Listing Quality Score: checks title, photos, price, docs, etc. */
  checkQuality(): void {
    this.checkingQuality.set(true);
    this.ai.quality({
      title: this.input.title,
      description: this.input.description,
      price: this.input.price,
      type: this.input.type,
      city: this.input.city,
      locality: this.input.locality,
      address: this.input.address,
      bedrooms: this.input.bedrooms,
      bathrooms: this.input.bathrooms,
      areaMarla: this.input.areaMarla,
      imageUrls: [...this.uploadedPaths(), ...this.imageUrlsText.split(',').map(s => s.trim()).filter(Boolean)],
      verifiedDocuments: 0
    }).subscribe({
      next: res => {
        this.quality.set(res);
        this.checkingQuality.set(false);
      },
      error: () => {
        this.error.set('Quality score abhi check nahi ho paaya.');
        this.checkingQuality.set(false);
      }
    });
  }

  /** Sell-in-X-days price advisor based on comparable listings. */
  getPriceAdvice(): void {
    this.checkingPrice.set(true);
    this.ai.sellPrice({
      type: this.input.type,
      city: this.input.city,
      locality: this.input.locality,
      areaMarla: this.input.areaMarla,
      targetDays: this.targetDays
    }).subscribe({
      next: res => {
        this.priceAdvice.set(res);
        this.checkingPrice.set(false);
      },
      error: () => {
        this.error.set('Price advice abhi available nahi hai.');
        this.checkingPrice.set(false);
      }
    });
  }

  applyAdvisedPrice(): void {
    const advice = this.priceAdvice();
    if (advice) this.input.price = advice.recommendedPrice;
  }

  /** Duplicate/fraud check against live listings before publishing. */
  checkDuplicates(): void {
    if (!this.input.title || this.input.price <= 0) {
      this.error.set('Duplicate check ke liye title aur price zaroori hain.');
      return;
    }

    this.checkingDuplicates.set(true);
    this.error.set('');
    this.trust.checkDraft({
      title: this.input.title,
      city: this.input.city,
      locality: this.input.locality,
      type: this.input.type,
      listingType: this.input.listingType,
      areaMarla: this.input.areaMarla,
      price: this.input.price,
      imageUrls: [...this.uploadedPaths(), ...this.imageUrlsText.split(',').map(s => s.trim()).filter(Boolean)]
    }).subscribe({
      next: res => {
        this.duplicateCheck.set(res);
        this.checkingDuplicates.set(false);
      },
      error: () => {
        this.error.set('Duplicate check abhi available nahi hai.');
        this.checkingDuplicates.set(false);
      }
    });
  }

  onFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []).slice(0, 8);
    if (files.length === 0) return;

    this.uploading.set(true);
    this.uploadError.set('');

    this.uploads.upload(files).subscribe({
      next: results => {
        const paths = results
          .filter(r => !!r.url && !r.error)
          .map(r => r.url);
        this.uploadedPaths.update(current => [...current, ...paths].slice(0, 8));
        this.uploading.set(false);
        input.value = '';
      },
      error: () => {
        this.uploadError.set('Upload nahi hua. Login check karein ya 5 MB se chhoti image try karein.');
        this.uploading.set(false);
        input.value = '';
      }
    });
  }

  removeUploaded(path: string): void {
    this.uploadedPaths.update(list => list.filter(p => p !== path));
  }

  previewUrl(path: string): string {
    return path.startsWith('http') ? path : this.uploadBase + path;
  }

  generateDescription(): void {
    if (!this.input.title) {
      this.error.set('Pehle title likhein, phir AI description banayega.');
      return;
    }

    this.describing.set(true);
    this.error.set('');
    this.ai.describe({
      title: this.input.title,
      type: this.input.type,
      city: this.input.city,
      locality: this.input.locality,
      bedrooms: this.input.bedrooms,
      bathrooms: this.input.bathrooms,
      areaMarla: this.input.areaMarla,
      price: this.input.price,
      features: []
    }).subscribe({
      next: res => {
        this.input.description = res.description;
        this.describing.set(false);
      },
      error: () => {
        this.error.set('AI description abhi available nahi hai. Khud likhein.');
        this.describing.set(false);
      }
    });
  }

  save(): void {
    if (!this.input.title || !this.input.locality || this.input.price <= 0) {
      this.error.set('Title, locality aur price zaroori hain.');
      return;
    }

    const linkedUrls = this.imageUrlsText
      .split(',')
      .map(s => s.trim())
      .filter(Boolean);

    this.input.imageUrls = [...this.uploadedPaths(), ...linkedUrls];

    this.saving.set(true);
    this.error.set('');

    this.properties.create(this.input).subscribe({
      next: p => {
        this.saving.set(false);
        this.router.navigate(['/property', p.id]);
      },
      error: () => {
        this.error.set('Property save nahi hui. Login check karein ya API running hai?');
        this.saving.set(false);
      }
    });
  }
}
