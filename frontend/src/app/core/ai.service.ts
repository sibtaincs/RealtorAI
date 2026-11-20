import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  AiChatResponse, GeneratedListing, ListingQualityResponse,
  PriceEstimate, SellPriceResponse, WhatsAppReply
} from './models';
import { API } from './api';

@Injectable({ providedIn: 'root' })
export class AiService {
  private http = inject(HttpClient);

  chat(message: string): Observable<AiChatResponse> {
    return this.http.post<AiChatResponse>(`${API}/ai/chat`, { message });
  }

  describe(input: Record<string, unknown>): Observable<{ description: string }> {
    return this.http.post<{ description: string }>(`${API}/ai/describe`, input);
  }

  estimate(input: Record<string, unknown>): Observable<PriceEstimate> {
    return this.http.post<PriceEstimate>(`${API}/ai/estimate`, input);
  }

  generateListing(input: Record<string, unknown>): Observable<GeneratedListing> {
    return this.http.post<GeneratedListing>(`${API}/ai/generate-listing`, input);
  }

  sellPrice(input: Record<string, unknown>): Observable<SellPriceResponse> {
    return this.http.post<SellPriceResponse>(`${API}/ai/sell-price`, input);
  }

  quality(input: Record<string, unknown>): Observable<ListingQualityResponse> {
    return this.http.post<ListingQualityResponse>(`${API}/ai/quality`, input);
  }

  whatsappReply(input: Record<string, unknown>): Observable<WhatsAppReply> {
    return this.http.post<WhatsAppReply>(`${API}/ai/whatsapp-reply`, input);
  }
}
