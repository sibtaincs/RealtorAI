import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { PropertyService } from '../core/property.service';
import { LeadService } from '../core/lead.service';
import { PropertyCard } from '../shared/property-card';
import { DocumentChecklist } from '../shared/document-checklist';
import { PropertyDto, RiskReport, formatPkr } from '../core/models';
import { TrustService } from '../core/trust.service';
import { assetUrl } from '../core/api';

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
  private sanitizer = inject(DomSanitizer);

  format = formatPkr;

  property = signal<PropertyDto | null>(null);
  similar = signal<PropertyDto[]>([]);
  loading = signal(true);
  error = signal('');
  activeImage = signal(0);
  risk = signal<RiskReport | null>(null);

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
        this.property.set({ ...p, imageUrls: p.imageUrls.map(assetUrl) });
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
