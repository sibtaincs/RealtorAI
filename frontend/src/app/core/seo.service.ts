import { DOCUMENT } from '@angular/common';
import { Injectable, inject } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';
import { PropertyDto, formatPkr } from './models';

const SITE_ORIGIN = 'https://polite-paletas-4014d8.netlify.app';
const JSON_LD_ID = 'property-json-ld';

@Injectable({ providedIn: 'root' })
export class SeoService {
  private title = inject(Title);
  private meta = inject(Meta);
  private doc = inject(DOCUMENT);

  setProperty(property: PropertyDto): void {
    const url = `${SITE_ORIGIN}/property/${property.id}`;
    const image = property.imageUrls[0] ?? `${SITE_ORIGIN}/icon.svg`;
    const description = `${property.title} — ${property.type} for ${property.listingType} in ${property.locality}, ${property.city}. ${formatPkr(property.price, property.listingType)}`;

    this.title.setTitle(`${property.title} | RealEstateAI`);
    this.meta.updateTag({ name: 'description', content: description });
    this.meta.updateTag({ property: 'og:title', content: property.title });
    this.meta.updateTag({ property: 'og:description', content: description });
    this.meta.updateTag({ property: 'og:type', content: 'website' });
    this.meta.updateTag({ property: 'og:url', content: url });
    this.meta.updateTag({ property: 'og:image', content: image });
    this.meta.updateTag({ name: 'twitter:card', content: 'summary_large_image' });
    this.meta.updateTag({ name: 'twitter:title', content: property.title });
    this.meta.updateTag({ name: 'twitter:description', content: description });
    this.meta.updateTag({ name: 'twitter:image', content: image });
    this.setCanonical(url);
    this.setJsonLd({
      '@context': 'https://schema.org',
      '@type': 'Product',
      name: property.title,
      description,
      image,
      offers: {
        '@type': 'Offer',
        price: property.price,
        priceCurrency: 'PKR',
        availability: 'https://schema.org/InStock',
        url
      },
      additionalProperty: [
        { '@type': 'PropertyValue', name: 'City', value: property.city },
        { '@type': 'PropertyValue', name: 'Locality', value: property.locality },
        { '@type': 'PropertyValue', name: 'Listing type', value: property.listingType }
      ]
    });
  }

  private setCanonical(url: string): void {
    let link = this.doc.querySelector<HTMLLinkElement>('link[rel="canonical"]');
    if (!link) {
      link = this.doc.createElement('link');
      link.rel = 'canonical';
      this.doc.head.appendChild(link);
    }
    link.href = url;
  }

  private setJsonLd(data: Record<string, unknown>): void {
    this.doc.getElementById(JSON_LD_ID)?.remove();
    const script = this.doc.createElement('script');
    script.id = JSON_LD_ID;
    script.type = 'application/ld+json';
    script.textContent = JSON.stringify(data);
    this.doc.head.appendChild(script);
  }
}
