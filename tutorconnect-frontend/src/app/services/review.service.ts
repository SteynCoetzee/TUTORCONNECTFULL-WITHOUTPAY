import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Review {
  review_ID: number;
  rating: number;
  feedback?: string;
  reviewer_ID: number;
  reviewed_Entity_ID: number;
}

@Injectable({
  providedIn: 'root'
})
export class ReviewService {
  private apiUrl = `${environment.apiUrl}/Reviews`;

  constructor(private http: HttpClient) {}

  // Get reviews for entity
  getReviews(entityId: number): Observable<Review[]> {
    return this.http.get<Review[]>(`${this.apiUrl}/entity/${entityId}`);
  }

  // Create review
  createReview(payload: any): Observable<any> {
    return this.http.post(this.apiUrl, payload);
  }

  // Update review
  updateReview(id: number, payload: any): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, payload);
  }

  // Delete review
  deleteReview(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  // Get average rating
  getAverageRating(entityId: number): Observable<{ averageRating: number; totalReviews: number }> {
    return this.http.get<{ averageRating: number; totalReviews: number }>(`${this.apiUrl}/average/${entityId}`);
  }
}
