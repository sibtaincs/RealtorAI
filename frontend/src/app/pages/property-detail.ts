import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { PropertyService } from '../core/property.service';
import { LeadService } from '../core/lead.service';
import { PropertyCard } from '../shared/property-card';
import { DocumentChecklist } from '../shared/document-checklist';
import { PropertyDto, Review, RiskReport, formatPkr } from '../core/models';
import { TrustService } from '../core/trust.service';
import { AuthService } from '../core/auth.service';
import { ReviewService } from '../core/review.service';
import { ModerationService } from '../core/moderation.service';
import { assetUrl } from '../core/api';
import { SeoService } from '../core/seo.service';
import { DisplayService } from '../core/display.service';
import { ShortlistService } from '../core/shortlist.service';

@Component({
  selector: 'app-property-detail',
  imports: [FormsModule, RouterLink, PropertyCard, DocumentChecklist],
  templateUrl: './property-detail.html',
  styleUrl: './property-detail.scss'
})
export class PropertyDetail implements OnInit {
  private route = inject(ActivatedRoute);
  private properties = inject(PropertyService);
  private leads = inject(LeadService);
  private trust = inject(TrustService);
  private reviews = inject(ReviewService);
  private moderation = inject(ModerationService);
  private sanitizer = inject(DomSanitizer);
  private seo = inject(SeoService);
  display = inject(DisplayService);
  shortlist = inject(ShortlistService);
  auth = inject(AuthService);

  format = formatPkr;

  property = signal<PropertyDto | null>(null);
  similar = signal<PropertyDto[]>([]);
  loading = signal(true);
  error = signal('');
  activeImage = signal(0);
  risk = signal<RiskReport | null>(null);
  approvedReviews = signal<Review[]>([]);

  rating = 5;
  reviewComment = '';
  reviewSending = signal(false);
  reviewDone = signal(false);
  reviewError = signal('');
  reportReason = '';
  reportSending = signal(false);
  reportDone = signal(false);
  reportError = signal('');

  name = '';
  email = '';
  phone = '';
  message = 'I am interested in this property. Please contact me.';
  sending = signal(false);
  sent = signal(false);
  formError = signal('');

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));

    this.properties.get(id).subscribe({
      next: p => {
        const listing = { ...p, imageUrls: p.imageUrls.map(assetUrl) };
        this.property.set(listing);
        this.shortlist.viewed(id);
        this.seo.setProperty(listing);
        this.loading.set(false);
        this.message = `I am interested in "${p.title}" (${formatPkr(p.price, p.listingType)}). Please contact me.`;
      },
      error: () => {
        this.error.set('Property not found.');
        this.loading.set(false);
      }
    });

    this.trust.report(id).subscribe({
      next: report => this.risk.set(report),
      error: () => this.risk.set(null)
    });

    this.reviews.list(id).subscribe({
      next: items => this.approvedReviews.set(items),
      error: () => this.approvedReviews.set([])
    });

    this.properties.search({ pageSize: 12 }).subscribe(res => {
      this.similar.set(res.items
        .filter(p => p.id !== id)
        .slice(0, 3)
        .map(p => ({ ...p, imageUrls: p.imageUrls.map(assetUrl) })));
    });
  }

  imageUrl(path: string): string {
    return assetUrl(path);
  }

  mapUrl(): SafeResourceUrl {
    const p = this.property();
    if (!p) return this.sanitizer.bypassSecurityTrustResourceUrl('about:blank');
    const d = 0.02;
    const bbox = `${p.longitude - d},${p.latitude - d},${p.longitude + d},${p.latitude + d}`;
    const url = `https://www.openstreetmap.org/export/embed.html?bbox=${encodeURIComponent(bbox)}&layer=mapnik&marker=${p.latitude},${p.longitude}`;
    return this.sanitizer.bypassSecurityTrustResourceUrl(url);
  }

  submitReview(): void {
    const p = this.property();
    if (!p || !this.reviewComment.trim()) {
      this.reviewError.set('Comment likhna zaroori hai.');
      return;
    }
    this.reviewSending.set(true);
    this.reviewError.set('');
    this.reviews.create({
      propertyListingId: p.id, rating: this.rating, comment: this.reviewComment.trim()
    }).subscribe({
      next: () => {
        this.reviewDone.set(true);
        this.reviewComment = '';
        this.reviewSending.set(false);
      },
      error: () => {
        this.reviewError.set('Review submit nahi hui. Dobara try karein.');
        this.reviewSending.set(false);
      }
    });
  }

  submitReport(): void {
    const p = this.property();
    if (!p || !this.reportReason.trim()) {
      this.reportError.set('Reason likhna zaroori hai.');
      return;
    }
    this.reportSending.set(true);
    this.reportError.set('');
    this.moderation.report({
      propertyListingId: p.id, reason: this.reportReason.trim()
    }).subscribe({
      next: () => {
        this.reportDone.set(true);
        this.reportReason = '';
        this.reportSending.set(false);
      },
      error: () => {
        this.reportError.set('Report submit nahi hua. Dobara try karein.');
        this.reportSending.set(false);
      }
    });
  }

  sendInquiry(): void {
    const p = this.property();
    if (!p || !this.name || !this.email || !this.phone) {
      this.formError.set('Naam, email aur phone number zaroori hain.');
      return;
    }

    this.sending.set(true);
    this.formError.set('');
    this.leads.create({
      name: this.name,
      email: this.email,
      phone: this.phone,
      message: this.message,
      propertyListingId: p.id
    }).subscribe({
      next: () => {
        this.sent.set(true);
        this.sending.set(false);
      },
      error: () => {
        this.formError.set('Inquiry send nahi hui. Dobara try karein.');
        this.sending.set(false);
      }
    });
  }
}
