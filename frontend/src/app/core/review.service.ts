import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API } from './api';
import { Review } from './models';

@Injectable({ providedIn: 'root' })
export class ReviewService {
  private http = inject(HttpClient);

  list(propertyId: number): Observable<Review[]> {
    return this.http.get<Review[]>(`${API}/reviews`, { params: { propertyId } });
  }

  create(input: { propertyListingId: number; rating: number; comment: string }): Observable<Review> {
    return this.http.post<Review>(`${API}/reviews`, input);
  }
}
